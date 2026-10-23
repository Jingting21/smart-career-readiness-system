using System.ComponentModel.DataAnnotations;

namespace SmartCareer.Web.Models;

public sealed class CareerCatalogSource
{
    public int CareerCatalogSourceId { get; set; }

    [Required, MaxLength(50)]
    public string SourceName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string SourceVersion { get; set; } = string.Empty;

    [Required, MaxLength(500), Url]
    public string OfficialSourceUrl { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LicenseName { get; set; } = string.Empty;

    [Required, MaxLength(500), Url]
    public string LicenseUrl { get; set; } = string.Empty;

    public DateOnly RetrievedOn { get; set; }

    [Required, MaxLength(1000)]
    public string AttributionText { get; set; } = string.Empty;
}
