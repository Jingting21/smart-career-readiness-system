using System.Threading.RateLimiting;

namespace SmartCareer.Web.Security;

public static class PasswordRecoveryPolicy
{
    public const string RateLimitPolicyName = "password-recovery";
    public static readonly TimeSpan TokenLifespan = TimeSpan.FromHours(2);

    public static FixedWindowRateLimiterOptions CreateRateLimiterOptions() => new()
    {
        PermitLimit = 6,
        Window = TimeSpan.FromMinutes(15),
        QueueLimit = 0,
        AutoReplenishment = true
    };
}
