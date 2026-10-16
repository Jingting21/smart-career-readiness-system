using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
namespace SmartCareer.Web.Areas.Identity.Pages.Account.Manage;
[Authorize(Roles = AppRoles.Student)]
public class TwoFactorAuthenticationModel(UserManager<ApplicationUser> userManager) : PageModel
{
 public bool IsEnabled {get;private set;} public bool HasAuthenticator {get;private set;} public int RecoveryCodesLeft {get;private set;}
 public async Task OnGetAsync(){var u=await userManager.GetUserAsync(User)??throw new InvalidOperationException("Unable to load the signed-in account.");IsEnabled=await userManager.GetTwoFactorEnabledAsync(u);HasAuthenticator=await userManager.GetAuthenticatorKeyAsync(u) is not null;RecoveryCodesLeft=await userManager.CountRecoveryCodesAsync(u);}
}
