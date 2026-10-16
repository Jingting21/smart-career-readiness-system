using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Areas.Identity.Pages.Account.Manage;

[Authorize(Roles = AppRoles.Student)]
public class IndexModel(UserManager<ApplicationUser> userManager) : PageModel
{
    public required ApplicationUser CurrentUser { get; set; }
    public IList<string> Roles { get; set; } = [];
    public bool GoogleConnected { get; set; }
    public bool HasPassword { get; set; }
    public int LoginCount { get; set; }

    public async Task OnGetAsync()
    {
        CurrentUser = await userManager.GetUserAsync(User) ?? throw new InvalidOperationException("Unable to load the signed-in account.");
        Roles = await userManager.GetRolesAsync(CurrentUser);
        var logins = await userManager.GetLoginsAsync(CurrentUser);
        GoogleConnected = logins.Any(login => login.LoginProvider == "Google");
        LoginCount = logins.Count;
        HasPassword = await userManager.HasPasswordAsync(CurrentUser);
    }
}
