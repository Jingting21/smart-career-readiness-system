using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using SmartCareer.Web.Services;

namespace SmartCareer.Web.Tests;

public sealed class AutocompleteCatalogTests
{
    private static readonly string WebRoot = Path.Combine(Directory.GetCurrentDirectory(), "SmartCareer.Web");

    [Theory]
    [InlineData("TARUMT", "Tunku Abdul Rahman University of Management and Technology")]
    [InlineData("TAR UMT", "Tunku Abdul Rahman University of Management and Technology")]
    [InlineData("TARUC", "Tunku Abdul Rahman University of Management and Technology")]
    [InlineData("Universiti Malaya", "Universiti Malaya")]
    [InlineData("University of Malaya", "Universiti Malaya")]
    [InlineData("UM", "Universiti Malaya")]
    [InlineData("UTM", "Universiti Teknologi Malaysia")]
    [InlineData("UTHM", "Universiti Tun Hussein Onn Malaysia")]
    [InlineData("UTAR", "Universiti Tunku Abdul Rahman")]
    [InlineData("MMU", "Multimedia University")]
    [InlineData("Oxford", "University of Oxford")]
    [InlineData("National University of Singapore", "National University of Singapore")]
    public void UniversityAliasesReturnExpectedInstitutionFirst(string query, string expected)
    {
        var results = CreateService().SearchUniversities(query);

        Assert.NotEmpty(results);
        Assert.Equal(expected, results[0].Value);
        Assert.True(results.Count <= 10);
        Assert.Equal(results.Count, results.Select(result => result.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [InlineData("Computer Science", "Computer Science")]
    [InlineData("Software Engineering", "Software Engineering")]
    [InlineData("Information Technology", "Information Technology")]
    [InlineData("Data Science", "Data Science")]
    [InlineData("Artificial Intelligence", "Artificial Intelligence")]
    [InlineData("Cybersecurity", "Cybersecurity")]
    [InlineData("Cyber Security", "Cybersecurity")]
    [InlineData("Accounting", "Accounting")]
    [InlineData("Accountancy", "Accounting")]
    [InlineData("Business Management", "Business Management")]
    [InlineData("Electrical and Electronic Engineering", "Electrical and Electronic Engineering")]
    [InlineData("Mass Communication", "Mass Communication")]
    public void CourseNamesAndAliasesReturnExpectedProgrammeFirst(string query, string expected)
    {
        var results = CreateService().SearchCourses(query);

        Assert.NotEmpty(results);
        Assert.Equal(expected, results[0].Value);
        Assert.True(results.Count <= 10);
        Assert.Equal(results.Count, results.Select(result => result.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void SearchIsCaseAndWhitespaceTolerant()
    {
        var service = CreateService();
        Assert.Equal("Universiti Malaya", service.SearchUniversities("  university   of   malaya ")[0].Value);
        Assert.Equal("Software Engineering", service.SearchCourses("  software   engineering ")[0].Value);
    }

    [Fact]
    public void ShortSearchDoesNotReturnIrrelevantSubstringMatches()
    {
        var results = CreateService().SearchUniversities("UM");
        Assert.Single(results);
        Assert.Equal("Universiti Malaya", results[0].Value);
        Assert.DoesNotContain(results, result => result.Value.StartsWith("Um", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("u")]
    public void EmptyOrTooShortSearchReturnsNoSuggestions(string? query)
    {
        var service = CreateService();
        Assert.Empty(service.SearchUniversities(query));
        Assert.Empty(service.SearchCourses(query));
    }

    private static AutocompleteCatalogService CreateService() =>
        new(new TestEnvironment(WebRoot));

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
