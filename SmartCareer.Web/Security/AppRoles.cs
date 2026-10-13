namespace SmartCareer.Web.Security;

public static class AppRoles
{
    public const string Student = "Student";
    public const string Administrator = "Administrator";
    public const string StudentOrAdministrator = Student + "," + Administrator;
}
