using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;

namespace SmartCareer.AdminSetup;

public sealed record AdminSetupCredentials(string? Email, string? Password);

public sealed record AdminProvisioningResult(bool Succeeded, string Message)
{
    public static AdminProvisioningResult Success() => new(true, "Administrator account created successfully.");
    public static AdminProvisioningResult Failure(string message) => new(false, message);
}

public sealed class AdminProvisioningService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager)
{
    public async Task<AdminProvisioningResult> ProvisionAsync(AdminSetupCredentials credentials)
    {
        var email = credentials.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(credentials.Password))
        {
            return AdminProvisioningResult.Failure(
                "AdminSetup:Email and AdminSetup:Password must be configured in .NET User Secrets.");
        }

        if (!new EmailAddressAttribute().IsValid(email))
        {
            return AdminProvisioningResult.Failure("AdminSetup:Email is not a valid email address.");
        }

        if (!await roleManager.RoleExistsAsync(AppRoles.Administrator))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AppRoles.Administrator));
            if (!roleResult.Succeeded) return Failure("Administrator role creation failed", roleResult);
        }

        // An existing account is never promoted or modified by this setup command.
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return AdminProvisioningResult.Failure(
                "The requested email is already registered. Choose a different administrator email.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "SmartCareer Administrator",
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, credentials.Password);
        if (!createResult.Succeeded) return Failure("Administrator account creation failed", createResult);

        var roleAssignment = await userManager.AddToRoleAsync(user, AppRoles.Administrator);
        if (roleAssignment.Succeeded) return AdminProvisioningResult.Success();

        var rollback = await userManager.DeleteAsync(user);
        return rollback.Succeeded
            ? Failure("Administrator role assignment failed; the incomplete account was removed", roleAssignment)
            : AdminProvisioningResult.Failure(
                "Administrator role assignment failed and the incomplete account could not be removed. Review the database and Identity logs before retrying.");
    }

    private static AdminProvisioningResult Failure(string context, IdentityResult result) =>
        AdminProvisioningResult.Failure($"{context}: {string.Join(" ", result.Errors.Select(error => error.Description))}");
}
