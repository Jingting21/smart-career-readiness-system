using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;

namespace SmartCareer.Web.Tests;

public sealed class CareerCatalogModelConfigurationTests
{
    private readonly IModel model = CreateModel();

    [Fact]
    public void CatalogEntitiesHaveExpectedKeysAndIndexes()
    {
        var category = Entity<CareerCategory>();
        Assert.Equal(nameof(CareerCategory.CategoryId), Assert.Single(category.FindPrimaryKey()!.Properties).Name);
        AssertUniqueIndex(category, nameof(CareerCategory.CategoryName));
        AssertUniqueIndex(category, nameof(CareerCategory.Slug));

        var career = Entity<Career>();
        Assert.Equal(nameof(Career.CareerId), Assert.Single(career.FindPrimaryKey()!.Properties).Name);
        AssertUniqueIndex(career, nameof(Career.OccupationCode));

        var skill = Entity<Skill>();
        Assert.Equal(nameof(Skill.SkillId), Assert.Single(skill.FindPrimaryKey()!.Properties).Name);
        AssertUniqueIndex(skill, nameof(Skill.ExternalSkillCode));

        var careerSkill = Entity<CareerSkill>();
        Assert.Equal(
            [nameof(CareerSkill.CareerId), nameof(CareerSkill.SkillId)],
            careerSkill.FindPrimaryKey()!.Properties.Select(property => property.Name));

        var source = Entity<CareerCatalogSource>();
        Assert.Equal(
            nameof(CareerCatalogSource.CareerCatalogSourceId),
            Assert.Single(source.FindPrimaryKey()!.Properties).Name);
        AssertUniqueIndex(
            source,
            nameof(CareerCatalogSource.SourceName),
            nameof(CareerCatalogSource.SourceVersion));
    }

    [Fact]
    public void CatalogPropertiesHaveExpectedSqlServerShape()
    {
        AssertProperty<CareerCategory>(nameof(CareerCategory.CategoryName), required: true, maxLength: 100);
        AssertProperty<CareerCategory>(nameof(CareerCategory.Slug), required: true, maxLength: 100, columnType: "varchar(100)");
        AssertProperty<CareerCategory>(nameof(CareerCategory.Description), required: false, maxLength: 500);

        AssertProperty<Career>(nameof(Career.OccupationCode), required: true, maxLength: 10, columnType: "varchar(10)");
        AssertProperty<Career>(nameof(Career.SourceTitle), required: true, maxLength: 150, columnType: "nvarchar(150)");
        AssertProperty<Career>(nameof(Career.Title), required: true, maxLength: 150);
        AssertProperty<Career>(nameof(Career.Description), required: true, maxLength: 1000);
        AssertProperty<Career>(nameof(Career.Responsibilities), required: false, columnType: "nvarchar(max)");
        AssertProperty<Career>(nameof(Career.Qualifications), required: false, columnType: "nvarchar(max)");
        AssertProperty<Career>(nameof(Career.SourceName), required: true, maxLength: 50);
        AssertProperty<Career>(nameof(Career.SourceVersion), required: true, maxLength: 20, columnType: "varchar(20)");
        AssertProperty<Career>(nameof(Career.SourceUpdatedOn), required: false, columnType: "date");
        AssertProperty<Career>(nameof(Career.RequiresLocalReview), required: true, columnType: "bit");
        Assert.Equal(false, Entity<Career>().FindProperty(nameof(Career.RequiresLocalReview))!.GetDefaultValue());
        AssertProperty<Career>(nameof(Career.LocalReviewNote), required: false, maxLength: 500, columnType: "nvarchar(500)");
        AssertProperty<Career>(nameof(Career.CreatedAt), required: true, columnType: "datetime2");
        AssertProperty<Career>(nameof(Career.UpdatedAt), required: true, columnType: "datetime2");

        AssertProperty<Skill>(nameof(Skill.ExternalSkillCode), required: true, maxLength: 20, columnType: "varchar(20)");
        AssertProperty<Skill>(nameof(Skill.SkillName), required: true, maxLength: 150);
        AssertProperty<Skill>(nameof(Skill.SkillCategory), required: true, maxLength: 100);
        AssertProperty<Skill>(nameof(Skill.Description), required: false, maxLength: 1000);

        var importance = Entity<CareerSkill>().FindProperty(nameof(CareerSkill.Importance))!;
        Assert.Equal(4, importance.GetPrecision());
        Assert.Equal(2, importance.GetScale());
        var requiredLevel = Entity<CareerSkill>().FindProperty(nameof(CareerSkill.RequiredLevel))!;
        Assert.True(requiredLevel.IsNullable);
        Assert.Equal(4, requiredLevel.GetPrecision());
        Assert.Equal(2, requiredLevel.GetScale());
        AssertProperty<CareerSkill>(nameof(CareerSkill.SourceVersion), required: true, maxLength: 20, columnType: "varchar(20)");

        AssertProperty<CareerCatalogSource>(nameof(CareerCatalogSource.SourceName), required: true, maxLength: 50);
        AssertProperty<CareerCatalogSource>(nameof(CareerCatalogSource.SourceVersion), required: true, maxLength: 20, columnType: "varchar(20)");
        AssertProperty<CareerCatalogSource>(nameof(CareerCatalogSource.OfficialSourceUrl), required: true, maxLength: 500, columnType: "varchar(500)");
        AssertProperty<CareerCatalogSource>(nameof(CareerCatalogSource.LicenseName), required: true, maxLength: 100);
        AssertProperty<CareerCatalogSource>(nameof(CareerCatalogSource.LicenseUrl), required: true, maxLength: 500, columnType: "varchar(500)");
        AssertProperty<CareerCatalogSource>(nameof(CareerCatalogSource.RetrievedOn), required: true, columnType: "date");
        AssertProperty<CareerCatalogSource>(nameof(CareerCatalogSource.AttributionText), required: true, maxLength: 1000);
    }

