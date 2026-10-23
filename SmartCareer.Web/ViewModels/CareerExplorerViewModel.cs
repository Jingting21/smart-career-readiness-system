namespace SmartCareer.Web.ViewModels;

public sealed class CareerExplorerViewModel
{
    public string? Query { get; set; }
    public string? SelectedCategorySlug { get; init; }
    public IReadOnlyList<CareerCategoryFilterViewModel> Categories { get; init; } = [];
    public IReadOnlyList<CareerCardViewModel> Careers { get; init; } = [];
    public int TotalActiveCareers { get; init; }
    public int FilteredCount { get; init; }
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool IsCatalogueAvailable => TotalActiveCareers > 0;
    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(Query) || !string.IsNullOrWhiteSpace(SelectedCategorySlug);
    public string? SelectedCategoryName => Categories
        .FirstOrDefault(category => category.Slug == SelectedCategorySlug)?.Name;
}

public sealed record CareerCategoryFilterViewModel(
    string Name,
    string Slug,
    string? Description,
    int ActiveCareerCount);

public sealed record CareerCardViewModel(
    string Title,
    string CategoryName,
    string CategorySlug,
    string Description);
