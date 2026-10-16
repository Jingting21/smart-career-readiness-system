using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(AppRoles.Administrator))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (User.IsInRole(AppRoles.Student))
            {
                return RedirectToAction(nameof(Dashboard));
            }
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Terms() => View();

    [Authorize(Roles = AppRoles.Student)]
    public IActionResult Dashboard() => View();

    [Authorize(Roles = AppRoles.Student)]
    public IActionResult ComingSoon(string feature = "This feature")
    {
        ViewData["Feature"] = feature;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
