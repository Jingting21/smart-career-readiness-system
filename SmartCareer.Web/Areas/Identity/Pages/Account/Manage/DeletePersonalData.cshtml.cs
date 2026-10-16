using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
namespace SmartCareer.Web.Areas.Identity.Pages.Account.Manage;
[Authorize(Roles = AppRoles.Student)]
public class DeletePersonalDataModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : PageModel
{
 [BindProperty] public InputModel Input {get;set;}=new(); public bool RequirePassword {get;private set;}
 public class InputModel { [DataType(DataType.Password)] public string? Password {get;set;} [Range(typeof(bool),"true","true",ErrorMessage="You must confirm that you understand this action is permanent.")] public bool ConfirmDeletion {get;set;} }
 public async Task<IActionResult> OnGetAsync(){var u=await userManager.GetUserAsync(User);if(u is null)return NotFound();RequirePassword=await userManager.HasPasswordAsync(u);return Page();}
 public async Task<IActionResult> OnPostAsync(){var u=await userManager.GetUserAsync(User);if(u is null)return NotFound();RequirePassword=await userManager.HasPasswordAsync(u);if(RequirePassword&&!await userManager.CheckPasswordAsync(u,Input.Password??""))ModelState.AddModelError("Input.Password","The password is incorrect.");if(!ModelState.IsValid)return Page();var result=await userManager.DeleteAsync(u);if(!result.Succeeded)throw new InvalidOperationException("An unexpected error occurred while deleting the account.");await signInManager.SignOutAsync();return LocalRedirect("/");}
}
