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

    public bool UniversityConfirmed { get; set; }
    [Required(ErrorMessage = "Please select a university from the suggestions.")]
    [StringLength(150)]
    public string? University { get; set; }

    public bool CourseConfirmed { get; set; }
    [Required(ErrorMessage = "Please select a course from the suggestions.")]
    [StringLength(150)]
    public string? Course { get; set; }

    [Required(ErrorMessage = "Select your level of education.")]
    [EnumDataType(typeof(EducationLevel), ErrorMessage = "Select a valid level of education.")]
    [Display(Name = "Level of education")]
    public EducationLevel? EducationLevel { get; set; }
}
