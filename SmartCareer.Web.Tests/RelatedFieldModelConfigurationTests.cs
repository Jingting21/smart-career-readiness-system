using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;

namespace SmartCareer.Web.Tests;

public sealed class RelatedFieldModelConfigurationTests
{
    private readonly IModel model = CreateModel();

    [Fact]
    public void RelationshipsKeysIndexesAndRestrictiveDeletesAreConfigured()
    {
        var field = Entity<DegreeField>();
        Assert.True(field.GetIndexes().Single(index => index.Properties.Select(p => p.Name).SequenceEqual(
            [nameof(DegreeField.ClassificationSystem), nameof(DegreeField.ClassificationCode), nameof(DegreeField.ClassificationVersion)])).IsUnique);
        var mapping = Entity<CareerDegreeField>();
        Assert.True(mapping.GetIndexes().Single(index => index.Properties.Select(p => p.Name).SequenceEqual(
            [nameof(CareerDegreeField.CareerId), nameof(CareerDegreeField.DegreeFieldId)])).IsUnique);
        Assert.All(mapping.GetForeignKeys(), foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.True(Entity<RelatedFieldDatasetSource>().GetIndexes().Single(index =>
            index.Properties.Select(p => p.Name).SequenceEqual([nameof(RelatedFieldDatasetSource.DatasetVersion)])).IsUnique);
    }

    [Fact]
    public void ControlledValuesAndPublicationGateHaveDatabaseChecks()
    {
        var checks = Entity<CareerDegreeField>().GetCheckConstraints().ToDictionary(check => check.Name!, check => check.Sql!);
        Assert.Contains("'High', 'Medium', 'Low'", checks["CK_CareerDegreeFields_ConfidenceLevel"], StringComparison.Ordinal);
        Assert.Contains("'In Review'", checks["CK_CareerDegreeFields_ReviewStatus"], StringComparison.Ordinal);
        Assert.Contains("[IsPublished] = 0", checks["CK_CareerDegreeFields_Publication"], StringComparison.Ordinal);
        Assert.Contains("[ReviewedBy] IS NOT NULL", checks["CK_CareerDegreeFields_Publication"], StringComparison.Ordinal);
    }

    private IEntityType Entity<T>() => model.FindEntityType(typeof(T))!;
    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=SmartCareerRelatedFieldModelTests;Trusted_Connection=True;").Options;
        using var context = new ApplicationDbContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }
}
