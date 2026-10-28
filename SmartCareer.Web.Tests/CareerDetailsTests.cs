using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartCareer.Web.Controllers;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services.CareerCatalog;

namespace SmartCareer.Web.Tests;

public sealed class CareerDetailsTests
{
    [Fact]
    public void DetailsUsesStudentProtectedOccupationCodeRoute()
    {
        var authorization = Assert.Single(typeof(CareersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        var method = typeof(CareersController).GetMethod(nameof(CareersController.Details))!;
        var route = Assert.Single(method.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>());
        Assert.Equal(AppRoles.Student, authorization.Roles);
        Assert.Equal("{occupationCode}", route.Template);
        Assert.Equal(CareersController.DetailsRouteName, route.Name);
    }

    [Fact]
    public async Task NamedDetailsRouteGeneratesAndResolvesOccupationCodePathWithReturnContext()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(CareersController).Assembly);
        await using var app = builder.Build();
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();

        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToList();
        var links = app.Services.GetRequiredService<LinkGenerator>();
        var path = links.GetPathByName(CareersController.DetailsRouteName, new
        {
            occupationCode = "13-2011.00",
            query = "financial analysis",
            category = "business-and-finance",
            page = 2
        });

        Assert.NotNull(path);
        Assert.StartsWith("/Careers/13-2011.00?", path, StringComparison.Ordinal);
        Assert.Contains("query=financial%20analysis", path, StringComparison.Ordinal);
        Assert.Contains("category=business-and-finance", path, StringComparison.Ordinal);
        Assert.Contains("page=2", path, StringComparison.Ordinal);
        Assert.DoesNotContain("/Careers/Details", path, StringComparison.Ordinal);

        var endpoint = endpoints
            .OfType<RouteEndpoint>()
            .Single(item => item.Metadata.GetMetadata<IRouteNameMetadata>()?.RouteName == CareersController.DetailsRouteName);
        Assert.Equal("Careers/{occupationCode}", endpoint.RoutePattern.RawText);
        await app.StopAsync();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("11-0002.00")]
    [InlineData("11-0003.00")]
    public async Task MissingInactiveCareerOrInactiveCategoryReturnsNoDetails(string code)
    {
        await using var context = CreateContext();
        Seed(context);
        var result = await new CareerCatalogQueryService(context).GetDetailsAsync(code, null, null, 1);
        Assert.Null(result);
    }

    [Fact]
    public async Task DetailsGroupsAndOrdersSkillsAndRetainsNullLevel()
    {
        await using var context = CreateContext();
        Seed(context);
        var result = await new CareerCatalogQueryService(context).GetDetailsAsync("11-0001.00", null, null, 1);
        Assert.NotNull(result);

        Assert.Equal(["Analysis", "Communication"], result.EssentialSkills.Select(skill => skill.Name));
        Assert.Equal("Teamwork", Assert.Single(result.TransferableSkills).Name);
        Assert.Equal(4.8m, result.EssentialSkills[0].Importance);
        Assert.Null(result.EssentialSkills[0].RequiredLevel);
    }

    [Fact]
    public async Task DetailsReturnsOnlyCoreTasksOrderedByImportanceThenTaskId()
    {
        await using var context = CreateContext();
        Seed(context);
        var result = await new CareerCatalogQueryService(context).GetDetailsAsync("11-0001.00", null, null, 1);

        Assert.NotNull(result);
        Assert.Equal([102, 103, 101, 105, 106, 107, 108], result.TypicalTasks.Select(task => task.OnetTaskId));
        Assert.DoesNotContain(result.TypicalTasks, task => task.OnetTaskId is 104 or 109);
        Assert.Null(result.TypicalTasks[^1].Importance);
    }

