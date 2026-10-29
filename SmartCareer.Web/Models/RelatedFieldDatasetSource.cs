namespace SmartCareer.Web.Models;

public sealed class RelatedFieldDatasetSource
{
    public int RelatedFieldDatasetSourceId { get; set; }
    public string DatasetVersion { get; set; } = string.Empty;
    public string MappingFileName { get; set; } = string.Empty;
    public DateOnly RetrievedOn { get; set; }
    public string SourceMetadataJson { get; set; } = string.Empty;
    public ICollection<CareerDegreeField> CareerDegreeFields { get; set; } = [];
}
