using Microsoft.AspNetCore.Identity;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Services;

public sealed class StudentAccountService(UserManager<ApplicationUser> userManager)
{
    public async Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
    {
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return createResult;
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Student);
        if (roleResult.Succeeded)
        {
            return IdentityResult.Success;
        }

        // Do not leave a newly registered account without its required role.
        var deleteResult = await userManager.DeleteAsync(user);
        if (deleteResult.Succeeded)
        {
            return roleResult;
        }

        return IdentityResult.Failed(
            roleResult.Errors.Concat(deleteResult.Errors).ToArray());
    }

    public async Task<IdentityResult> CreateExternalAsync(
        ApplicationUser user,
        UserLoginInfo login)
    {
        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            return createResult;
        }

        var loginResult = await userManager.AddLoginAsync(user, login);
        if (!loginResult.Succeeded)
        {
            return await RollBackAsync(user, loginResult);
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Student);
        if (!roleResult.Succeeded)
        {
            return await RollBackAsync(user, roleResult);
        }

        return IdentityResult.Success;
    }

    private async Task<IdentityResult> RollBackAsync(ApplicationUser user, IdentityResult failure)
    {
        var deleteResult = await userManager.DeleteAsync(user);
        return deleteResult.Succeeded
            ? failure
            : IdentityResult.Failed(failure.Errors.Concat(deleteResult.Errors).ToArray());
    }
}
