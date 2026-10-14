
using Microsoft.AspNetCore.Identity;

namespace SmartCareer.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string? FullName { get; set; }

        [PersonalData]
        public string? University { get; set; }

        [PersonalData]
        public string? Course { get; set; }

        [PersonalData]
        public EducationLevel? EducationLevel { get; set; }

        [PersonalData]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum EducationLevel
    {
        Foundation = 1,
        Diploma = 2,
        [System.ComponentModel.DataAnnotations.Display(Name = "Bachelor's Degree")]
        BachelorsDegree = 3
    }
}
