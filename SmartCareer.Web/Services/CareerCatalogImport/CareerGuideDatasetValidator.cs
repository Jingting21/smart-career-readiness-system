using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartCareer.Web.Services.CareerCatalogImport;

public sealed class CareerGuideDatasetValidator
{
    public static readonly string[] GuideSourceFiles =
    [
        "task_statements.json", "task_ratings.json", "education.json", "education_categories.json",
        "training_and_experience.json", "training_and_experience_categories.json",
        "job_zones.json", "job_zone_reference.json"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = false };

    public CareerGuideDatasetValidationResult Validate(CareerCatalogDatasetPaths paths)
    {
        var result = new CareerGuideDatasetValidationResult();
        try
        {
            ValidateFilesAndHashes(paths, result);
            if (result.Errors.Count > 0) return result;

            var manifest = CareerCatalogCsv.Read(paths.ManifestPath);
            var approvedCodes = manifest.Select(row => row["OccupationCode"]).ToHashSet(StringComparer.Ordinal);
            result.ApprovedCareerCount = approvedCodes.Count;
            if (manifest.Count != 175 || approvedCodes.Count != 175)
                result.Errors.Add($"Approved manifest must contain 175 unique occupation codes; found {manifest.Count} rows and {approvedCodes.Count} unique codes.");

            ValidateTasks(paths, approvedCodes, result);
            ValidateEducation(paths, approvedCodes, result);
            ValidateTraining(paths, approvedCodes, result);
            ValidateJobZones(paths, approvedCodes, result);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException or KeyNotFoundException)
        {
            result.Errors.Add($"Validation could not complete: {exception.Message}");
        }
        return result;
    }

    public static string CreateMarkdownReport(CareerGuideDatasetValidationResult result)
    {
        var text = new StringBuilder();
        text.AppendLine("# SmartCareer O*NET 31.0 Career Guide Dataset Validation").AppendLine()
            .AppendLine($"- Validation status: **{(result.IsValid ? "PASS" : "FAIL")}**")
            .AppendLine($"- Validation errors: **{result.Errors.Count}**")
            .AppendLine($"- Validation warnings: **{result.Warnings.Count}**")
            .AppendLine($"- Approved occupations: **{result.ApprovedCareerCount}**").AppendLine()
            .AppendLine("## Pinned source hashes").AppendLine()
            .AppendLine("| File | SHA-256 |")
            .AppendLine("|---|---|");
        foreach (var hash in result.SourceHashes.OrderBy(item => item.Key, StringComparer.Ordinal))
            text.AppendLine($"| `{hash.Key}` | `{hash.Value}` |");

        text.AppendLine().AppendLine("## Coverage and row counts").AppendLine()
            .AppendLine("| Dataset | Full source rows | Approved-career rows | Usable display rows | Covered careers |")
            .AppendLine("|---|---:|---:|---:|---:|")
            .AppendLine($"| Task statements | {result.SourceTaskStatementRows} | {result.SelectedTaskStatementRows} | {result.SelectedTaskStatementRows} | {result.TaskCareerCoverage} |")
            .AppendLine($"| Task ratings | {result.SourceTaskRatingRows} | {result.SelectedTaskRatingRows} | {result.UsableTaskRatingRows} | {result.TaskCareerCoverage} |")
            .AppendLine($"| Education | {result.SourceEducationRows} | {result.SelectedEducationRows} | {result.UsableEducationRows} | {result.EducationCareerCoverage} |")
            .AppendLine($"| Training and experience | {result.SourceTrainingRows} | {result.SelectedTrainingRows} | {result.UsableTrainingRows} | {result.TrainingCareerCoverage} |")
            .AppendLine($"| Job Zones | {result.SourceJobZoneRows} | {result.SelectedJobZoneRows} | {result.SelectedJobZoneRows} | {result.JobZoneCareerCoverage} |").AppendLine()
            .AppendLine("## Task validation").AppendLine()
            .AppendLine($"- Core task statements: {result.CoreTaskCount}")
            .AppendLine($"- Supplemental task statements: {result.SupplementalTaskCount}")
            .AppendLine($"- Missing task type: {result.MissingTaskTypeCount}")
            .AppendLine($"- Suppressed task ratings: {result.SuppressedTaskRatingCount}")
            .AppendLine($"- Task statements without a usable Importance rating: {result.MissingTaskImportanceCount}").AppendLine()
            .AppendLine("## Education and preparation").AppendLine()
            .AppendLine($"- RL occupation coverage: {result.EducationRlCareerCoverage}")
            .AppendLine($"- RQ occupation coverage: {result.EducationRqCareerCoverage}")
            .AppendLine($"- Education modal ties: {result.EducationTieCount}")
            .AppendLine($"- Training modal ties — RW: {result.TrainingTieCounts.GetValueOrDefault("RW")}, PT: {result.TrainingTieCounts.GetValueOrDefault("PT")}, OJ: {result.TrainingTieCounts.GetValueOrDefault("OJ")}")
            .AppendLine($"- Missing education/training occupations: {string.Join(", ", result.MissingEducationOccupationCodes)}").AppendLine()
            .AppendLine("## Job Zone distribution").AppendLine();
        foreach (var item in result.JobZoneDistribution.OrderBy(item => item.Key)) text.AppendLine($"- Zone {item.Key}: {item.Value}");
        text.AppendLine().AppendLine("## Warnings");
        foreach (var warning in result.Warnings) text.AppendLine($"- {warning}");
        text.AppendLine().AppendLine("## Errors");
        if (result.Errors.Count == 0) text.AppendLine("- None."); else foreach (var error in result.Errors) text.AppendLine($"- {error}");
        return text.ToString();
    }

