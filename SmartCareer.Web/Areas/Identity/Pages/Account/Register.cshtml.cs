using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services;
using SmartCareer.Web.ViewModels;

namespace SmartCareer.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel(
    UserManager<ApplicationUser> userManager,
    IUserStore<ApplicationUser> userStore,
    SignInManager<ApplicationUser> signInManager,
    StudentAccountService studentAccountService,
    StudentDetailsValidator detailsValidator,
    ILogger<RegisterModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = [];

    public bool ShowPasswordReentryNotice { get; private set; }

    public sealed class InputModel : StudentDetailsInputModel
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl = StudentRedirects.Resolve(returnUrl, Url.IsLocalUrl);
        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        detailsValidator.ValidateAndNormalize(Input, ModelState, nameof(Input));
        if (!ModelState.IsValid) { PrepareInvalidRegistration(); return Page(); }
        var user = new ApplicationUser { FullName = Input.FullName.Trim(), University = Input.University, Course = Input.Course, EducationLevel = Input.EducationLevel, CreatedAt = DateTime.UtcNow };
        await userStore.SetUserNameAsync(user, Input.Email.Trim(), CancellationToken.None);
        await GetEmailStore().SetEmailAsync(user, Input.Email.Trim(), CancellationToken.None);
        var result = await studentAccountService.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            logger.LogInformation("A new student account was created.");
            await signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }
        RegistrationIdentityErrorMapper.AddToModelState(ModelState, result.Errors);
        PrepareInvalidRegistration();
        return Page();
    }

    public async Task<IActionResult> OnPostExternalLoginAsync(string provider, string? returnUrl = null)
    {
        if (!string.Equals(provider, "Google", StringComparison.Ordinal)) return BadRequest("Google is the only supported external provider.");
        var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
        if (!schemes.Any(s => s.Name == provider)) return BadRequest("Unknown external authentication provider.");
        var redirectUrl = Url.Page("./ExternalLogin", "Callback", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        LoginModel.RequestGoogleAccountSelection(provider, properties);
        return new ChallengeResult(provider, properties);
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore() => userManager.SupportsUserEmail ? (IUserEmailStore<ApplicationUser>)userStore : throw new NotSupportedException("The configured user store must support email addresses.");

    private void PrepareInvalidRegistration()
    {
        ShowPasswordReentryNotice = true;
        SensitiveModelStateCleaner.ClearAttemptedValue(ModelState, "Input.Password");
        SensitiveModelStateCleaner.ClearAttemptedValue(ModelState, "Input.ConfirmPassword");
        Input.Password = "";
        Input.ConfirmPassword = "";
    }

}
