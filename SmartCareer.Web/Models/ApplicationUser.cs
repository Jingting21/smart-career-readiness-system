
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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
