using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using SmartCareer.Web.Areas.Identity.Pages.Account.Manage;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services;
using SmartCareer.Web.Services.Email;
using System.Threading.RateLimiting;

namespace SmartCareer.Web.Tests;

public sealed class EmailVerificationTests
{
    [Fact]
    public async Task RegistrationVerificationTokenCanConfirmEmail()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var user = await CreateStudentAsync(scope, "confirm@example.test", confirmed: false);
        var service = scope.ServiceProvider.GetRequiredService<StudentEmailVerificationService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var sender = scope.ServiceProvider.GetRequiredService<RecordingVerificationSender>();

        var outcome = await service.SendAsync(
            user,
            (account, code) => $"https://localhost/Identity/Account/ConfirmEmail?userId={account.Id}&code={code}");

        Assert.Equal(EmailVerificationDeliveryOutcome.Sent, outcome);
        Assert.Contains("/Identity/Account/ConfirmEmail", sender.ConfirmationLink);
        var token = DecodeToken(QueryValue(sender.ConfirmationLink!, "code"));
        Assert.True((await users.ConfirmEmailAsync(user, token)).Succeeded);
        Assert.True(await users.IsEmailConfirmedAsync(user));
    }

    [Fact]
    public async Task InvalidAndExpiredConfirmationTokensFail()
    {
        await using var provider = CreateProvider(TimeSpan.Zero);
        using var scope = provider.CreateScope();
        var user = await CreateStudentAsync(scope, "expired-confirm@example.test", confirmed: false);
        var service = scope.ServiceProvider.GetRequiredService<StudentEmailVerificationService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var sender = scope.ServiceProvider.GetRequiredService<RecordingVerificationSender>();
        await service.SendAsync(user, (_, code) => $"https://localhost/confirm?code={code}");

        Assert.False((await users.ConfirmEmailAsync(user, "invalid-token")).Succeeded);
        Assert.False((await users.ConfirmEmailAsync(
            user,
            DecodeToken(QueryValue(sender.ConfirmationLink!, "code")))).Succeeded);
        Assert.False(await users.IsEmailConfirmedAsync(user));
    }

    [Fact]
    public async Task DeliveryFailureIsReportedWithoutChangingAccountOrConfirmationState()
    {
        var sender = new RecordingVerificationSender { FailDelivery = true };
        await using var provider = CreateProvider(sender: sender);
        using var scope = provider.CreateScope();
        var user = await CreateStudentAsync(scope, "delivery-failure@example.test", confirmed: false);
        var service = scope.ServiceProvider.GetRequiredService<StudentEmailVerificationService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var outcome = await service.SendAsync(user, (_, code) => $"https://localhost/confirm?code={code}");

        Assert.Equal(EmailVerificationDeliveryOutcome.DeliveryFailed, outcome);
        Assert.NotNull(await users.FindByIdAsync(user.Id));
        Assert.False(await users.IsEmailConfirmedAsync(user));
    }

    [Fact]
    public async Task ConfirmedStudentCannotReceiveAnotherVerificationEmail()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var user = await CreateStudentAsync(scope, "confirmed@example.test", confirmed: true);
        var service = scope.ServiceProvider.GetRequiredService<StudentEmailVerificationService>();
        var sender = scope.ServiceProvider.GetRequiredService<RecordingVerificationSender>();

        var outcome = await service.SendAsync(user, (_, code) => $"https://localhost/confirm?code={code}");

        Assert.Equal(EmailVerificationDeliveryOutcome.AlreadyConfirmed, outcome);
        Assert.Equal(0, sender.SendCount);
    }

    [Fact]
    public void RegistrationAndResendKeepImmediateLoginAndSecurityBoundaries()
    {
        var registration = ReadWebFile("Areas", "Identity", "Pages", "Account", "Register.cshtml.cs");
        var resendView = ReadWebFile("Areas", "Identity", "Pages", "Account", "Manage", "Email.cshtml");
        var admin = ReadWebFile("Controllers", "AdminController.cs");
        var external = ReadWebFile("Areas", "Identity", "Pages", "Account", "ExternalLogin.cshtml.cs");

        Assert.Contains("emailVerificationService.SendAsync", registration);
        Assert.Contains("signInManager.SignInAsync(user, isPersistent: false)", registration);
        Assert.Contains("EmailVerificationDeliveryOutcome.DeliveryFailed", registration);
        Assert.Contains("method=\"post\" asp-page=\"ResendEmailConfirmation\"", resendView);
        Assert.Contains("FindByEmailAsync(Input.Email)", external);
        Assert.Contains("IsInRoleAsync(user, AppRoles.Administrator)", admin);
    }

    [Fact]
    public void ResendEndpointRequiresStudentAntiforgeryAndRateLimiting()
    {
        var type = typeof(ResendEmailConfirmationModel);
        var authorize = Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        var limiter = Assert.Single(type.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true).Cast<EnableRateLimitingAttribute>());

        Assert.Equal(AppRoles.Student, authorize.Roles);
        Assert.Equal(EmailVerificationPolicy.RateLimitPolicyName, limiter.PolicyName);
        Assert.Single(type.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true));

        using var rateLimiter = new FixedWindowRateLimiter(EmailVerificationPolicy.CreateRateLimiterOptions());
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var lease = rateLimiter.AttemptAcquire();
            Assert.True(lease.IsAcquired);
        }
        using var rejected = rateLimiter.AttemptAcquire();
        Assert.False(rejected.IsAcquired);
    }

    private static ServiceProvider CreateProvider(
        TimeSpan? tokenLifespan = null,
        RecordingVerificationSender? sender = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"EmailVerification-{Guid.NewGuid()}", new InMemoryDatabaseRoot()));
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
            options.TokenLifespan = tokenLifespan ?? TimeSpan.FromHours(2));
        services.AddScoped<StudentEmailVerificationService>();
        services.AddSingleton(sender ?? new RecordingVerificationSender());
        services.AddSingleton<IEmailSender<ApplicationUser>>(provider =>
            provider.GetRequiredService<RecordingVerificationSender>());
        return services.BuildServiceProvider();
    }

    private static async Task<ApplicationUser> CreateStudentAsync(
        IServiceScope scope,
        string email,
        bool confirmed)
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True((await roles.CreateAsync(new IdentityRole(AppRoles.Student))).Succeeded);
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = confirmed,
            FullName = "Verification Test User",
            University = "Test University",
            Course = "Test Course"
        };
        Assert.True((await users.CreateAsync(user, "Valid#Pass1")).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, AppRoles.Student)).Succeeded);
        return user;
    }

    private static string QueryValue(string url, string name) =>
        Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(url).Query)[name].ToString();

    private static string DecodeToken(string encodedToken) =>
        Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));

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

    private sealed class RecordingVerificationSender : IEmailSender<ApplicationUser>
    {
        public int SendCount { get; private set; }
        public string? ConfirmationLink { get; private set; }
        public bool FailDelivery { get; init; }

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            SendCount++;
            ConfirmationLink = confirmationLink;
            return FailDelivery
                ? Task.FromException(new EmailDeliveryException(
                    "The email could not be delivered.",
                    new InvalidOperationException("Simulated failure")))
                : Task.CompletedTask;
        }

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
            throw new NotSupportedException();

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
            throw new NotSupportedException();
    }
}
