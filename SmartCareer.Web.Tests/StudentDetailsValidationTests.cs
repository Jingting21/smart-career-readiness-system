using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.FileProviders;
using SmartCareer.Web.Areas.Identity.Pages.Account;
using SmartCareer.Web.Models;
using SmartCareer.Web.Services;
using SmartCareer.Web.Security;
using SmartCareer.Web.Validation;
using SmartCareer.Web.ViewModels;

namespace SmartCareer.Web.Tests;

public sealed class StudentDetailsValidationTests
{
    [Theory]
    [InlineData("John Tan")]
    [InlineData("Anne-Marie")]
    [InlineData("O'Connor")]
    [InlineData("王小明")]
    public void FullNameAcceptsInternationalNamesAndCommonPunctuation(string name) => Assert.True(new FullNameAttribute().IsValid(name));

    [Theory]
    [InlineData("John123")]
    [InlineData("Alice@")]
    [InlineData("---")]
    [InlineData("   ")]
    public void FullNameRejectsDigitsSymbolsAndNonNames(string name) => Assert.False(new FullNameAttribute().IsValid(name));

    [Fact]
    public void ValidUniversityAndCourseSelectionsAreCanonicalized()
    {
        var input = ValidInput(); input.University = "tarumt"; input.Course = "cyber security";
        var state = Validate(input);
        Assert.True(state.IsValid);
        Assert.Equal("Tunku Abdul Rahman University of Management and Technology", input.University);
        Assert.Equal("Cybersecurity", input.Course);
    }

    [Theory]
    [InlineData("Arbitrary University", true, "Computer Science", true, "University")]
    [InlineData("Universiti Malaya", true, "Invented Course", true, "Course")]
    [InlineData("Universiti Malaya", false, "Computer Science", true, "University")]
    [InlineData("Universiti Malaya", true, "Computer Science", false, "Course")]
    [InlineData(null, false, "Computer Science", true, "University")]
    [InlineData("Universiti Malaya", true, null, false, "Course")]
    public void UnselectedEmptyChangedOrForgedValuesAreRejected(string? university, bool universityConfirmed, string? course, bool courseConfirmed, string invalidField)
    {
        var input = ValidInput(); input.University = university; input.UniversityConfirmed = universityConfirmed; input.Course = course; input.CourseConfirmed = courseConfirmed;
        var state = Validate(input);
        Assert.False(state.IsValid);
        Assert.Contains(state[invalidField]!.Errors, error => error.ErrorMessage.Contains("suggestions"));
    }

    [Fact]
    public void RazorPageValidationUsesInputPrefixedFieldKeys()
    {
        var input = ValidInput(); input.UniversityConfirmed = false; input.CourseConfirmed = false; var state = new ModelStateDictionary();
        new StudentDetailsValidator(CreateCatalogs()).ValidateAndNormalize(input, state, "Input");
        Assert.Contains("Input.University", state.Keys); Assert.Contains("Input.Course", state.Keys);
        Assert.DoesNotContain("University", state.Keys); Assert.DoesNotContain("Course", state.Keys);
    }

    [Fact]
    public void EducationLevelIsRequiredForEmailAndGoogleRegistration()
    {
        var email = new RegisterModel.InputModel { FullName = "John Tan" }; var google = new ExternalLoginModel.InputModel { FullName = "John Tan" };
        Assert.Contains(ValidateAnnotations(email), result => result.MemberNames.Contains(nameof(email.EducationLevel)));
        Assert.Contains(ValidateAnnotations(google), result => result.MemberNames.Contains(nameof(google.EducationLevel)));
    }

    [Fact]
    public void InvalidEducationLevelIsRejected()
    {
        var input = ValidInput(); input.EducationLevel = (EducationLevel)999;
        Assert.Contains(ValidateAnnotations(input), result => result.MemberNames.Contains(nameof(input.EducationLevel)));
    }

    [Fact]
    public void ExistingStudentCanRetainNullEducationLevel()
    {
        var user = new ApplicationUser { EducationLevel = null };

        Assert.Null(user.EducationLevel);
    }

    [Fact]
    public void RegistrationAndGoogleSelectionOnlyInputsValidateWithoutManualFields()
    {
        var registration = new RegisterModel.InputModel
        {
            FullName = "John Tan", University = "Universiti Malaya", UniversityConfirmed = true,
            Course = "Software Engineering", CourseConfirmed = true,
            EducationLevel = EducationLevel.BachelorsDegree, Email = "student@example.test",
            Password = "Valid#Pass1", ConfirmPassword = "Valid#Pass1"
        };
        var google = new ExternalLoginModel.InputModel
        {
            FullName = "John Tan", University = "Taylor's College", UniversityConfirmed = true,
            Course = "Software Engineering", CourseConfirmed = true,
            EducationLevel = EducationLevel.BachelorsDegree, Email = "student@example.test"
        };

        Assert.True(Validate(registration).IsValid);
        Assert.True(Validate(google).IsValid);
    }

    [Fact]
    public void SelectionTextPropertiesRemainNullableForMvcBinding()
    {
        foreach (var name in new[] { nameof(StudentDetailsInputModel.University), nameof(StudentDetailsInputModel.Course) })
            Assert.Equal(NullabilityState.Nullable,
                new NullabilityInfoContext().Create(typeof(StudentDetailsInputModel).GetProperty(name)!).WriteState);
    }

