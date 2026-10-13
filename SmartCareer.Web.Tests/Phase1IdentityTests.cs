using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using SmartCareer.Web.Areas.Identity.Pages.Account;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services;

namespace SmartCareer.Web.Tests;

public sealed class Phase1IdentityTests
{
    [Fact]
    public void RegistrationInput_RejectsMissingAndOversizedStudentDetails()
    {
        var input = new RegisterModel.InputModel
        {
            FullName = string.Empty,
            University = new string('U', 151),
            Course = "C",
            Email = "not-an-email",
            Password = "short",
            ConfirmPassword = "different"
        };

        var results = Validate(input);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(input.FullName)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(input.University)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(input.Course)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(input.Email)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(input.Password)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(input.ConfirmPassword)));
    }

    [Fact]
    public async Task StudentAccountService_CreatesUserAndAssignsOnlyStudentRole()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IdentityDataInitializer>().InitializeAsync();
        var service = scope.ServiceProvider.GetRequiredService<StudentAccountService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = NewUser("new.student@example.test");

        var result = await service.CreateAsync(user, "Valid#Pass1");

        Assert.True(result.Succeeded);
        Assert.True(await userManager.IsInRoleAsync(user, AppRoles.Student));
        Assert.False(await userManager.IsInRoleAsync(user, AppRoles.Administrator));
    }

    [Fact]
    public async Task StudentAccountService_ExternalAccountGetsLoginAndOnlyStudentRole()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IdentityDataInitializer>().InitializeAsync();
        var service = scope.ServiceProvider.GetRequiredService<StudentAccountService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = NewUser("google.student@example.test");
        var login = new UserLoginInfo("Google", "provider-key", "Google");

        var result = await service.CreateExternalAsync(user, login);

        Assert.True(result.Succeeded);
        Assert.True(await userManager.IsInRoleAsync(user, AppRoles.Student));
        Assert.False(await userManager.IsInRoleAsync(user, AppRoles.Administrator));
        Assert.Contains(await userManager.GetLoginsAsync(user), saved =>
            saved.LoginProvider == "Google" && saved.ProviderKey == "provider-key");
    }

    [Fact]
    public void LoginUsesRequiredGenericCredentialError()
    {
        Assert.Equal("Invalid email or password. Please try again.", LoginModel.InvalidCredentialsMessage);
    }

    [Fact]
    public async Task Initializer_IsIdempotentAndDoesNotOverwriteExistingAdministratorRole()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var initializer = scope.ServiceProvider.GetRequiredService<IdentityDataInitializer>();
        Assert.True((await roleManager.CreateAsync(new IdentityRole(AppRoles.Administrator))).Succeeded);

        var administrator = NewUser("administrator@example.test");
        Assert.True((await userManager.CreateAsync(administrator, "Valid#Pass1")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(administrator, AppRoles.Administrator)).Succeeded);

        var rolelessStudent = NewUser("existing.student@example.test");
        Assert.True((await userManager.CreateAsync(rolelessStudent, "Valid#Pass1")).Succeeded);

        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        Assert.Equal([AppRoles.Administrator], await userManager.GetRolesAsync(administrator));
        Assert.Equal([AppRoles.Student], await userManager.GetRolesAsync(rolelessStudent));
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        var databaseRoot = new InMemoryDatabaseRoot();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"SmartCareerPhase1-{Guid.NewGuid()}", databaseRoot));
        services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IdentityDataInitializer>();
        services.AddScoped<StudentAccountService>();
        return services.BuildServiceProvider();
    }

    private static ApplicationUser NewUser(string email) => new()
    {
        UserName = email,
        Email = email,
        FullName = "Test Student",
        University = "Test University",
        Course = "Test Course"
    };

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