    private static void ValidateFilesAndHashes(CareerCatalogDatasetPaths paths, CareerGuideDatasetValidationResult result)
    {
        var expected = File.ReadLines(paths.ChecksumsPath).Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[1].Replace('\\', '/'), parts => parts[0], StringComparer.OrdinalIgnoreCase);
        using var metadata = JsonDocument.Parse(File.ReadAllText(paths.SourceMetadataPath));
        var metadataHashes = metadata.RootElement.GetProperty("artifacts").EnumerateArray().ToDictionary(
            item => item.GetProperty("path").GetString()!, item => item.GetProperty("sha256").GetString()!, StringComparer.OrdinalIgnoreCase);
        using var archive = ZipFile.OpenRead(paths.ArchivePath);

        foreach (var fileName in GuideSourceFiles)
        {
            var relative = $"raw/db_31_0_json/{fileName}";
            var fullPath = Path.Combine(paths.RawDirectory, fileName);
            if (!File.Exists(fullPath)) { result.Errors.Add($"Required source file is missing: {relative}"); continue; }
            var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath)));
            result.SourceHashes[relative] = actual;
            if (!expected.TryGetValue(relative, out var checksum) || !string.Equals(checksum, actual, StringComparison.OrdinalIgnoreCase))
                result.Errors.Add($"Checksum validation failed for '{relative}'.");
            if (!metadataHashes.TryGetValue(relative, out var metadataHash) || !string.Equals(metadataHash, actual, StringComparison.OrdinalIgnoreCase))
                result.Errors.Add($"Source metadata hash validation failed for '{relative}'.");
            var entry = archive.Entries.SingleOrDefault(item => string.Equals(Path.GetFileName(item.FullName), fileName, StringComparison.OrdinalIgnoreCase));
            if (entry is null) { result.Errors.Add($"Pinned archive does not contain '{fileName}'."); continue; }
            using var stream = entry.Open();
            if (!string.Equals(Convert.ToHexString(SHA256.HashData(stream)), actual, StringComparison.Ordinal))
                result.Errors.Add($"Extracted '{fileName}' does not match the pinned archive.");
        }
    }

    private static void ValidateTasks(CareerCatalogDatasetPaths paths, HashSet<string> approvedCodes, CareerGuideDatasetValidationResult result)
    {
        var allTasks = ReadRows<TaskRow>(paths.TaskStatementsPath);
        var allRatings = ReadRows<RatingRow>(paths.TaskRatingsPath);
        var tasks = allTasks.Where(row => approvedCodes.Contains(row.OccupationCode)).ToList();
        var ratings = allRatings.Where(row => approvedCodes.Contains(row.OccupationCode)).ToList();
        result.SourceTaskStatementRows = allTasks.Count; result.SelectedTaskStatementRows = tasks.Count;
        result.SourceTaskRatingRows = allRatings.Count; result.SelectedTaskRatingRows = ratings.Count;
        result.UsableTaskRatingRows = ratings.Count(IsUsable); result.SuppressedTaskRatingCount = ratings.Count(IsSuppressed);
        result.TaskCareerCoverage = tasks.Select(row => row.OccupationCode).Distinct(StringComparer.Ordinal).Count();
        result.CoreTaskCount = tasks.Count(row => row.TaskType == "Core");
        result.SupplementalTaskCount = tasks.Count(row => row.TaskType == "Supplemental");
        result.MissingTaskTypeCount = tasks.Count(row => string.IsNullOrWhiteSpace(row.TaskType));

        foreach (var duplicate in tasks.GroupBy(row => (row.OccupationCode, row.TaskId)).Where(group => group.Count() > 1))
            result.Errors.Add($"Duplicate task ID '{duplicate.Key.TaskId}' for '{duplicate.Key.OccupationCode}'.");
        foreach (var row in tasks.Where(row => row.TaskType is not ("Core" or "Supplemental" or null) && row.TaskType != ""))
            result.Errors.Add($"Unexpected task type '{row.TaskType}' for '{row.OccupationCode}/{row.TaskId}'.");

        var taskKeys = tasks.Select(row => (row.OccupationCode, row.TaskId)).ToHashSet();
        foreach (var orphan in ratings.Where(row => !taskKeys.Contains((row.OccupationCode, row.TaskId))))
            result.Errors.Add($"Task rating does not match a task using occupation code and task ID: '{orphan.OccupationCode}/{orphan.TaskId}'.");
        var importanceGroups = ratings.Where(row => row.ScaleId == "IM" && IsUsable(row))
            .GroupBy(row => (row.OccupationCode, row.TaskId)).ToList();
        foreach (var duplicate in importanceGroups.Where(group => group.Count() > 1))
            result.Errors.Add($"Multiple usable Importance ratings exist for '{duplicate.Key.OccupationCode}/{duplicate.Key.TaskId}'.");
        var importance = importanceGroups.ToDictionary(group => group.Key, group => group.First().DataValue!.Value);
        var relevanceGroups = ratings.Where(row => row.ScaleId == "RT" && IsUsable(row))
            .GroupBy(row => (row.OccupationCode, row.TaskId)).ToList();
        foreach (var duplicate in relevanceGroups.Where(group => group.Count() > 1))
            result.Errors.Add($"Multiple usable Relevance ratings exist for '{duplicate.Key.OccupationCode}/{duplicate.Key.TaskId}'.");
        var relevance = relevanceGroups.ToDictionary(group => group.Key, group => group.First().DataValue!.Value);
        result.MissingTaskImportanceCount = tasks.Count(row => !importance.ContainsKey((row.OccupationCode, row.TaskId)));
        result.Tasks = tasks.Select(row => new ValidatedTask(row.OccupationCode, row.TaskId, row.Task,
                string.IsNullOrWhiteSpace(row.TaskType) ? null : row.TaskType,
                importance.TryGetValue((row.OccupationCode, row.TaskId), out var importanceValue) ? importanceValue : null,
                relevance.TryGetValue((row.OccupationCode, row.TaskId), out var relevanceValue) ? relevanceValue : null, row.IncumbentsResponding,
                row.DateUpdated, row.DomainSource))
            .OrderBy(row => row.OccupationCode, StringComparer.Ordinal).ThenBy(row => row.TaskId).ToList();
        result.CoreTasks = result.Tasks.Where(row => row.TaskType == "Core")
            .OrderBy(row => row.OccupationCode, StringComparer.Ordinal).ThenByDescending(row => row.Importance).ThenBy(row => row.TaskId).ToList();
        if (result.TaskCareerCoverage != approvedCodes.Count) result.Errors.Add("Not every approved occupation has task statements.");
    }

    private static void ValidateEducation(CareerCatalogDatasetPaths paths, HashSet<string> approvedCodes, CareerGuideDatasetValidationResult result)
    {
        var allRows = ReadRows<RatingRow>(paths.EducationPath);
        var rows = allRows.Where(row => approvedCodes.Contains(row.OccupationCode)).ToList();
        var categories = ReadRows<CategoryRow>(paths.EducationCategoriesPath)
            .ToDictionary(row => (row.ScaleId, row.Category), row => row.CategoryDescription);
        result.SourceEducationRows = allRows.Count; result.SelectedEducationRows = rows.Count;
        result.UsableEducationRows = rows.Count(IsUsable);
        var display = rows.Where(row => row.ScaleId is "RL" or "RQ").ToList();
        result.EducationRlCareerCoverage = display.Where(row => row.ScaleId == "RL").Select(row => row.OccupationCode).Distinct().Count();
        result.EducationRqCareerCoverage = display.Where(row => row.ScaleId == "RQ").Select(row => row.OccupationCode).Distinct().Count();
        result.EducationCareerCoverage = display.Select(row => row.OccupationCode).Distinct().Count();
        foreach (var row in display.Where(IsUsable))
            if (!row.Category.HasValue || !categories.ContainsKey((row.ScaleId, row.Category.Value))) result.Errors.Add($"Education category is unresolved: '{row.ScaleId}/{row.Category}'.");
        result.EducationModes = BuildModes(display, row => categories[(row.ScaleId, row.Category!.Value)], out var ties);
        result.EducationRatings = display.Where(row => row.Category.HasValue)
            .Select(row => new ValidatedRating(row.OccupationCode, row.ElementId, row.ScaleId, row.Category!.Value,
                categories[(row.ScaleId, row.Category.Value)], row.DataValue, row.RespondentCount,
                row.StandardError, row.LowerConfidenceBound, row.UpperConfidenceBound,
                IsSuppressed(row), row.DateUpdated, row.DomainSource))
            .OrderBy(row => row.OccupationCode, StringComparer.Ordinal).ThenBy(row => row.ScaleId, StringComparer.Ordinal)
            .ThenBy(row => row.Category).ToList();
        result.EducationTieCount = ties;
        result.MissingEducationOccupationCodes = approvedCodes.Except(display.Select(row => row.OccupationCode), StringComparer.Ordinal).Order().ToList();
        if (result.MissingEducationOccupationCodes.Count > 0)
            result.Warnings.Add($"Education data is unavailable for {result.MissingEducationOccupationCodes.Count} approved occupations; no values may be inferred.");
    }

    private static void ValidateTraining(CareerCatalogDatasetPaths paths, HashSet<string> approvedCodes, CareerGuideDatasetValidationResult result)
    {
        var allRows = ReadRows<RatingRow>(paths.TrainingAndExperiencePath);
        var rows = allRows.Where(row => approvedCodes.Contains(row.OccupationCode)).ToList();
        var categories = ReadRows<CategoryRow>(paths.TrainingAndExperienceCategoriesPath)
            .ToDictionary(row => (row.ElementId, row.ScaleId, row.Category), row => row.CategoryDescription);
        result.SourceTrainingRows = allRows.Count; result.SelectedTrainingRows = rows.Count;
        result.UsableTrainingRows = rows.Count(IsUsable);
        var display = rows.Where(row => row.ScaleId is "RW" or "PT" or "OJ").ToList();
        result.TrainingCareerCoverage = display.Select(row => row.OccupationCode).Distinct().Count();
        foreach (var scale in new[] { "RW", "PT", "OJ" })
        {
            var scaleRows = display.Where(row => row.ScaleId == scale).ToList();
            foreach (var row in scaleRows.Where(IsUsable))
                if (!row.Category.HasValue || !categories.ContainsKey((row.ElementId, row.ScaleId, row.Category.Value))) result.Errors.Add($"Training category is unresolved: '{row.ElementId}/{row.ScaleId}/{row.Category}'.");
            result.TrainingModes[scale] = BuildModes(scaleRows, row => categories[(row.ElementId, row.ScaleId, row.Category!.Value)], out var ties);
            result.TrainingTieCounts[scale] = ties;
        }
        result.TrainingRatings = display.Where(row => row.Category.HasValue)
            .Select(row => new ValidatedRating(row.OccupationCode, row.ElementId, row.ScaleId, row.Category!.Value,
                categories[(row.ElementId, row.ScaleId, row.Category.Value)], row.DataValue, row.RespondentCount,
                row.StandardError, row.LowerConfidenceBound, row.UpperConfidenceBound,
                IsSuppressed(row), row.DateUpdated, row.DomainSource))
            .OrderBy(row => row.OccupationCode, StringComparer.Ordinal).ThenBy(row => row.ElementId, StringComparer.Ordinal)
            .ThenBy(row => row.ScaleId, StringComparer.Ordinal).ThenBy(row => row.Category).ToList();
        result.MissingTrainingOccupationCodes = approvedCodes.Except(display.Select(row => row.OccupationCode), StringComparer.Ordinal).Order().ToList();
        if (!result.MissingTrainingOccupationCodes.SequenceEqual(result.MissingEducationOccupationCodes))
            result.Errors.Add("Education and training missing-data occupation sets differ unexpectedly.");
        if (result.MissingTrainingOccupationCodes.Count > 0)
            result.Warnings.Add($"Training data is unavailable for {result.MissingTrainingOccupationCodes.Count} approved occupations; no values may be inferred.");
    }

    private static void ValidateJobZones(CareerCatalogDatasetPaths paths, HashSet<string> approvedCodes, CareerGuideDatasetValidationResult result)
    {
        var allRows = ReadRows<JobZoneRow>(paths.JobZonesPath);
        var rows = allRows.Where(row => approvedCodes.Contains(row.OccupationCode)).ToList();
        var references = ReadRows<JobZoneReferenceRow>(paths.JobZoneReferencePath).ToDictionary(row => row.JobZone);
        result.SourceJobZoneRows = allRows.Count; result.SelectedJobZoneRows = rows.Count;
        result.JobZoneCareerCoverage = rows.Select(row => row.OccupationCode).Distinct().Count();
        foreach (var code in approvedCodes)
        {
            var matches = rows.Where(row => row.OccupationCode == code).ToList();
            if (matches.Count != 1) result.Errors.Add($"Occupation '{code}' has {matches.Count} Job Zone assignments; expected exactly one.");
            else if (!references.ContainsKey(matches[0].JobZone)) result.Errors.Add($"Job Zone {matches[0].JobZone} for '{code}' has no reference row.");
        }
        result.JobZoneDistribution = rows.GroupBy(row => row.JobZone).OrderBy(group => group.Key).ToDictionary(group => group.Key, group => group.Count());
        result.JobZoneDefinitions = references.Values.OrderBy(row => row.JobZone)
            .Select(row => new ValidatedJobZoneDefinition(row.JobZone, row.Name, row.Experience,
                row.Education, row.JobTraining, row.Examples, row.SvpRange)).ToList();
        result.CareerJobZones = rows.OrderBy(row => row.OccupationCode, StringComparer.Ordinal)
            .Select(row => new ValidatedCareerJobZone(row.OccupationCode, row.JobZone, row.DateUpdated, row.DomainSource)).ToList();
    }

    private static List<ModalResponse> BuildModes(List<RatingRow> rows, Func<RatingRow, string> description, out int tieCount)
    {
        tieCount = 0; var modes = new List<ModalResponse>();
        foreach (var group in rows.Where(IsUsable).GroupBy(row => row.OccupationCode, StringComparer.Ordinal))
        {
            var maximum = group.Max(row => row.DataValue!.Value);
            var top = group.Where(row => row.DataValue == maximum).OrderBy(row => row.Category).ToList();
            if (top.Count > 1) tieCount++;
            modes.Add(new ModalResponse(group.Key, top.Select(description).ToList(), maximum));
        }
        return modes;
    }

    private static bool IsSuppressed(RatingRow row) => string.Equals(row.RecommendSuppress, "Y", StringComparison.Ordinal);
    private static bool IsUsable(RatingRow row) => !IsSuppressed(row) && row.DataValue.HasValue;
    private static List<T> ReadRows<T>(string path) => JsonSerializer.Deserialize<OnetTable<T>>(File.ReadAllText(path), JsonOptions)?.Rows
        ?? throw new InvalidDataException($"Could not deserialize '{path}'.");

    private sealed class OnetTable<T> { [JsonPropertyName("row")] public List<T> Rows { get; init; } = []; }
    private sealed class TaskRow
    {
        [JsonPropertyName("onetsoc_code")] public string OccupationCode { get; init; } = "";
        [JsonPropertyName("task_id")] public int TaskId { get; init; }
        [JsonPropertyName("task")] public string Task { get; init; } = "";
        [JsonPropertyName("task_type")] public string? TaskType { get; init; }
        [JsonPropertyName("incumbents_responding")] public int? IncumbentsResponding { get; init; }
        [JsonPropertyName("date_updated")] public DateOnly? DateUpdated { get; init; }
        [JsonPropertyName("domain_source")] public string DomainSource { get; init; } = "";
    }
    private sealed class RatingRow
    {
        [JsonPropertyName("onetsoc_code")] public string OccupationCode { get; init; } = "";
        [JsonPropertyName("task_id")] public int TaskId { get; init; }
        [JsonPropertyName("element_id")] public string ElementId { get; init; } = "";
        [JsonPropertyName("scale_id")] public string ScaleId { get; init; } = "";
        [JsonPropertyName("category")] public int? Category { get; init; }
        [JsonPropertyName("data_value")] public decimal? DataValue { get; init; }
        [JsonPropertyName("recommend_suppress")] public string? RecommendSuppress { get; init; }
        [JsonPropertyName("n")] public int? RespondentCount { get; init; }
        [JsonPropertyName("standard_error")] public decimal? StandardError { get; init; }
        [JsonPropertyName("lower_ci_bound")] public decimal? LowerConfidenceBound { get; init; }
        [JsonPropertyName("upper_ci_bound")] public decimal? UpperConfidenceBound { get; init; }
        [JsonPropertyName("date_updated")] public DateOnly? DateUpdated { get; init; }
        [JsonPropertyName("domain_source")] public string DomainSource { get; init; } = "";
    }
    private sealed class CategoryRow
    {
        [JsonPropertyName("element_id")] public string ElementId { get; init; } = "";
        [JsonPropertyName("scale_id")] public string ScaleId { get; init; } = "";
        [JsonPropertyName("category")] public int Category { get; init; }
        [JsonPropertyName("category_description")] public string CategoryDescription { get; init; } = "";
    }
    private sealed class JobZoneRow
    {
        [JsonPropertyName("onetsoc_code")] public string OccupationCode { get; init; } = "";
        [JsonPropertyName("job_zone")] public int JobZone { get; init; }
        [JsonPropertyName("date_updated")] public DateOnly? DateUpdated { get; init; }
        [JsonPropertyName("domain_source")] public string DomainSource { get; init; } = "";
    }
    private sealed class JobZoneReferenceRow
    {
        [JsonPropertyName("job_zone")] public int JobZone { get; init; }
        [JsonPropertyName("name")] public string Name { get; init; } = "";
        [JsonPropertyName("experience")] public string Experience { get; init; } = "";
        [JsonPropertyName("education")] public string Education { get; init; } = "";
        [JsonPropertyName("job_training")] public string JobTraining { get; init; } = "";
        [JsonPropertyName("examples")] public string Examples { get; init; } = "";
        [JsonPropertyName("svp_range")] public string SvpRange { get; init; } = "";
    }
}

