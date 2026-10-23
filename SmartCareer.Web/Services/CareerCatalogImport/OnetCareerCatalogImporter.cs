using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartCareer.Web.Services.CareerCatalogImport;

public sealed class OnetCareerCatalogImporter
{
    public const string SourceName = "O*NET";
    public const string SourceVersion = "31.0";
    public const string OfficialSourceUrl = "https://www.onetcenter.org/dl_files/database/db_31_0_json.zip";
    public const string LicenseName = "Creative Commons Attribution 4.0 International (CC BY 4.0)";
    public const string LicenseUrl = "https://creativecommons.org/licenses/by/4.0/";
    public const string Attribution =
        "This application includes information from the O*NET 31.0 Database by the U.S. Department of Labor, " +
        "Employment and Training Administration (USDOL/ETA), used under the CC BY 4.0 licence. " +
        "O*NET® is a trademark of USDOL/ETA. SmartCareer has modified some titles, selected a subset of occupations, " +
        "and assigned SmartCareer categories. USDOL/ETA has not approved, endorsed, or tested these modifications.";

    private static readonly IReadOnlyDictionary<string, string> ExpectedReviewFlags =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["13-1041.08"] = "Jurisdiction-sensitive career - Malaysian licensing and terminology review required",
            ["17-2011.00"] = "Specialised career - retain for Malaysian labour-market review",
            ["17-2151.00"] = "Specialised career - later Malaysian sector and pathway review required",
            ["17-2171.00"] = "Specialised career - later Malaysian energy-sector review required",
            ["29-9092.00"] = "Emerging profession - Malaysian pathway and governance review required",
            ["29-9099.01"] = "Regulated profession - Malaysian midwifery requirements must be verified",
            ["33-3021.00"] = "Jurisdiction-sensitive career - Malaysian recruitment and duties review required",
            ["33-3021.06"] = "Jurisdiction-sensitive career - Malaysian public-sector context review required",
            ["33-3051.00"] = "Jurisdiction-sensitive career - Malaysian recruitment requirements must be verified",
            ["39-7011.00"] = "Malaysian licensing and graduate-suitability review required",
            ["41-3041.00"] = "Later review of Malaysian graduate relevance recommended"
        };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    private static readonly CareerCategoryImportRecord[] ExpectedCategories =
    [
        new("Digital, Data and Computing", "digital-data-computing", "Digital technology, computing, cybersecurity, data and information careers.", 1),
        new("Engineering and Manufacturing", "engineering-manufacturing", "Engineering, technical design, production and manufacturing careers.", 2),
        new("Business, Finance and Management", "business-finance-management", "Business, financial, administrative and management careers.", 3),
        new("Health and Life Sciences", "health-life-sciences", "Healthcare, clinical and life-science careers.", 4),
        new("Science, Research and Environment", "science-research-environment", "Scientific research, analysis and environmental careers.", 5),
        new("Education and Social Services", "education-social-services", "Education, learning support and social-service careers.", 6),
        new("Arts, Media and Design", "arts-media-design", "Creative, communication, media and design careers.", 7),
        new("Law, Government and Public Services", "law-government-public-services", "Legal, government, safety and public-service careers.", 8),
        new("Built Environment, Transport and Logistics", "built-environment-transport-logistics", "Construction, planning, property, transport and logistics careers.", 9),
        new("Agriculture, Food, Hospitality and Tourism", "agriculture-food-hospitality-tourism", "Agriculture, food, hospitality and tourism careers.", 10)
    ];

    private static readonly IReadOnlyDictionary<string, int> ExpectedCategoryCounts =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Digital, Data and Computing"] = 20,
            ["Engineering and Manufacturing"] = 25,
            ["Business, Finance and Management"] = 25,
            ["Health and Life Sciences"] = 20,
            ["Science, Research and Environment"] = 15,
            ["Education and Social Services"] = 15,
            ["Arts, Media and Design"] = 15,
            ["Law, Government and Public Services"] = 15,
            ["Built Environment, Transport and Logistics"] = 15,
            ["Agriculture, Food, Hospitality and Tourism"] = 10
        };

    public CareerCatalogImportPlan BuildPlan(CareerCatalogDatasetPaths paths)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        ValidateRequiredFiles(paths, errors);
        if (errors.Count > 0)
        {
            return EmptyPlan(errors, warnings);
        }

        var expectedHashes = ReadChecksums(paths.ChecksumsPath, errors);
        ValidateHashes(paths, expectedHashes, errors);
        ValidateExtractedFilesAgainstArchive(paths, errors);

        var metadata = ReadJson<SourceMetadata>(paths.SourceMetadataPath);
        ValidateMetadata(metadata, errors);
        var provenance = CreateProvenance(metadata, expectedHashes, errors);
        var sources = new[]
        {
            new CareerCatalogSourceImportRecord(
                SourceName,
                SourceVersion,
                OfficialSourceUrl,
                LicenseName,
                LicenseUrl,
                new DateOnly(2026, 10, 24),
                Attribution)
        };

        var validationReport = File.ReadAllText(paths.ValidationReportPath);
        if (!validationReport.Contains("Validation status: **PASS**", StringComparison.Ordinal) ||
            !validationReport.Contains("Validation issues: **0**", StringComparison.Ordinal))
        {
            errors.Add("The approved validation report does not record PASS with zero validation issues.");
        }

        var manifest = ReadManifest(paths.ManifestPath, errors);
        var occupations = ReadJson<OnetTable<OccupationRow>>(paths.OccupationsPath).Rows;
        var essentialRows = ReadJson<OnetTable<SkillRatingRow>>(paths.EssentialSkillsPath).Rows;
        var transferableRows = ReadJson<OnetTable<SkillRatingRow>>(paths.TransferableSkillsPath).Rows;
        var contentModel = ReadJson<OnetTable<ContentModelRow>>(paths.ContentModelPath).Rows;
        var scales = ReadJson<OnetTable<ScaleRow>>(paths.ScalesPath).Rows;

        ValidateScales(scales, errors);

        var occupationByCode = ToUniqueDictionary(
            occupations, row => row.OccupationCode, "O*NET occupation", errors);
        var contentById = ToUniqueDictionary(
            contentModel, row => row.ElementId, "O*NET content-model element", errors);
        var categoryByName = ExpectedCategories.ToDictionary(category => category.CategoryName, StringComparer.Ordinal);

        var careers = new List<CareerImportRecord>();
        foreach (var item in manifest.OrderBy(row => row.OccupationCode, StringComparer.Ordinal))
        {
            if (!occupationByCode.TryGetValue(item.OccupationCode, out var occupation))
            {
                errors.Add($"Manifest occupation '{item.OccupationCode}' is missing from occupation_data.json.");
                continue;
            }

            if (!string.Equals(item.SourceTitle, occupation.Title, StringComparison.Ordinal))
            {
                errors.Add($"Manifest SourceTitle for '{item.OccupationCode}' does not exactly match O*NET 31.0.");
            }

            if (!categoryByName.TryGetValue(item.Category, out var category))
            {
                errors.Add($"Manifest occupation '{item.OccupationCode}' has unknown category '{item.Category}'.");
                continue;
            }

            bool requiresLocalReview;
            string? localReviewNote;
            try
            {
                (requiresLocalReview, localReviewNote) = ResolveReviewStatus(item.OccupationCode, item.ReviewStatus);
            }
            catch (InvalidDataException exception)
            {
                errors.Add(exception.Message);
                requiresLocalReview = false;
                localReviewNote = null;
            }

            careers.Add(new CareerImportRecord(
                item.OccupationCode,
                item.SourceTitle,
                item.Title,
                category.Slug,
                occupation.Description,
                SourceName,
                SourceVersion,
                null,
                item.ReviewStatus,
                requiresLocalReview,
                localReviewNote));
        }

        var selectedCodes = careers.Select(career => career.OccupationCode)
            .ToHashSet(StringComparer.Ordinal);
        var ratingPairs = BuildRatingPairs(essentialRows, "Essential", selectedCodes, errors)
            .Concat(BuildRatingPairs(transferableRows, "Transferable", selectedCodes, errors))
            .OrderBy(pair => pair.OccupationCode, StringComparer.Ordinal)
            .ThenBy(pair => pair.ElementId, StringComparer.Ordinal)
            .ToList();

        var skills = ratingPairs
            .GroupBy(pair => pair.ElementId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var categories = group.Select(pair => pair.SkillCategory).Distinct(StringComparer.Ordinal).ToList();
                if (categories.Count != 1)
                {
                    errors.Add($"Skill '{group.Key}' appears in more than one skill category.");
                }

                if (!contentById.TryGetValue(group.Key, out var content))
                {
                    errors.Add($"Skill element '{group.Key}' is missing from content_model_reference.json.");
                    return new SkillImportRecord(group.Key, group.First().ElementName, categories.FirstOrDefault() ?? "", null);
                }

                if (group.Any(pair => !string.Equals(pair.ElementName, content.ElementName, StringComparison.Ordinal)))
                {
                    errors.Add($"Skill name for element '{group.Key}' is inconsistent with the content model.");
                }

                return new SkillImportRecord(group.Key, content.ElementName, categories.SingleOrDefault() ?? "", content.Description);
            })
            .ToList();

        var careerSkills = ratingPairs.Select(pair => new CareerSkillImportRecord(
                pair.OccupationCode,
                pair.ElementId,
                pair.Importance,
                pair.RequiredLevel,
                SourceVersion))
            .ToList();

        ValidatePlan(ExpectedCategories, careers, skills, careerSkills, sources, manifest, errors);
        warnings.Add("Responsibilities and Qualifications remain null: the four-table schema cannot preserve structured O*NET tasks or education/training records without lossy concatenation.");
        warnings.Add("Career.SourceUpdatedOn remains null: O*NET occupation_data has no row update date and the release date has only month-level precision.");
        warnings.Add("Per-rating update dates remain report-only because CareerSkill has no matching source-date column.");

        return new CareerCatalogImportPlan
        {
            Categories = ExpectedCategories,
            Careers = careers,
            Skills = skills,
            CareerSkills = careerSkills,
            Sources = sources,
            Provenance = provenance,
            Errors = errors,
            Warnings = warnings
        };
    }

    public static CareerCatalogDryRunReport CreateDryRunReport(CareerCatalogImportPlan plan)
    {
        var source = plan.Sources.SingleOrDefault() ??
            new CareerCatalogSourceImportRecord("", "", "", "", "", default, "");
        return new CareerCatalogDryRunReport(
            plan.IsValid ? "PASS" : "FAIL",
            DatabaseWritesPerformed: false,
            plan.Categories.Count,
            plan.Careers.Count,
            plan.Skills.Count,
            plan.Skills.Count(skill => skill.SkillCategory == "Essential"),
            plan.Skills.Count(skill => skill.SkillCategory == "Transferable"),
            plan.CareerSkills.Count,
            plan.CareerSkills.Count(link => link.RequiredLevel.HasValue),
            plan.CareerSkills.Count(link => !link.RequiredLevel.HasValue),
            plan.Sources.Count,
            source.SourceName,
            source.SourceVersion,
            source.LicenseName,
            source.LicenseUrl,
            source.AttributionText,
            plan.Careers.Count(career => !career.RequiresLocalReview),
            plan.Careers.Count(career => career.RequiresLocalReview),
            plan.Careers.All(career => career.RequiresLocalReview
                ? !string.IsNullOrWhiteSpace(career.LocalReviewNote) && career.LocalReviewNote.Length <= 500
                : career.LocalReviewNote is null),
            plan.Careers.GroupBy(career => career.CategorySlug)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal),
            plan.Careers.Where(career => career.ReviewStatus != "Approved")
                .OrderBy(career => career.OccupationCode, StringComparer.Ordinal)
                .ToDictionary(career => career.OccupationCode, career => career.ReviewStatus, StringComparer.Ordinal),
            plan.Errors,
            plan.Warnings,
            plan.Provenance);
    }

    public static (bool RequiresLocalReview, string? LocalReviewNote) ResolveReviewStatus(
        string occupationCode,
        string reviewStatus)
    {
        if (ExpectedReviewFlags.TryGetValue(occupationCode, out var expectedNote))
        {
            if (!string.Equals(reviewStatus, expectedNote, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Manifest review status for '{occupationCode}' does not match its approved review note.");
            }
            return (true, reviewStatus);
        }

        if (!string.Equals(reviewStatus, "Approved", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Manifest occupation '{occupationCode}' has an unexpected review status '{reviewStatus}'.");
        }
        return (false, null);
    }

    public static IReadOnlyList<string> ValidateSourceRecord(CareerCatalogSourceImportRecord source)
    {
        var errors = new List<string>();
        ValidateRequired(source.SourceName, 50, "SourceName", "catalogue source", errors);
        ValidateRequired(source.SourceVersion, 20, "SourceVersion", "catalogue source", errors);
        ValidateRequired(source.OfficialSourceUrl, 500, "OfficialSourceUrl", "catalogue source", errors);
        ValidateRequired(source.LicenseName, 100, "LicenseName", "catalogue source", errors);
        ValidateRequired(source.LicenseUrl, 500, "LicenseUrl", "catalogue source", errors);
        ValidateRequired(source.AttributionText, 1000, "AttributionText", "catalogue source", errors);
        ValidateHttpsUrl(source.OfficialSourceUrl, "OfficialSourceUrl", errors);
        ValidateHttpsUrl(source.LicenseUrl, "LicenseUrl", errors);
        if (source.RetrievedOn == default)
        {
            errors.Add("RetrievedOn is required for the catalogue source.");
        }
        return errors;
    }

    private static IReadOnlyList<RatingPair> BuildRatingPairs(
        IReadOnlyList<SkillRatingRow> rows,
        string category,
        IReadOnlySet<string> selectedCodes,
        ICollection<string> errors)
    {
        var selected = rows.Where(row => selectedCodes.Contains(row.OccupationCode)).ToList();
        var duplicates = selected.GroupBy(row => (row.OccupationCode, row.ElementId, row.ScaleId))
            .Where(group => group.Count() > 1);
        foreach (var duplicate in duplicates)
        {
            errors.Add($"Duplicate {category} rating for {duplicate.Key.OccupationCode}/{duplicate.Key.ElementId}/{duplicate.Key.ScaleId}.");
        }

        var levels = selected.Where(row => IsUsable(row) && row.ScaleId == "LV")
            .ToDictionary(row => (row.OccupationCode, row.ElementId), row => row.DataValue);

        var importanceRows = selected.Where(row => IsUsable(row) && row.ScaleId == "IM").ToList();
        var result = new List<RatingPair>(importanceRows.Count);
        foreach (var row in importanceRows)
        {
            if (row.DataValue is < 1m or > 5m)
            {
                errors.Add($"Importance is outside 1-5 for {row.OccupationCode}/{row.ElementId}.");
            }

            levels.TryGetValue((row.OccupationCode, row.ElementId), out var level);
            decimal? requiredLevel = levels.ContainsKey((row.OccupationCode, row.ElementId)) ? level : null;
            if (requiredLevel is < 0m or > 7m)
            {
                errors.Add($"Level is outside 0-7 for {row.OccupationCode}/{row.ElementId}.");
            }

            result.Add(new RatingPair(
                row.OccupationCode,
                row.ElementId,
                row.ElementName,
                category,
                row.DataValue,
                requiredLevel));
        }

        return result;
    }

    private static bool IsUsable(SkillRatingRow row) =>
        !string.Equals(row.RecommendSuppress, "Y", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(row.NotRelevant, "Y", StringComparison.OrdinalIgnoreCase);

    private static void ValidatePlan(
        IReadOnlyList<CareerCategoryImportRecord> categories,
        IReadOnlyList<CareerImportRecord> careers,
        IReadOnlyList<SkillImportRecord> skills,
        IReadOnlyList<CareerSkillImportRecord> careerSkills,
        IReadOnlyList<CareerCatalogSourceImportRecord> sources,
        IReadOnlyList<ManifestRow> manifest,
        ICollection<string> errors)
    {
        RequireCount(categories.Count, 10, "career categories", errors);
        RequireCount(careers.Count, 175, "careers", errors);
        RequireCount(skills.Count, 35, "skills", errors);
        RequireCount(skills.Count(skill => skill.SkillCategory == "Essential"), 10, "Essential skills", errors);
        RequireCount(skills.Count(skill => skill.SkillCategory == "Transferable"), 25, "Transferable skills", errors);
        RequireCount(careerSkills.Count, 6125, "career-skill relationships", errors);
        RequireCount(sources.Count, 1, "catalogue sources", errors);
        RequireCount(careers.Count(career => !career.RequiresLocalReview), 164, "normally approved careers", errors);
        RequireCount(careers.Count(career => career.RequiresLocalReview), 11, "careers requiring local review", errors);

        ValidateUnique(categories, category => category.CategoryName, "category name", errors);
        ValidateUnique(categories, category => category.Slug, "category slug", errors);
        ValidateUnique(careers, career => career.OccupationCode, "occupation code", errors);
        ValidateUnique(skills, skill => skill.ExternalSkillCode, "skill code", errors);
        ValidateUnique(careerSkills, link => $"{link.OccupationCode}|{link.ExternalSkillCode}", "career-skill key", errors);

        var categorySlugs = categories.Select(category => category.Slug).ToHashSet(StringComparer.Ordinal);
        var careerCodes = careers.Select(career => career.OccupationCode).ToHashSet(StringComparer.Ordinal);
        var skillCodes = skills.Select(skill => skill.ExternalSkillCode).ToHashSet(StringComparer.Ordinal);
        foreach (var career in careers)
        {
            if (!categorySlugs.Contains(career.CategorySlug))
            {
                errors.Add($"Career '{career.OccupationCode}' references unknown category '{career.CategorySlug}'.");
            }

            ValidateRequired(career.OccupationCode, 10, "OccupationCode", career.OccupationCode, errors);
            ValidateRequired(career.SourceTitle, 150, "SourceTitle", career.OccupationCode, errors);
            ValidateRequired(career.Title, 150, "Title", career.OccupationCode, errors);
            ValidateRequired(career.Description, 1000, "Description", career.OccupationCode, errors);
            ValidateRequired(career.SourceName, 50, "SourceName", career.OccupationCode, errors);
            ValidateRequired(career.SourceVersion, 20, "SourceVersion", career.OccupationCode, errors);
            if (career.RequiresLocalReview)
            {
                ValidateRequired(career.LocalReviewNote, 500, "LocalReviewNote", career.OccupationCode, errors);
                if (!string.Equals(career.LocalReviewNote, career.ReviewStatus, StringComparison.Ordinal))
                {
                    errors.Add($"LocalReviewNote does not preserve ReviewStatus for '{career.OccupationCode}'.");
                }
            }
            else if (career.LocalReviewNote is not null)
            {
                errors.Add($"Unflagged career '{career.OccupationCode}' must not have a LocalReviewNote.");
            }
        }

        foreach (var sourceError in sources.SelectMany(ValidateSourceRecord))
        {
            errors.Add(sourceError);
        }
        if (sources.Count == 1)
        {
            var source = sources[0];
            if (source.SourceName != SourceName || source.SourceVersion != SourceVersion ||
                source.OfficialSourceUrl != OfficialSourceUrl || source.LicenseName != LicenseName ||
                source.LicenseUrl != LicenseUrl || source.RetrievedOn != new DateOnly(2026, 10, 24) ||
                source.AttributionText != Attribution)
            {
                errors.Add("Catalogue source metadata does not exactly match the approved O*NET 31.0 provenance record.");
            }
        }

        foreach (var skill in skills)
        {
            ValidateRequired(skill.ExternalSkillCode, 20, "ExternalSkillCode", skill.ExternalSkillCode, errors);
            ValidateRequired(skill.SkillName, 150, "SkillName", skill.ExternalSkillCode, errors);
            ValidateRequired(skill.SkillCategory, 100, "SkillCategory", skill.ExternalSkillCode, errors);
            ValidateOptional(skill.Description, 1000, "Skill.Description", skill.ExternalSkillCode, errors);
        }

        foreach (var link in careerSkills)
        {
            if (!careerCodes.Contains(link.OccupationCode) || !skillCodes.Contains(link.ExternalSkillCode))
            {
                errors.Add($"Career-skill relationship '{link.OccupationCode}/{link.ExternalSkillCode}' has an invalid foreign-key reference.");
            }
        }

        var actualCategoryCounts = manifest.GroupBy(row => row.Category, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        foreach (var expected in ExpectedCategoryCounts)
        {
            if (!actualCategoryCounts.TryGetValue(expected.Key, out var actual) || actual != expected.Value)
            {
                errors.Add($"Category '{expected.Key}' has {actual} careers; expected {expected.Value}.");
            }
        }

        foreach (var career in careers)
        {
            var links = careerSkills.Where(link => link.OccupationCode == career.OccupationCode).ToList();
            if (links.Count(link => skills.Single(skill => skill.ExternalSkillCode == link.ExternalSkillCode).SkillCategory == "Essential") != 10 ||
                links.Count(link => skills.Single(skill => skill.ExternalSkillCode == link.ExternalSkillCode).SkillCategory == "Transferable") != 25)
            {
                errors.Add($"Career '{career.OccupationCode}' does not have exactly 10 Essential and 25 Transferable importance ratings.");
            }
        }
    }

    private static void ValidateRequiredFiles(CareerCatalogDatasetPaths paths, ICollection<string> errors)
    {
        var files = new[]
        {
            paths.ManifestPath, paths.ValidationReportPath, paths.ChecksumsPath, paths.SourceMetadataPath,
            paths.ArchivePath, paths.OccupationsPath, paths.EssentialSkillsPath, paths.TransferableSkillsPath,
            paths.ContentModelPath, paths.ScalesPath
        };
        foreach (var file in files.Where(file => !File.Exists(file)))
        {
            errors.Add($"Required source file is missing: {file}");
        }
    }

    private static Dictionary<string, string> ReadChecksums(string path, ICollection<string> errors)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(path).Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || parts[0].Length != 64)
            {
                errors.Add($"Invalid checksum line: {line}");
                continue;
            }
            result[parts[1].Replace('/', Path.DirectorySeparatorChar)] = parts[0];
        }
        return result;
    }

    private static void ValidateHashes(
        CareerCatalogDatasetPaths paths,
        IReadOnlyDictionary<string, string> expectedHashes,
        ICollection<string> errors)
    {
        foreach (var expected in expectedHashes)
        {
            var fullPath = Path.Combine(paths.RootDirectory, expected.Key);
            if (!File.Exists(fullPath))
            {
                errors.Add($"Checksummed file is missing: {expected.Key}");
                continue;
            }
            var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath)));
            if (!string.Equals(actual, expected.Value, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"SHA-256 mismatch for '{expected.Key}'.");
            }
        }
    }

    private static void ValidateExtractedFilesAgainstArchive(CareerCatalogDatasetPaths paths, ICollection<string> errors)
    {
        using var archive = ZipFile.OpenRead(paths.ArchivePath);
        foreach (var file in new[]
                 {
                     paths.OccupationsPath, paths.EssentialSkillsPath, paths.TransferableSkillsPath,
                     paths.ContentModelPath, paths.ScalesPath
                 })
        {
            var entry = archive.Entries.SingleOrDefault(candidate =>
                string.Equals(Path.GetFileName(candidate.FullName), Path.GetFileName(file), StringComparison.OrdinalIgnoreCase));
            if (entry is null)
            {
                errors.Add($"Pinned archive does not contain '{Path.GetFileName(file)}'.");
                continue;
            }
            using var entryStream = entry.Open();
            var archiveHash = Convert.ToHexString(SHA256.HashData(entryStream));
            var extractedHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
            if (!string.Equals(archiveHash, extractedHash, StringComparison.Ordinal))
            {
                errors.Add($"Extracted '{Path.GetFileName(file)}' does not match the pinned archive.");
            }
        }
    }

    private static IReadOnlyList<ManifestRow> ReadManifest(string path, ICollection<string> errors)
    {
        var requiredHeaders = new[]
        {
            "OccupationCode", "SourceTitle", "Title", "SmartCareerCategory", "SkillDataAvailable", "ReviewStatus"
        };
        var rows = CareerCatalogCsv.Read(path);
        if (rows.Count > 0)
        {
            foreach (var header in requiredHeaders.Where(header => !rows[0].ContainsKey(header)))
            {
                errors.Add($"Approved manifest is missing required column '{header}'.");
            }
        }

        if (errors.Count > 0)
        {
            return [];
        }

        return rows.Select((row, index) =>
        {
            var reviewStatus = row["ReviewStatus"];
            var item = new ManifestRow(
                row["OccupationCode"], row["SourceTitle"], row["Title"], row["SmartCareerCategory"], reviewStatus);
            if (string.IsNullOrWhiteSpace(reviewStatus))
            {
                errors.Add($"Manifest row {index + 2} has no approval or review status.");
            }
            if (!string.Equals(row["SkillDataAvailable"], "Yes (10 essential IM; 25 transferable IM)", StringComparison.Ordinal))
            {
                errors.Add($"Manifest row {index + 2} does not assert the approved skill coverage.");
            }
            return item;
        }).ToList();
    }

    private static void ValidateMetadata(SourceMetadata metadata, ICollection<string> errors)
    {
        if (metadata.Dataset != "O*NET Database 31.0" || metadata.ReleaseDate != "August 2026" ||
            metadata.SourceUrl != "https://www.onetcenter.org/dl_files/database/db_31_0_json.zip" ||
            metadata.LicenseUrl != "https://www.onetcenter.org/license_db.html" ||
            metadata.RetrievedOn != new DateOnly(2026, 10, 24))
        {
            errors.Add("Source metadata does not match the approved pinned O*NET 31.0 release.");
        }
    }

    private static CareerCatalogProvenance CreateProvenance(
        SourceMetadata metadata,
        IReadOnlyDictionary<string, string> hashes,
        ICollection<string> errors)
    {
        hashes.TryGetValue("db_31_0_json.zip", out var archiveHash);
        hashes.TryGetValue("smartcareer-175-career-selection-manifest-approved.csv", out var manifestHash);
        if (archiveHash is null || manifestHash is null)
        {
            errors.Add("Approved checksums do not include both the source archive and approved manifest.");
        }
        return new CareerCatalogProvenance(
            metadata.Dataset ?? "", metadata.ReleaseDate ?? "", metadata.RetrievedOn,
            metadata.SourceUrl ?? "", LicenseUrl, archiveHash ?? "", manifestHash ?? "", Attribution);
    }

    private static void ValidateScales(IReadOnlyList<ScaleRow> rows, ICollection<string> errors)
    {
        var importance = rows.SingleOrDefault(row => row.ScaleId == "IM");
        var level = rows.SingleOrDefault(row => row.ScaleId == "LV");
        if (importance is not { Minimum: 1, Maximum: 5 })
        {
            errors.Add("O*NET IM scale is not the expected raw 1-5 range.");
        }
        if (level is not { Minimum: 0, Maximum: 7 })
        {
            errors.Add("O*NET LV scale is not the expected raw 0-7 range.");
        }
    }

    private static Dictionary<string, T> ToUniqueDictionary<T>(
        IEnumerable<T> values,
        Func<T, string> keySelector,
        string label,
        ICollection<string> errors)
    {
        var result = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var key = keySelector(value);
            if (!result.TryAdd(key, value))
            {
                errors.Add($"Duplicate {label} key '{key}'.");
            }
        }
        return result;
    }

    private static void ValidateUnique<T>(
        IEnumerable<T> values,
        Func<T, string> keySelector,
        string label,
        ICollection<string> errors)
    {
        foreach (var group in values.GroupBy(keySelector, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            errors.Add($"Duplicate {label} '{group.Key}'.");
        }
    }

    private static void ValidateRequired(
        string? value, int maximumLength, string field, string key, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field} is required for '{key}'.");
        }
        else if (value.Length > maximumLength)
        {
            errors.Add($"{field} exceeds {maximumLength} characters for '{key}'.");
        }
    }

    private static void ValidateOptional(
        string? value, int maximumLength, string field, string key, ICollection<string> errors)
    {
        if (value?.Length > maximumLength)
        {
            errors.Add($"{field} exceeds {maximumLength} characters for '{key}'.");
        }
    }

    private static void ValidateHttpsUrl(string value, string field, ICollection<string> errors)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            errors.Add($"{field} must be an absolute HTTPS URL.");
        }
    }

    private static void RequireCount(int actual, int expected, string label, ICollection<string> errors)
    {
        if (actual != expected)
        {
            errors.Add($"Expected {expected} {label}; found {actual}.");
        }
    }

    private static T ReadJson<T>(string path) where T : class =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)
        ?? throw new InvalidDataException($"Could not deserialize '{path}'.");

    private static CareerCatalogImportPlan EmptyPlan(IReadOnlyList<string> errors, IReadOnlyList<string> warnings) =>
        new()
        {
            Categories = [], Careers = [], Skills = [], CareerSkills = [], Sources = [],
            Provenance = new CareerCatalogProvenance("", "", default, "", "", "", "", Attribution),
            Errors = errors, Warnings = warnings
        };

    private sealed record ManifestRow(
        string OccupationCode, string SourceTitle, string Title, string Category, string ReviewStatus);
    private sealed record RatingPair(
        string OccupationCode, string ElementId, string ElementName, string SkillCategory,
        decimal Importance, decimal? RequiredLevel);

    private sealed class OnetTable<T>
    {
        [JsonPropertyName("row")]
        public List<T> Rows { get; init; } = [];
    }

    private sealed class OccupationRow
    {
        [JsonPropertyName("onetsoc_code")] public string OccupationCode { get; init; } = "";
        [JsonPropertyName("title")] public string Title { get; init; } = "";
        [JsonPropertyName("description")] public string Description { get; init; } = "";
    }

    private sealed class SkillRatingRow
    {
        [JsonPropertyName("onetsoc_code")] public string OccupationCode { get; init; } = "";
        [JsonPropertyName("element_id")] public string ElementId { get; init; } = "";
        [JsonPropertyName("element_name")] public string ElementName { get; init; } = "";
        [JsonPropertyName("scale_id")] public string ScaleId { get; init; } = "";
        [JsonPropertyName("data_value")] public decimal DataValue { get; init; }
        [JsonPropertyName("recommend_suppress")] public string? RecommendSuppress { get; init; }
        [JsonPropertyName("not_relevant")] public string? NotRelevant { get; init; }
    }

    private sealed class ContentModelRow
    {
        [JsonPropertyName("element_id")] public string ElementId { get; init; } = "";
        [JsonPropertyName("element_name")] public string ElementName { get; init; } = "";
        [JsonPropertyName("description")] public string? Description { get; init; }
    }

    private sealed class ScaleRow
    {
        [JsonPropertyName("scale_id")] public string ScaleId { get; init; } = "";
        [JsonPropertyName("minimum")] public int Minimum { get; init; }
        [JsonPropertyName("maximum")] public int Maximum { get; init; }
    }

    private sealed class SourceMetadata
    {
        [JsonPropertyName("dataset")] public string? Dataset { get; init; }
        [JsonPropertyName("releaseDate")] public string? ReleaseDate { get; init; }
        [JsonPropertyName("retrievedOn")] public DateOnly RetrievedOn { get; init; }
        [JsonPropertyName("sourceUrl")] public string? SourceUrl { get; init; }
        [JsonPropertyName("licenseUrl")] public string? LicenseUrl { get; init; }
    }
}
