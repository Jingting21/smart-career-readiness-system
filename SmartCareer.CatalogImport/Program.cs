using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartCareer.Web.Data;
using SmartCareer.Web.Services.CareerCatalogImport;

var dryRun = args.Contains("--dry-run", StringComparer.Ordinal);
var import = args.Contains("--import", StringComparer.Ordinal);
if (dryRun == import)
{
    Console.Error.WriteLine("Specify exactly one operation: --dry-run or --import.");
    return 2;
}

var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var datasetRoot = GetOption(args, "--dataset")
    ?? Path.Combine(repositoryRoot, "dataset-research", "onet-31.0");
var reportPath = GetOption(args, "--report");

try
{
    if (dryRun)
    {
        return RunDryRun(datasetRoot, reportPath);
    }

    var confirmation = new CareerCatalogImportConfirmation(
        GetOption(args, "--confirm-database"),
        GetOption(args, "--confirm-server"),
        GetOption(args, "--confirm-dataset"));
    var confirmationErrors = CareerCatalogImportGuard.ValidateConfirmationOnly(confirmation);
    if (confirmationErrors.Count != 0)
    {
        foreach (var error in confirmationErrors)
        {
            Console.Error.WriteLine(error);
        }
        return 2;
    }

    var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? "Development";
    if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Live catalogue import is restricted to the Development environment.");
        return 2;
    }

    var plan = new OnetCareerCatalogImporter().BuildPlan(new CareerCatalogDatasetPaths(datasetRoot));
    if (!plan.IsValid)
    {
        foreach (var error in plan.Errors)
        {
            Console.Error.WriteLine(error);
        }
        return 1;
    }

    var configuration = BuildConfiguration(repositoryRoot, environment);
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine("DefaultConnection is missing. No import was attempted.");
        return 1;
    }

    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(connectionString)
        .Options;
    await using var dbContext = new ApplicationDbContext(options);
    var target = await new CareerCatalogDatabasePreflight(dbContext).InspectAsync();
    var guardErrors = CareerCatalogImportGuard.Validate(confirmation, target);
    if (guardErrors.Count != 0)
    {
        foreach (var error in guardErrors)
        {
            Console.Error.WriteLine(error);
        }
        return 1;
    }

    var intendedOperation = target.IsCatalogueEmpty
        ? "Transactional insert into an empty catalogue"
        : "Exact-catalogue comparison (AlreadyCurrent or reject; no replacement)";
    Console.WriteLine($"Target server: {target.ServerName}");
    Console.WriteLine($"Target database: {target.DatabaseName}");
    Console.WriteLine($"Dataset: {CareerCatalogImportGuard.RequiredDataset}");
    Console.WriteLine($"Records: {plan.Sources.Count} source, {plan.Categories.Count} categories, " +
                      $"{plan.Careers.Count} careers, {plan.Skills.Count} skills, " +
                      $"{plan.CareerSkills.Count} career-skill relationships");
    Console.WriteLine($"Intended operation: {intendedOperation}");

    var outcome = await new CareerCatalogDatabaseImporter(dbContext).ApplyAsync(plan);
    Console.WriteLine($"Import outcome: {outcome}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Career catalogue command failed: {exception.Message}");
    return 1;
}

static int RunDryRun(string datasetRoot, string? reportPath)
{
    var report = CareerCatalogDryRun.Execute(new CareerCatalogDatasetPaths(datasetRoot));
    var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);

    if (reportPath is not null)
    {
        var fullReportPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullReportPath)!);
        File.WriteAllText(fullReportPath, json + Environment.NewLine);
        Console.Error.WriteLine($"Dry-run report written to {fullReportPath}");
    }

    return report.Status == "PASS" ? 0 : 1;
}

static IConfigurationRoot BuildConfiguration(string repositoryRoot, string environment) =>
    new ConfigurationBuilder()
        .SetBasePath(Path.Combine(repositoryRoot, "SmartCareer.Web"))
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{environment}.json", optional: true)
        .AddUserSecrets(typeof(ApplicationDbContext).Assembly, optional: true)
        .AddEnvironmentVariables()
        .Build();

static string? GetOption(string[] arguments, string option)
{
    var index = Array.IndexOf(arguments, option);
    if (index < 0)
    {
        return null;
    }
    if (index == arguments.Length - 1 || arguments[index + 1].StartsWith("--", StringComparison.Ordinal))
    {
        throw new ArgumentException($"{option} requires a value.");
    }
    return arguments[index + 1];
}
