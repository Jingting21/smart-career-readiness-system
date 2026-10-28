using SmartCareer.Web.Services.CareerCatalogImport;

namespace SmartCareer.Web.Tests;

public sealed class CareerGuideDatasetValidationTests
{
    private static readonly Lazy<CareerGuideDatasetValidationResult> Result = new(() =>
        new CareerGuideDatasetValidator().Validate(GetDatasetPaths()));

    [Fact]
    public void EightGuideFilesMatchChecksumsMetadataAndPinnedArchive()
    {
        var result = Result.Value;
        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(8, result.SourceHashes.Count);
        Assert.Equal("D48D985C2BCF39A8760C9FFAC599CD12309D597D564856E4BB3E9759EE948545",
            result.SourceHashes["raw/db_31_0_json/task_statements.json"]);
        Assert.Equal("F66D665A2E507C825A71AEDB2C13BA22765E8259BC6C7FE5B3CDFD8105475A66",
            result.SourceHashes["raw/db_31_0_json/job_zone_reference.json"]);
    }

    [Fact]
    public void TasksAreFilteredJoinedUniqueAndSortedWithoutInventedImportance()
    {
        var result = Result.Value;
        Assert.Equal(18_838, result.SourceTaskStatementRows);
        Assert.Equal(3_724, result.SelectedTaskStatementRows);
        Assert.Equal(175, result.TaskCareerCoverage);
        Assert.Equal(2_955, result.CoreTaskCount);
        Assert.Equal(702, result.SupplementalTaskCount);
        Assert.Equal(67, result.MissingTaskTypeCount);
        Assert.Equal(143, result.SuppressedTaskRatingCount);
        Assert.Equal(69, result.MissingTaskImportanceCount);
        foreach (var group in result.CoreTasks.GroupBy(task => task.OccupationCode))
            Assert.Equal(group.OrderByDescending(task => task.Importance).ThenBy(task => task.TaskId), group);
    }

    [Fact]
    public void EducationSupportsRlRqTiesSuppressionAndMissingData()
    {
        var result = Result.Value;
        Assert.Equal(11_495, result.SourceEducationRows);
        Assert.Equal(2_219, result.SelectedEducationRows);
        Assert.Equal(2_177, result.UsableEducationRows);
        Assert.Equal(142, result.EducationRlCareerCoverage);
        Assert.Equal(30, result.EducationRqCareerCoverage);
        Assert.Equal(172, result.EducationCareerCoverage);
        Assert.Equal(2, result.EducationTieCount);
        Assert.All(result.EducationModes.SelectMany(mode => mode.CategoryDescriptions), Assert.NotEmpty);
        Assert.Equal(["11-9013.00", "13-1041.00", "17-2111.00"], result.MissingEducationOccupationCodes);
    }

    [Fact]
    public void TrainingResolvesEachScaleSeparatelyAndPreservesTiedModes()
    {
        var result = Result.Value;
        Assert.Equal(26_812, result.SourceTrainingRows);
        Assert.Equal(5_143, result.SelectedTrainingRows);
        Assert.Equal(5_024, result.UsableTrainingRows);
        Assert.Equal(172, result.TrainingCareerCoverage);
        Assert.Equal(12, result.TrainingTieCounts["RW"]);
        Assert.Equal(17, result.TrainingTieCounts["PT"]);
        Assert.Equal(16, result.TrainingTieCounts["OJ"]);
        Assert.All(new[] { "RW", "PT", "OJ" }, scale =>
            Assert.All(result.TrainingModes[scale].SelectMany(mode => mode.CategoryDescriptions), Assert.NotEmpty));
        Assert.Equal(result.MissingEducationOccupationCodes, result.MissingTrainingOccupationCodes);
    }

    [Fact]
    public void EveryApprovedCareerHasOneResolvedJobZone()
    {
        var result = Result.Value;
        Assert.Equal(923, result.SourceJobZoneRows);
        Assert.Equal(175, result.SelectedJobZoneRows);
        Assert.Equal(175, result.JobZoneCareerCoverage);
        Assert.Equal(new Dictionary<int, int> { [2] = 3, [3] = 23, [4] = 113, [5] = 36 }, result.JobZoneDistribution);
    }

    [Fact]
    public void MissingSourcesReturnFailedValidationWithoutDatabaseOrUiWork()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"CareerGuideValidation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var result = new CareerGuideDatasetValidator().Validate(new CareerCatalogDatasetPaths(directory));
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Empty(result.SourceHashes);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void MarkdownReportSeparatesSourceSelectedAndUsableRows()
    {
        var report = CareerGuideDatasetValidator.CreateMarkdownReport(Result.Value);
        Assert.Contains("Validation status: **PASS**", report);
        Assert.Contains("Full source rows", report);
        Assert.Contains("Approved-career rows", report);
        Assert.Contains("Usable display rows", report);
        Assert.Contains("Task statements without a usable Importance rating: 69", report);
    }

    private static CareerCatalogDatasetPaths GetDatasetPaths()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var dataset = Path.Combine(directory.FullName, "dataset-research", "onet-31.0");
            if (File.Exists(Path.Combine(dataset, "smartcareer-175-career-selection-manifest-approved.csv")))
                return new CareerCatalogDatasetPaths(dataset);
        }
        throw new DirectoryNotFoundException("Could not locate the pinned O*NET 31.0 dataset.");
    }
}
