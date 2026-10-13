using System.Text.Json;

namespace SmartCareer.Web.Services;

public sealed class AutocompleteCatalogService
{
    private static readonly HashSet<string> CommonPrograms = new(StringComparer.OrdinalIgnoreCase)
    {
        "Computer Science", "Software Engineering", "Information Technology", "Data Science",
        "Artificial Intelligence", "Information Systems", "Business Administration", "Accounting",
        "Finance", "Electrical Engineering", "Mechanical Engineering", "Civil Engineering",
        "Medicine", "Nursing", "Psychology", "Education"
    };

    private readonly CatalogItem[] universities;
    private readonly CatalogItem[] courses;

    public AutocompleteCatalogService(IWebHostEnvironment environment)
    {
        var catalogDirectory = Path.Combine(environment.ContentRootPath, "Data", "Catalogs");
        universities = Load(Path.Combine(catalogDirectory, "universities.json"));
        courses = Load(Path.Combine(catalogDirectory, "courses.json"))
            .Concat(CommonPrograms.Select(name => new CatalogItem(name, null, null)))
            .DistinctBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<Suggestion> SearchUniversities(string? query) =>
        Search(universities, query, prioritiseMalaysia: true, prioritiseCommonPrograms: false);

    public IReadOnlyList<Suggestion> SearchCourses(string? query) =>
        Search(courses, query, prioritiseMalaysia: false, prioritiseCommonPrograms: true);

    private static CatalogItem[] Load(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<CatalogItem[]>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];
    }

    private static IReadOnlyList<Suggestion> Search(
        IEnumerable<CatalogItem> source,
        string? query,
        bool prioritiseMalaysia,
        bool prioritiseCommonPrograms)
    {
        var term = query?.Trim();
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return [];
        }

        return source
            .Select(item => new { Item = item, Index = item.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) })
            .Where(match => match.Index >= 0)
            .OrderBy(match => match.Index == 0 ? 0 : 1)
            .ThenBy(match => prioritiseMalaysia && match.Item.CountryCode == "MY" ? 0 : 1)
            .ThenBy(match => prioritiseCommonPrograms && CommonPrograms.Contains(match.Item.Name) ? 0 : 1)
            .ThenBy(match => match.Index)
            .ThenBy(match => match.Item.Name)
            .Take(10)
            .Select(match => new Suggestion(
                match.Item.Name,
                string.IsNullOrWhiteSpace(match.Item.Country)
                    ? match.Item.Name
                    : $"{match.Item.Name} — {match.Item.Country}"))
            .ToArray();
    }

    private sealed record CatalogItem(string Name, string? Country, string? CountryCode);
}

public sealed record Suggestion(string Value, string Label);
