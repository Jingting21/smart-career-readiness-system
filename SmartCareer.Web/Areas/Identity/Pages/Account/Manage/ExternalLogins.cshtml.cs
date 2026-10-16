using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Areas.Identity.Pages.Account;

namespace SmartCareer.Web.Areas.Identity.Pages.Account.Manage;

[Authorize(Roles = AppRoles.Student)]
public class ExternalLoginsModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : PageModel
{
    public IList<UserLoginInfo> CurrentLogins { get; set; } = [];
    public IList<AuthenticationScheme> OtherLogins { get; set; } = [];
    public string Email { get; set; } = "";
    public bool GoogleConnected => CurrentLogins.Any(login => login.LoginProvider == "Google");
    public bool CanRemoveGoogle { get; set; }
    [TempData] public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(bool migration = false)
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return NotFound();
        await LoadAsync(user);
        if (migration && !GoogleConnected) StatusMessage = "Your existing account is verified. Connect Google below to complete migration.";
        return Page();
    }
    public async Task<IActionResult> OnPostLinkLoginAsync(string provider)
    {
        if (!string.Equals(provider, "Google", StringComparison.Ordinal)) return BadRequest("Google is the only supported provider.");
        var user = await userManager.GetUserAsync(User); if (user is null) return NotFound();
        var available = await signInManager.GetExternalAuthenticationSchemesAsync();
        if (!available.Any(s => s.Name == provider)) return BadRequest("Unknown sign-in provider.");
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        var redirect = Url.Page("./ExternalLogins", "LinkLoginCallback");
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirect!, user.Id);
        LoginModel.RequestGoogleAccountSelection(provider, properties);
        return new ChallengeResult(provider, properties);
    }
    public async Task<IActionResult> OnGetLinkLoginCallbackAsync()
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return NotFound();
        var info = await signInManager.GetExternalLoginInfoAsync(user.Id);
        if (info is null) { StatusMessage = "The connected account could not be verified. Please try again."; return RedirectToPage(); }
        var result = await userManager.AddLoginAsync(user, info);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        StatusMessage = result.Succeeded ? $"{info.ProviderDisplayName} is now connected." : string.Join(" ", result.Errors.Select(e => e.Description));
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return NotFound();
        var logins = await userManager.GetLoginsAsync(user);
        if (!await userManager.HasPasswordAsync(user) && logins.Count <= 1) { StatusMessage = "This is your only sign-in method. Add a password before disconnecting it."; return RedirectToPage(); }
        if (!logins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey)) return BadRequest();
        var result = await userManager.RemoveLoginAsync(user, loginProvider, providerKey);
        if (result.Succeeded) await signInManager.RefreshSignInAsync(user);
        StatusMessage = result.Succeeded ? "The connected account was removed." : "The connected account could not be removed.";
        return RedirectToPage();
    }
    private async Task LoadAsync(ApplicationUser user)
    {
        CurrentLogins = await userManager.GetLoginsAsync(user);
        var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
        OtherLogins = schemes.Where(s => s.Name == "Google" && CurrentLogins.All(l => l.LoginProvider != s.Name)).ToList();
        Email = await userManager.GetEmailAsync(user) ?? "";
        CanRemoveGoogle = await userManager.HasPasswordAsync(user) || CurrentLogins.Count > 1;
    }
}
