using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SmartCareer.Web.Security;

public static class RegistrationIdentityErrorMapper
{
    public const string DuplicateEmailMessage =
        "This email address is already registered. Please log in or use a different email address.";

    public static void AddToModelState(ModelStateDictionary modelState, IEnumerable<IdentityError> errors)
    {
        var errorList = errors.ToList();
        if (errorList.Any(IsDuplicateIdentity))
            modelState.AddModelError("Input.Email", DuplicateEmailMessage);

        foreach (var error in errorList.Where(error => !IsDuplicateIdentity(error)))
        {
            var key = error.Code.StartsWith("Password", StringComparison.OrdinalIgnoreCase)
                ? "Input.Password"
                : string.Empty;
            modelState.AddModelError(key, error.Description);
        }
    }

    private static bool IsDuplicateIdentity(IdentityError error) =>
        string.Equals(error.Code, nameof(IdentityErrorDescriber.DuplicateEmail), StringComparison.OrdinalIgnoreCase) ||
        string.Equals(error.Code, nameof(IdentityErrorDescriber.DuplicateUserName), StringComparison.OrdinalIgnoreCase);
}
