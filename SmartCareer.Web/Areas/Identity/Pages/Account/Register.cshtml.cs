using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Services;

namespace SmartCareer.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel(
    UserManager<ApplicationUser> userManager,
    IUserStore<ApplicationUser> userStore,
    SignInManager<ApplicationUser> signInManager,
    StudentAccountService studentAccountService,
    ILogger<RegisterModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = [];

    public sealed class InputModel
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string University { get; set; } = string.Empty;

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Course { get; set; } = string.Empty;

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
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new ApplicationUser
        {
            FullName = Input.FullName.Trim(),
            University = Input.University.Trim(),
            Course = Input.Course.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await userStore.SetUserNameAsync(user, Input.Email.Trim(), CancellationToken.None);
        var emailStore = GetEmailStore();
        await emailStore.SetEmailAsync(user, Input.Email.Trim(), CancellationToken.None);

        var result = await studentAccountService.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            logger.LogInformation("A new student account was created.");
            await signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostExternalLoginAsync(string provider, string? returnUrl = null)
    {
        var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
        if (!schemes.Any(scheme => string.Equals(scheme.Name, provider, StringComparison.Ordinal)))
        {
            return BadRequest("Unknown external authentication provider.");
        }

        var redirectUrl = Url.Page("./ExternalLogin", "Callback", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore()
    {
        if (!userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The configured user store must support email addresses.");
        }

        return (IUserEmailStore<ApplicationUser>)userStore;
    }
}