    [Fact]
    public void SelectionOnlyViewsAndScriptsRemoveManualFallbackAndKeepKeyboardSelection()
    {
        var root = FindWebProjectRoot();
        var views = new[] { "Areas/Identity/Pages/Account/Register.cshtml", "Areas/Identity/Pages/Account/ExternalLogin.cshtml", "Views/Profile/Index.cshtml" }
            .Select(relative => File.ReadAllText(Path.Combine(root, relative))).ToArray();
        var autocomplete = File.ReadAllText(Path.Combine(root, "wwwroot/js/autocomplete.js"));
        Assert.All(views, view =>
        {
            Assert.DoesNotContain("Other — Enter manually", view); Assert.DoesNotContain("CustomUniversity", view); Assert.DoesNotContain("CustomCourse", view);
            Assert.DoesNotContain("unverified", view, StringComparison.OrdinalIgnoreCase); Assert.Contains("data-selection-confirmed", view);
        });
        Assert.Contains("ArrowDown", autocomplete); Assert.Contains("ArrowUp", autocomplete); Assert.Contains("event.key===\"Enter\"", autocomplete);
        Assert.Contains("confirmed.value=\"false\"", autocomplete); Assert.Contains("confirmed.value=\"true\"", autocomplete); Assert.DoesNotContain("Choose Other", autocomplete);
    }

    [Fact]
    public void ValidationSummariesUseNeutralMarkupAndHideTheirValidState()
    {
        var root = FindWebProjectRoot();
        foreach (var relative in new[]
        {
            "Views/Profile/Index.cshtml", "Areas/Identity/Pages/Account/Register.cshtml",
            "Areas/Identity/Pages/Account/ExternalLogin.cshtml", "Areas/Identity/Pages/Account/Login.cshtml"
        })
        {
            var markup = File.ReadAllText(Path.Combine(root, relative));
            Assert.Contains("class=\"validation-summary\"", markup);
            Assert.DoesNotContain("class=\"validation-summary-errors\"", markup);
        }
        Assert.Contains(".validation-summary.validation-summary-valid{display:none}",
            File.ReadAllText(Path.Combine(root, "wwwroot/css/site.css")));
    }

    [Fact]
    public void RegistrationViewsExposeFieldErrorsAndSharedFirstInvalidFocus()
    {
        var root = FindWebProjectRoot();
        var register = File.ReadAllText(Path.Combine(root, "Areas/Identity/Pages/Account/Register.cshtml"));
        var google = File.ReadAllText(Path.Combine(root, "Areas/Identity/Pages/Account/ExternalLogin.cshtml"));
        var focusScript = File.ReadAllText(Path.Combine(root, "wwwroot/js/form-validation-focus.js"));

        Assert.Contains("asp-validation-summary=\"ModelOnly\"", register);
        Assert.Contains("asp-validation-for=\"Input.University\"", register);
        Assert.Contains("asp-validation-for=\"Input.Course\"", register);
        Assert.Contains("data-validation-focus", register);
        Assert.Contains("data-validation-focus", google);
        Assert.Contains("scrollIntoView", focusScript);
        Assert.Contains("focus({ preventScroll: true })", focusScript);
        Assert.Contains("[data-autocomplete-url]", focusScript);
        Assert.DoesNotContain("[data-custom-field]", focusScript);
        Assert.DoesNotContain("value=\"@Model.Input.Password\"", register);
    }

    [Fact]
    public void InvalidRegistrationClearsPasswordAttemptedValueButKeepsItsValidationError()
    {
        var state = new ModelStateDictionary(); state.SetModelValue("Input.Password", "Secret#123", "Secret#123"); state.AddModelError("Input.Password", "Password policy error.");
        SensitiveModelStateCleaner.ClearAttemptedValue(state, "Input.Password");
        Assert.Equal(string.Empty, state["Input.Password"]!.AttemptedValue);
        Assert.Contains(state["Input.Password"]!.Errors, error => error.ErrorMessage == "Password policy error.");
    }

    private static StudentDetailsInputModel ValidInput() => new() { FullName = "John Tan", University = "Universiti Malaya", UniversityConfirmed = true, Course = "Computer Science", CourseConfirmed = true, EducationLevel = EducationLevel.Foundation };
    private static ModelStateDictionary Validate(StudentDetailsInputModel input)
    {
        var state = new ModelStateDictionary(); foreach (var result in ValidateAnnotations(input)) foreach (var member in result.MemberNames.DefaultIfEmpty(string.Empty)) state.AddModelError(member, result.ErrorMessage!);
        new StudentDetailsValidator(CreateCatalogs()).ValidateAndNormalize(input, state); return state;
    }
    private static List<ValidationResult> ValidateAnnotations(object value) { var results = new List<ValidationResult>(); Validator.TryValidateObject(value, new ValidationContext(value), results, true); return results; }
    private static AutocompleteCatalogService CreateCatalogs() => new(new TestEnvironment(Path.Combine(Directory.GetCurrentDirectory(), "SmartCareer.Web")));
    private static string FindWebProjectRoot()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent) { var candidate = Path.Combine(directory.FullName, "SmartCareer.Web"); if (File.Exists(Path.Combine(candidate, "SmartCareer.Web.csproj"))) return candidate; }
        throw new DirectoryNotFoundException("Could not locate the SmartCareer.Web project root.");
    }
    private sealed class TestEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests"; public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider(); public string WebRootPath { get; set; } = ""; public string EnvironmentName { get; set; } = "Testing"; public string ContentRootPath { get; set; } = contentRootPath; public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
