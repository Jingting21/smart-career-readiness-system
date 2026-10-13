using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Services;

public sealed class IdentityDataInitializer(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    ILogger<IdentityDataInitializer> logger)
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IdentityDataInitializer>();
        await initializer.InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        foreach (var roleName in new[] { AppRoles.Student, AppRoles.Administrator })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                EnsureSucceeded(result, $"create the {roleName} role");
            }
        }

        var users = await userManager.Users.ToListAsync();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Count != 0)
            {
                continue;
            }

            var result = await userManager.AddToRoleAsync(user, AppRoles.Student);
            EnsureSucceeded(result, $"assign the Student role to user {user.Id}");
            logger.LogInformation("Assigned the Student role to existing roleless user {UserId}.", user.Id);
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"Unable to {operation}. {errors}");
    }
}
