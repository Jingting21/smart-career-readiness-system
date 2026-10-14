using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SmartCareer.Web.Security;

public static class SensitiveModelStateCleaner
{
    public static void ClearAttemptedValue(ModelStateDictionary modelState, string key)
    {
        if (!modelState.TryGetValue(key, out var entry)) return;
        var errors = entry.Errors.Select(error => error.ErrorMessage).ToArray();
        modelState.Remove(key);
        modelState.SetModelValue(key, string.Empty, string.Empty);
        foreach (var error in errors) modelState.AddModelError(key, error);
    }
}
