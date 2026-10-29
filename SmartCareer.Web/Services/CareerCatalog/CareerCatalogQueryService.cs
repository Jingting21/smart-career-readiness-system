using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Data;
using SmartCareer.Web.ViewModels;

namespace SmartCareer.Web.Services.CareerCatalog;

public sealed class CareerCatalogQueryService(ApplicationDbContext dbContext) : ICareerCatalogQueryService
{
    public const int PageSize = 12;

    public async Task<CareerExplorerQueryResult> GetExplorerAsync(
        string? query,
        string? categorySlug,
        int page,
        CancellationToken cancellationToken = default)
    {
        query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        categorySlug = string.IsNullOrWhiteSpace(categorySlug) ? null : categorySlug.Trim();

        if (query is { Length: > 100 })
        {
            throw new ArgumentException("Search terms cannot exceed 100 characters.", nameof(query));
        }

        var categories = await dbContext.CareerCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.CategoryName)
            .Select(category => new CareerCategoryFilterViewModel(
                category.CategoryName,
                category.Slug,
                category.Description,
                category.Careers.Count(career => career.IsActive)
                ))
            .ToListAsync(cancellationToken);

        var categoryExists = categorySlug is null ||
            categories.Any(category => category.Slug == categorySlug);

        var activeCareers = dbContext.Careers
            .AsNoTracking()
            .Where(career => career.IsActive && career.Category.IsActive);

        var totalActiveCareers = await activeCareers.CountAsync(cancellationToken);
        var filteredCareers = activeCareers;

        if (categorySlug is not null && categoryExists)
        {
            filteredCareers = filteredCareers.Where(career => career.Category.Slug == categorySlug);
        }

        if (query is not null)
        {
            filteredCareers = filteredCareers.Where(career =>
                career.Title.Contains(query) ||
                career.SourceTitle.Contains(query) ||
                career.Description.Contains(query));
        }

