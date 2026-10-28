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

        var careerTask = Entity<CareerTask>();
        Assert.Equal(nameof(CareerTask.CareerTaskId), Assert.Single(careerTask.FindPrimaryKey()!.Properties).Name);
        AssertUniqueIndex(careerTask, nameof(CareerTask.CareerId), nameof(CareerTask.OnetTaskId));

        AssertUniqueIndex(Entity<CareerEducationRating>(), nameof(CareerEducationRating.CareerId),
            nameof(CareerEducationRating.ElementId), nameof(CareerEducationRating.ScaleId), nameof(CareerEducationRating.Category));
        AssertUniqueIndex(Entity<CareerTrainingRating>(), nameof(CareerTrainingRating.CareerId),
            nameof(CareerTrainingRating.ElementId), nameof(CareerTrainingRating.ScaleId), nameof(CareerTrainingRating.Category));
        Assert.Equal(nameof(JobZoneDefinition.JobZone), Assert.Single(Entity<JobZoneDefinition>().FindPrimaryKey()!.Properties).Name);
        AssertUniqueIndex(Entity<CareerJobZone>(), nameof(CareerJobZone.CareerId));

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

        AssertProperty<CareerTask>(nameof(CareerTask.Statement), required: true, maxLength: 1000);
        AssertProperty<CareerTask>(nameof(CareerTask.TaskType), required: false, maxLength: 20);
        AssertProperty<CareerTask>(nameof(CareerTask.SourceUpdatedOn), required: false, columnType: "date");
        AssertProperty<CareerTask>(nameof(CareerTask.DomainSource), required: true, maxLength: 50);
        AssertProperty<CareerTask>(nameof(CareerTask.SourceVersion), required: true, maxLength: 20, columnType: "varchar(20)");

        AssertRatingShape<CareerEducationRating>();
        AssertRatingShape<CareerTrainingRating>();
        AssertProperty<JobZoneDefinition>(nameof(JobZoneDefinition.Name), required: true, maxLength: 100);
        AssertProperty<JobZoneDefinition>(nameof(JobZoneDefinition.Experience), required: true, maxLength: 1000);
        AssertProperty<JobZoneDefinition>(nameof(JobZoneDefinition.Education), required: true, maxLength: 1000);
        AssertProperty<JobZoneDefinition>(nameof(JobZoneDefinition.JobTraining), required: true, maxLength: 1000);
        AssertProperty<JobZoneDefinition>(nameof(JobZoneDefinition.Examples), required: true, maxLength: 1000);
        AssertProperty<JobZoneDefinition>(nameof(JobZoneDefinition.SvpRange), required: true, maxLength: 30, columnType: "varchar(30)");
        AssertProperty<JobZoneDefinition>(nameof(JobZoneDefinition.SourceVersion), required: true, maxLength: 20, columnType: "varchar(20)");
        AssertProperty<CareerJobZone>(nameof(CareerJobZone.SourceUpdatedOn), required: false, columnType: "date");
        AssertProperty<CareerJobZone>(nameof(CareerJobZone.DomainSource), required: true, maxLength: 50);
        AssertProperty<CareerJobZone>(nameof(CareerJobZone.SourceVersion), required: true, maxLength: 20, columnType: "varchar(20)");

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
    public void EducationPreparationChecksPreserveSupportedScalesAndValidRanges()
    {
        var educationSql = Entity<CareerEducationRating>().GetCheckConstraints().Select(item => item.Sql).ToList();
        Assert.Contains(educationSql, sql => sql.Contains("[ScaleId] IN ('RL', 'RQ')", StringComparison.Ordinal));
        Assert.Contains(educationSql, sql => sql.Contains("[Percentage] IS NULL", StringComparison.Ordinal));

        var trainingSql = Entity<CareerTrainingRating>().GetCheckConstraints().Select(item => item.Sql).ToList();
        Assert.Contains(trainingSql, sql => sql.Contains("[ScaleId] IN ('RW', 'PT', 'OJ')", StringComparison.Ordinal));
        Assert.Contains(trainingSql, sql => sql.Contains("[Percentage] IS NULL", StringComparison.Ordinal));

        var jobZone = Assert.Single(Entity<JobZoneDefinition>().GetCheckConstraints());
        Assert.Contains("BETWEEN 1 AND 5", jobZone.Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogRelationshipsUseRestrictiveDeleteBehavior()
    {
        AssertForeignKey<Career>(nameof(Career.CategoryId), typeof(CareerCategory));
        AssertForeignKey<CareerSkill>(nameof(CareerSkill.CareerId), typeof(Career));
        AssertForeignKey<CareerSkill>(nameof(CareerSkill.SkillId), typeof(Skill));
        AssertForeignKey<CareerTask>(nameof(CareerTask.CareerId), typeof(Career));
        AssertForeignKey<CareerEducationRating>(nameof(CareerEducationRating.CareerId), typeof(Career));
        AssertForeignKey<CareerTrainingRating>(nameof(CareerTrainingRating.CareerId), typeof(Career));
        AssertForeignKey<CareerJobZone>(nameof(CareerJobZone.CareerId), typeof(Career));
        AssertForeignKey<CareerJobZone>(nameof(CareerJobZone.JobZone), typeof(JobZoneDefinition));
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

    private void AssertRatingShape<TEntity>() where TEntity : class
    {
        AssertProperty<TEntity>("ElementId", required: true, maxLength: 20, columnType: "varchar(20)");
        AssertProperty<TEntity>("ScaleId", required: true, maxLength: 3, columnType: "varchar(3)");
        AssertProperty<TEntity>("CategoryDescription", required: true, maxLength: 500);
        AssertPrecision<TEntity>("Percentage", 6, 2, nullable: true);
        AssertPrecision<TEntity>("StandardError", 10, 4, nullable: true);
        AssertPrecision<TEntity>("LowerConfidenceBound", 10, 4, nullable: true);
        AssertPrecision<TEntity>("UpperConfidenceBound", 10, 4, nullable: true);
        AssertProperty<TEntity>("SourceUpdatedOn", required: false, columnType: "date");
        AssertProperty<TEntity>("DomainSource", required: true, maxLength: 50);
        AssertProperty<TEntity>("SourceVersion", required: true, maxLength: 20, columnType: "varchar(20)");
    }

    private void AssertPrecision<TEntity>(string name, int precision, int scale, bool nullable) where TEntity : class
    {
        var property = Entity<TEntity>().FindProperty(name)!;
        Assert.Equal(nullable, property.IsNullable);
        Assert.Equal(precision, property.GetPrecision());
        Assert.Equal(scale, property.GetScale());
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
