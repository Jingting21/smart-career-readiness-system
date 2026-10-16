using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using SmartCareer.Web.Areas.Identity.Pages.Account;
using SmartCareer.Web.Areas.Identity.Pages.Account.Manage;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Tests;

public sealed class AccountSettingsTests
{
    [Fact]
    public void GoogleChallengesRequestAccountSelectionWithoutAffectingOtherProviders()
    {
        var method = typeof(LoginModel).GetMethod("RequestGoogleAccountSelection", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var google = new AuthenticationProperties();
        var other = new AuthenticationProperties();

        method.Invoke(null, ["Google", google]);
        method.Invoke(null, ["OtherProvider", other]);

        Assert.Equal("select_account", google.Parameters["prompt"]);
        Assert.False(other.Parameters.ContainsKey("prompt"));
    }

    [Fact]
    public void LoginRegistrationAndAccountLinkingApplyGoogleAccountSelection()
    {
        var login = ReadWebFile("Areas", "Identity", "Pages", "Account", "Login.cshtml.cs");
        var register = ReadWebFile("Areas", "Identity", "Pages", "Account", "Register.cshtml.cs");
        var linking = ReadWebFile("Areas", "Identity", "Pages", "Account", "Manage", "ExternalLogins.cshtml.cs");
        Assert.Contains("RequestGoogleAccountSelection(provider, properties)", login);
        Assert.Contains("LoginModel.RequestGoogleAccountSelection(provider, properties)", register);
        Assert.Contains("LoginModel.RequestGoogleAccountSelection(provider, properties)", linking);
    }

    [Fact]
    public void PasswordFormsUseSharedNonSubmittingControl()
    {
        var legacy = ReadWebFile("Areas", "Identity", "Pages", "Account", "LegacyAccount.cshtml");
        var admin = ReadWebFile("Views", "Admin", "Login.cshtml");
        var login = ReadWebFile("Areas", "Identity", "Pages", "Account", "Login.cshtml");
        var register = ReadWebFile("Areas", "Identity", "Pages", "Account", "Register.cshtml");
        var change = ReadWebFile("Areas", "Identity", "Pages", "Account", "Manage", "ChangePassword.cshtml");
        var set = ReadWebFile("Areas", "Identity", "Pages", "Account", "Manage", "SetPassword.cshtml");
        var script = ReadWebFile("wwwroot", "js", "password-toggle.js");
        Assert.Contains("type=\"button\" class=\"password-toggle\"", legacy);
        Assert.Contains("type=\"button\" class=\"password-toggle\"", admin);
        Assert.Contains("data-toggle-password=\"Input_Password\"", login);
        Assert.Contains("data-toggle-password=\"Input_ConfirmPassword\"", register);
        Assert.Contains("data-toggle-password=\"Input_OldPassword\"", change);
        Assert.Contains("data-toggle-password=\"Input_NewPassword\"", set);
        Assert.Contains("~/js/password-toggle.js", legacy);
        Assert.Contains("document.getElementById(button.dataset.togglePassword)", script);
        Assert.Contains("aria-pressed", script);
    }

    [Theory]
    [InlineData(typeof(IndexModel))]
    [InlineData(typeof(EmailModel))]
    [InlineData(typeof(ExternalLoginsModel))]
    [InlineData(typeof(PersonalDataModel))]
    [InlineData(typeof(DeletePersonalDataModel))]
    public void SettingsPagesRequireAuthenticatedStudent(Type pageModel)
    {
        var authorize = Assert.Single(pageModel.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(AppRoles.Student, authorize.Roles);
    }

    [Fact]
    public void SettingsOverviewHasDistinctWorkingDestinationsAndNoProfileForm()
    {
        var view = ReadWebFile("Areas", "Identity", "Pages", "Account", "Manage", "Index.cshtml");
        Assert.Contains("Account Settings", view);
        Assert.Contains("Manage your account, security and privacy.", view);
        Assert.Contains("asp-page=\"Email\"", view);
        Assert.Contains("asp-page=\"ExternalLogins\"", view);
        Assert.Contains("asp-page=\"PersonalData\"", view);
        Assert.Contains("ChangePassword", view);
        Assert.Contains("SetPassword", view);
        Assert.Contains("TwoFactorAuthentication", view);
        Assert.DoesNotContain("University", view);
        Assert.DoesNotContain("EducationLevel", view);
    }

    [Fact]
    public void ProfileRetainsStudentFieldsAndReadOnlyEmail()
    {
        var view = ReadWebFile("Views", "Profile", "Index.cshtml");
        Assert.Contains("asp-for=\"FullName\"", view);
        Assert.Contains("asp-for=\"University\"", view);
        Assert.Contains("asp-for=\"Course\"", view);
        Assert.Contains("asp-for=\"EducationLevel\"", view);
        Assert.Contains("asp-for=\"Email\" class=\"form-control readonly-field\" readonly", view);
        Assert.DoesNotContain("PhoneNumber", view);
    }

    [Fact]
    public void ConnectedAccountsProtectLastUsableLoginAndDoNotLinkByEmail()
    {
        var model = ReadWebFile("Areas", "Identity", "Pages", "Account", "Manage", "ExternalLogins.cshtml.cs");
        Assert.Contains("!await userManager.HasPasswordAsync(user) && logins.Count <= 1", model);
        Assert.Contains("GetExternalLoginInfoAsync(user.Id)", model);
        Assert.Contains("AddLoginAsync(user, info)", model);
        Assert.DoesNotContain("FindByEmailAsync", model);
    }

    [Fact]
    public void SensitiveActionsUsePostAndExplicitDeletionConfirmation()
    {
        var personalData = ReadWebFile("Areas", "Identity", "Pages", "Account", "Manage", "PersonalData.cshtml");
        var deletion = ReadWebFile("Areas", "Identity", "Pages", "Account", "Manage", "DeletePersonalData.cshtml");
        Assert.Contains("method=\"post\" asp-page=\"DownloadPersonalData\"", personalData);
        Assert.Contains("I understand that deleting my account is permanent.", deletion);
        Assert.Contains("autocomplete=\"current-password\"", deletion);
    }

    [Fact]
    public void SidebarHighlightsManagePages()
    {
        var sidebar = ReadWebFile("Views", "Shared", "_StudentSidebar.cshtml");
        Assert.Contains("isSettings", sidebar);
        Assert.Contains("aria-current", sidebar);
    }

    private static string ReadWebFile(params string[] parts)
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var root = Path.Combine(directory.FullName, "SmartCareer.Web");
            if (File.Exists(Path.Combine(root, "SmartCareer.Web.csproj"))) return File.ReadAllText(Path.Combine([root, .. parts]));
        }
        throw new DirectoryNotFoundException("Could not locate the SmartCareer.Web project root.");
    }
}
