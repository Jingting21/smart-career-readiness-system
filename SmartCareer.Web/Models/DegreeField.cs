namespace SmartCareer.Web.Models;

public sealed class DegreeField
{
    public int DegreeFieldId { get; set; }
    public string CanonicalName { get; set; } = string.Empty;
    public string ClassificationSystem { get; set; } = string.Empty;
    public string ClassificationCode { get; set; } = string.Empty;
    public string ClassificationVersion { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<CareerDegreeField> CareerDegreeFields { get; set; } = [];
}
