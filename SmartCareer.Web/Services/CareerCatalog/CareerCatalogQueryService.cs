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
}