public sealed class CareerGuideDatasetValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public Dictionary<string, string> SourceHashes { get; } = new(StringComparer.Ordinal);
    public int ApprovedCareerCount { get; set; }
    public int SourceTaskStatementRows { get; set; }
    public int SelectedTaskStatementRows { get; set; }
    public int SourceTaskRatingRows { get; set; }
    public int SelectedTaskRatingRows { get; set; }
    public int UsableTaskRatingRows { get; set; }
    public int SuppressedTaskRatingCount { get; set; }
    public int TaskCareerCoverage { get; set; }
    public int CoreTaskCount { get; set; }
    public int SupplementalTaskCount { get; set; }
    public int MissingTaskTypeCount { get; set; }
    public int MissingTaskImportanceCount { get; set; }
    public IReadOnlyList<ValidatedTask> CoreTasks { get; set; } = [];
    public IReadOnlyList<ValidatedTask> Tasks { get; set; } = [];
    public int SourceEducationRows { get; set; }
    public int SelectedEducationRows { get; set; }
    public int UsableEducationRows { get; set; }
    public int EducationCareerCoverage { get; set; }
    public int EducationRlCareerCoverage { get; set; }
    public int EducationRqCareerCoverage { get; set; }
    public int EducationTieCount { get; set; }
    public IReadOnlyList<ModalResponse> EducationModes { get; set; } = [];
    public IReadOnlyList<ValidatedRating> EducationRatings { get; set; } = [];
    public IReadOnlyList<string> MissingEducationOccupationCodes { get; set; } = [];
    public int SourceTrainingRows { get; set; }
    public int SelectedTrainingRows { get; set; }
    public int UsableTrainingRows { get; set; }
    public int TrainingCareerCoverage { get; set; }
    public Dictionary<string, IReadOnlyList<ModalResponse>> TrainingModes { get; } = new(StringComparer.Ordinal);
    public IReadOnlyList<ValidatedRating> TrainingRatings { get; set; } = [];
    public Dictionary<string, int> TrainingTieCounts { get; } = new(StringComparer.Ordinal);
    public IReadOnlyList<string> MissingTrainingOccupationCodes { get; set; } = [];
    public int SourceJobZoneRows { get; set; }
    public int SelectedJobZoneRows { get; set; }
    public int JobZoneCareerCoverage { get; set; }
    public Dictionary<int, int> JobZoneDistribution { get; set; } = [];
    public IReadOnlyList<ValidatedJobZoneDefinition> JobZoneDefinitions { get; set; } = [];
    public IReadOnlyList<ValidatedCareerJobZone> CareerJobZones { get; set; } = [];
}

public sealed record ValidatedTask(
    string OccupationCode, int TaskId, string Statement, string? TaskType,
    decimal? Importance, decimal? Relevance, int? IncumbentsResponding,
    DateOnly? SourceUpdatedOn, string DomainSource);
public sealed record ModalResponse(string OccupationCode, IReadOnlyList<string> CategoryDescriptions, decimal Percentage);
public sealed record ValidatedRating(
    string OccupationCode, string ElementId, string ScaleId, int Category, string CategoryDescription,
    decimal? Percentage, int? RespondentCount, decimal? StandardError,
    decimal? LowerConfidenceBound, decimal? UpperConfidenceBound, bool IsSuppressed,
    DateOnly? SourceUpdatedOn, string DomainSource);
public sealed record ValidatedJobZoneDefinition(
    int JobZone, string Name, string Experience, string Education,
    string JobTraining, string Examples, string SvpRange);
public sealed record ValidatedCareerJobZone(
    string OccupationCode, int JobZone, DateOnly? SourceUpdatedOn, string DomainSource);
