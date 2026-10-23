using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Controllers;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services.CareerCatalog;

namespace SmartCareer.Web.Tests;

public sealed class CareerExplorerTests
{
    [Fact]
    public void CareersControllerRequiresStudentRoleAndUsesCareersRoute()
    {
        var authorization = Assert.Single(
            typeof(CareersController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());
        var route = Assert.Single(
            typeof(CareersController).GetCustomAttributes(typeof(RouteAttribute), true)
                .Cast<RouteAttribute>());

        Assert.Equal(AppRoles.Student, authorization.Roles);
        Assert.Equal("Careers", route.Template);
    }

    [Fact]
    public async Task QueryReturnsOnlyActiveCareersFromActiveCategories()
    {
        await using var context = CreateContext();
        SeedCatalogue(context);
        var result = await new CareerCatalogQueryService(context).GetExplorerAsync(null, null, 1);

        Assert.True(result.CategoryExists);
        Assert.Equal(14, result.ViewModel.TotalActiveCareers);
        Assert.Equal(14, result.ViewModel.FilteredCount);
        Assert.Equal(2, result.ViewModel.Categories.Count);
        Assert.DoesNotContain(result.ViewModel.Careers, career => career.Title == "Inactive career");
        Assert.DoesNotContain(result.ViewModel.Careers, career => career.Title == "Hidden category career");
    }

    [Theory]
    [InlineData("Data Analyst", "Data Analyst")]
    [InlineData("Physiotherapists", "Physical Therapy Specialist")]
    [InlineData("marine habitats", "Environmental Scientist")]
    public async Task SearchMatchesTitleSourceTitleAndDescription(string query, string expectedTitle)
    {
        await using var context = CreateContext();
        SeedCatalogue(context);
        var result = await new CareerCatalogQueryService(context).GetExplorerAsync($"  {query}  ", null, 1);

        var career = Assert.Single(result.ViewModel.Careers);
        Assert.Equal(expectedTitle, career.Title);
        Assert.Equal(query, result.ViewModel.Query);
    }

    [Fact]
    public async Task CategoryAndSearchCombineAndUnknownCategoryIsReported()
    {
        await using var context = CreateContext();
        SeedCatalogue(context);
        var service = new CareerCatalogQueryService(context);

        var filtered = await service.GetExplorerAsync("digital", "digital", 1);
        var unknown = await service.GetExplorerAsync(null, "not-a-category", 1);

        Assert.True(filtered.CategoryExists);
        Assert.All(filtered.ViewModel.Careers, career => Assert.Equal("digital", career.CategorySlug));
        Assert.Equal(11, filtered.ViewModel.FilteredCount);
        Assert.False(unknown.CategoryExists);
        Assert.Empty(unknown.ViewModel.Careers);
    }

    [Fact]
    public async Task PaginationUsesTwelveItemsAndClampsInvalidPages()
    {
        await using var context = CreateContext();
        SeedCatalogue(context);
        var service = new CareerCatalogQueryService(context);

        var first = await service.GetExplorerAsync(null, null, -8);
        var last = await service.GetExplorerAsync(null, null, 999);

        Assert.Equal(1, first.ViewModel.CurrentPage);
        Assert.Equal(12, first.ViewModel.Careers.Count);
        Assert.Equal(2, first.ViewModel.TotalPages);
        Assert.Equal(2, last.ViewModel.CurrentPage);
        Assert.Equal(2, last.ViewModel.Careers.Count);
        Assert.Equal(
            last.ViewModel.Careers.OrderBy(career => career.Title).Select(career => career.Title),
            last.ViewModel.Careers.Select(career => career.Title));
    }

    [Fact]
    public async Task SearchLongerThanOneHundredCharactersIsRejected()
    {
        await using var context = CreateContext();
        SeedCatalogue(context);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new CareerCatalogQueryService(context).GetExplorerAsync(new string('x', 101), null, 1));
    }

