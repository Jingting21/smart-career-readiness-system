using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SmartCareer.Web.Services;

public sealed class AutocompleteCatalogService
{
    private readonly CatalogItem[] universities;
    private readonly CatalogItem[] courses;

    public AutocompleteCatalogService(IWebHostEnvironment environment)
    {
        var directory = Path.Combine(environment.ContentRootPath, "Data", "Catalogs");
        universities = MergeCatalogs(
            Load(Path.Combine(directory, "universities.json")),
            Load(Path.Combine(directory, "malaysian-institutions.json")));
        courses = MergeCatalogs(
            Load(Path.Combine(directory, "courses.json")),
            Load(Path.Combine(directory, "malaysian-courses.json")));
    }

    public IReadOnlyList<Suggestion> SearchUniversities(string? query) =>
        Search(universities, query, prioritiseMalaysia: true, penaliseSubUnits: true);

    public IReadOnlyList<Suggestion> SearchCourses(string? query) =>
        Search(courses, query, prioritiseMalaysia: false, penaliseSubUnits: false);

    private static CatalogItem[] Load(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<CatalogItem[]>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];
    }

    private static CatalogItem[] MergeCatalogs(CatalogItem[] worldwide, CatalogItem[] curated)
    {
        var curatedKeys = curated
            .SelectMany(item => item.SearchTerms)
            .Select(Normalize)
            .ToHashSet(StringComparer.Ordinal);

        return curated
            .Concat(worldwide.Where(item => !curatedKeys.Contains(Normalize(item.Name))))
            .GroupBy(item => Normalize(item.Name), StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }

    private static IReadOnlyList<Suggestion> Search(
        IEnumerable<CatalogItem> source,
        string? query,
        bool prioritiseMalaysia,
        bool penaliseSubUnits)
    {
        var term = Normalize(query);
        if (term.Length < 2)
        {
            return [];
        }

        return source
            .Select(item => new { Item = item, Score = MatchScore(item, term) })
            .Where(match => match.Score < int.MaxValue)
            .OrderBy(match => match.Score)
            .ThenBy(match => penaliseSubUnits && IsLikelySubUnit(match.Item.Name) ? 1 : 0)
            .ThenBy(match => prioritiseMalaysia && match.Item.CountryCode == "MY" ? 0 : 1)
            .ThenBy(match => match.Item.Name.Length)
            .ThenBy(match => match.Item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(term.Length <= 2 ? 1 : 10)
            .Select(match => new Suggestion(
                match.Item.Name,
                string.IsNullOrWhiteSpace(match.Item.Country)
                    ? match.Item.Name
                    : $"{match.Item.Name} — {match.Item.Country}"))
            .ToArray();
    }

    private static int MatchScore(CatalogItem item, string term)
    {
        var name = Normalize(item.Name);
        var aliases = (item.Aliases ?? []).Select(Normalize).Where(alias => alias.Length > 0).ToArray();

        if (name == term) return 0;
        if (aliases.Contains(term, StringComparer.Ordinal)) return 1;

        // Two-character queries such as UM and IT are useful abbreviations but
        // far too noisy as name prefixes or arbitrary substrings.
        if (term.Length <= 2) return int.MaxValue;

        if (name.StartsWith(term, StringComparison.Ordinal)) return 2;
        if (aliases.Any(alias => alias.StartsWith(term, StringComparison.Ordinal)))
            return name.Contains(term, StringComparison.Ordinal) ? 2 : 3;
        if (name.Contains(term, StringComparison.Ordinal)) return 3;
        if (aliases.Any(alias => alias.Contains(term, StringComparison.Ordinal))) return 3;
        return int.MaxValue;
    }

    private static bool IsLikelySubUnit(string name)
    {
        var normalized = $" {Normalize(name)} ";
        return normalized.Contains(" DEPARTMENT ", StringComparison.Ordinal) ||
               normalized.Contains(" FACULTY ", StringComparison.Ordinal) ||
               normalized.Contains(" RESEARCH CENTRE ", StringComparison.Ordinal) ||
               normalized.Contains(" RESEARCH CENTER ", StringComparison.Ordinal) ||
               normalized.Contains(" LABORATORY ", StringComparison.Ordinal);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var builder = new StringBuilder(value.Length);
        var previousWasSpace = false;
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
                previousWasSpace = false;
            }
            else if (!previousWasSpace && builder.Length > 0)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        return builder.ToString().TrimEnd();
    }

    private sealed class CatalogItem
    {
        public string Name { get; init; } = string.Empty;
        public string? Country { get; init; }
        public string? CountryCode { get; init; }
        public string[]? Aliases { get; init; } = [];
        public IEnumerable<string> SearchTerms => new[] { Name }.Concat(Aliases ?? []);
    }
}

public sealed record Suggestion(string Value, string Label);
