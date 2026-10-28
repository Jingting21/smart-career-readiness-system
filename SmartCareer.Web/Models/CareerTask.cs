namespace SmartCareer.Web.Models;

public sealed class CareerTask
{
    public int CareerTaskId { get; set; }
    public int CareerId { get; set; }
    public int OnetTaskId { get; set; }
    public string Statement { get; set; } = string.Empty;
    public string? TaskType { get; set; }
    public decimal? Importance { get; set; }
    public decimal? Relevance { get; set; }
    public int? IncumbentsResponding { get; set; }
    public DateOnly? SourceUpdatedOn { get; set; }
    public string DomainSource { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;

    public Career Career { get; set; } = null!;
}