    [Fact]
    public async Task DetailsBuildsUsableEducationTrainingDistributionsWithTiesAndZeroValues()
    {
        await using var context = CreateContext();
        Seed(context);
        var result = await new CareerCatalogQueryService(context).GetDetailsAsync("11-0001.00", null, null, 1);

        Assert.NotNull(result);
        Assert.Equal(["RL", "RQ"], result.EducationBackgrounds.Select(item => item.ScaleId));
        var rl = result.EducationBackgrounds[0];
        Assert.Equal(["Bachelor's degree", "Master's degree"], rl.MostCommonResponses.Select(item => item.CategoryDescription));
        Assert.All(rl.MostCommonResponses, item => Assert.Equal(45m, item.Percentage));
        Assert.Contains(rl.AllResponses, item => item.CategoryDescription == "No response" && item.Percentage == 0m);
        Assert.DoesNotContain(rl.AllResponses, item => item.CategoryDescription is "Suppressed" or "Missing");

        Assert.Equal(2, result.RelatedWorkExperience!.MostCommonResponses.Count);
        Assert.Equal("PT", result.OnSiteTraining!.ScaleId);
        Assert.Equal("OJ", result.OnTheJobTraining!.ScaleId);
        Assert.Contains(result.OnTheJobTraining.AllResponses, item => item.Percentage == 0m);
        Assert.Equal(4, result.JobZone!.Number);
        Assert.Equal("Zone education", result.JobZone.Education);
        Assert.Equal("Zone experience", result.JobZone.Experience);
        Assert.Equal("Zone training", result.JobZone.JobTraining);
    }

    [Fact]
    public async Task DetailsKeepsJobZoneWhenEducationAndTrainingAreUnavailable()
    {
        await using var context = CreateContext();
        Seed(context);
        var result = await new CareerCatalogQueryService(context).GetDetailsAsync("11-9013.00", null, null, 1);

        Assert.NotNull(result);
        Assert.False(result.HasEducationOrTraining);
        Assert.Empty(result.EducationBackgrounds);
        Assert.Null(result.RelatedWorkExperience);
        Assert.NotNull(result.JobZone);
    }

    [Fact]
    public async Task DetailsIncludesProvenanceNoticesAndSafeExplorerContext()
    {
        await using var context = CreateContext();
        Seed(context);
        var result = await new CareerCatalogQueryService(context)
            .GetDetailsAsync("11-0001.00", new string('x', 101), "digital", -2);
        Assert.NotNull(result);

        Assert.Equal("O*NET", result.Provenance.SourceName);
        Assert.Equal("31.0", result.Provenance.SourceVersion);
        Assert.Equal("https://www.onetonline.org/", result.Provenance.OfficialSourceUrl);
        Assert.Contains("CC BY 4.0", result.Provenance.LicenseName);
        Assert.Contains("O*NET", result.Provenance.AttributionText);
        Assert.True(result.RequiresLocalReview);
        Assert.Null(result.ExplorerQuery);
        Assert.Equal("digital", result.ExplorerCategory);
        Assert.Equal(1, result.ExplorerPage);
    }

    [Fact]
    public void ViewsExplainRatingsNoticesProvenanceAndNavigationWithoutInternalNote()
    {
        var detail = ReadWebFile("Views", "Careers", "Details.cshtml");
        var skills = ReadWebFile("Views", "Careers", "_CareerSkillList.cshtml");
        var skillCard = ReadWebFile("Views", "Careers", "_CareerSkillCard.cshtml");
        Assert.Contains("Back to Career Explorer", detail);
        Assert.Contains("asp-fragment=\"career-results\"", detail);
        Assert.Contains("Importance (1–5)", detail);
        Assert.Contains("Required level (0–7)", detail);
        Assert.Contains("not personal skill or readiness scores", detail);
        Assert.Contains("O*NET information is US-based", detail);
        Assert.Contains("Local verification recommended", detail);
        Assert.Contains("Provenance.AttributionText", detail);
        Assert.Contains("Provenance.LicenseName", detail);
        Assert.Contains("Level data not available", skillCard);
        Assert.DoesNotContain("LocalReviewNote", detail);
        Assert.Contains("Typical Tasks", detail);
        Assert.Contains("Show All Typical Tasks", detail);
        Assert.Contains("Show Less", detail);
        Assert.Contains("Actual responsibilities may vary by employer and country", detail);
        Assert.Contains("Education &amp; Preparation", detail);
        Assert.Contains("not mandatory qualifications or licensing requirements in Malaysia", detail);
        Assert.Contains("Education or training information is not available in the current O*NET dataset", detail);
        Assert.Contains("Job Zones describe typical preparation", detail);
        var preparation = ReadWebFile("Views", "Careers", "_PreparationDistribution.cshtml");
        Assert.Contains("MostCommonResponses", preparation);
        Assert.Contains("AllResponses", preparation);
        Assert.Contains("<details class=\"preparation-distribution\">", preparation);
        Assert.Contains("<summary>", preparation);
        Assert.Contains("View all response categories", preparation);
        var explorer = ReadWebFile("Views", "Careers", "Index.cshtml");
        Assert.Contains("asp-route=\"CareerDetails\"", explorer);
        Assert.DoesNotContain("asp-action=\"Details\"", explorer);
    }

