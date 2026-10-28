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
            await dbContext.CareerCatalogSources.CountAsync(cancellationToken),
            await dbContext.CareerTasks.CountAsync(cancellationToken)
        };

        if (existingCounts.Any(count => count != 0))
        {
            if (!await MatchesExactlyAsync(plan, cancellationToken, includeTasks: false))
            {
                throw new InvalidOperationException(
                    "Career catalogue tables are not empty and do not exactly match this import plan. " +
                    "No records were changed; destructive replacement is intentionally unsupported.");
            }

            var existingTaskCount = await dbContext.CareerTasks.CountAsync(cancellationToken);
            if (existingTaskCount != plan.CareerTasks.Count ||
                !await MatchesTasksExactlyAsync(plan, cancellationToken))
            {
                if (existingTaskCount != 0)
                    throw new InvalidOperationException("Career task data is partial or does not match the validated plan. No records were changed.");
                await InsertTasksIntoExistingCatalogueAsync(plan, cancellationToken);
            }

            if (await MatchesPreparationExactlyAsync(plan, cancellationToken))
                return CareerCatalogDatabaseImportOutcome.AlreadyCurrent;

            var preparationCounts = new[]
            {
                await dbContext.CareerEducationRatings.CountAsync(cancellationToken),
                await dbContext.CareerTrainingRatings.CountAsync(cancellationToken),
                await dbContext.JobZoneDefinitions.CountAsync(cancellationToken),
                await dbContext.CareerJobZones.CountAsync(cancellationToken)
            };
            if (preparationCounts.Any(count => count != 0))
                throw new InvalidOperationException("Education and preparation data is partial or does not match the validated plan. No records were changed.");

            await InsertPreparationIntoExistingCatalogueAsync(plan, cancellationToken);
            return CareerCatalogDatabaseImportOutcome.Imported;
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
            dbContext.CareerTasks.AddRange(plan.CareerTasks.Select(record => new CareerTask
            {
                Career = careers[record.OccupationCode], OnetTaskId = record.OnetTaskId,
                Statement = record.Statement, TaskType = record.TaskType, Importance = record.Importance,
                Relevance = record.Relevance, IncumbentsResponding = record.IncumbentsResponding,
                SourceUpdatedOn = record.SourceUpdatedOn, DomainSource = record.DomainSource,
                SourceVersion = record.SourceVersion
            }));
            AddPreparationEntities(plan, careers.ToDictionary(item => item.Key, item => item.Value.CareerId, StringComparer.Ordinal), careers);
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
        CancellationToken cancellationToken,
        bool includeTasks = true)
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
        var baseMatches = plan.CareerSkills.All(record =>
            linkLookup.TryGetValue((record.OccupationCode, record.ExternalSkillCode), out var entity) &&
            entity.Importance == record.Importance && entity.RequiredLevel == record.RequiredLevel &&
            entity.SourceVersion == record.SourceVersion);
        return baseMatches && (!includeTasks || await MatchesTasksExactlyAsync(plan, cancellationToken));
    }

    private async Task InsertTasksIntoExistingCatalogueAsync(
        CareerCatalogImportPlan plan, CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = null;
        if (dbContext.Database.IsRelational()) transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var careerIds = await dbContext.Careers.AsNoTracking()
                .ToDictionaryAsync(career => career.OccupationCode, career => career.CareerId, StringComparer.Ordinal, cancellationToken);
            dbContext.CareerTasks.AddRange(plan.CareerTasks.Select(record => new CareerTask
            {
                CareerId = careerIds[record.OccupationCode], OnetTaskId = record.OnetTaskId,
                Statement = record.Statement, TaskType = record.TaskType, Importance = record.Importance,
                Relevance = record.Relevance, IncumbentsResponding = record.IncumbentsResponding,
                SourceUpdatedOn = record.SourceUpdatedOn, DomainSource = record.DomainSource,
                SourceVersion = record.SourceVersion
            }));
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private async Task<bool> MatchesTasksExactlyAsync(
        CareerCatalogImportPlan plan, CancellationToken cancellationToken)
    {
        if (await dbContext.CareerTasks.CountAsync(cancellationToken) != plan.CareerTasks.Count) return false;
        var tasks = await dbContext.CareerTasks.AsNoTracking().Include(task => task.Career)
            .ToDictionaryAsync(task => (task.Career.OccupationCode, task.OnetTaskId), cancellationToken);
        return plan.CareerTasks.All(record =>
            tasks.TryGetValue((record.OccupationCode, record.OnetTaskId), out var entity) &&
            entity.Statement == record.Statement && entity.TaskType == record.TaskType &&
            entity.Importance == record.Importance && entity.Relevance == record.Relevance &&
            entity.IncumbentsResponding == record.IncumbentsResponding &&
            entity.SourceUpdatedOn == record.SourceUpdatedOn && entity.DomainSource == record.DomainSource &&
            entity.SourceVersion == record.SourceVersion);
    }

    private async Task InsertPreparationIntoExistingCatalogueAsync(
        CareerCatalogImportPlan plan, CancellationToken cancellationToken)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            var careerIds = await dbContext.Careers.AsNoTracking().ToDictionaryAsync(
                career => career.OccupationCode, career => career.CareerId, StringComparer.Ordinal, cancellationToken);
            AddPreparationEntities(plan, careerIds);
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private void AddPreparationEntities(
        CareerCatalogImportPlan plan,
        IReadOnlyDictionary<string, int> careerIds,
        IReadOnlyDictionary<string, Career>? newCareers = null)
    {
        Career ResolveCareer(string code) => newCareers is not null
            ? newCareers[code]
            : dbContext.Careers.Local.Single(career => career.CareerId == careerIds[code]);

        dbContext.JobZoneDefinitions.AddRange(plan.JobZoneDefinitions.Select(record => new JobZoneDefinition
        {
            JobZone = record.JobZone, Name = record.Name, Experience = record.Experience,
            Education = record.Education, JobTraining = record.JobTraining, Examples = record.Examples,
            SvpRange = record.SvpRange, SourceVersion = record.SourceVersion
        }));
        dbContext.CareerEducationRatings.AddRange(plan.EducationRatings.Select(record => new CareerEducationRating
        {
            CareerId = newCareers is null ? careerIds[record.OccupationCode] : 0,
            Career = newCareers is null ? null! : ResolveCareer(record.OccupationCode),
            ElementId = record.ElementId, ScaleId = record.ScaleId, Category = record.Category,
            CategoryDescription = record.CategoryDescription, Percentage = record.Percentage,
            RespondentCount = record.RespondentCount, StandardError = record.StandardError,
            LowerConfidenceBound = record.LowerConfidenceBound, UpperConfidenceBound = record.UpperConfidenceBound,
            IsSuppressed = record.IsSuppressed, SourceUpdatedOn = record.SourceUpdatedOn,
            DomainSource = record.DomainSource, SourceVersion = record.SourceVersion
        }));
        dbContext.CareerTrainingRatings.AddRange(plan.TrainingRatings.Select(record => new CareerTrainingRating
        {
            CareerId = newCareers is null ? careerIds[record.OccupationCode] : 0,
            Career = newCareers is null ? null! : ResolveCareer(record.OccupationCode),
            ElementId = record.ElementId, ScaleId = record.ScaleId, Category = record.Category,
            CategoryDescription = record.CategoryDescription, Percentage = record.Percentage,
            RespondentCount = record.RespondentCount, StandardError = record.StandardError,
            LowerConfidenceBound = record.LowerConfidenceBound, UpperConfidenceBound = record.UpperConfidenceBound,
            IsSuppressed = record.IsSuppressed, SourceUpdatedOn = record.SourceUpdatedOn,
            DomainSource = record.DomainSource, SourceVersion = record.SourceVersion
        }));
        dbContext.CareerJobZones.AddRange(plan.CareerJobZones.Select(record => new CareerJobZone
        {
            CareerId = newCareers is null ? careerIds[record.OccupationCode] : 0,
            Career = newCareers is null ? null! : ResolveCareer(record.OccupationCode),
            JobZone = record.JobZone, SourceUpdatedOn = record.SourceUpdatedOn,
            DomainSource = record.DomainSource, SourceVersion = record.SourceVersion
        }));
    }

    private async Task<bool> MatchesPreparationExactlyAsync(
        CareerCatalogImportPlan plan, CancellationToken cancellationToken)
    {
        if (await dbContext.CareerEducationRatings.CountAsync(cancellationToken) != plan.EducationRatings.Count ||
            await dbContext.CareerTrainingRatings.CountAsync(cancellationToken) != plan.TrainingRatings.Count ||
            await dbContext.JobZoneDefinitions.CountAsync(cancellationToken) != plan.JobZoneDefinitions.Count ||
            await dbContext.CareerJobZones.CountAsync(cancellationToken) != plan.CareerJobZones.Count)
            return false;

        var education = await dbContext.CareerEducationRatings.AsNoTracking().Include(item => item.Career)
            .ToDictionaryAsync(item => (item.Career.OccupationCode, item.ElementId, item.ScaleId, item.Category), cancellationToken);
        var training = await dbContext.CareerTrainingRatings.AsNoTracking().Include(item => item.Career)
            .ToDictionaryAsync(item => (item.Career.OccupationCode, item.ElementId, item.ScaleId, item.Category), cancellationToken);
        var definitions = await dbContext.JobZoneDefinitions.AsNoTracking().ToDictionaryAsync(item => item.JobZone, cancellationToken);
        var assignments = await dbContext.CareerJobZones.AsNoTracking().Include(item => item.Career)
            .ToDictionaryAsync(item => item.Career.OccupationCode, StringComparer.Ordinal, cancellationToken);

        return plan.EducationRatings.All(record => education.TryGetValue(
                   (record.OccupationCode, record.ElementId, record.ScaleId, record.Category), out var entity) &&
               RatingMatches(record, entity)) &&
            plan.TrainingRatings.All(record => training.TryGetValue(
                   (record.OccupationCode, record.ElementId, record.ScaleId, record.Category), out var entity) &&
               RatingMatches(record, entity)) &&
            plan.JobZoneDefinitions.All(record => definitions.TryGetValue(record.JobZone, out var entity) &&
               entity.Name == record.Name && entity.Experience == record.Experience && entity.Education == record.Education &&
               entity.JobTraining == record.JobTraining && entity.Examples == record.Examples &&
               entity.SvpRange == record.SvpRange && entity.SourceVersion == record.SourceVersion) &&
            plan.CareerJobZones.All(record => assignments.TryGetValue(record.OccupationCode, out var entity) &&
               entity.JobZone == record.JobZone && entity.SourceUpdatedOn == record.SourceUpdatedOn &&
               entity.DomainSource == record.DomainSource && entity.SourceVersion == record.SourceVersion);
    }

    private static bool RatingMatches(CareerEducationRatingImportRecord record, CareerEducationRating entity) =>
        entity.CategoryDescription == record.CategoryDescription && entity.Percentage == record.Percentage &&
        entity.RespondentCount == record.RespondentCount && entity.StandardError == record.StandardError &&
        entity.LowerConfidenceBound == record.LowerConfidenceBound && entity.UpperConfidenceBound == record.UpperConfidenceBound &&
        entity.IsSuppressed == record.IsSuppressed && entity.SourceUpdatedOn == record.SourceUpdatedOn &&
        entity.DomainSource == record.DomainSource && entity.SourceVersion == record.SourceVersion;

    private static bool RatingMatches(CareerTrainingRatingImportRecord record, CareerTrainingRating entity) =>
        entity.CategoryDescription == record.CategoryDescription && entity.Percentage == record.Percentage &&
        entity.RespondentCount == record.RespondentCount && entity.StandardError == record.StandardError &&
        entity.LowerConfidenceBound == record.LowerConfidenceBound && entity.UpperConfidenceBound == record.UpperConfidenceBound &&
        entity.IsSuppressed == record.IsSuppressed && entity.SourceUpdatedOn == record.SourceUpdatedOn &&
        entity.DomainSource == record.DomainSource && entity.SourceVersion == record.SourceVersion;
}
