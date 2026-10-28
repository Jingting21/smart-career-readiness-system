namespace SmartCareer.Web.Services.CareerCatalogImport;

public sealed record CareerCategoryImportRecord(
    string CategoryName,
    string Slug,
    string Description,
    short DisplayOrder);

public sealed record CareerImportRecord(
    string OccupationCode,
    string SourceTitle,
    string Title,
    string CategorySlug,
    string Description,
    string SourceName,
    string SourceVersion,
    DateOnly? SourceUpdatedOn,
    string ReviewStatus,
    bool RequiresLocalReview,
    string? LocalReviewNote);

public sealed record CareerCatalogSourceImportRecord(
    string SourceName,
    string SourceVersion,
    string OfficialSourceUrl,
    string LicenseName,
    string LicenseUrl,
    DateOnly RetrievedOn,
    string AttributionText);

public sealed record SkillImportRecord(
    string ExternalSkillCode,
    string SkillName,
    string SkillCategory,
    string? Description);

public sealed record CareerSkillImportRecord(
    string OccupationCode,
    string ExternalSkillCode,
    decimal Importance,
    decimal? RequiredLevel,
    string SourceVersion);

public sealed record CareerTaskImportRecord(
    string OccupationCode,
    int OnetTaskId,
    string Statement,
    string? TaskType,
    decimal? Importance,
    decimal? Relevance,
    int? IncumbentsResponding,
    DateOnly? SourceUpdatedOn,
    string DomainSource,
    string SourceVersion);

public sealed record CareerEducationRatingImportRecord(
    string OccupationCode, string ElementId, string ScaleId, int Category,
    string CategoryDescription, decimal? Percentage, int? RespondentCount,
    decimal? StandardError, decimal? LowerConfidenceBound, decimal? UpperConfidenceBound,
    bool IsSuppressed, DateOnly? SourceUpdatedOn, string DomainSource, string SourceVersion);

public sealed record CareerTrainingRatingImportRecord(
    string OccupationCode, string ElementId, string ScaleId, int Category,
    string CategoryDescription, decimal? Percentage, int? RespondentCount,
    decimal? StandardError, decimal? LowerConfidenceBound, decimal? UpperConfidenceBound,
    bool IsSuppressed, DateOnly? SourceUpdatedOn, string DomainSource, string SourceVersion);

public sealed record JobZoneDefinitionImportRecord(
    int JobZone, string Name, string Experience, string Education,
    string JobTraining, string Examples, string SvpRange, string SourceVersion);

public sealed record CareerJobZoneImportRecord(
    string OccupationCode, int JobZone, DateOnly? SourceUpdatedOn,
    string DomainSource, string SourceVersion);

public sealed record CareerCatalogProvenance(
    string Dataset,
    string ReleaseDate,
    DateOnly RetrievedOn,
    string SourceUrl,
    string LicenseUrl,
    string ArchiveSha256,
    string ManifestSha256,
    string Attribution);

public sealed class CareerCatalogImportPlan
{
    public required IReadOnlyList<CareerCategoryImportRecord> Categories { get; init; }
    public required IReadOnlyList<CareerImportRecord> Careers { get; init; }
    public required IReadOnlyList<SkillImportRecord> Skills { get; init; }
    public required IReadOnlyList<CareerSkillImportRecord> CareerSkills { get; init; }
    public required IReadOnlyList<CareerTaskImportRecord> CareerTasks { get; init; }
    public required IReadOnlyList<CareerEducationRatingImportRecord> EducationRatings { get; init; }
    public required IReadOnlyList<CareerTrainingRatingImportRecord> TrainingRatings { get; init; }
    public required IReadOnlyList<JobZoneDefinitionImportRecord> JobZoneDefinitions { get; init; }
    public required IReadOnlyList<CareerJobZoneImportRecord> CareerJobZones { get; init; }
    public required IReadOnlyList<CareerCatalogSourceImportRecord> Sources { get; init; }
    public required CareerCatalogProvenance Provenance { get; init; }
    public required IReadOnlyList<string> Errors { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }

    public bool IsValid => Errors.Count == 0;
}

public sealed record CareerCatalogDatasetPaths(string RootDirectory)
{
    public string ManifestPath => Path.Combine(RootDirectory, "smartcareer-175-career-selection-manifest-approved.csv");
    public string ValidationReportPath => Path.Combine(RootDirectory, "smartcareer-175-career-validation-report.md");
    public string ChecksumsPath => Path.Combine(RootDirectory, "smartcareer-175-career-approved-checksums.sha256");
    public string SourceMetadataPath => Path.Combine(RootDirectory, "source-metadata.json");
    public string ArchivePath => Path.Combine(RootDirectory, "db_31_0_json.zip");
    public string RawDirectory => Path.Combine(RootDirectory, "raw", "db_31_0_json");
    public string OccupationsPath => Path.Combine(RawDirectory, "occupation_data.json");
    public string EssentialSkillsPath => Path.Combine(RawDirectory, "essential_skills.json");
    public string TransferableSkillsPath => Path.Combine(RawDirectory, "transferable_skills.json");
    public string ContentModelPath => Path.Combine(RawDirectory, "content_model_reference.json");
    public string ScalesPath => Path.Combine(RawDirectory, "scales_reference.json");
    public string TaskStatementsPath => Path.Combine(RawDirectory, "task_statements.json");
    public string TaskRatingsPath => Path.Combine(RawDirectory, "task_ratings.json");
    public string EducationPath => Path.Combine(RawDirectory, "education.json");
    public string EducationCategoriesPath => Path.Combine(RawDirectory, "education_categories.json");
    public string TrainingAndExperiencePath => Path.Combine(RawDirectory, "training_and_experience.json");
    public string TrainingAndExperienceCategoriesPath => Path.Combine(RawDirectory, "training_and_experience_categories.json");
    public string JobZonesPath => Path.Combine(RawDirectory, "job_zones.json");
    public string JobZoneReferencePath => Path.Combine(RawDirectory, "job_zone_reference.json");
}

public sealed record CareerCatalogDryRunReport(
    string Status,
    bool DatabaseWritesPerformed,
    int CareerCategories,
    int Careers,
    int Skills,
    int EssentialSkills,
    int TransferableSkills,
    int CareerSkills,
    int CareerSkillsWithRequiredLevel,
    int CareerSkillsWithoutRequiredLevel,
    int Sources,
    string SourceName,
    string SourceVersion,
    string LicenseName,
    string LicenseUrl,
    string AttributionText,
    int NormalApprovedCareers,
    int CareersRequiringLocalReview,
    bool ReviewNotesValid,
    IReadOnlyDictionary<string, int> CareersByCategory,
    IReadOnlyDictionary<string, string> ReviewFlags,
    IReadOnlyList<string> RejectedRecords,
    IReadOnlyList<string> ValidationWarnings,
    CareerCatalogProvenance Provenance);
