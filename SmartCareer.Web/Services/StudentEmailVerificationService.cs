using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using SmartCareer.Web.Models;
using SmartCareer.Web.Services.Email;

namespace SmartCareer.Web.Services;

public enum EmailVerificationDeliveryOutcome
{
    Sent,
    AlreadyConfirmed,
    DeliveryFailed
}

public sealed class StudentEmailVerificationService(
    UserManager<ApplicationUser> userManager,
    IEmailSender<ApplicationUser> emailSender)
{
    public async Task<EmailVerificationDeliveryOutcome> SendAsync(
        ApplicationUser user,
        Func<ApplicationUser, string, string> confirmationLinkFactory)
    {
        if (await userManager.IsEmailConfirmedAsync(user))
        {
            return EmailVerificationDeliveryOutcome.AlreadyConfirmed;
        }

        var email = await userManager.GetEmailAsync(user);
        if (string.IsNullOrWhiteSpace(email))
        {
            return EmailVerificationDeliveryOutcome.DeliveryFailed;
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmationLink = confirmationLinkFactory(user, encodedToken);

        try
        {
            await emailSender.SendConfirmationLinkAsync(user, email, confirmationLink);
            return EmailVerificationDeliveryOutcome.Sent;
        }
        catch (EmailDeliveryException)
        {
            return EmailVerificationDeliveryOutcome.DeliveryFailed;
        }
    }
}
