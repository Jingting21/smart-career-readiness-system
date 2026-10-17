using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services;

namespace SmartCareer.Web.Areas.Identity.Pages.Account.Manage;

[Authorize(Roles = AppRoles.Student)]
[ValidateAntiForgeryToken]
[EnableRateLimiting(EmailVerificationPolicy.RateLimitPolicyName)]
public sealed class ResendEmailConfirmationModel(
    UserManager<ApplicationUser> userManager,
    StudentEmailVerificationService emailVerificationService,
    ILogger<ResendEmailConfirmationModel> logger) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public IActionResult OnGet() => RedirectToPage("./Email");

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var outcome = await emailVerificationService.SendAsync(
            user,
            (account, encodedToken) => Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = account.Id, code = encodedToken },
                protocol: Request.Scheme)
                ?? throw new InvalidOperationException("Unable to create the email confirmation URL."));

        StatusMessage = outcome switch
        {
            EmailVerificationDeliveryOutcome.Sent => "A new verification email has been sent.",
            EmailVerificationDeliveryOutcome.AlreadyConfirmed => "Your email address is already verified.",
            _ => "The verification email could not be sent. Please try again later."
        };

        if (outcome == EmailVerificationDeliveryOutcome.DeliveryFailed)
        {
            logger.LogWarning("A student verification email could not be delivered from Account Settings.");
        }

        return RedirectToPage("./Email");
    }
}
