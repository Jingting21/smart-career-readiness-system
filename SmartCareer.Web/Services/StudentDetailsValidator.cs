using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartCareer.Web.ViewModels;

namespace SmartCareer.Web.Services;

public sealed class StudentDetailsValidator(AutocompleteCatalogService catalogs)
{
    public void ValidateAndNormalize(StudentDetailsInputModel input, ModelStateDictionary modelState, string? fieldPrefix = null)
    {
        input.FullName = input.FullName?.Trim() ?? string.Empty;
        ValidateSelection(input.UniversityConfirmed, input.University, catalogs.TryResolveUniversity,
            FieldKey(fieldPrefix, "University"), "university", modelState, value => input.University = value);
        ValidateSelection(input.CourseConfirmed, input.Course, catalogs.TryResolveCourse,
            FieldKey(fieldPrefix, "Course"), "course", modelState, value => input.Course = value);
    }

    private static void ValidateSelection(bool confirmed, string? value, TryResolve resolve, string field,
        string displayName, ModelStateDictionary modelState, Action<string> setValue)
    {
        if (!confirmed || !resolve(value, out var canonical))
        {
            modelState.AddModelError(field, $"Please select a valid {displayName} from the suggestions.");
            return;
        }

        setValue(canonical);
    }

    private static string FieldKey(string? prefix, string field) =>
        string.IsNullOrWhiteSpace(prefix) ? field : $"{prefix}.{field}";

    private delegate bool TryResolve(string? value, out string canonical);
}
