using System.Net;
using SmartCareer.Web.Models;
using IIdentityUiEmailSender = Microsoft.AspNetCore.Identity.UI.Services.IEmailSender;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Services.Email;

public sealed class IdentityEmailSender(IEmailService emailService) :
    IIdentityUiEmailSender,
    Microsoft.AspNetCore.Identity.IEmailSender<ApplicationUser>
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage) =>
        emailService.SendHtmlAsync(email, subject, htmlMessage);

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        emailService.SendHtmlAsync(
            email,
            "Confirm your SmartCareer email",
            $"<p>Please confirm your email by <a href=\"{WebUtility.HtmlEncode(confirmationLink)}\">clicking here</a>.</p>");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        emailService.SendHtmlAsync(
            email,
            "Reset Your SmartCareer Password",
            $$"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;color:#17324d;line-height:1.6">
              <div style="background:#1769e0;color:#fff;padding:18px 24px;border-radius:10px 10px 0 0"><strong style="font-size:20px">SmartCareer</strong></div>
              <div style="border:1px solid #dbe7f5;border-top:0;padding:24px;border-radius:0 0 10px 10px">
                <h1 style="font-size:22px;margin-top:0">Reset your password</h1>
                <p>We received a request to reset the password for a SmartCareer student account.</p>
                <p><a href="{{WebUtility.HtmlEncode(resetLink)}}" style="display:inline-block;background:#1769e0;color:#fff;text-decoration:none;padding:12px 18px;border-radius:7px;font-weight:700">Reset Password</a></p>
                <p>This secure link expires in {{PasswordRecoveryPolicy.TokenLifespan.TotalHours:0}} hours and can be used only once.</p>
                <p>If you did not request this reset, you can ignore this email. Your current password will remain unchanged.</p>
              </div>
            </div>
            """);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        emailService.SendHtmlAsync(
            email,
            "Reset your SmartCareer password",
            $"<p>Your password reset code is <strong>{WebUtility.HtmlEncode(resetCode)}</strong>.</p>");
}
