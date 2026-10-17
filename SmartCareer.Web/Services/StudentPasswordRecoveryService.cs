using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services.Email;

namespace SmartCareer.Web.Services;

public enum PasswordResetRequestOutcome
{
    Neutral,
    Sent,
    DeliveryFailed
}

public sealed record StudentPasswordResetResult(
    bool Succeeded,
    bool InvalidOrIneligible,
    IReadOnlyCollection<IdentityError> Errors)
{
    public static StudentPasswordResetResult Success() => new(true, false, []);
    public static StudentPasswordResetResult Invalid() => new(false, true, []);
    public static StudentPasswordResetResult Validation(IEnumerable<IdentityError> errors) =>
        new(false, false, errors.ToArray());
}

public sealed class StudentPasswordRecoveryService(
    UserManager<ApplicationUser> userManager,
    IEmailSender<ApplicationUser> emailSender)
{
    public async Task<PasswordResetRequestOutcome> RequestResetAsync(
        string email,
        Func<ApplicationUser, string, string> resetLinkFactory)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (!await IsEligibleAsync(user))
        {
            return PasswordResetRequestOutcome.Neutral;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user!);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetLink = resetLinkFactory(user!, encodedToken);

        try
        {
            await emailSender.SendPasswordResetLinkAsync(user!, email, resetLink);
            return PasswordResetRequestOutcome.Sent;
        }
        catch (EmailDeliveryException)
        {
            return PasswordResetRequestOutcome.DeliveryFailed;
        }
    }

    public async Task<StudentPasswordResetResult> ResetAsync(
        string? userId,
        string? encodedToken,
        string newPassword)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(encodedToken))
        {
            return StudentPasswordResetResult.Invalid();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (!await IsEligibleAsync(user))
        {
            return StudentPasswordResetResult.Invalid();
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch (FormatException)
        {
            return StudentPasswordResetResult.Invalid();
        }

        var result = await userManager.ResetPasswordAsync(user!, token, newPassword);
        if (result.Succeeded)
        {
            return StudentPasswordResetResult.Success();
        }

        return result.Errors.Any(error => error.Code == "InvalidToken")
            ? StudentPasswordResetResult.Invalid()
            : StudentPasswordResetResult.Validation(result.Errors);
    }

    public async Task<bool> IsResetTokenValidAsync(string? userId, string? encodedToken)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(encodedToken))
        {
            return false;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (!await IsEligibleAsync(user) || !TryDecodeToken(encodedToken, out var token))
        {
            return false;
        }

        return await userManager.VerifyUserTokenAsync(
            user!,
            userManager.Options.Tokens.PasswordResetTokenProvider,
            UserManager<ApplicationUser>.ResetPasswordTokenPurpose,
            token);
    }

    private static bool TryDecodeToken(string encodedToken, out string token)
    {
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
            return true;
        }
        catch (FormatException)
        {
            token = string.Empty;
            return false;
        }
    }

    private async Task<bool> IsEligibleAsync(ApplicationUser? user) =>
        user is not null &&
        await userManager.IsInRoleAsync(user, AppRoles.Student) &&
        await userManager.HasPasswordAsync(user);
}
