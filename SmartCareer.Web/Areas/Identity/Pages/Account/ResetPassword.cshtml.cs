using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services;

namespace SmartCareer.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting(PasswordRecoveryPolicy.RateLimitPolicyName)]
public sealed class ResetPasswordModel(StudentPasswordRecoveryService passwordRecoveryService) : PageModel
{
    public const string InvalidLinkMessage = "This password-reset link is invalid, expired, or has already been used.";

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool InvalidLink { get; private set; }

    public sealed class InputModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(string? userId, string? code)
    {
        Input.UserId = userId ?? string.Empty;
        Input.Code = code ?? string.Empty;
        InvalidLink = !await passwordRecoveryService.IsResetTokenValidAsync(userId, code);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await passwordRecoveryService.ResetAsync(
            Input.UserId,
            Input.Code,
            Input.NewPassword);

        if (result.Succeeded)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        if (result.InvalidOrIneligible)
        {
            InvalidLink = true;
            ModelState.AddModelError(string.Empty, InvalidLinkMessage);
            ClearPasswords();
            return Page();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(nameof(Input.NewPassword), error.Description);
        }

        ClearPasswords();
        return Page();
    }

    private void ClearPasswords()
    {
        SensitiveModelStateCleaner.ClearAttemptedValue(ModelState, "Input.NewPassword");
        SensitiveModelStateCleaner.ClearAttemptedValue(ModelState, "Input.ConfirmPassword");
        Input.NewPassword = string.Empty;
        Input.ConfirmPassword = string.Empty;
    }
}