        var filteredCount = categoryExists
            ? await filteredCareers.CountAsync(cancellationToken)
            : 0;
        var totalPages = filteredCount == 0
            ? 0
            : (int)Math.Ceiling(filteredCount / (double)PageSize);
        var currentPage = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);

        var careers = categoryExists
            ? await filteredCareers
                .OrderBy(career => career.Title)
                .ThenBy(career => career.CareerId)
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(career => new CareerCardViewModel(
                    career.OccupationCode,
                    career.Title,
                    career.Category.CategoryName,
                    career.Category.Slug,
                    career.Description))
                .ToListAsync(cancellationToken)
            : [];

        return new CareerExplorerQueryResult(
            new CareerExplorerViewModel
            {
                Query = query,
                SelectedCategorySlug = categorySlug,
                Categories = categories,
                Careers = careers,
                TotalActiveCareers = totalActiveCareers,
                FilteredCount = filteredCount,
                CurrentPage = currentPage,
                PageSize = PageSize,
                TotalPages = totalPages
            },
            categoryExists);
    }

    public async Task<CareerDetailsViewModel?> GetDetailsAsync(
        string occupationCode, string? explorerQuery, string? explorerCategory, int explorerPage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(occupationCode)) return null;
        explorerQuery = string.IsNullOrWhiteSpace(explorerQuery) ? null : explorerQuery.Trim();
        explorerCategory = string.IsNullOrWhiteSpace(explorerCategory) ? null : explorerCategory.Trim();
        if (explorerQuery is { Length: > 100 }) explorerQuery = null;

        var career = await dbContext.Careers.AsNoTracking()
            .Where(item => item.OccupationCode == occupationCode.Trim() && item.IsActive && item.Category.IsActive)
            .Select(item => new { item.CareerId, item.OccupationCode, item.Title, item.SourceTitle,
                CategoryName = item.Category.CategoryName, item.Description, item.Responsibilities,
                item.Qualifications, item.RequiresLocalReview, item.SourceName, item.SourceVersion })
            .SingleOrDefaultAsync(cancellationToken);
        if (career is null) return null;

        var tasks = await dbContext.CareerTasks.AsNoTracking()
            .Where(task => task.CareerId == career.CareerId && task.TaskType == "Core")
            .OrderByDescending(task => task.Importance)
            .ThenBy(task => task.OnetTaskId)
            .Select(task => new CareerTaskViewModel(task.OnetTaskId, task.Statement, task.Importance))
            .ToListAsync(cancellationToken);

        var skills = await dbContext.CareerSkills.AsNoTracking()
            .Where(link => link.CareerId == career.CareerId && link.Skill.IsActive)
            .OrderBy(link => link.Skill.SkillCategory).ThenByDescending(link => link.Importance)
            .ThenBy(link => link.Skill.SkillName)
            .Select(link => new { link.Skill.SkillCategory,
                Skill = new CareerSkillViewModel(link.Skill.SkillName, link.Skill.Description, link.Importance, link.RequiredLevel) })
            .ToListAsync(cancellationToken);

        var education = await dbContext.CareerEducationRatings.AsNoTracking()
            .Where(rating => rating.CareerId == career.CareerId && !rating.IsSuppressed && rating.Percentage != null)
            .OrderBy(rating => rating.ScaleId).ThenByDescending(rating => rating.Percentage).ThenBy(rating => rating.Category)
            .Select(rating => new PreparationRatingRow(rating.ScaleId, rating.CategoryDescription, rating.Percentage!.Value))
            .ToListAsync(cancellationToken);

        var training = await dbContext.CareerTrainingRatings.AsNoTracking()
            .Where(rating => rating.CareerId == career.CareerId && !rating.IsSuppressed && rating.Percentage != null &&
                rating.ScaleId == "RW")
            .OrderBy(rating => rating.ScaleId).ThenByDescending(rating => rating.Percentage).ThenBy(rating => rating.Category)
            .Select(rating => new PreparationRatingRow(rating.ScaleId, rating.CategoryDescription, rating.Percentage!.Value))
            .ToListAsync(cancellationToken);

        var jobZone = await dbContext.CareerJobZones.AsNoTracking()
            .Where(assignment => assignment.CareerId == career.CareerId)
            .Select(assignment => new JobZoneViewModel(assignment.JobZone, assignment.JobZoneDefinition.Name,
                assignment.JobZoneDefinition.Education, assignment.JobZoneDefinition.Experience,
                assignment.JobZoneDefinition.JobTraining))
            .SingleOrDefaultAsync(cancellationToken);

        var source = await dbContext.CareerCatalogSources.AsNoTracking()
            .Where(item => item.SourceName == career.SourceName && item.SourceVersion == career.SourceVersion)
            .Select(item => new CareerProvenanceViewModel(item.SourceName, item.SourceVersion, item.OfficialSourceUrl,
                item.LicenseName, item.LicenseUrl, item.RetrievedOn, item.AttributionText))
            .SingleOrDefaultAsync(cancellationToken);
        if (source is null) return null;

        return new CareerDetailsViewModel
        {
            OccupationCode = career.OccupationCode, Title = career.Title, SourceTitle = career.SourceTitle,
            CategoryName = career.CategoryName, Description = career.Description,
            Responsibilities = career.Responsibilities, Qualifications = career.Qualifications,
            RequiresLocalReview = career.RequiresLocalReview, Provenance = source,
            EssentialSkills = skills.Where(item => item.SkillCategory == "Essential").Select(item => item.Skill).ToList(),
            TransferableSkills = skills.Where(item => item.SkillCategory == "Transferable").Select(item => item.Skill).ToList(),
            TypicalTasks = tasks,
            EducationBackgrounds = education.GroupBy(item => item.ScaleId, StringComparer.Ordinal)
                .Select(group => BuildDistribution(group.Key,
                    "Education Background", "Most commonly reported education level in O*NET responses",
                    group.Select(item => new PreparationResponseViewModel(item.CategoryDescription, item.Percentage))))
                .ToList(),
            RelatedWorkExperience = BuildTrainingDistribution(training, "RW", "Related Work Experience"),
            JobZone = jobZone,
            ExplorerQuery = explorerQuery, ExplorerCategory = explorerCategory, ExplorerPage = Math.Max(1, explorerPage)
        };
    }

    private static PreparationDistributionViewModel? BuildTrainingDistribution(
        IEnumerable<PreparationRatingRow> ratings, string scaleId, string heading)
    {
        var selected = ratings.Where(item => item.ScaleId == scaleId)
            .Select(item => new PreparationResponseViewModel(item.CategoryDescription, item.Percentage)).ToList();
        return selected.Count > 0
            ? BuildDistribution(scaleId, heading, "Most commonly reported response", selected)
            : null;
    }

    private static PreparationDistributionViewModel BuildDistribution(
        string scaleId, string heading, string mostCommonLabel,
        IEnumerable<PreparationResponseViewModel> responses)
    {
        var ordered = responses.OrderByDescending(item => item.Percentage)
            .ThenBy(item => item.CategoryDescription, StringComparer.Ordinal).ToList();
        var maximum = ordered[0].Percentage;
        return new PreparationDistributionViewModel(scaleId, heading, mostCommonLabel,
            ordered.Where(item => item.Percentage == maximum).ToList(), ordered);
    }

    private sealed record PreparationRatingRow(string ScaleId, string CategoryDescription, decimal Percentage);
}
