using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartCareer.AdminSetup;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddUserSecrets<AdminSetupSecretsMarker>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("DefaultConnection is missing. No account was created.");
    return 2;
}

var services = new ServiceCollection();
services.AddLogging();
services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
services.AddScoped<AdminProvisioningService>();

using var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var credentials = new AdminSetupCredentials(
    configuration["AdminSetup:Email"],
    configuration["AdminSetup:Password"]);

try
{
    var result = await scope.ServiceProvider.GetRequiredService<AdminProvisioningService>()
        .ProvisionAsync(credentials);
    (result.Succeeded ? Console.Out : Console.Error).WriteLine(result.Message);
    if (result.Succeeded)
    {
        Console.WriteLine("Remove AdminSetup credentials from .NET User Secrets now that setup is complete.");
    }
    return result.Succeeded ? 0 : 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Administrator setup failed safely: {exception.GetType().Name}. No database migration was attempted.");
    return 2;
}

internal sealed class AdminSetupSecretsMarker;
