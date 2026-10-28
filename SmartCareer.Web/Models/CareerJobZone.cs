namespace SmartCareer.Web.Models;

public sealed class CareerJobZone
{
    public int CareerJobZoneId { get; set; }
    public int CareerId { get; set; }
    public int JobZone { get; set; }
    public DateOnly? SourceUpdatedOn { get; set; }
    public string DomainSource { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;

    public Career Career { get; set; } = null!;
    public JobZoneDefinition JobZoneDefinition { get; set; } = null!;
}
