namespace SmartCareer.Web.Models;

using System.ComponentModel.DataAnnotations;

public sealed class Career
{
    public int CareerId { get; set; }
    public string OccupationCode { get; set; } = string.Empty;
    public string SourceTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Responsibilities { get; set; }
    public string? Qualifications { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public DateOnly? SourceUpdatedOn { get; set; }
    public bool RequiresLocalReview { get; set; }
    [MaxLength(500)]
    public string? LocalReviewNote { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public CareerCategory Category { get; set; } = null!;
    public ICollection<CareerSkill> CareerSkills { get; set; } = [];
}
