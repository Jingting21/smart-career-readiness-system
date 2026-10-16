using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.ViewModels;

namespace SmartCareer.Web.Controllers;

public class AdminController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AdminController> logger) : Controller
{
    public const string InvalidCredentialsMessage = "Invalid administrator credentials.";

    [AllowAnonymous, HttpGet("/Admin/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return User.IsInRole(AppRoles.Administrator) ? RedirectToAction(nameof(Index)) : Forbid();
        return View(new AdminLoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost("/Admin/Login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email.Trim());
        var result = user is null ? Microsoft.AspNetCore.Identity.SignInResult.Failed : await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (user is null || !result.Succeeded || !await userManager.IsInRoleAsync(user, AppRoles.Administrator))
        {
            ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
            return View(model);
        }
        await signInManager.SignInAsync(user, isPersistent: false);
        logger.LogInformation("An administrator signed in.");
        return AdministratorRedirects.IsSafe(model.ReturnUrl, Url.IsLocalUrl)
            ? LocalRedirect(model.ReturnUrl!)
            : RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Administrator), HttpGet("/Admin")]
    public IActionResult Index() => View();

}
