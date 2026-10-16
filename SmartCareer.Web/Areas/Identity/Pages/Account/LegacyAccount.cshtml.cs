using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LegacyAccountModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<LegacyAccountModel> logger) : PageModel
{
    public const string InvalidCredentialsMessage = "The account details could not be verified.";
    [BindProperty] public InputModel Input { get; set; } = new();
    public string? ReturnUrl { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public sealed class InputModel
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return Page();
        var user = await userManager.FindByEmailAsync(Input.Email.Trim());
        var result = user is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await signInManager.CheckPasswordSignInAsync(user, Input.Password, lockoutOnFailure: true);
        if (result.IsLockedOut) { ModelState.AddModelError(string.Empty, InvalidCredentialsMessage); return Page(); }
        if (user is null || !result.Succeeded || !await userManager.IsInRoleAsync(user, AppRoles.Student))
        {
            ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
            return Page();
        }
        await signInManager.SignInAsync(user, isPersistent: false);
        logger.LogInformation("A legacy student account was verified for Google migration.");
        return RedirectToPage("./Manage/ExternalLogins", new { migration = true });
    }
}
