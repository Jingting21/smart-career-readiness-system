using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Controllers;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services.CareerCatalog;

namespace SmartCareer.Web.Tests;

public sealed class RelatedFieldsPublicVisibilityTests
{
    [Fact]
    public async Task DetailsReturnsAllImportedActiveFieldsWithoutApprovalOrPublicationAndWithoutDuplicates()
    {
        await using var context = CreateContext();
        var career = SeedCareer(context);
        AddMapping(context, career, "Software Engineering", "Approved", true, "Builds software systems.");
        AddMapping(context, career, "Software Engineering", "Approved", true, "Duplicate mapping.");
        AddMapping(context, career, "Computer Science", "Approved", true);
        AddMapping(context, career, "Draft Field", "Draft", true);
        AddMapping(context, career, "Review Field", "In Review", true);
        AddMapping(context, career, "Rejected Field", "Rejected", true);
        AddMapping(context, career, "Unpublished Field", "Approved", false);
        AddMapping(context, career, "Inactive Field", "Approved", true, active: false);
        await context.SaveChangesAsync();

        var result = await new CareerCatalogQueryService(context).GetDetailsAsync("15-1252.00", null, null, 1);

        Assert.NotNull(result);
        Assert.Equal(["Computer Science", "Draft Field", "Rejected Field", "Review Field", "Software Engineering", "Unpublished Field"],
            result.RelatedFieldsOfStudy.Select(field => field.Name));
        Assert.Equal("Builds software systems.", result.RelatedFieldsOfStudy[4].Description);
        Assert.Contains(result.RelatedFieldsOfStudy, field => field.Name == "Review Field");
        Assert.Contains(result.RelatedFieldsOfStudy, field => field.Name == "Unpublished Field");
        Assert.DoesNotContain(result.RelatedFieldsOfStudy, field => field.Name == "Inactive Field");
    }

    [Fact]
    public async Task CareerWithoutMappingsReturnsAnEmptyCollectionAndKeepsExistingDetails()
    {
        await using var context = CreateContext();
        SeedCareer(context);
        await context.SaveChangesAsync();

        var result = await new CareerCatalogQueryService(context).GetDetailsAsync("15-1252.00", null, null, 1);

        Assert.NotNull(result);
        Assert.Empty(result.RelatedFieldsOfStudy);
        Assert.Equal("Software Developers", result.Title);
        Assert.Equal("Career description", result.Description);
        Assert.NotNull(result.Provenance);
    }

    [Fact]
    public void PublicViewContainsDisclaimerAndEmptyStateButNoInternalReviewProperties()
    {
        var view = ReadWebFile("Views", "Careers", "Details.cshtml");
        var viewModelProperties = typeof(SmartCareer.Web.ViewModels.RelatedFieldOfStudyViewModel).GetProperties()
            .Select(property => property.Name).ToArray();

        Assert.Contains("Related Fields of Study", view);
        Assert.Contains("These fields of study may be relevant to this career. Actual education requirements may vary by employer and location.", view);
        Assert.Contains("No related fields of study are available for this career yet.", view);
        Assert.DoesNotContain("No verified related fields", view);
        Assert.Equal(["Name", "Description"], viewModelProperties);
        Assert.DoesNotContain("ReviewerNotes", view);
        Assert.DoesNotContain("ReviewedBy", view);
        Assert.DoesNotContain("EvidenceSummary", view);
        Assert.DoesNotContain("ReviewEvents", view);
    }

    [Fact]
    public void RelatedFieldsLayoutUsesResponsiveBlueThemeStyles()
    {
        var css = ReadWebFile("wwwroot", "css", "site.css");
        Assert.Contains(".related-fields-list{display:grid;grid-template-columns:repeat(2", css);
        Assert.Contains("background:var(--blue-pale)", css);
        Assert.Contains("@media(max-width:767.98px){.related-fields-list{grid-template-columns:1fr}}", css);
    }

    [Fact]
    public void AdministratorReviewRoutesAreRemovedWhileAdministratorDashboardRemainsProtected()
    {
        var assembly = typeof(AdminController).Assembly;
        Assert.Null(assembly.GetType("SmartCareer.Web.Controllers.AdminRelatedFieldsController"));

        var index = typeof(AdminController).GetMethod(nameof(AdminController.Index))!;
        var authorization = Assert.Single(index.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal(AppRoles.Administrator, authorization.Roles);

        var dashboard = ReadWebFile("Views", "Admin", "Index.cshtml");
        Assert.Contains("SmartCareer Administration", dashboard);
        Assert.DoesNotContain("AdminRelatedFields", dashboard);
        Assert.DoesNotContain("Review related fields of study", dashboard);
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"RelatedFieldsPublic-{Guid.NewGuid():N}").Options);

    private static Career SeedCareer(ApplicationDbContext context)
    {
        var career = new Career
        {
            Category = new CareerCategory { CategoryName = "Digital", Slug = "digital", IsActive = true },
            OccupationCode = "15-1252.00", SourceTitle = "Software Developers", Title = "Software Developers",
            Description = "Career description", SourceName = "O*NET", SourceVersion = "31.0", IsActive = true
        };
        context.Careers.Add(career);
        context.CareerCatalogSources.Add(new CareerCatalogSource
        {
            SourceName = "O*NET", SourceVersion = "31.0", OfficialSourceUrl = "https://www.onetonline.org/",
            LicenseName = "CC BY 4.0", LicenseUrl = "https://creativecommons.org/licenses/by/4.0/",
            RetrievedOn = new DateOnly(2026, 10, 24), AttributionText = "O*NET attribution"
        });
        return career;
    }

    private static void AddMapping(ApplicationDbContext context, Career career, string name, string status,
        bool published, string? description = null, bool active = true) => context.CareerDegreeFields.Add(new CareerDegreeField
    {
        Career = career,
        DegreeField = new DegreeField { CanonicalName = name, Description = description, IsActive = active,
            ClassificationSystem = "CIP", ClassificationCode = Guid.NewGuid().ToString("N"), ClassificationVersion = "2020" },
        DatasetSource = new RelatedFieldDatasetSource { DatasetVersion = "test", MappingFileName = "test.csv",
            RetrievedOn = new DateOnly(2026, 10, 29), SourceMetadataJson = "{}" },
        RelationshipType = "Primary", ConfidenceLevel = "High", ReviewStatus = status,
        EvidenceSummary = "Evidence", SourceUrls = "https://example.test", SourceVersions = "test",
        OriginalSocCode = "15-1252", OriginalCipCode = "11.0701", MalaysianNecCode = "0613",
        ReviewDate = new DateOnly(2026, 10, 29), ReviewerNotes = "Internal", ReviewedBy = "admin",
        IsPublished = published
    });

    private static string ReadWebFile(params string[] parts)
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var root = Path.Combine(directory.FullName, "SmartCareer.Web");
            if (File.Exists(Path.Combine(root, "SmartCareer.Web.csproj"))) return File.ReadAllText(Path.Combine([root, .. parts]));
        }
        throw new DirectoryNotFoundException();
    }
}
