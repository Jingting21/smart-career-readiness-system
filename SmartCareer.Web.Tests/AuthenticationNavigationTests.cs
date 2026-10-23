using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCareer.Web.Controllers;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Tests;

public sealed class AuthenticationNavigationTests
{
    [Theory]
    [InlineData(null, StudentRedirects.DashboardPath)]
    [InlineData("/", StudentRedirects.DashboardPath)]
    [InlineData("/Home/Index", StudentRedirects.DashboardPath)]
    [InlineData("/Admin", StudentRedirects.DashboardPath)]
    [InlineData("/Identity/Account/Login", StudentRedirects.DashboardPath)]
    [InlineData("https://attacker.example/", StudentRedirects.DashboardPath)]
    [InlineData("/Home/Dashboard", "/Home/Dashboard")]
    [InlineData("/Profile", "/Profile")]
    [InlineData("/Home/ComingSoon?feature=Skills", "/Home/ComingSoon?feature=Skills")]
    [InlineData("/Identity/Account/Manage/Index", "/Identity/Account/Manage/Index")]
    public void StudentRedirectsAllowOnlyLocalStudentDestinations(string? returnUrl, string expected)
    {
        static bool IsLocal(string value) => value.StartsWith('/') && !value.StartsWith("//");

        Assert.Equal(expected, StudentRedirects.Resolve(returnUrl, IsLocal));
    }

    [Theory]
    [InlineData("/Admin", true)]
    [InlineData("/Admin/Reports?period=current", true)]
    [InlineData("/Admin/Login", false)]
    [InlineData("/Administrator", false)]
    [InlineData("/Home/Dashboard", false)]
    [InlineData("https://attacker.example/Admin", false)]
    public void AdministratorRedirectsAllowOnlyLocalAdminDestinations(string returnUrl, bool expected)
    {
        static bool IsLocal(string value) => value.StartsWith('/') && !value.StartsWith("//");

        Assert.Equal(expected, AdministratorRedirects.IsSafe(returnUrl, IsLocal));
    }

    [Fact]
    public void PublicHomeRemainsAvailableToAnonymousVisitors()
    {
        var result = CreateHomeController().Index();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void AuthenticatedStudentHomeRedirectsToDashboard()
    {
        var result = Assert.IsType<RedirectToActionResult>(
            CreateHomeController(AppRoles.Student).Index());

        Assert.Equal(nameof(HomeController.Dashboard), result.ActionName);
        Assert.Null(result.ControllerName);
    }

    [Fact]
    public void AuthenticatedAdministratorHomeRedirectsToAdmin()
    {
        var result = Assert.IsType<RedirectToActionResult>(
            CreateHomeController(AppRoles.Administrator).Index());

        Assert.Equal("Index", result.ActionName);
        Assert.Equal("Admin", result.ControllerName);
    }

    [Fact]
    public void RegistrationAndLoginSignInStudentsWithoutEmailConfirmationGate()
    {
        var registration = ReadWebFile("Areas", "Identity", "Pages", "Account", "Register.cshtml.cs");
        var login = ReadWebFile("Areas", "Identity", "Pages", "Account", "Login.cshtml.cs");
        var program = ReadWebFile("Program.cs");

        Assert.Contains("signInManager.SignInAsync(user, isPersistent: false)", registration);
        Assert.Contains("StudentRedirects.Resolve", registration);
        Assert.Contains("emailVerificationService.SendAsync", registration);
        Assert.True(
            registration.IndexOf("emailVerificationService.SendAsync", StringComparison.Ordinal) <
            registration.IndexOf("signInManager.SignInAsync(user, isPersistent: false)", StringComparison.Ordinal));
        Assert.DoesNotContain("CheckEmail", registration);
        Assert.Contains("options.SignIn.RequireConfirmedAccount = false", program);
        Assert.Contains("StudentRedirects.Resolve", login);
        Assert.DoesNotContain("IsEmailConfirmedAsync", login);
        Assert.Contains("IsInRoleAsync(user, AppRoles.Student)", login);
        Assert.Contains("lockoutOnFailure: true", login);
        Assert.Contains("Input.RememberMe", login);
    }

    [Fact]
    public void GoogleAdminAuthorizationAndSmtpRegistrationRemainIntact()
    {
        var external = ReadWebFile("Areas", "Identity", "Pages", "Account", "ExternalLogin.cshtml.cs");
        var admin = ReadWebFile("Controllers", "AdminController.cs");
        var program = ReadWebFile("Program.cs");

        Assert.Contains("StudentRedirects.Resolve", external);
        Assert.Contains("FindByEmailAsync(Input.Email)", external);
        Assert.Contains("CreateExternalAsync(user, info)", external);
        Assert.Contains("IsInRoleAsync(user, AppRoles.Administrator)", admin);
        Assert.Contains("AdministratorRedirects.IsSafe", admin);
        Assert.Contains("IEmailService, SmtpEmailService", program);
        Assert.Contains("IEmailSender<ApplicationUser>, IdentityEmailSender", program);
    }

    [Fact]
    public void RoleAuthorizationSeparatesStudentAndAdministratorPages()
    {
        var dashboard = typeof(HomeController).GetMethod(nameof(HomeController.Dashboard))!;
        var dashboardAuthorization = Assert.Single(
            dashboard.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
                .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>());
        var adminIndex = typeof(AdminController).GetMethod(nameof(AdminController.Index))!;
        var adminAuthorization = Assert.Single(
            adminIndex.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
                .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>());

        Assert.Equal(AppRoles.Student, dashboardAuthorization.Roles);
        Assert.Equal(AppRoles.Administrator, adminAuthorization.Roles);
    }

    private static HomeController CreateHomeController(string? role = null)
    {
        ClaimsIdentity identity = role is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "test-user"), new Claim(ClaimTypes.Role, role)],
                "TestAuthentication");
        return new HomeController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

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
}
