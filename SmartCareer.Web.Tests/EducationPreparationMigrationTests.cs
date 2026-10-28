using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;

namespace SmartCareer.Web.Tests;

public sealed class EducationPreparationMigrationTests
{
    [Fact]
    public void LatestMigrationAndSnapshotContainEducationPreparationSchema()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SmartCareerMigrationConsistency;Trusted_Connection=True;")
            .Options;
        using var context = new ApplicationDbContext(options);

        var migrations = context.GetService<IMigrationsAssembly>();
        var snapshot = migrations.ModelSnapshot;

        Assert.NotNull(snapshot);
        Assert.EndsWith("_AddEducationAndPreparationSchema", migrations.Migrations.Last().Key, StringComparison.Ordinal);
        Assert.NotNull(snapshot!.Model.FindEntityType(typeof(CareerEducationRating)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(CareerTrainingRating)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(JobZoneDefinition)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(CareerJobZone)));
    }

    [Fact]
    public void MigrationCreatesOnlyEducationPreparationTablesAndDoesNotSeedData()
    {
        var migration = ReadMigration();

        foreach (var table in new[] { "CareerEducationRatings", "CareerTrainingRatings", "JobZoneDefinitions", "CareerJobZones" })
            Assert.Contains($"name: \"{table}\"", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("InsertData", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateData", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("DropTable(\n                name: \"Careers\"", migration, StringComparison.Ordinal);
    }

    private static string ReadMigration()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var migrations = Path.Combine(directory.FullName, "SmartCareer.Web", "Migrations");
            if (!Directory.Exists(migrations)) continue;
            var path = Assert.Single(Directory.GetFiles(migrations, "*AddEducationAndPreparationSchema.cs"));
            return File.ReadAllText(path);
        }
        throw new DirectoryNotFoundException();
    }
}
