using System.Text.Json;
using System.Text.RegularExpressions;
using SmartCareer.Web.Services.CareerCatalogImport;

namespace SmartCareer.Web.Tests;

public sealed class RelatedFieldsDatasetValidationTests
{
    private static readonly string[] RequiredPilotCodes =
    [
        "13-2011.00", "15-1252.00", "15-2051.00", "17-2051.00",
        "29-1141.00", "27-1024.00", "23-1011.00"
    ];

    private static readonly HashSet<string> ReviewStatuses =
        ["Draft", "In Review", "Approved", "Rejected", "Needs Revalidation"];
    private static readonly HashSet<string> ConfidenceLevels = ["High", "Medium", "Low"];
    private static readonly HashSet<string> RelationshipTypes = ["Primary", "Related", "Specialised", "Alternative"];

    [Fact]
    public void PilotMappingsStayWithinApprovedManifestAndMeetCoverageRules()
    {
        var manifest = CareerCatalogCsv.Read(DataPath("onet-31.0", "smartcareer-175-career-selection-manifest-approved.csv"));
        var mappings = ReadMappings();
        var approvedCodes = manifest.Select(row => row["OccupationCode"]).ToHashSet(StringComparer.Ordinal);
        var manifestCategories = manifest.Select(row => row["SmartCareerCategory"]).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(175, approvedCodes.Count);
        Assert.InRange(mappings.Select(row => row["OnetOccupationCode"]).Distinct(StringComparer.Ordinal).Count(), 40, 60);
        Assert.All(mappings, row => Assert.Contains(row["OnetOccupationCode"], approvedCodes));
        Assert.All(RequiredPilotCodes, code => Assert.Contains(mappings, row => row["OnetOccupationCode"] == code));
        Assert.Equal(manifestCategories, mappings.Select(row => row["SmartCareerCategory"]).ToHashSet(StringComparer.Ordinal));
        Assert.All(mappings.GroupBy(row => row["SmartCareerCategory"]), group =>
            Assert.InRange(group.Select(row => row["OnetOccupationCode"]).Distinct(StringComparer.Ordinal).Count(), 4, 6));
    }

    [Fact]
    public void MappingRowsHaveUniqueKeysValidCodesEvidenceAndReviewMetadata()
    {
        var mappings = ReadMappings();
        var keys = mappings.Select(row => $"{row["OnetOccupationCode"]}|{row["RelatedFieldName"].Trim().ToUpperInvariant()}").ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.All(mappings, row =>
        {
            Assert.Matches(new Regex(@"^\d{2}-\d{4}\.\d{2}$", RegexOptions.CultureInvariant), row["OnetOccupationCode"]);
            Assert.Matches(new Regex(@"^\d{4}$", RegexOptions.CultureInvariant), row["NecCode"]);
            Assert.Matches(new Regex(@"^\d{2}\.\d{4}$", RegexOptions.CultureInvariant), row["CipCode"]);
            Assert.Contains(row["ReviewStatus"], ReviewStatuses);
            Assert.Contains(row["ConfidenceLevel"], ConfidenceLevels);
            Assert.Contains(row["RelationshipType"], RelationshipTypes);
            Assert.True(DateOnly.TryParseExact(row["ReviewDate"], "yyyy-MM-dd", out _));
            Assert.False(string.IsNullOrWhiteSpace(row["EvidenceSummary"]));
            Assert.False(string.IsNullOrWhiteSpace(row["SourceVersions"]));
            Assert.False(string.IsNullOrWhiteSpace(row["ReviewerNotes"]));
            Assert.All(row["SourceUrls"].Split('|'), value =>
                Assert.True(Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps,
                    $"Invalid evidence URL for {row["OnetOccupationCode"]}/{row["RelatedFieldName"]}: {value}"));
        });
    }

    [Fact]
    public void EveryCipCandidateExistsInPinnedOfficialCrosswalkEvidenceSubset()
    {
        var evidence = CareerCatalogCsv.Read(RelatedFieldsPath("cip-soc-evidence-subset-v1.0.csv"))
            .Select(row => $"{row["Soc2018Code"]}|{row["Cip2020Code"]}")
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(ReadMappings(), row =>
        {
            var socParent = row["OnetOccupationCode"][..7];
            Assert.Contains($"{socParent}|{row["CipCode"]}", evidence);
        });
    }

    [Fact]
    public void ReviewAndRegulationRulesPreventUnsupportedApprovalClaims()
    {
        Assert.All(ReadMappings(), row =>
        {
            if (row["ReviewStatus"] == "Approved")
            {
                Assert.Contains(row["ConfidenceLevel"], new[] { "High", "Medium" });
                Assert.DoesNotContain("requires human", row["ReviewerNotes"], StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("needs human", row["ReviewerNotes"], StringComparison.OrdinalIgnoreCase);
            }

            Assert.Contains(row["ProfessionalRegulationFlag"], new[] { "Yes", "No" });
            if (row["ProfessionalRegulationFlag"] == "Yes")
            {
                Assert.True(row["SourceUrls"].Split('|').Length >= 3);
                var notes = row["ReviewerNotes"];
                Assert.True(
                    notes.Contains("registration", StringComparison.OrdinalIgnoreCase) ||
                    notes.Contains("accreditation", StringComparison.OrdinalIgnoreCase) ||
                    notes.Contains("pract", StringComparison.OrdinalIgnoreCase) ||
                    notes.Contains("eligib", StringComparison.OrdinalIgnoreCase) ||
                    notes.Contains("requirement", StringComparison.OrdinalIgnoreCase) ||
                    notes.Contains("regulation", StringComparison.OrdinalIgnoreCase));
            }
        });
    }

    [Fact]
    public void ProvenanceMetadataIsVersionedCompleteAndConservativeAboutReuse()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RelatedFieldsPath("source-metadata-v1.0.json")));
        var root = document.RootElement;
        Assert.Equal("1.0", root.GetProperty("datasetVersion").GetString());
        Assert.True(DateOnly.TryParseExact(root.GetProperty("retrievedOn").GetString(), "yyyy-MM-dd", out _));

        var sources = root.GetProperty("sources").EnumerateArray().ToList();
        Assert.True(sources.Count >= 7);
        Assert.All(sources, source =>
        {
            Assert.False(string.IsNullOrWhiteSpace(source.GetProperty("sourceId").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(source.GetProperty("organisation").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(source.GetProperty("title").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(source.GetProperty("version").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(source.GetProperty("reuseTerms").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(source.GetProperty("identifiers").GetString()));
            Assert.True(Uri.TryCreate(source.GetProperty("officialUrl").GetString(), UriKind.Absolute, out _));
            Assert.True(Uri.TryCreate(source.GetProperty("reuseTermsUrl").GetString(), UriKind.Absolute, out _));
        });
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> ReadMappings() =>
        CareerCatalogCsv.Read(RelatedFieldsPath("smartcareer-related-fields-pilot-v1.0.csv"));

    private static string RelatedFieldsPath(string fileName) => DataPath("related-fields", fileName);

    private static string DataPath(params string[] parts)
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var root = Path.Combine(directory.FullName, "dataset-research");
            if (Directory.Exists(root)) return Path.Combine([root, .. parts]);
        }

        throw new DirectoryNotFoundException("Could not locate dataset-research.");
    }
}
