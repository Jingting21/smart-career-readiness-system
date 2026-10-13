using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using SmartCareer.Web.Services;

namespace SmartCareer.Web.Tests;

public sealed class AutocompleteCatalogTests
{
    private static readonly string WebRoot = Path.Combine(Directory.GetCurrentDirectory(), "SmartCareer.Web");

    [Fact]
    public void UniversitySearchIsCaseInsensitiveLimitedAndIncludesMalaysia()
    {
        var service = new AutocompleteCatalogService(new TestEnvironment(WebRoot));

        var results = service.SearchUniversities("uNiVeRsItY");

        Assert.NotEmpty(results);
        Assert.True(results.Count <= 10);
        Assert.Contains(results, item => item.Label.Contains("Malaysia", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("computer", "Computer Science")]
    [InlineData("software", "Software Engineering")]
    [InlineData("nursing", "Nursing")]
    public void CourseSearchSupportsPartialNames(string query, string expectedText)
    {
        var service = new AutocompleteCatalogService(new TestEnvironment(WebRoot));

        var results = service.SearchCourses(query);

        Assert.True(results.Count <= 10);
        Assert.Contains(results, item => item.Value.Contains(expectedText, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SearchesRequireAtLeastTwoCharacters()
    {
        var service = new AutocompleteCatalogService(new TestEnvironment(WebRoot));
        Assert.Empty(service.SearchUniversities("u"));
        Assert.Empty(service.SearchCourses("c"));
    }

    private sealed class TestEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "SmartCareer.Web.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(contentRootPath, "wwwroot");
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
