using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services.CareerCatalog;

namespace SmartCareer.Web.Controllers;

[Authorize(Roles = AppRoles.Student)]
[Route("Careers")]
public sealed class CareersController(ICareerCatalogQueryService careerCatalog) : Controller
{
    public const string DetailsRouteName = "CareerDetails";

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? query,
        string? category,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var trimmedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        if (trimmedQuery is { Length: > 100 })
        {
            ModelState.AddModelError(nameof(query), "Search terms cannot exceed 100 characters.");
            trimmedQuery = null;
        }

        var result = await careerCatalog.GetExplorerAsync(
            trimmedQuery,
            category,
            page,
            cancellationToken);

        if (!result.CategoryExists)
        {
            return NotFound();
        }

        return View(result.ViewModel);
    }

    [HttpGet("{occupationCode}", Name = DetailsRouteName)]
    public async Task<IActionResult> Details(string occupationCode, string? query, string? category, int page = 1,
        CancellationToken cancellationToken = default)
    {
        var viewModel = await careerCatalog.GetDetailsAsync(occupationCode, query, category, page, cancellationToken);
        return viewModel is null ? NotFound() : View(viewModel);
    }
}