    [Fact]
    public void SkillLayoutUsesTopSixAccessibleExpansionExactRatingsAndResponsiveGrid()
    {
        var detail = ReadWebFile("Views", "Careers", "Details.cshtml");
        var skills = ReadWebFile("Views", "Careers", "_CareerSkillList.cshtml");
        var card = ReadWebFile("Views", "Careers", "_CareerSkillCard.cshtml");
        var css = ReadWebFile("wwwroot", "css", "site.css");

        Assert.Contains("Career Overview", detail);
        Assert.Contains("Essential Skills", detail);
        Assert.Contains("Transferable Skills", detail);
        Assert.Contains("1 means not important and 5 means extremely important", detail);
        Assert.Contains("not a student's ability", detail);
        Assert.Contains("occupational ratings, not personal skill or readiness scores", detail);
        Assert.Contains("Source &amp; Malaysia Context", detail);
        Assert.Contains("const int initiallyVisible = 6", skills);
        Assert.Contains("Model.Take(initiallyVisible)", skills);
        Assert.Contains("Model.Skip(initiallyVisible)", skills);
        Assert.Contains("<details class=\"skill-expander\">", skills);
        Assert.Contains("<summary>", skills);
        Assert.Contains("Show All Skills", skills);
        Assert.Contains("Show Less", skills);
        Assert.Contains("How important is this skill?", card);
        Assert.Contains("What skill level is typically needed?", card);
        Assert.Equal(2, CountOccurrences(card, "<progress"));
        Assert.Equal(2, CountOccurrences(card, "aria-label="));
        Assert.DoesNotContain("High", card);
        Assert.DoesNotContain("Medium", card);
        Assert.DoesNotContain("Low", card);
        Assert.Contains(".skill-card-grid{display:grid;grid-template-columns:repeat(2", css);
        Assert.Contains("@media(max-width:767.98px){.skill-card-grid{grid-template-columns:1fr}", css);
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"CareerDetails-{Guid.NewGuid():N}").Options);

    private static void Seed(ApplicationDbContext context)
    {
        var active = new CareerCategory { CategoryName = "Digital", Slug = "digital", IsActive = true };
        var inactive = new CareerCategory { CategoryName = "Hidden", Slug = "hidden", IsActive = false };
        var career = Career(active, "11-0001.00", true); career.RequiresLocalReview = true; career.LocalReviewNote = "internal only";
        var missingPreparation = Career(active, "11-9013.00", true);
        context.Careers.AddRange(career, missingPreparation, Career(active, "11-0002.00", false), Career(inactive, "11-0003.00", true));
        var analysis = new Skill { ExternalSkillCode = "S1", SkillName = "Analysis", SkillCategory = "Essential", IsActive = true };
        var communication = new Skill { ExternalSkillCode = "S2", SkillName = "Communication", SkillCategory = "Essential", IsActive = true };
        var teamwork = new Skill { ExternalSkillCode = "S3", SkillName = "Teamwork", SkillCategory = "Transferable", IsActive = true };
        context.CareerSkills.AddRange(
            new CareerSkill { Career = career, Skill = communication, Importance = 3.2m, RequiredLevel = 2m, SourceVersion = "31.0" },
            new CareerSkill { Career = career, Skill = analysis, Importance = 4.8m, RequiredLevel = null, SourceVersion = "31.0" },
            new CareerSkill { Career = career, Skill = teamwork, Importance = 4.1m, RequiredLevel = 3m, SourceVersion = "31.0" });
        context.CareerTasks.AddRange(
            Task(career, 101, "Core one", "Core", 3m), Task(career, 102, "Core two", "Core", 5m),
            Task(career, 103, "Core three", "Core", 5m), Task(career, 104, "Supplemental", "Supplemental", 5m),
            Task(career, 105, "Core four", "Core", 2m), Task(career, 106, "Core five", "Core", 1m),
            Task(career, 107, "Core six", "Core", 1m), Task(career, 108, "Core seven", "Core", null),
            Task(career, 109, "Unclassified", null, 5m));
        context.CareerEducationRatings.AddRange(
            Education(career, "RL", 1, "Bachelor's degree", 45m),
            Education(career, "RL", 2, "Master's degree", 45m),
            Education(career, "RL", 3, "No response", 0m),
            Education(career, "RL", 4, "Suppressed", 99m, suppressed: true),
            Education(career, "RL", 5, "Missing", null),
            Education(career, "RQ", 1, "Bachelor's degree", 70m));
        context.CareerTrainingRatings.AddRange(
            Training(career, "3.A.1", "RW", 1, "One year", 50m),
            Training(career, "3.A.1", "RW", 2, "Two years", 50m),
            Training(career, "3.A.2", "PT", 1, "Up to one month", 60m),
            Training(career, "3.A.3", "OJ", 1, "None", 0m),
            Training(career, "3.A.3", "OJ", 2, "Up to one month", 40m),
            Training(career, "3.A.3", "OJ", 3, "Suppressed", 90m, suppressed: true));
        var zone = new JobZoneDefinition { JobZone = 4, Name = "Job Zone Four", Education = "Zone education",
            Experience = "Zone experience", JobTraining = "Zone training", Examples = "Examples", SvpRange = "7.0 to < 8.0", SourceVersion = "31.0" };
        context.CareerJobZones.AddRange(
            new CareerJobZone { Career = career, JobZoneDefinition = zone, DomainSource = "Analyst", SourceVersion = "31.0" },
            new CareerJobZone { Career = missingPreparation, JobZoneDefinition = zone, DomainSource = "Analyst", SourceVersion = "31.0" });
        context.CareerCatalogSources.Add(new CareerCatalogSource { SourceName = "O*NET", SourceVersion = "31.0", OfficialSourceUrl = "https://www.onetonline.org/", LicenseName = "CC BY 4.0", LicenseUrl = "https://creativecommons.org/licenses/by/4.0/", RetrievedOn = new DateOnly(2026, 10, 24), AttributionText = "O*NET data used under CC BY 4.0." });
        context.SaveChanges();
    }

    private static Career Career(CareerCategory category, string code, bool active) => new()
    {
        Category = category, OccupationCode = code, SourceTitle = "Source occupation", Title = "Local title",
        Description = "Full original description.", SourceName = "O*NET", SourceVersion = "31.0", IsActive = active,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    private static CareerTask Task(Career career, int id, string statement, string? type, decimal? importance) => new()
    {
        Career = career, OnetTaskId = id, Statement = statement, TaskType = type, Importance = importance,
        DomainSource = "Incumbent", SourceVersion = "31.0"
    };

    private static CareerEducationRating Education(
        Career career, string scale, int category, string description, decimal? percentage, bool suppressed = false) => new()
    {
        Career = career, ElementId = "2.D.1", ScaleId = scale, Category = category,
        CategoryDescription = description, Percentage = percentage, IsSuppressed = suppressed,
        DomainSource = "Incumbent", SourceVersion = "31.0"
    };

    private static CareerTrainingRating Training(
        Career career, string element, string scale, int category, string description, decimal? percentage, bool suppressed = false) => new()
    {
        Career = career, ElementId = element, ScaleId = scale, Category = category,
        CategoryDescription = description, Percentage = percentage, IsSuppressed = suppressed,
        DomainSource = "Incumbent", SourceVersion = "31.0"
    };

    private static string ReadWebFile(params string[] parts)
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var root = Path.Combine(directory.FullName, "SmartCareer.Web");
            if (File.Exists(Path.Combine(root, "SmartCareer.Web.csproj"))) return File.ReadAllText(Path.Combine([root, .. parts]));
        }
        throw new DirectoryNotFoundException();
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        for (var offset = 0; (offset = value.IndexOf(search, offset, StringComparison.Ordinal)) >= 0; offset += search.Length) count++;
        return count;
    }
}
