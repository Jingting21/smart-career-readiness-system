namespace SmartCareer.Web.Security;

public static class StudentRedirects
{
    public const string DashboardPath = "/Home/Dashboard";

    private static readonly string[] AllowedPathPrefixes =
    [
        DashboardPath,
        "/Home/ComingSoon",
        "/Profile",
        "/Identity/Account/Manage"
    ];

    public static string Resolve(string? returnUrl, Func<string, bool> isLocalUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !isLocalUrl(returnUrl))
        {
            return DashboardPath;
        }

        var path = returnUrl.Split('?', '#')[0].TrimEnd('/');
        return AllowedPathPrefixes.Any(prefix =>
            path.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            ? returnUrl
            : DashboardPath;
    }
}
