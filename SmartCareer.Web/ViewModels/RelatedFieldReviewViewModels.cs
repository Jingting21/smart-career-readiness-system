using System.ComponentModel.DataAnnotations;
using SmartCareer.Web.Models;

namespace SmartCareer.Web.ViewModels;

public sealed class RelatedFieldReviewInputModel
{
    public int MappingId { get; set; }
    [Required] public string Decision { get; set; } = string.Empty;
    [Required, StringLength(2000, MinimumLength = 1)] public string Rationale { get; set; } = string.Empty;
    [Required, StringLength(64, MinimumLength = 64)] public string EvidenceFingerprint { get; set; } = string.Empty;
    public int ExpectedRevision { get; set; }
}

public sealed class RelatedFieldStateInputModel
{
    public int MappingId { get; set; }
    [Required, StringLength(2000, MinimumLength = 1)] public string Rationale { get; set; } = string.Empty;
    [Required, StringLength(64, MinimumLength = 64)] public string EvidenceFingerprint { get; set; } = string.Empty;
    public int ExpectedRevision { get; set; }
}

public sealed record RelatedFieldReviewDetailsViewModel(CareerDegreeField Mapping, string EvidenceFingerprint);
