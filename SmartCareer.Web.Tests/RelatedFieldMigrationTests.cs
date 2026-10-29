using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;

namespace SmartCareer.Web.Tests;

public sealed class RelatedFieldMigrationTests
{
    [Fact]
    public void LatestMigrationAndSnapshotContainRelatedFieldSchema()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=SmartCareerRelatedFieldMigrationTests;Trusted_Connection=True;").Options;
        using var context = new ApplicationDbContext(options);
        var migrations = context.GetService<IMigrationsAssembly>();

        Assert.EndsWith("_AddRelatedFieldsOfStudySchema", migrations.Migrations.Last().Key, StringComparison.Ordinal);
        Assert.NotNull(migrations.ModelSnapshot!.Model.FindEntityType(typeof(DegreeField)));
        Assert.NotNull(migrations.ModelSnapshot.Model.FindEntityType(typeof(CareerDegreeField)));
        Assert.NotNull(migrations.ModelSnapshot.Model.FindEntityType(typeof(RelatedFieldDatasetSource)));
    }

    [Fact]
    public void MigrationCreatesOnlyNewTablesAndDoesNotSeedOrAlterExistingTables()
    {
        var migration = ReadMigration();
        foreach (var table in new[] { "DegreeFields", "CareerDegreeFields", "RelatedFieldDatasetSources" })
            Assert.Contains($"name: \"{table}\"", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("InsertData", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateData", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("AlterColumn", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("DropTable(\n                name: \"Careers\"", migration, StringComparison.Ordinal);
    }

    private static string ReadMigration()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var migrations = Path.Combine(directory.FullName, "SmartCareer.Web", "Migrations");
            if (!Directory.Exists(migrations)) continue;
            return File.ReadAllText(Assert.Single(Directory.GetFiles(migrations, "*AddRelatedFieldsOfStudySchema.cs")));
        }
        throw new DirectoryNotFoundException();
    }
}