    [Fact]
    public void ViewProvidesFiltersEmptyStatesAndNoDetailsOrInternalData()
    {
        var view = ReadWebFile("Views", "Careers", "Index.cshtml");
        var sidebar = ReadWebFile("Views", "Shared", "_StudentSidebar.cshtml");
        var dashboard = ReadWebFile("Views", "Home", "Dashboard.cshtml");

        Assert.Contains("method=\"get\"", view);
        Assert.Contains("maxlength=\"100\"", view);
        Assert.Contains("Browse by category", view);
        Assert.Contains("class=\"category-chips\"", view);
        Assert.Contains("class=\"category-chip", view);
        Assert.DoesNotContain("class=\"category-card", view);
        Assert.Contains("id=\"career-results\"", view);
        Assert.Equal(7, CountOccurrences(view, "asp-fragment=\"career-results\""));
        Assert.Contains("asp-route-query=\"@Model.Query\" asp-route-category=\"@Model.SelectedCategorySlug\" asp-route-page=", view);
        Assert.Contains("Active filters", view);
        Assert.Contains("SelectedCategoryName", view);
        Assert.Contains("No careers matched your search", view);
        Assert.Contains("Career catalogue unavailable", view);
        Assert.DoesNotContain("OccupationCode", view);
        Assert.DoesNotContain("LocalReviewNote", view);
        Assert.DoesNotContain("asp-action=\"Details\"", view);
        Assert.Contains("asp-controller=\"Careers\" asp-action=\"Index\"", sidebar);
        Assert.DoesNotContain("asp-route-feature=\"Career Explorer\"", sidebar);
        Assert.Contains("<span class=\"status-label available\">Available</span>", dashboard);
        Assert.Contains("asp-controller=\"Careers\" asp-action=\"Index\"", dashboard);
    }

    [Fact]
    public void ExplorerStylesUseCompactResponsiveCardsAndPreserveFullDescriptionMarkup()
    {
        var view = ReadWebFile("Views", "Careers", "Index.cshtml");
        var css = ReadWebFile("wwwroot", "css", "site.css");

        Assert.Contains("<p>@career.Description</p>", view);
        Assert.Contains(".career-results{scroll-margin-top:6rem}", css);
        Assert.Contains("-webkit-line-clamp:3", css);
        Assert.Contains(".career-grid{display:grid;grid-template-columns:repeat(3,1fr)", css);
        Assert.Contains("@media(max-width:991.98px){.career-grid{grid-template-columns:repeat(2,1fr)}}", css);
        Assert.Contains(".career-grid{grid-template-columns:1fr}", css);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"CareerExplorer-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SeedCatalogue(ApplicationDbContext context)
    {
        var digital = new CareerCategory
        {
            CategoryName = "Digital",
            Slug = "digital",
            Description = "Digital careers",
            DisplayOrder = 1,
            IsActive = true
        };
        var science = new CareerCategory
        {
            CategoryName = "Science",
            Slug = "science",
            DisplayOrder = 2,
            IsActive = true
        };
        var hidden = new CareerCategory
        {
            CategoryName = "Hidden",
            Slug = "hidden",
            DisplayOrder = 3,
            IsActive = false
        };
        context.CareerCategories.AddRange(digital, science, hidden);

        for (var index = 1; index <= 11; index++)
        {
            context.Careers.Add(CreateCareer(
                digital,
                $"10-000{index:0}.00",
                index == 1 ? "Data Analyst" : $"Digital Career {index:00}",
                index == 1 ? "Data Analysts" : $"Digital Career {index:00}",
                "A sourced digital career description."));
        }

        context.Careers.Add(CreateCareer(
            science, "20-0001.00", "Physical Therapy Specialist", "Physiotherapists", "Supports patient mobility."));
        context.Careers.Add(CreateCareer(
            science, "20-0002.00", "Environmental Scientist", "Environmental Scientists", "Studies marine habitats."));
        context.Careers.Add(CreateCareer(
            science, "20-0003.00", "Research Chemist", "Chemists", "Conducts laboratory research."));
        context.Careers.Add(CreateCareer(
            digital, "10-9999.00", "Inactive career", "Inactive career", "Not visible.", false));
        context.Careers.Add(CreateCareer(
            hidden, "90-9999.00", "Hidden category career", "Hidden category career", "Not visible."));
        context.SaveChanges();
    }

    private static Career CreateCareer(
        CareerCategory category,
        string code,
        string title,
        string sourceTitle,
        string description,
        bool isActive = true) => new()
    {
        Category = category,
        OccupationCode = code,
        Title = title,
        SourceTitle = sourceTitle,
        Description = description,
        SourceName = "O*NET",
        SourceVersion = "31.0",
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static string ReadWebFile(params string[] parts)
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var root = Path.Combine(directory.FullName, "SmartCareer.Web");
            if (File.Exists(Path.Combine(root, "SmartCareer.Web.csproj")))
            {
                return File.ReadAllText(Path.Combine([root, .. parts]));
            }
        }

        throw new DirectoryNotFoundException();
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(search, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += search.Length;
        }

        return count;
    }
}
