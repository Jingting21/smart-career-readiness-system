using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using SmartCareer.AdminSetup;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Tests;

public sealed class AdminProvisioningTests
{
    [Fact]
    public async Task ValidCredentialsCreateConfirmedAdministratorWithHashedPassword()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminProvisioningService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var result = await service.ProvisionAsync(new("admin@example.test", "Valid#Pass1"));
        var user = await users.FindByEmailAsync("admin@example.test");

        Assert.True(result.Succeeded);
        Assert.NotNull(user);
        Assert.True(user.EmailConfirmed);
        Assert.True(await users.IsInRoleAsync(user, AppRoles.Administrator));
        Assert.False(await users.IsInRoleAsync(user, AppRoles.Student));
        Assert.True(await users.CheckPasswordAsync(user, "Valid#Pass1"));
        Assert.NotEqual("Valid#Pass1", user.PasswordHash);
    }

    [Fact]
    public async Task ExistingStudentCannotBePromotedOrModified()
    {
        await using var provider = CreateProvider(); using var scope = provider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await roles.CreateAsync(new IdentityRole(AppRoles.Student));
        var student = new ApplicationUser { UserName = "student@example.test", Email = "student@example.test", FullName = "Student" };
        Assert.True((await users.CreateAsync(student, "Student#Pass1")).Succeeded);
        Assert.True((await users.AddToRoleAsync(student, AppRoles.Student)).Succeeded);

        var result = await scope.ServiceProvider.GetRequiredService<AdminProvisioningService>()
            .ProvisionAsync(new("student@example.test", "Different#Pass1"));

        Assert.False(result.Succeeded);
        Assert.False(await users.IsInRoleAsync(student, AppRoles.Administrator));
        Assert.True(await users.CheckPasswordAsync(student, "Student#Pass1"));
    }

    [Fact]
    public async Task DuplicateAndRepeatedSetupCreateNoAdditionalAccount()
    {
        await using var provider = CreateProvider(); using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminProvisioningService>();
        Assert.True((await service.ProvisionAsync(new("admin@example.test", "Valid#Pass1"))).Succeeded);

        var repeated = await service.ProvisionAsync(new("admin@example.test", "Valid#Pass1"));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.False(repeated.Succeeded);
        Assert.Equal(1, await users.Users.CountAsync());
    }

    [Theory]
    [InlineData(null, "Valid#Pass1")]
    [InlineData("", "Valid#Pass1")]
    [InlineData("not-an-email", "Valid#Pass1")]
    [InlineData("admin@example.test", null)]
    [InlineData("admin@example.test", "weak")]
    public async Task MissingOrInvalidCredentialsAreRejected(string? email, string? password)
    {
        await using var provider = CreateProvider(); using var scope = provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<AdminProvisioningService>()
            .ProvisionAsync(new(email, password));
        Assert.False(result.Succeeded);
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().Users.ToListAsync());
    }

    [Fact]
    public void NormalWebStartupContainsNoProvisioningCall()
    {
        var root = FindRoot();
        var program = File.ReadAllText(Path.Combine(root, "SmartCareer.Web", "Program.cs"));
        Assert.DoesNotContain("AdminProvisioningService", program);
        Assert.DoesNotContain("AdminSetup:Password", program);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"AdminSetup-{Guid.NewGuid()}", new InMemoryDatabaseRoot()));
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<AdminProvisioningService>();
        return services.BuildServiceProvider();
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, "SmartCareer.Web"))) return directory.FullName;
        throw new DirectoryNotFoundException();
    }
}
