using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
namespace SmartCareer.Web.Areas.Identity.Pages.Account.Manage;
[Authorize(Roles = AppRoles.Student)]
public class PersonalDataModel(UserManager<ApplicationUser> userManager) : PageModel
{
 public bool RequirePassword {get;private set;}
 public async Task OnGetAsync(){var u=await userManager.GetUserAsync(User)??throw new InvalidOperationException("Unable to load the signed-in account.");RequirePassword=await userManager.HasPasswordAsync(u);}
}
