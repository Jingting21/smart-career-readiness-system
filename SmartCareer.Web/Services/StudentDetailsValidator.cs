using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartCareer.Web.ViewModels;
using SmartCareer.Web.Validation;

namespace SmartCareer.Web.Services;

public sealed class StudentDetailsValidator(AutocompleteCatalogService catalogs)
{
    public void ValidateAndNormalize(StudentDetailsInputModel input, ModelStateDictionary modelState, string? fieldPrefix = null)
    {
        input.FullName = input.FullName?.Trim() ?? string.Empty;
        ValidateSelection(input.UniversitySelectionMode, input.UniversityConfirmed, input.University, input.CustomUniversity,
            catalogs.TryResolveUniversity, catalogs.IsMalformedCustomUniversity, FieldKey(fieldPrefix, "University"), FieldKey(fieldPrefix, "CustomUniversity"), "university", modelState,
            value => input.University = value, catalogs.FindSimilarUniversity);
        ValidateSelection(input.CourseSelectionMode, input.CourseConfirmed, input.Course, input.CustomCourse,
            catalogs.TryResolveCourse, catalogs.IsMalformedCustomCourse, FieldKey(fieldPrefix, "Course"), FieldKey(fieldPrefix, "CustomCourse"), "course", modelState,
            value => input.Course = value, catalogs.FindSimilarCourse);
    }

    private static void ValidateSelection(string mode, bool confirmed, string? catalogValue, string? customValue,
        TryResolve resolve, Func<string, bool> malformed, string field, string customField, string displayName, ModelStateDictionary modelState, Action<string> setValue, Func<string, string?> findSimilar)
    {
        if (mode == SelectionModes.Catalog)
        {
            if (!confirmed || !resolve(catalogValue, out var canonical))
                modelState.AddModelError(field, $"Please select a {displayName} from the suggestions or choose Other — Enter manually.");
            else setValue(canonical);
            return;
        }
        if (mode != SelectionModes.Other)
        {
            modelState.AddModelError(field, "Choose a valid selection option.");
            return;
        }
        if (!ManualEntryValidator.TryNormalize(customValue, displayName, out var normalized, out var error))
            modelState.AddModelError(customField, error);
        else if (resolve(normalized, out _) || malformed(normalized) || findSimilar(normalized) is not null)
            modelState.AddModelError(customField, $"This looks similar to a {displayName} in our list. Please check the suggestions.");
        else setValue(normalized);
    }

    private static string FieldKey(string? prefix, string field) =>
        string.IsNullOrWhiteSpace(prefix) ? field : $"{prefix}.{field}";

    private delegate bool TryResolve(string? value, out string canonical);
}
