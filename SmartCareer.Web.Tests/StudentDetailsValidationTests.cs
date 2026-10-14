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
    public void CatalogResolvesCanonicalAliasesCaseInsensitively()
    {
        var catalogs = CreateCatalogs();
        Assert.True(catalogs.TryResolveUniversity("tarumt", out var university));
        Assert.Equal("Tunku Abdul Rahman University of Management and Technology", university);
        Assert.True(catalogs.TryResolveCourse("cyber security", out var course));
        Assert.Equal("Cybersecurity", course);
    }

    [Fact]
    public void CatalogModeRejectsUnconfirmedInput()
    {
        var input = ValidInput(); input.UniversityConfirmed = false; input.University = "Taylor University123";
        var state = Validate(input);
        Assert.Contains(state[nameof(input.University)]!.Errors, error => error.ErrorMessage.Contains("suggestions"));
    }

    [Fact]
    public void OtherModeAcceptsLegitimateCustomValuesAndRejectsRecognisedNamesWithJunk()
    {
        var valid = ValidInput(); valid.UniversitySelectionMode = SelectionModes.Other; valid.CustomUniversity = "Institut Teknologi Contoh 21";
        Assert.True(Validate(valid).IsValid);
        var malformed = ValidInput(); malformed.CourseSelectionMode = SelectionModes.Other; malformed.CustomCourse = "Computer Science123";
        Assert.False(Validate(malformed).IsValid);
    }

    [Fact]
    public void EducationLevelIsRequiredForEmailAndGoogleRegistration()
    {
        var email = new RegisterModel.InputModel { FullName = "John Tan" };
        var google = new ExternalLoginModel.InputModel { FullName = "John Tan" };
        Assert.Contains(ValidateAnnotations(email), result => result.MemberNames.Contains(nameof(email.EducationLevel)));
        Assert.Contains(ValidateAnnotations(google), result => result.MemberNames.Contains(nameof(google.EducationLevel)));
    }

    [Fact]
    public void ExistingStudentCanDisplayNullEducationLevel()
    {
        var user = new ApplicationUser { EducationLevel = null };
        Assert.Null(user.EducationLevel);
    }

    [Fact]
    public void InvalidEducationLevelIsRejected()
    {
        var input = ValidInput(); input.EducationLevel = (EducationLevel)999;
        Assert.Contains(ValidateAnnotations(input), result => result.MemberNames.Contains(nameof(input.EducationLevel)));
    }

    [Theory]
    [InlineData(SelectionModes.Catalog, SelectionModes.Catalog)]
    [InlineData(SelectionModes.Other, SelectionModes.Other)]
    [InlineData(SelectionModes.Catalog, SelectionModes.Other)]
    [InlineData(SelectionModes.Other, SelectionModes.Catalog)]
    public void EveryUniversityAndCourseModeCombinationValidatesIndependently(string universityMode, string courseMode)
    {
        var input = ValidInput();
        ConfigureUniversity(input, universityMode);
        ConfigureCourse(input, courseMode);
        Assert.True(Validate(input).IsValid);
    }

    [Fact]
    public void SwitchingOtherToCatalogIgnoresEmptyManualFields()
    {
        var input = ValidInput(); input.CustomUniversity = null; input.CustomCourse = null;
        var state = Validate(input);
        Assert.True(state.IsValid);
        Assert.Null(state[nameof(input.CustomUniversity)]);
        Assert.Null(state[nameof(input.CustomCourse)]);
    }

    [Fact]
    public void SwitchingCatalogToOtherDoesNotRequireConfirmation()
    {
        var input = ValidInput(); ConfigureUniversity(input, SelectionModes.Other); ConfigureCourse(input, SelectionModes.Other);
        input.UniversityConfirmed = false; input.CourseConfirmed = false;
        Assert.True(Validate(input).IsValid);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ManualValueIsRequiredOnlyForTheSelectedOtherMode(bool otherUniversity, bool otherCourse)
    {
        var input = ValidInput();
        if (otherUniversity) { ConfigureUniversity(input, SelectionModes.Other); input.CustomUniversity = null; }
        if (otherCourse) { ConfigureCourse(input, SelectionModes.Other); input.CustomCourse = null; }
        var state = Validate(input);
        Assert.Equal(otherUniversity, state.ContainsKey(nameof(input.CustomUniversity)));
        Assert.Equal(otherCourse, state.ContainsKey(nameof(input.CustomCourse)));
    }

    [Fact]
    public void RegistrationAndGoogleCatalogSelectionsDoNotRequireManualFields()
    {
        var registration = new RegisterModel.InputModel { FullName="John Tan", University="Universiti Malaya", UniversityConfirmed=true, Course="Software Engineering", CourseConfirmed=true, EducationLevel=EducationLevel.BachelorsDegree, Email="student@example.test", Password="Valid#Pass1", ConfirmPassword="Valid#Pass1" };
        var google = new ExternalLoginModel.InputModel { FullName="John Tan", University="Taylor's College", UniversityConfirmed=true, Course="Software Engineering", CourseConfirmed=true, EducationLevel=EducationLevel.BachelorsDegree, Email="student@example.test" };
        Assert.DoesNotContain(ValidateAnnotations(registration), error => error.MemberNames.Contains(nameof(registration.CustomUniversity)) || error.MemberNames.Contains(nameof(registration.CustomCourse)));
        Assert.DoesNotContain(ValidateAnnotations(google), error => error.MemberNames.Contains(nameof(google.CustomUniversity)) || error.MemberNames.Contains(nameof(google.CustomCourse)));
        Assert.True(Validate(registration).IsValid);
        Assert.True(Validate(google).IsValid);
    }

    [Fact]
    public void ConditionalTextPropertiesAreNullableForMvcBinding()
    {
        var context = new NullabilityInfoContext();
        foreach (var name in new[] { nameof(StudentDetailsInputModel.University), nameof(StudentDetailsInputModel.CustomUniversity), nameof(StudentDetailsInputModel.Course), nameof(StudentDetailsInputModel.CustomCourse) })
            Assert.Equal(NullabilityState.Nullable, context.Create(typeof(StudentDetailsInputModel).GetProperty(name)!).WriteState);
    }

    [Theory]
    [InlineData("xxx")]
    [InlineData("aaa")]
    [InlineData("asdf")]
    [InlineData("12345")]
    [InlineData("@@@@")]
    [InlineData("@AA")]
    [InlineData("xxxxxxxx")]
    public void MeaninglessManualEntriesAreRejected(string value)
    {
        var university = ValidInput(); ConfigureUniversity(university, SelectionModes.Other); university.CustomUniversity = value;
        var course = ValidInput(); ConfigureCourse(course, SelectionModes.Other); course.CustomCourse = value;
        Assert.False(Validate(university).IsValid);
        Assert.False(Validate(course).IsValid);
    }

    [Theory]
    [InlineData("École Supérieure d'Ingénierie 21")]
    [InlineData("Институт прикладных наук")]
    [InlineData("東京工科学院")]
    public void LegitimateInternationalManualUniversitiesAreAccepted(string value)
    {
        var input = ValidInput(); ConfigureUniversity(input, SelectionModes.Other); input.CustomUniversity = value;
        Assert.True(Validate(input).IsValid);
    }

    [Theory]
    [InlineData("3D Visualisation Arts")]
    [InlineData("Computer Systems II")]
    [InlineData("BSc (Hons) Quantum Computing")]
    [InlineData("C++ Systems Design")]
    public void LegitimateTechnicalManualCoursesAreAccepted(string value)
    {
        var input = ValidInput(); ConfigureCourse(input, SelectionModes.Other); input.CustomCourse = value;
        Assert.True(Validate(input).IsValid);
    }

    [Fact]
    public void ManualWhitespaceIsNormalizedBeforeSaving()
    {
        var input = ValidInput(); ConfigureUniversity(input, SelectionModes.Other); input.CustomUniversity = "  Institut   Teknologi   Contoh  ";
        Assert.True(Validate(input).IsValid);
        Assert.Equal("Institut Teknologi Contoh", input.University);
    }

    [Fact]
    public void RecognisedManualValuePromptsForCatalogSelection()
    {
        var input = ValidInput(); ConfigureCourse(input, SelectionModes.Other); input.CustomCourse = "Software Engineering";
        var state = Validate(input);
        Assert.Contains(state[nameof(input.CustomCourse)]!.Errors, error => error.ErrorMessage.Contains("suggestions"));
    }

    [Fact]
    public void ValidationSummariesUseNeutralMarkupAndHideTheirValidState()
    {
        var root = FindWebProjectRoot();
        foreach (var relative in new[] { "Views/Profile/Index.cshtml", "Areas/Identity/Pages/Account/Register.cshtml", "Areas/Identity/Pages/Account/ExternalLogin.cshtml", "Areas/Identity/Pages/Account/Login.cshtml" })
        {
            var markup = File.ReadAllText(Path.Combine(root, relative));
            Assert.Contains("class=\"validation-summary\"", markup);
            Assert.DoesNotContain("class=\"validation-summary-errors\"", markup);
        }
        Assert.Contains(".validation-summary.validation-summary-valid{display:none}", File.ReadAllText(Path.Combine(root, "wwwroot/css/site.css")));
    }

    [Fact]
    public void RazorPageValidationUsesInputPrefixedFieldKeys()
    {
        var input = ValidInput();
        input.UniversityConfirmed = false;
        input.CourseConfirmed = false;
        var state = new ModelStateDictionary();

        new StudentDetailsValidator(CreateCatalogs()).ValidateAndNormalize(input, state, "Input");

        Assert.Contains("Input.University", state.Keys);
        Assert.Contains("Input.Course", state.Keys);
        Assert.DoesNotContain("University", state.Keys);
        Assert.DoesNotContain("Course", state.Keys);
    }

    [Fact]
    public void InvalidRegistrationClearsPasswordAttemptedValueButKeepsItsValidationError()
    {
        var state = new ModelStateDictionary();
        state.SetModelValue("Input.Password", "Secret#123", "Secret#123");
        state.AddModelError("Input.Password", "Password policy error.");

        SensitiveModelStateCleaner.ClearAttemptedValue(state, "Input.Password");

        Assert.Equal(string.Empty, state["Input.Password"]!.AttemptedValue);
        Assert.Contains(state["Input.Password"]!.Errors, error => error.ErrorMessage == "Password policy error.");
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
        Assert.Contains("[data-custom-field] input", focusScript);
        Assert.DoesNotContain("value=\"@Model.Input.Password\"", register);
    }

    private static string FindWebProjectRoot()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "SmartCareer.Web");
            if (File.Exists(Path.Combine(candidate, "SmartCareer.Web.csproj"))) return candidate;
        }
        throw new DirectoryNotFoundException("Could not locate the SmartCareer.Web project root.");
    }

    private static StudentDetailsInputModel ValidInput() => new() { FullName = "John Tan", University = "Universiti Malaya", UniversityConfirmed = true, Course = "Computer Science", CourseConfirmed = true, EducationLevel = EducationLevel.Foundation };
    private static void ConfigureUniversity(StudentDetailsInputModel input, string mode) { input.UniversitySelectionMode=mode; input.UniversityConfirmed=mode==SelectionModes.Catalog; input.University=mode==SelectionModes.Catalog?"Universiti Malaya":null; input.CustomUniversity=mode==SelectionModes.Other?"Custom Global Institute":null; }
    private static void ConfigureCourse(StudentDetailsInputModel input, string mode) { input.CourseSelectionMode=mode; input.CourseConfirmed=mode==SelectionModes.Catalog; input.Course=mode==SelectionModes.Catalog?"Software Engineering":null; input.CustomCourse=mode==SelectionModes.Other?"Applied Systems II":null; }
    private static ModelStateDictionary Validate(StudentDetailsInputModel input) { var state = new ModelStateDictionary(); foreach (var result in ValidateAnnotations(input)) foreach (var member in result.MemberNames.DefaultIfEmpty(string.Empty)) state.AddModelError(member, result.ErrorMessage!); new StudentDetailsValidator(CreateCatalogs()).ValidateAndNormalize(input, state); return state; }
    private static List<ValidationResult> ValidateAnnotations(object value) { var results = new List<ValidationResult>(); Validator.TryValidateObject(value, new ValidationContext(value), results, true); return results; }
    private static AutocompleteCatalogService CreateCatalogs() => new(new TestEnvironment(Path.Combine(Directory.GetCurrentDirectory(), "SmartCareer.Web")));
    private sealed class TestEnvironment(string contentRootPath) : IWebHostEnvironment { public string ApplicationName { get; set; }="Tests"; public IFileProvider WebRootFileProvider { get; set; }=new NullFileProvider(); public string WebRootPath { get; set; }=""; public string EnvironmentName { get; set; }="Testing"; public string ContentRootPath { get; set; }=contentRootPath; public IFileProvider ContentRootFileProvider { get; set; }=new NullFileProvider(); }
}
