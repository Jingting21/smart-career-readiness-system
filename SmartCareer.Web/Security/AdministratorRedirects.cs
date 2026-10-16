namespace SmartCareer.Web.Security;

public static class AdministratorRedirects
{
    public const string DashboardPath = "/Admin";

    public static bool IsSafe(string? returnUrl, Func<string, bool> isLocalUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !isLocalUrl(returnUrl))
        {
            return false;
        }

        var path = returnUrl.Split('?', '#')[0].TrimEnd('/');
        return (path.Equals(DashboardPath, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(DashboardPath + "/", StringComparison.OrdinalIgnoreCase)) &&
               !path.Equals("/Admin/Login", StringComparison.OrdinalIgnoreCase);
    }
}
