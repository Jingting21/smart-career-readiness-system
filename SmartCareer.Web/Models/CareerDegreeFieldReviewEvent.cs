namespace SmartCareer.Web.Models;

public sealed class CareerDegreeFieldReviewEvent
{
    public long CareerDegreeFieldReviewEventId { get; set; }
    public int CareerDegreeFieldId { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? Decision { get; set; }
    public bool IsIndependentReview { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public string EvidenceFingerprint { get; set; } = string.Empty;
    public string PreviousReviewStatus { get; set; } = string.Empty;
    public string NewReviewStatus { get; set; } = string.Empty;
    public bool PreviousIsPublished { get; set; }
    public bool NewIsPublished { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
    public CareerDegreeField CareerDegreeField { get; set; } = null!;
    public ApplicationUser ActorUser { get; set; } = null!;
}
