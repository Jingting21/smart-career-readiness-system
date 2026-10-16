using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
namespace SmartCareer.Web.Areas.Identity.Pages.Account.Manage;
[Authorize(Roles = AppRoles.Student)]
public class SetPasswordModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : PageModel
{
 [BindProperty] public InputModel Input { get; set; } = new(); [TempData] public string? StatusMessage { get; set; }
 public class InputModel { [Required,StringLength(100,MinimumLength=8),DataType(DataType.Password),Display(Name="New password")] public string NewPassword {get;set;}=""; [DataType(DataType.Password),Compare(nameof(NewPassword)),Display(Name="Confirm new password")] public string ConfirmPassword {get;set;}=""; }
 public async Task<IActionResult> OnGetAsync(){var u=await userManager.GetUserAsync(User);if(u is null)return NotFound();return await userManager.HasPasswordAsync(u)?RedirectToPage("ChangePassword"):Page();}
 public async Task<IActionResult> OnPostAsync(){if(!ModelState.IsValid)return Page();var u=await userManager.GetUserAsync(User);if(u is null)return NotFound();var r=await userManager.AddPasswordAsync(u,Input.NewPassword);if(!r.Succeeded){foreach(var e in r.Errors)ModelState.AddModelError(string.Empty,e.Description);return Page();}await signInManager.RefreshSignInAsync(u);StatusMessage="A password has been added to your account.";return RedirectToPage("ChangePassword");}
}