    [Fact]
    public void CareerLocalReviewConstraintRequiresConsistentFlagAndNote()
    {
        var constraint = Assert.Single(Entity<Career>().GetCheckConstraints());

        Assert.Equal("CK_Careers_LocalReview", constraint.Name);
        Assert.Contains("[RequiresLocalReview] = 0 AND [LocalReviewNote] IS NULL", constraint.Sql, StringComparison.Ordinal);
        Assert.Contains("[RequiresLocalReview] = 1", constraint.Sql, StringComparison.Ordinal);
        Assert.Contains("LEN(LTRIM(RTRIM", constraint.Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogRelationshipsUseRestrictiveDeleteBehavior()
    {
        AssertForeignKey<Career>(nameof(Career.CategoryId), typeof(CareerCategory));
        AssertForeignKey<CareerSkill>(nameof(CareerSkill.CareerId), typeof(Career));
        AssertForeignKey<CareerSkill>(nameof(CareerSkill.SkillId), typeof(Skill));
        Assert.Empty(Entity<CareerCatalogSource>().GetForeignKeys());
    }

    [Fact]
    public void IdentityEntitiesRemainInTheModel()
    {
        Assert.NotNull(model.FindEntityType(typeof(ApplicationUser)));
        Assert.NotNull(model.FindEntityType("Microsoft.AspNetCore.Identity.IdentityRole"));
        Assert.NotNull(model.FindEntityType("Microsoft.AspNetCore.Identity.IdentityUserRole<string>"));
    }

    private IEntityType Entity<TEntity>() =>
        model.FindEntityType(typeof(TEntity)) ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is missing from the model.");

    private void AssertProperty<TEntity>(string name, bool required, int? maxLength = null, string? columnType = null)
    {
        var property = Entity<TEntity>().FindProperty(name)!;
        Assert.Equal(!required, property.IsNullable);
        if (maxLength.HasValue) Assert.Equal(maxLength, property.GetMaxLength());
        if (columnType is not null) Assert.Equal(columnType, property.GetColumnType());
    }

    private static void AssertUniqueIndex(IEntityType entity, params string[] propertyNames)
    {
        var index = entity.GetIndexes().Single(candidate =>
            candidate.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
        Assert.True(index.IsUnique);
    }

    private void AssertForeignKey<TEntity>(string propertyName, Type principalType)
    {
        var foreignKey = Entity<TEntity>().GetForeignKeys().Single(candidate =>
            candidate.Properties.Select(property => property.Name).SequenceEqual([propertyName]));
        Assert.Equal(principalType, foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SmartCareerModelTests;Trusted_Connection=True;")
            .Options;
        using var context = new ApplicationDbContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }
}
