using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services;
using SmartCareer.Web.ViewModels;

namespace SmartCareer.Web.Controllers;

[Authorize(Roles = AppRoles.Student)]
public sealed class ProfileController(UserManager<ApplicationUser> userManager, StudentDetailsValidator detailsValidator, AutocompleteCatalogService catalogs, ILogger<ProfileController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(ToViewModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(StudentProfileViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        model.Email = user.Email ?? string.Empty;
        model.IsEditing = true;
        ModelState.Remove(nameof(model.Email));
        ModelState.Remove(nameof(model.IsEditing));
        detailsValidator.ValidateAndNormalize(model, ModelState);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.University = model.University;
        user.Course = model.Course;
        user.EducationLevel = model.EducationLevel;

        IdentityResult result;
        try
        {
            result = await userManager.UpdateAsync(user);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "The current student's profile could not be saved.");
            ModelState.AddModelError(string.Empty,
                "Your profile could not be saved because the database is not ready for the latest profile fields. Ask the project administrator to apply the pending migration.");
            return View(model);
        }
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["ProfileSuccess"] = "Your profile has been updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private StudentProfileViewModel ToViewModel(ApplicationUser user)
    {
        var hasUniversity = catalogs.TryResolveUniversity(user.University, out var university);
        var hasCourse = catalogs.TryResolveCourse(user.Course, out var course);
        return new()
        {
        FullName = user.FullName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        EducationLevel = user.EducationLevel,
        UniversitySelectionMode = hasUniversity ? SelectionModes.Catalog : SelectionModes.Other,
        UniversityConfirmed = hasUniversity,
        University = university,
        CustomUniversity = hasUniversity ? string.Empty : user.University ?? string.Empty,
        CourseSelectionMode = hasCourse ? SelectionModes.Catalog : SelectionModes.Other,
        CourseConfirmed = hasCourse,
        Course = course,
        CustomCourse = hasCourse ? string.Empty : user.Course ?? string.Empty
        };
    }
}
