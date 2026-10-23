using System.Data;
using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Data;

namespace SmartCareer.Web.Services.CareerCatalogImport;

public sealed record CareerCatalogImportConfirmation(
    string? DatabaseName,
    string? ServerName,
    string? Dataset);

public sealed record CareerCatalogDatabasePreflightResult(
    string DataSource,
    string ServerName,
    string DatabaseName,
    IReadOnlyList<string> AppliedMigrations,
    IReadOnlyList<string> PendingMigrations,
    int Sources,
    int Categories,
    int Careers,
    int Skills,
    int CareerSkills)
{
    public bool IsCatalogueEmpty =>
        Sources == 0 && Categories == 0 && Careers == 0 && Skills == 0 && CareerSkills == 0;
}

public static class CareerCatalogImportGuard
{
    public const string RequiredDatabase = "SmartCareerDB";
    public const string RequiredServer = "DESKTOP-N7746K8";
    public const string RequiredDataset = "O*NET 31.0";
    public const string RequiredMigration = "20261023181916_AddCareerCatalogProvenanceAndReview";

    public static IReadOnlyList<string> Validate(
        CareerCatalogImportConfirmation confirmation,
        CareerCatalogDatabasePreflightResult target)
    {
        var errors = ValidateConfirmationOnly(confirmation).ToList();

        if (!string.Equals(target.DatabaseName, RequiredDatabase, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(target.DatabaseName, confirmation.DatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Resolved database '{target.DatabaseName}' is not the explicitly confirmed local development database.");
        }

        if (!string.Equals(target.ServerName, RequiredServer, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(target.ServerName, confirmation.ServerName, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Resolved SQL Server '{target.ServerName}' is not the explicitly confirmed local development server.");
        }

        var localAliases = new[] { "localhost", ".", "(local)", RequiredServer };
        if (!localAliases.Contains(target.DataSource, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Connection data source '{target.DataSource}' is not an approved local SQL Server alias.");
        }

        if (!target.AppliedMigrations.Contains(RequiredMigration, StringComparer.Ordinal) ||
            target.AppliedMigrations.LastOrDefault() != RequiredMigration)
        {
            errors.Add($"Expected latest migration '{RequiredMigration}' is not applied.");
        }

        if (target.PendingMigrations.Count != 0)
        {
            errors.Add($"The target database has {target.PendingMigrations.Count} pending migration(s).");
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateConfirmationOnly(CareerCatalogImportConfirmation confirmation)
    {
        var errors = new List<string>();
        RequireExact(confirmation.DatabaseName, RequiredDatabase, "--confirm-database", errors);
        RequireExact(confirmation.ServerName, RequiredServer, "--confirm-server", errors);
        RequireExact(confirmation.Dataset, RequiredDataset, "--confirm-dataset", errors);
        return errors;
    }

    private static void RequireExact(string? actual, string expected, string option, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(actual))
        {
            errors.Add($"Required confirmation {option} is missing.");
        }
        else if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Confirmation {option} must be exactly '{expected}'.");
        }
    }
}

public sealed class CareerCatalogDatabasePreflight(ApplicationDbContext dbContext)
{
    public async Task<CareerCatalogDatabasePreflightResult> InspectAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        string serverName;
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CAST(SERVERPROPERTY('ServerName') AS nvarchar(128));";
            serverName = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken))
                ?? throw new InvalidOperationException("SQL Server did not return its server identity.");
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        var applied = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
        var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        return new CareerCatalogDatabasePreflightResult(
            connection.DataSource,
            serverName,
            connection.Database,
            applied,
            pending,
            await dbContext.CareerCatalogSources.CountAsync(cancellationToken),
            await dbContext.CareerCategories.CountAsync(cancellationToken),
            await dbContext.Careers.CountAsync(cancellationToken),
            await dbContext.Skills.CountAsync(cancellationToken),
            await dbContext.CareerSkills.CountAsync(cancellationToken));
    }
}
