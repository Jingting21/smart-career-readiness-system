
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Services;
using SmartCareer.Web.Services.Email;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services.CareerCatalog;
using SmartCareer.Web.Services.RelatedFields;

var builder = WebApplication.CreateBuilder(args);

// Add MVC services
builder.Services.AddControllersWithViews();

// Connect to SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure ASP.NET Core Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = PasswordRecoveryPolicy.TokenLifespan);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    // Preserve secure cookies over HTTPS while allowing the checked-in HTTP
    // development profile to authenticate locally.
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddScoped<IdentityDataInitializer>();
builder.Services.AddScoped<StudentAccountService>();
builder.Services.AddSingleton<AutocompleteCatalogService>();
builder.Services.AddScoped<StudentDetailsValidator>();
builder.Services.AddScoped<StudentPasswordRecoveryService>();
builder.Services.AddScoped<StudentEmailVerificationService>();
builder.Services.AddScoped<ICareerCatalogQueryService, CareerCatalogQueryService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<RelatedFieldReviewService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(PasswordRecoveryPolicy.RateLimitPolicyName, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => PasswordRecoveryPolicy.CreateRateLimiterOptions()));
    options.AddPolicy(EmailVerificationPolicy.RateLimitPolicyName, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => EmailVerificationPolicy.CreateRateLimiterOptions()));
});
builder.Services.AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => options.Port == 587, "Smtp:Port must be 587 for Gmail STARTTLS.")
    .ValidateOnStart();
builder.Services.AddTransient<ISmtpTransport, MailKitSmtpTransport>();
builder.Services.AddTransient<IEmailService, SmtpEmailService>();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, IdentityEmailSender>();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.IEmailSender<ApplicationUser>, IdentityEmailSender>();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.ClaimActions.Add(new JsonKeyClaimAction("email_verified", ClaimValueTypes.Boolean, "verified_email"));
        });
}

// Enable Identity UI Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

await IdentityDataInitializer.InitializeAsync(app.Services);

app.Run();

public partial class Program;
