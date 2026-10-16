using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCareer.Web.Controllers;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Tests;

public sealed class AuthenticationPolicyTests
{
    [Fact]
    public void PublicStudentLoginOffersPasswordAndGoogle()
    {
        var login = ReadWebFile("Areas", "Identity", "Pages", "Account", "Login.cshtml");
        Assert.Contains("Continue with Google", login);
        Assert.Contains("Input.Email", login);
        Assert.Contains("Input.Password", login);
        Assert.Contains("Forgot password?", login);
        Assert.Contains("asp-page=\"./Register\"", login);
    }

    [Fact]
    public void StudentRegistrationUsesIdentityAndOffersGoogle()
    {
        var model = ReadWebFile("Areas", "Identity", "Pages", "Account", "Register.cshtml.cs");
        var view = ReadWebFile("Areas", "Identity", "Pages", "Account", "Register.cshtml");
        Assert.Contains("studentAccountService.CreateAsync(user, Input.Password)", model);
        Assert.Contains("LoginModel.RequestGoogleAccountSelection", model);
        Assert.Contains("Input.FullName", view);
        Assert.Contains("Input.EducationLevel", view);
    }

    [Fact]
    public void DashboardAndProfileRequireStudentRole()
    {
        var dashboard = typeof(HomeController).GetMethod(nameof(HomeController.Dashboard))!;
        var dashboardAuth = Assert.Single(dashboard.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        var profileAuth = Assert.Single(typeof(ProfileController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(AppRoles.Student, dashboardAuth.Roles);
        Assert.Equal(AppRoles.Student, profileAuth.Roles);
    }

    [Fact]
    public void AdministratorAreaRequiresRoleAndHasNoRegistration()
    {
        var index = typeof(AdminController).GetMethod(nameof(AdminController.Index))!;
        var auth = Assert.Single(index.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        var controller = ReadWebFile("Controllers", "AdminController.cs");
        Assert.Equal(AppRoles.Administrator, auth.Roles);
        Assert.Contains("CheckPasswordSignInAsync", controller);
        Assert.Contains("IsInRoleAsync(user, AppRoles.Administrator)", controller);
        Assert.DoesNotContain("CreateAsync", controller);
    }

    [Fact]
    public void NewGoogleAccountHasNoPasswordAndCollisionRequiresAuthentication()
    {
        var service = ReadWebFile("Services", "StudentAccountService.cs");
        var external = ReadWebFile("Areas", "Identity", "Pages", "Account", "ExternalLogin.cshtml.cs");
        Assert.Contains("userManager.CreateAsync(user);", service);
        Assert.DoesNotContain("CreateAsync(user,", service[service.IndexOf("CreateExternalAsync", StringComparison.Ordinal)..]);
        Assert.Contains("FindByEmailAsync(Input.Email)", external);
        Assert.Contains("Sign in with your SmartCareer password", external);
        Assert.Contains("RedirectToPage(\"./Login\"", external);
    }

    [Fact]
    public void PublicCallsToActionLinkLoginAndRegistration()
    {
        var home = ReadWebFile("Views", "Home", "Index.cshtml");
        var nav = ReadWebFile("Views", "Shared", "_LoginPartial.cshtml");
        Assert.Contains("/Account/Register", home);
        Assert.Contains("/Account/Register", nav);
        Assert.Contains("/Account/Login", nav);
    }

    private static string ReadWebFile(params string[] parts)
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var root = Path.Combine(directory.FullName, "SmartCareer.Web");
            if (File.Exists(Path.Combine(root, "SmartCareer.Web.csproj"))) return File.ReadAllText(Path.Combine([root, .. parts]));
        }
        throw new DirectoryNotFoundException();
    }
}
