using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
namespace SmartCareer.Web.Areas.Identity.Pages.Account.Manage;
[Authorize(Roles = AppRoles.Student)]
public class DownloadPersonalDataModel(UserManager<ApplicationUser> userManager) : PageModel
{
 public IActionResult OnGet()=>NotFound();
 public async Task<IActionResult> OnPostAsync(){var u=await userManager.GetUserAsync(User);if(u is null)return NotFound();var data=new Dictionary<string,string?>();foreach(var p in typeof(ApplicationUser).GetProperties().Where(p=>Attribute.IsDefined(p,typeof(PersonalDataAttribute))))data[p.Name]=p.GetValue(u)?.ToString();var logins=await userManager.GetLoginsAsync(u);data["Connected login providers"]=string.Join(", ",logins.Select(l=>l.LoginProvider));Response.Headers.ContentDisposition="attachment; filename=SmartCareer-personal-data.json";return new FileContentResult(JsonSerializer.SerializeToUtf8Bytes(data,new JsonSerializerOptions{WriteIndented=true}),"application/json");}
}
