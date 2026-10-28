namespace SmartCareer.Web.Models;

public sealed class CareerEducationRating
{
    public int CareerEducationRatingId { get; set; }
    public int CareerId { get; set; }
    public string ElementId { get; set; } = string.Empty;
    public string ScaleId { get; set; } = string.Empty;
    public int Category { get; set; }
    public string CategoryDescription { get; set; } = string.Empty;
    public decimal? Percentage { get; set; }
    public int? RespondentCount { get; set; }
    public decimal? StandardError { get; set; }
    public decimal? LowerConfidenceBound { get; set; }
    public decimal? UpperConfidenceBound { get; set; }
    public bool IsSuppressed { get; set; }
    public DateOnly? SourceUpdatedOn { get; set; }
    public string DomainSource { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;

    public Career Career { get; set; } = null!;
}
