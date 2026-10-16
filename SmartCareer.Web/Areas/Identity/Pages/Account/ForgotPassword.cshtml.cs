using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartCareer.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public bool Submitted { get; private set; }
    public sealed class InputModel { [Required, EmailAddress] public string Email { get; set; } = ""; }
    public void OnGet() { }
    public IActionResult OnPost()
    {
        if (!ModelState.IsValid) return Page();
        Submitted = true;
        return Page();
    }
}
