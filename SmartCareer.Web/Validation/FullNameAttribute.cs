using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SmartCareer.Web.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class FullNameAttribute : ValidationAttribute, IClientModelValidator
{
    public FullNameAttribute() : base("Enter a valid name using letters, spaces, apostrophes, or hyphens.") { }

    public override bool IsValid(object? value)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name)) return false;
        var hasLetter = false;
        for (var index = 0; index < name.Length; index++)
        {
            if (char.IsLetter(name, index)) { hasLetter = true; if (char.IsSurrogatePair(name, index)) index++; continue; }
            if (char.IsWhiteSpace(name[index]) || name[index] is '\'' or '’' or '-') continue;
            return false;
        }
        return hasLetter;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes.TryAdd("data-val", "true");
        context.Attributes.TryAdd("data-val-fullname", ErrorMessageString);
    }
}
