using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Areas.Identity.Pages.Account.Manage;

[Authorize(Roles = AppRoles.Student)]
public class ChangePasswordModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [TempData] public string? StatusMessage { get; set; }
    public class InputModel
    {
        [Required, DataType(DataType.Password), Display(Name="Current password")] public string OldPassword { get; set; } = "";
        [Required, StringLength(100, MinimumLength=8), DataType(DataType.Password), Display(Name="New password")] public string NewPassword { get; set; } = "";
        [DataType(DataType.Password), Compare(nameof(NewPassword)), Display(Name="Confirm new password")] public string ConfirmPassword { get; set; } = "";
    }
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return NotFound();
        return await userManager.HasPasswordAsync(user) ? Page() : RedirectToPage("SetPassword");
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await userManager.GetUserAsync(User); if (user is null) return NotFound();
        var result = await userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return Page(); }
        await signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your password has been changed successfully.";
        return RedirectToPage();
    }
}
