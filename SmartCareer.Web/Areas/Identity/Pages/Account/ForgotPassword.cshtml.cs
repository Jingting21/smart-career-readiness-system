using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services;
using System.Diagnostics;

namespace SmartCareer.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting(PasswordRecoveryPolicy.RateLimitPolicyName)]
public class ForgotPasswordModel(
    StudentPasswordRecoveryService passwordRecoveryService,
    ILogger<ForgotPasswordModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public bool Submitted { get; private set; }
    public sealed class InputModel { [Required, EmailAddress] public string Email { get; set; } = ""; }
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var stopwatch = Stopwatch.StartNew();
        var outcome = await passwordRecoveryService.RequestResetAsync(
            Input.Email.Trim(),
            (user, encodedToken) => Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = encodedToken },
                protocol: Request.Scheme)
                ?? throw new InvalidOperationException("Unable to create the password reset URL."));

        if (outcome == PasswordResetRequestOutcome.DeliveryFailed)
        {
            logger.LogWarning("A student password-reset email could not be delivered.");
        }

        var remainingDelay = TimeSpan.FromMilliseconds(300) - stopwatch.Elapsed;
        if (remainingDelay > TimeSpan.Zero)
        {
            await Task.Delay(remainingDelay, HttpContext.RequestAborted);
        }

        Submitted = true;
        return Page();
    }
}
