using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Areas.Identity.Pages.Account.Manage;

[Authorize(Roles = AppRoles.Student)]
public class EmailModel(UserManager<ApplicationUser> userManager) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public string Email { get; private set; } = "";
    public bool IsConfirmed { get; private set; }
    public async Task OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User) ?? throw new InvalidOperationException("Unable to load the signed-in account.");
        Email = await userManager.GetEmailAsync(user) ?? "";
        IsConfirmed = await userManager.IsEmailConfirmedAsync(user);
    }
}
