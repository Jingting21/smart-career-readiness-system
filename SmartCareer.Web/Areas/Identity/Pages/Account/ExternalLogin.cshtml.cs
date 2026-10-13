using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Services;

namespace SmartCareer.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ExternalLoginModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IUserStore<ApplicationUser> userStore,
    StudentAccountService studentAccountService,
    ILogger<ExternalLoginModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ProviderDisplayName { get; set; } = "external provider";
    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public sealed class InputModel
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(150, MinimumLength = 2)]
        public string University { get; set; } = string.Empty;

        [Required, StringLength(150, MinimumLength = 2)]
        public string Course { get; set; } = string.Empty;
    }

    public IActionResult OnGet() => RedirectToPage("./Login");

    public async Task<IActionResult> OnGetCallbackAsync(
        string? returnUrl = null,
        string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");
        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            ErrorMessage = "Google authentication was cancelled or could not be completed. Please try again.";
            return RedirectToPage("./Login", new { returnUrl });
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "Google authentication details could not be loaded. Please try again.";
            return RedirectToPage("./Login", new { returnUrl });
        }

        var result = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);
        if (result.Succeeded)
        {
            logger.LogInformation("User logged in with {Provider}.", info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;
        ReturnUrl = returnUrl;
        Input.Email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        Input.FullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "Google authentication details expired. Please start again.";
            return RedirectToPage("./Login", new { returnUrl });
        }

        ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;
        ReturnUrl = returnUrl;
        var providerEmail = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(providerEmail) ||
            !string.Equals(providerEmail, Input.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(Input.Email), "The email must match the verified Google account email.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Deliberately refuse implicit linking by email. Existing users must sign in
        // first before a future authenticated account-linking flow can be offered.
        if (await userManager.FindByEmailAsync(Input.Email.Trim()) is not null)
        {
            ModelState.AddModelError(string.Empty,
                "An account already uses this email. Sign in with your password; Google was not linked automatically.");
            return Page();
        }

        var user = new ApplicationUser
        {
            FullName = Input.FullName.Trim(),
            University = Input.University.Trim(),
            Course = Input.Course.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        await userStore.SetUserNameAsync(user, Input.Email.Trim(), CancellationToken.None);
        await GetEmailStore().SetEmailAsync(user, Input.Email.Trim(), CancellationToken.None);

        var result = await studentAccountService.CreateExternalAsync(user, info);
        if (result.Succeeded)
        {
            logger.LogInformation("A new student account was created with {Provider}.", info.LoginProvider);
            await signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore() =>
        userManager.SupportsUserEmail
            ? (IUserEmailStore<ApplicationUser>)userStore
            : throw new NotSupportedException("The configured user store must support email addresses.");
}
