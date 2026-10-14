using System.ComponentModel.DataAnnotations;
using SmartCareer.Web.Models;
using SmartCareer.Web.Validation;

namespace SmartCareer.Web.ViewModels;

public class StudentDetailsInputModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [FullName]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string UniversitySelectionMode { get; set; } = SelectionModes.Catalog;
    public bool UniversityConfirmed { get; set; }
    [StringLength(150)]
    public string? University { get; set; }
    [StringLength(150)]
    [Display(Name = "Other university")]
    public string? CustomUniversity { get; set; }

    [Required]
    public string CourseSelectionMode { get; set; } = SelectionModes.Catalog;
    public bool CourseConfirmed { get; set; }
    [StringLength(150)]
    public string? Course { get; set; }
    [StringLength(150)]
    [Display(Name = "Other course")]
    public string? CustomCourse { get; set; }

    [Required(ErrorMessage = "Select your level of education.")]
    [EnumDataType(typeof(EducationLevel), ErrorMessage = "Select a valid level of education.")]
    [Display(Name = "Level of education")]
    public EducationLevel? EducationLevel { get; set; }
}

public static class SelectionModes
{
    public const string Catalog = "Catalog";
    public const string Other = "Other";
}
