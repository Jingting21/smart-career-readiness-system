using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services;
using SmartCareer.Web.Services.Email;
using System.Threading.RateLimiting;

namespace SmartCareer.Web.Tests;

public sealed class PasswordRecoveryTests
{
    [Fact]
    public async Task EligibleConfirmedStudentReceivesHttpsResetLink()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var user = await CreatePasswordStudentAsync(scope, "eligible@example.test", confirmed: true);
        var service = scope.ServiceProvider.GetRequiredService<StudentPasswordRecoveryService>();
        var sender = scope.ServiceProvider.GetRequiredService<RecordingPasswordResetSender>();

        var outcome = await service.RequestResetAsync(
            user.Email!,
            (account, code) => $"https://localhost:7204/Identity/Account/ResetPassword?userId={account.Id}&code={code}");

        Assert.Equal(PasswordResetRequestOutcome.Sent, outcome);
        Assert.Equal(1, sender.SendCount);
        Assert.StartsWith("https://localhost:7204/", sender.ResetLink);
        Assert.DoesNotContain(' ', QueryValue(sender.ResetLink!, "code"));
    }

    [Fact]
    public async Task UnverifiedPasswordStudentIsEligibleButUnknownAdministratorAndGoogleOnlyAreNot()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        await InitializeRolesAsync(scope);
        var unverified = await CreatePasswordStudentAsync(scope, "unverified@example.test", confirmed: false);
        await CreateAdministratorAsync(scope, "administrator@example.test");
        await CreateGoogleOnlyStudentAsync(scope, "google-only@example.test");
        var service = scope.ServiceProvider.GetRequiredService<StudentPasswordRecoveryService>();
        var sender = scope.ServiceProvider.GetRequiredService<RecordingPasswordResetSender>();

        foreach (var email in new[]
                 {
                     "unknown@example.test",
                     "administrator@example.test",
                     "google-only@example.test"
                 })
        {
            var outcome = await service.RequestResetAsync(
                email,
                (_, code) => $"https://localhost/reset?code={code}");
            Assert.Equal(PasswordResetRequestOutcome.Neutral, outcome);
        }

        var unverifiedOutcome = await service.RequestResetAsync(
            unverified.Email!,
            (account, code) => $"https://localhost/reset?userId={account.Id}&code={code}");

        Assert.Equal(PasswordResetRequestOutcome.Sent, unverifiedOutcome);
        Assert.Equal(1, sender.SendCount);
    }

    [Fact]
    public async Task ResettingUnverifiedStudentDoesNotConfirmEmail()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var user = await CreatePasswordStudentAsync(scope, "still-unverified@example.test", confirmed: false);
        var service = scope.ServiceProvider.GetRequiredService<StudentPasswordRecoveryService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var sender = scope.ServiceProvider.GetRequiredService<RecordingPasswordResetSender>();
        await service.RequestResetAsync(
            user.Email!,
            (account, code) => $"https://localhost/reset?userId={account.Id}&code={code}");

        var result = await service.ResetAsync(
            user.Id,
            QueryValue(sender.ResetLink!, "code"),
            "NewValid#Pass2");

        Assert.True(result.Succeeded);
        Assert.False(await users.IsEmailConfirmedAsync(user));
    }

    [Fact]
    public async Task ValidTokenChangesPasswordRejectsOldPasswordAndCannotBeReused()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var user = await CreatePasswordStudentAsync(scope, "reset@example.test", confirmed: true);
        var service = scope.ServiceProvider.GetRequiredService<StudentPasswordRecoveryService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var sender = scope.ServiceProvider.GetRequiredService<RecordingPasswordResetSender>();
        await service.RequestResetAsync(
            user.Email!,
            (account, code) => $"https://localhost/reset?userId={account.Id}&code={code}");
        var code = QueryValue(sender.ResetLink!, "code");

        var result = await service.ResetAsync(user.Id, code, "NewValid#Pass2");

        Assert.True(result.Succeeded);
        Assert.False(await users.CheckPasswordAsync(user, "Valid#Pass1"));
        Assert.True(await users.CheckPasswordAsync(user, "NewValid#Pass2"));
        Assert.True((await service.ResetAsync(user.Id, code, "Another#Pass3")).InvalidOrIneligible);
    }

    [Fact]
    public async Task TamperedAndMalformedTokensAreRejectedWithoutChangingPassword()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var user = await CreatePasswordStudentAsync(scope, "tampered@example.test", confirmed: true);
        var service = scope.ServiceProvider.GetRequiredService<StudentPasswordRecoveryService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.True((await service.ResetAsync(user.Id, "not-valid-base64!", "NewValid#Pass2")).InvalidOrIneligible);
        Assert.True((await service.ResetAsync(user.Id, WebEncoders.Base64UrlEncode("tampered"u8.ToArray()), "NewValid#Pass2")).InvalidOrIneligible);
        Assert.True(await users.CheckPasswordAsync(user, "Valid#Pass1"));
    }

    [Fact]
    public async Task ExpiredTokenIsRejected()
    {
        await using var provider = CreateProvider(TimeSpan.Zero);
        using var scope = provider.CreateScope();
        var user = await CreatePasswordStudentAsync(scope, "expired@example.test", confirmed: true);
        var service = scope.ServiceProvider.GetRequiredService<StudentPasswordRecoveryService>();
        var sender = scope.ServiceProvider.GetRequiredService<RecordingPasswordResetSender>();
        await service.RequestResetAsync(
            user.Email!,
            (account, code) => $"https://localhost/reset?userId={account.Id}&code={code}");

        var result = await service.ResetAsync(user.Id, QueryValue(sender.ResetLink!, "code"), "NewValid#Pass2");

        Assert.True(result.InvalidOrIneligible);
    }

    [Fact]
    public async Task PasswordPolicyErrorsDoNotConsumeValidToken()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var user = await CreatePasswordStudentAsync(scope, "validation@example.test", confirmed: true);
        var service = scope.ServiceProvider.GetRequiredService<StudentPasswordRecoveryService>();
        var sender = scope.ServiceProvider.GetRequiredService<RecordingPasswordResetSender>();
        await service.RequestResetAsync(
            user.Email!,
            (account, code) => $"https://localhost/reset?userId={account.Id}&code={code}");
        var code = QueryValue(sender.ResetLink!, "code");

        var weakResult = await service.ResetAsync(user.Id, code, "weak");

        Assert.False(weakResult.Succeeded);
        Assert.False(weakResult.InvalidOrIneligible);
        Assert.NotEmpty(weakResult.Errors);
        Assert.True((await service.ResetAsync(user.Id, code, "NewValid#Pass2")).Succeeded);
    }

    [Fact]
    public async Task SmtpFailureReturnsDeliveryFailedWithoutThrowingSensitiveDetails()
    {
        var sender = new RecordingPasswordResetSender { FailDelivery = true };
        await using var provider = CreateProvider(sender: sender);
        using var scope = provider.CreateScope();
        var user = await CreatePasswordStudentAsync(scope, "smtp-failure@example.test", confirmed: true);
        var service = scope.ServiceProvider.GetRequiredService<StudentPasswordRecoveryService>();

        var outcome = await service.RequestResetAsync(
            user.Email!,
            (account, code) => $"https://localhost/reset?userId={account.Id}&code={code}");

        Assert.Equal(PasswordResetRequestOutcome.DeliveryFailed, outcome);
    }

    [Fact]
    public void PagesRateLimitRecoveryAndPreserveAuthenticationBoundaries()
    {
        var forgot = ReadWebFile("Areas", "Identity", "Pages", "Account", "ForgotPassword.cshtml.cs");
        var reset = ReadWebFile("Areas", "Identity", "Pages", "Account", "ResetPassword.cshtml.cs");
        var program = ReadWebFile("Program.cs");
        var external = ReadWebFile("Areas", "Identity", "Pages", "Account", "ExternalLogin.cshtml.cs");
        var admin = ReadWebFile("Controllers", "AdminController.cs");

        Assert.Contains("EnableRateLimiting(PasswordRecoveryPolicy.RateLimitPolicyName)", forgot);
        Assert.Contains("EnableRateLimiting(PasswordRecoveryPolicy.RateLimitPolicyName)", reset);
        Assert.Contains("PasswordRecoveryPolicy.TokenLifespan", program);
        Assert.Contains("StudentRedirects.Resolve", external);
        Assert.Contains("IsInRoleAsync(user, AppRoles.Administrator)", admin);
        Assert.Contains("IsInRoleAsync(user, AppRoles.Student)", ReadWebFile("Areas", "Identity", "Pages", "Account", "Login.cshtml.cs"));
    }

    [Fact]
    public void RecoveryRateLimiterRejectsRequestsBeyondConfiguredWindowLimit()
    {
        using var limiter = new FixedWindowRateLimiter(PasswordRecoveryPolicy.CreateRateLimiterOptions());

        for (var attempt = 0; attempt < 6; attempt++)
        {
            using var lease = limiter.AttemptAcquire();
            Assert.True(lease.IsAcquired);
        }

        using var rejected = limiter.AttemptAcquire();
        Assert.False(rejected.IsAcquired);
    }

    private static ServiceProvider CreateProvider(
        TimeSpan? tokenLifespan = null,
        RecordingPasswordResetSender? sender = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(
                $"PasswordRecovery-{Guid.NewGuid()}",
                new InMemoryDatabaseRoot()));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = tokenLifespan ?? PasswordRecoveryPolicy.TokenLifespan);
        services.AddScoped<IdentityDataInitializer>();
        services.AddScoped<StudentPasswordRecoveryService>();
        services.AddSingleton(sender ?? new RecordingPasswordResetSender());
        services.AddSingleton<IEmailSender<ApplicationUser>>(provider =>
            provider.GetRequiredService<RecordingPasswordResetSender>());
        return services.BuildServiceProvider();
    }

    private static async Task InitializeRolesAsync(IServiceScope scope) =>
        await scope.ServiceProvider.GetRequiredService<IdentityDataInitializer>().InitializeAsync();

    private static async Task<ApplicationUser> CreatePasswordStudentAsync(
        IServiceScope scope,
        string email,
        bool confirmed)
    {
        await InitializeRolesAsync(scope);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = NewUser(email, confirmed);
        Assert.True((await users.CreateAsync(user, "Valid#Pass1")).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, AppRoles.Student)).Succeeded);
        return user;
    }

    private static async Task CreateAdministratorAsync(IServiceScope scope, string email)
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = NewUser(email, confirmed: true);
        Assert.True((await users.CreateAsync(user, "Valid#Pass1")).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, AppRoles.Administrator)).Succeeded);
    }

    private static async Task CreateGoogleOnlyStudentAsync(IServiceScope scope, string email)
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = NewUser(email, confirmed: true);
        Assert.True((await users.CreateAsync(user)).Succeeded);
        Assert.True((await users.AddLoginAsync(user, new UserLoginInfo("Google", "google-key", "Google"))).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, AppRoles.Student)).Succeeded);
    }

    private static ApplicationUser NewUser(string email, bool confirmed) => new()
    {
        UserName = email,
        Email = email,
        EmailConfirmed = confirmed,
        FullName = "Recovery Test User",
        University = "Test University",
        Course = "Test Course"
    };

    private static string QueryValue(string url, string name) =>
        QueryHelpers.ParseQuery(new Uri(url).Query)[name].ToString();

    private static string ReadWebFile(params string[] parts)
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var root = Path.Combine(directory.FullName, "SmartCareer.Web");
            var path = Path.Combine([root, .. parts]);
            if (File.Exists(path)) return File.ReadAllText(path);
        }

        throw new FileNotFoundException("Could not locate the requested SmartCareer.Web file.");
    }

    private sealed class RecordingPasswordResetSender : IEmailSender<ApplicationUser>
    {
        public int SendCount { get; private set; }
        public string? ResetLink { get; private set; }
        public bool FailDelivery { get; init; }

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            SendCount++;
            ResetLink = resetLink;
            return FailDelivery
                ? Task.FromException(new EmailDeliveryException(
                    "The email could not be delivered.",
                    new InvalidOperationException("Simulated failure")))
                : Task.CompletedTask;
        }

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
            throw new NotSupportedException();

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
            throw new NotSupportedException();
    }
}
