using System.Text.RegularExpressions;

namespace SmartCareer.Web.Validation;

public static partial class ManualEntryValidator
{
    private static readonly HashSet<string> MeaninglessValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "aaa", "xxx", "asdf", "qwerty", "test", "unknown", "n/a", "na", "none"
    };

    public static bool TryNormalize(string? value, string label, out string normalized, out string error)
    {
        normalized = WhitespaceRegex().Replace(value?.Trim() ?? string.Empty, " ");
        error = string.Empty;
        if (normalized.Length < 3) { error = $"Please enter a valid {label} name."; return false; }
        if (normalized.Length > 150) { error = $"The {label} name cannot exceed 150 characters."; return false; }
        if (normalized.Any(char.IsControl) || normalized.IndexOfAny(['<', '>', '{', '}']) >= 0) { error = "Markup and control characters are not allowed."; return false; }

        var compact = new string(normalized.Where(character => !char.IsWhiteSpace(character)).ToArray());
        var lettersAndDigits = compact.Count(char.IsLetterOrDigit);
        var letters = compact.Count(char.IsLetter);
        var comparable = new string(compact.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        if (letters < 2 || MeaninglessValues.Contains(normalized) || MeaninglessValues.Contains(comparable)) { error = $"Please enter a valid {label} name."; return false; }
        if (compact.Contains('@') || lettersAndDigits * 2 < compact.Length) { error = "Avoid symbols or meaningless text."; return false; }
        if (comparable.Length >= 3 && comparable.GroupBy(character => character).Max(group => group.Count()) >= comparable.Length - 1) { error = "Avoid repeated-character or meaningless text."; return false; }
        return true;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
