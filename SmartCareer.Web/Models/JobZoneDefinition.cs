namespace SmartCareer.Web.Models;

public sealed class JobZoneDefinition
{
    public int JobZone { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string Education { get; set; } = string.Empty;
    public string JobTraining { get; set; } = string.Empty;
    public string Examples { get; set; } = string.Empty;
    public string SvpRange { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;

    public ICollection<CareerJobZone> CareerJobZones { get; set; } = [];
}
