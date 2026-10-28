using SmartCareer.Web.ViewModels;

namespace SmartCareer.Web.Services.CareerCatalog;

public interface ICareerCatalogQueryService
{
    Task<CareerExplorerQueryResult> GetExplorerAsync(
        string? query,
        string? categorySlug,
        int page,
        CancellationToken cancellationToken = default);

    Task<CareerDetailsViewModel?> GetDetailsAsync(
        string occupationCode, string? explorerQuery, string? explorerCategory, int explorerPage,
        CancellationToken cancellationToken = default);
}

public sealed record CareerExplorerQueryResult(
    CareerExplorerViewModel ViewModel,
    bool CategoryExists);
