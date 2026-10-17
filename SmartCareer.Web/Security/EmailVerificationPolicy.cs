using System.Threading.RateLimiting;

namespace SmartCareer.Web.Security;

public static class EmailVerificationPolicy
{
    public const string RateLimitPolicyName = "email-verification";

    public static FixedWindowRateLimiterOptions CreateRateLimiterOptions() => new()
    {
        PermitLimit = 3,
        Window = TimeSpan.FromMinutes(15),
        QueueLimit = 0,
        AutoReplenishment = true
    };
}
