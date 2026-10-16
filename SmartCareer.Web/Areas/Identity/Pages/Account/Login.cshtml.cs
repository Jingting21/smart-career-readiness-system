using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    public const string InvalidCredentialsMessage = "Invalid email or password. Please try again.";
    [BindProperty] public InputModel Input { get; set; } = new();
    public IList<AuthenticationScheme> ExternalLogins { get; set; } = [];

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public sealed class InputModel
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
        [Display(Name = "Remember me")] public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        returnUrl = StudentRedirects.Resolve(returnUrl, Url.IsLocalUrl);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl = StudentRedirects.Resolve(returnUrl, Url.IsLocalUrl);
        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid) { ClearPassword(); return Page(); }
        var user = await signInManager.UserManager.FindByEmailAsync(Input.Email.Trim());
        var result = user is null ? Microsoft.AspNetCore.Identity.SignInResult.Failed : await signInManager.CheckPasswordSignInAsync(user, Input.Password, lockoutOnFailure: true);
        if (result.IsLockedOut) return RedirectToPage("./Lockout");
        if (user is null || !result.Succeeded || !await signInManager.UserManager.IsInRoleAsync(user, AppRoles.Student))
        {
            ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
            ClearPassword();
            return Page();
        }
        await signInManager.SignInAsync(user, Input.RememberMe);
        return LocalRedirect(returnUrl);
    }

    private void ClearPassword()
    {
        SensitiveModelStateCleaner.ClearAttemptedValue(ModelState, "Input.Password");
        Input.Password = "";
    }

    public async Task<IActionResult> OnPostExternalLoginAsync(string provider, string? returnUrl = null)
    {
        if (!string.Equals(provider, "Google", StringComparison.Ordinal))
        {
            return BadRequest("Google is the only student sign-in provider.");
        }
        var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
        if (!schemes.Any(scheme => string.Equals(scheme.Name, provider, StringComparison.Ordinal)))
        {
            return BadRequest("Unknown external authentication provider.");
        }

        var redirectUrl = Url.Page("./ExternalLogin", "Callback", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        RequestGoogleAccountSelection(provider, properties);
        return new ChallengeResult(provider, properties);
    }

    internal static void RequestGoogleAccountSelection(string provider, AuthenticationProperties properties)
    {
        if (string.Equals(provider, "Google", StringComparison.Ordinal))
        {
            properties.Parameters["prompt"] = "select_account";
        }
    }
}
