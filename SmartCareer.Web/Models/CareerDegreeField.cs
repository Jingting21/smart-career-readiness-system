namespace SmartCareer.Web.Models;

public sealed class CareerDegreeField
{
    public int CareerDegreeFieldId { get; set; }
    public int CareerId { get; set; }
    public int DegreeFieldId { get; set; }
    public int RelatedFieldDatasetSourceId { get; set; }
    public string RelationshipType { get; set; } = string.Empty;
    public string ConfidenceLevel { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = string.Empty;
    public string EvidenceSummary { get; set; } = string.Empty;
    public string OriginalSocCode { get; set; } = string.Empty;
    public string OriginalCipCode { get; set; } = string.Empty;
    public string MalaysianNecCode { get; set; } = string.Empty;
    public string SourceUrls { get; set; } = string.Empty;
    public string SourceVersions { get; set; } = string.Empty;
    public DateOnly ReviewDate { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool ProfessionalRegulationFlag { get; set; }
    public string ReviewerNotes { get; set; } = string.Empty;
    public DateOnly? RevalidationDueOn { get; set; }
    public bool IsPublished { get; set; }
    public int ReviewRevision { get; set; }
    public Career Career { get; set; } = null!;
    public DegreeField DegreeField { get; set; } = null!;
    public RelatedFieldDatasetSource DatasetSource { get; set; } = null!;
    public ICollection<CareerDegreeFieldReviewEvent> ReviewEvents { get; set; } = [];
}
