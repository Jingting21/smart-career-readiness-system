using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;

namespace SmartCareer.Web.Tests;

public sealed class RelatedFieldReviewMigrationTests
{
    [Fact]
    public void LatestMigrationAndSnapshotContainReviewWorkflow()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=SmartCareerReviewMigrationTests;Trusted_Connection=True;").Options;
        using var context = new ApplicationDbContext(options);
        var migrations = context.GetService<IMigrationsAssembly>();
        Assert.EndsWith("_AddRelatedFieldReviewWorkflow", migrations.Migrations.Last().Key, StringComparison.Ordinal);
        Assert.NotNull(migrations.ModelSnapshot!.Model.FindEntityType(typeof(CareerDegreeFieldReviewEvent)));
        Assert.True(migrations.ModelSnapshot.Model.FindEntityType(typeof(CareerDegreeField))!
            .FindProperty(nameof(CareerDegreeField.ReviewRevision))!.IsConcurrencyToken);
    }

    [Fact]
    public void MigrationAddsOnlyReviewPersistenceAndDoesNotChangeMappingData()
    {
        var migration = ReadMigration();
        Assert.Contains("name: \"ReviewRevision\"", migration, StringComparison.Ordinal);
        Assert.Contains("name: \"CareerDegreeFieldReviewEvents\"", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("InsertData", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateData", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteData", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("DropTable(\n                name: \"CareerDegreeFields\"", migration, StringComparison.Ordinal);
    }

    private static string ReadMigration()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var root = Path.Combine(directory.FullName, "SmartCareer.Web", "Migrations");
            if (Directory.Exists(root)) return File.ReadAllText(Assert.Single(Directory.GetFiles(root, "*AddRelatedFieldReviewWorkflow.cs")));
        }
        throw new DirectoryNotFoundException();
    }
}
