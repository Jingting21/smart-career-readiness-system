namespace SmartCareer.Web.ViewModels;

public sealed class CareerDetailsViewModel
{
    public required string OccupationCode { get; init; }
    public required string Title { get; init; }
    public required string SourceTitle { get; init; }
    public required string CategoryName { get; init; }
    public required string Description { get; init; }
    public string? Responsibilities { get; init; }
    public string? Qualifications { get; init; }
    public bool RequiresLocalReview { get; init; }
    public required CareerProvenanceViewModel Provenance { get; init; }
    public IReadOnlyList<CareerSkillViewModel> EssentialSkills { get; init; } = [];
    public IReadOnlyList<CareerSkillViewModel> TransferableSkills { get; init; } = [];
    public IReadOnlyList<CareerTaskViewModel> TypicalTasks { get; init; } = [];
    public IReadOnlyList<PreparationDistributionViewModel> EducationBackgrounds { get; init; } = [];
    public PreparationDistributionViewModel? RelatedWorkExperience { get; init; }
    public PreparationDistributionViewModel? OnSiteTraining { get; init; }
    public PreparationDistributionViewModel? OnTheJobTraining { get; init; }
    public JobZoneViewModel? JobZone { get; init; }
    public bool HasEducationOrTraining => EducationBackgrounds.Count > 0 || RelatedWorkExperience is not null ||
        OnSiteTraining is not null || OnTheJobTraining is not null;
    public string? ExplorerQuery { get; init; }
    public string? ExplorerCategory { get; init; }
    public int ExplorerPage { get; init; } = 1;
    public bool HasDifferentSourceTitle => !string.Equals(Title, SourceTitle, StringComparison.Ordinal);
}

public sealed record CareerSkillViewModel(string Name, string? Description, decimal Importance, decimal? RequiredLevel);

public sealed record CareerTaskViewModel(int OnetTaskId, string Statement, decimal? Importance);

public sealed record PreparationResponseViewModel(string CategoryDescription, decimal Percentage);

public sealed record PreparationDistributionViewModel(
    string ScaleId, string Heading, string MostCommonLabel,
    IReadOnlyList<PreparationResponseViewModel> MostCommonResponses,
    IReadOnlyList<PreparationResponseViewModel> AllResponses);

public sealed record JobZoneViewModel(
    int Number, string Name, string Education, string Experience, string JobTraining);

public sealed record CareerProvenanceViewModel(
    string SourceName, string SourceVersion, string OfficialSourceUrl, string LicenseName,
    string LicenseUrl, DateOnly RetrievedOn, string AttributionText);
