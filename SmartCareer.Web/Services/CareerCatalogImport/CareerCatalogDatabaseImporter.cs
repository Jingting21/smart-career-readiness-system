using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;

namespace SmartCareer.Web.Services.CareerCatalogImport;

public enum CareerCatalogDatabaseImportOutcome
{
    Imported,
    AlreadyCurrent
}

public sealed class CareerCatalogDatabaseImporter(ApplicationDbContext dbContext)
{
    public async Task<CareerCatalogDatabaseImportOutcome> ApplyAsync(
        CareerCatalogImportPlan plan,
        CancellationToken cancellationToken = default)
    {
        if (!plan.IsValid)
        {
            throw new InvalidOperationException("An invalid career catalogue plan cannot be imported.");
        }

        var existingCounts = new[]
        {
            await dbContext.CareerCategories.CountAsync(cancellationToken),
            await dbContext.Careers.CountAsync(cancellationToken),
            await dbContext.Skills.CountAsync(cancellationToken),
            await dbContext.CareerSkills.CountAsync(cancellationToken),
            await dbContext.CareerCatalogSources.CountAsync(cancellationToken)
        };

        if (existingCounts.Any(count => count != 0))
        {
            if (await MatchesExactlyAsync(plan, cancellationToken))
            {
                return CareerCatalogDatabaseImportOutcome.AlreadyCurrent;
            }

            throw new InvalidOperationException(
                "Career catalogue tables are not empty and do not exactly match this import plan. " +
                "No records were changed; destructive replacement is intentionally unsupported.");
        }

        IDbContextTransaction? transaction = null;
        if (dbContext.Database.IsRelational())
        {
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var categories = plan.Categories.ToDictionary(
                record => record.Slug,
                record => new CareerCategory
                {
                    CategoryName = record.CategoryName,
                    Slug = record.Slug,
                    Description = record.Description,
                    DisplayOrder = record.DisplayOrder,
                    IsActive = true
                },
                StringComparer.Ordinal);

            var skills = plan.Skills.ToDictionary(
                record => record.ExternalSkillCode,
                record => new Skill
                {
                    ExternalSkillCode = record.ExternalSkillCode,
                    SkillName = record.SkillName,
                    SkillCategory = record.SkillCategory,
                    Description = record.Description,
                    IsActive = true
                },
                StringComparer.Ordinal);

            var timestamp = DateTime.UtcNow;
            var sources = plan.Sources.Select(record => new CareerCatalogSource
            {
                SourceName = record.SourceName,
                SourceVersion = record.SourceVersion,
                OfficialSourceUrl = record.OfficialSourceUrl,
                LicenseName = record.LicenseName,
                LicenseUrl = record.LicenseUrl,
                RetrievedOn = record.RetrievedOn,
                AttributionText = record.AttributionText
            }).ToList();

            var careers = plan.Careers.ToDictionary(
                record => record.OccupationCode,
                record => new Career
                {
                    OccupationCode = record.OccupationCode,
                    SourceTitle = record.SourceTitle,
                    Title = record.Title,
                    Category = categories[record.CategorySlug],
                    Description = record.Description,
                    Responsibilities = null,
                    Qualifications = null,
                    SourceName = record.SourceName,
                    SourceVersion = record.SourceVersion,
                    SourceUpdatedOn = record.SourceUpdatedOn,
                    RequiresLocalReview = record.RequiresLocalReview,
                    LocalReviewNote = record.LocalReviewNote,
                    IsActive = true,
                    CreatedAt = timestamp,
                    UpdatedAt = timestamp
                },
                StringComparer.Ordinal);

            var links = plan.CareerSkills.Select(record => new CareerSkill
            {
                Career = careers[record.OccupationCode],
                Skill = skills[record.ExternalSkillCode],
                Importance = record.Importance,
                RequiredLevel = record.RequiredLevel,
                SourceVersion = record.SourceVersion
            });

            dbContext.CareerCategories.AddRange(categories.Values);
            dbContext.Careers.AddRange(careers.Values);
            dbContext.Skills.AddRange(skills.Values);
            dbContext.CareerSkills.AddRange(links);
            dbContext.CareerCatalogSources.AddRange(sources);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return CareerCatalogDatabaseImportOutcome.Imported;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private async Task<bool> MatchesExactlyAsync(
        CareerCatalogImportPlan plan,
        CancellationToken cancellationToken)
    {
        if (await dbContext.CareerCategories.CountAsync(cancellationToken) != plan.Categories.Count ||
            await dbContext.Careers.CountAsync(cancellationToken) != plan.Careers.Count ||
            await dbContext.Skills.CountAsync(cancellationToken) != plan.Skills.Count ||
            await dbContext.CareerSkills.CountAsync(cancellationToken) != plan.CareerSkills.Count ||
            await dbContext.CareerCatalogSources.CountAsync(cancellationToken) != plan.Sources.Count)
        {
            return false;
        }

        var categories = await dbContext.CareerCategories.AsNoTracking().ToDictionaryAsync(
            category => category.Slug, StringComparer.Ordinal, cancellationToken);
        if (plan.Categories.Any(record => !categories.TryGetValue(record.Slug, out var entity) ||
            entity.CategoryName != record.CategoryName || entity.Description != record.Description ||
            entity.DisplayOrder != record.DisplayOrder || !entity.IsActive))
        {
            return false;
        }

        var careers = await dbContext.Careers.AsNoTracking().Include(career => career.Category)
            .ToDictionaryAsync(career => career.OccupationCode, StringComparer.Ordinal, cancellationToken);
        if (plan.Careers.Any(record => !careers.TryGetValue(record.OccupationCode, out var entity) ||
            entity.SourceTitle != record.SourceTitle || entity.Title != record.Title ||
            entity.Category.Slug != record.CategorySlug || entity.Description != record.Description ||
            entity.Responsibilities is not null || entity.Qualifications is not null ||
            entity.SourceName != record.SourceName || entity.SourceVersion != record.SourceVersion ||
            entity.SourceUpdatedOn != record.SourceUpdatedOn ||
            entity.RequiresLocalReview != record.RequiresLocalReview ||
            entity.LocalReviewNote != record.LocalReviewNote || !entity.IsActive))
        {
            return false;
        }

        var skills = await dbContext.Skills.AsNoTracking().ToDictionaryAsync(
            skill => skill.ExternalSkillCode, StringComparer.Ordinal, cancellationToken);
        if (plan.Skills.Any(record => !skills.TryGetValue(record.ExternalSkillCode, out var entity) ||
            entity.SkillName != record.SkillName || entity.SkillCategory != record.SkillCategory ||
            entity.Description != record.Description || !entity.IsActive))
        {
            return false;
        }

        var sources = await dbContext.CareerCatalogSources.AsNoTracking().ToListAsync(cancellationToken);
        if (plan.Sources.Any(record => !sources.Any(entity =>
            entity.SourceName == record.SourceName && entity.SourceVersion == record.SourceVersion &&
            entity.OfficialSourceUrl == record.OfficialSourceUrl && entity.LicenseName == record.LicenseName &&
            entity.LicenseUrl == record.LicenseUrl && entity.RetrievedOn == record.RetrievedOn &&
            entity.AttributionText == record.AttributionText)))
        {
            return false;
        }

        var links = await dbContext.CareerSkills.AsNoTracking()
            .Include(link => link.Career)
            .Include(link => link.Skill)
            .ToListAsync(cancellationToken);
        var linkLookup = links.ToDictionary(
            link => (link.Career.OccupationCode, link.Skill.ExternalSkillCode),
            link => link);
        return plan.CareerSkills.All(record =>
            linkLookup.TryGetValue((record.OccupationCode, record.ExternalSkillCode), out var entity) &&
            entity.Importance == record.Importance && entity.RequiredLevel == record.RequiredLevel &&
            entity.SourceVersion == record.SourceVersion);
    }
}
