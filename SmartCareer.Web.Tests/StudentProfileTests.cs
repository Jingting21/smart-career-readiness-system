using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using SmartCareer.Web.Controllers;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services;
using SmartCareer.Web.ViewModels;

namespace SmartCareer.Web.Tests;

public sealed class StudentProfileTests
{
    [Fact]
    public void ProfileRequiresStudentRole()
    {
        var authorization = Assert.Single(typeof(ProfileController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(AppRoles.Student, authorization.Roles);
    }

    [Fact]
    public async Task AuthenticatedStudentViewsOnlyOwnProfile()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var student = await CreateStudentAsync(manager, roles, "student@example.test", "Current Student");
        await CreateStudentAsync(manager, roles, "other@example.test", "Other Student");
        var controller = CreateController(manager, student, scope.ServiceProvider);

        var result = Assert.IsType<ViewResult>(await controller.Index());
        var model = Assert.IsType<StudentProfileViewModel>(result.Model);

        Assert.Equal("Current Student", model.FullName);
        Assert.Equal("student@example.test", model.Email);
        Assert.NotEqual("Other Student", model.FullName);
        Assert.True(model.UniversityConfirmed);
        Assert.True(model.CourseConfirmed);
        Assert.Equal(SelectionModes.Catalog, model.UniversitySelectionMode);
        Assert.Equal(SelectionModes.Catalog, model.CourseSelectionMode);
    }

    [Fact]
    public async Task PrepopulatedCatalogValuesSubmitWithoutReselection()
    {
        await using var provider = CreateProvider(); using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var student = await CreateStudentAsync(manager, roles, "student@example.test", "Current Student"); var controller = CreateController(manager, student, scope.ServiceProvider);
        var get = Assert.IsType<ViewResult>(await controller.Index()); var model = Assert.IsType<StudentProfileViewModel>(get.Model); model.EducationLevel = EducationLevel.Diploma;
        Assert.IsType<RedirectToActionResult>(await controller.Index(model));
        var updated = await manager.FindByIdAsync(student.Id); Assert.Equal("Universiti Malaya", updated!.University); Assert.Equal("Computer Science", updated.Course); Assert.Equal(EducationLevel.Diploma, updated.EducationLevel);
    }

    [Fact]
    public async Task ExistingManualValuesOpenAndSaveInOtherMode()
    {
        await using var provider = CreateProvider(); using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var student = await CreateStudentAsync(manager, roles, "student@example.test", "Current Student"); student.University = "Custom Global Institute"; student.Course = "Applied Systems II"; await manager.UpdateAsync(student);
        var controller = CreateController(manager, student, scope.ServiceProvider); var get = Assert.IsType<ViewResult>(await controller.Index()); var model = Assert.IsType<StudentProfileViewModel>(get.Model);
        Assert.Equal(SelectionModes.Other, model.UniversitySelectionMode); Assert.Equal("Custom Global Institute", model.CustomUniversity); Assert.Equal(SelectionModes.Other, model.CourseSelectionMode);
        model.EducationLevel = EducationLevel.Foundation; Assert.IsType<RedirectToActionResult>(await controller.Index(model));
    }

    [Fact]
    public async Task StudentUpdatesAllowedFieldsWhileEmailAndRoleRemainUnchanged()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var student = await CreateStudentAsync(manager, roles, "student@example.test", "Old Name");
        var other = await CreateStudentAsync(manager, roles, "other@example.test", "Other Student");
        var controller = CreateController(manager, student, scope.ServiceProvider);
        var input = new StudentProfileViewModel { FullName = "  Updated Student  ", University = "Universiti Malaya", UniversityConfirmed = true, Course = "Data Science", CourseConfirmed = true, EducationLevel = EducationLevel.BachelorsDegree, Email = "attacker@example.test" };

        Assert.IsType<RedirectToActionResult>(await controller.Index(input));
        Assert.Equal("Your profile has been updated successfully.", controller.TempData["ProfileSuccess"]);

        var updated = await manager.FindByIdAsync(student.Id);
        var untouched = await manager.FindByIdAsync(other.Id);
        Assert.Equal("Updated Student", updated!.FullName);
        Assert.Equal("Universiti Malaya", updated.University);
        Assert.Equal("Data Science", updated.Course);
        Assert.Equal(EducationLevel.BachelorsDegree, updated.EducationLevel);
        Assert.Equal("student@example.test", updated.Email);
        Assert.Equal([AppRoles.Student], await manager.GetRolesAsync(updated));
        Assert.Equal("Other Student", untouched!.FullName);
    }

    [Fact]
    public async Task InvalidProfileIsRejectedAndExistingDataIsPreserved()
    {
        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var student = await CreateStudentAsync(manager, roles, "student@example.test", "Original Name");
        var controller = CreateController(manager, student, scope.ServiceProvider);
        var input = new StudentProfileViewModel { FullName = "", University = new string('U', 151), Course = "C" };
        foreach (var error in Validate(input)) controller.ModelState.AddModelError(error.MemberNames.First(), error.ErrorMessage!);

        var result = Assert.IsType<ViewResult>(await controller.Index(input));
        var returned = Assert.IsType<StudentProfileViewModel>(result.Model);
        var unchanged = await manager.FindByIdAsync(student.Id);

        Assert.False(controller.ModelState.IsValid);
        Assert.True(returned.IsEditing);
        Assert.Equal("Original Name", unchanged!.FullName);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase($"Profile-{Guid.NewGuid()}", new InMemoryDatabaseRoot()));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddSingleton(new AutocompleteCatalogService(new TestEnvironment(Path.Combine(Directory.GetCurrentDirectory(), "SmartCareer.Web"))));
        services.AddScoped<StudentDetailsValidator>();
        return services.BuildServiceProvider();
    }

    private static async Task<ApplicationUser> CreateStudentAsync(UserManager<ApplicationUser> manager, RoleManager<IdentityRole> roles, string email, string name)
    {
        if (!await roles.RoleExistsAsync(AppRoles.Student)) Assert.True((await roles.CreateAsync(new IdentityRole(AppRoles.Student))).Succeeded);
        var user = new ApplicationUser { UserName = email, Email = email, FullName = name, University = "Universiti Malaya", Course = "Computer Science" };
        Assert.True((await manager.CreateAsync(user)).Succeeded);
        Assert.True((await manager.AddToRoleAsync(user, AppRoles.Student)).Succeeded);
        return user;
    }

    private static ProfileController CreateController(UserManager<ApplicationUser> manager, ApplicationUser user, IServiceProvider services)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Name, user.UserName!), new Claim(ClaimTypes.Role, AppRoles.Student)], "Test"));
        var httpContext = new DefaultHttpContext { User = principal };
        var catalogs = services.GetRequiredService<AutocompleteCatalogService>();
        var validator = services.GetRequiredService<StudentDetailsValidator>();
        var controller = new ProfileController(manager, validator, catalogs, NullLogger<ProfileController>.Instance) { ControllerContext = new ControllerContext { HttpContext = httpContext } };
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        return controller;
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class TestEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests"; public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider(); public string WebRootPath { get; set; } = ""; public string EnvironmentName { get; set; } = "Testing"; public string ContentRootPath { get; set; } = contentRootPath; public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
