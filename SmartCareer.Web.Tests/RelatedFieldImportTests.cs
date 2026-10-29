using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Services.CareerCatalogImport;
using SmartCareer.Web.Services.RelatedFields;

namespace SmartCareer.Web.Tests;

public sealed class RelatedFieldImportTests
{
    [Fact]
    public async Task PilotImportPreservesReviewConfidenceAndProvenanceAndIsIdempotent()
    {
        await using var context = CreateContext();
        await SeedPilotCareers(context);
        var importer = new RelatedFieldDatasetImporter(context);

        var first = await importer.ImportAsync(Paths());
        var second = await importer.ImportAsync(Paths());

        Assert.Equal(53, first.MappingCount);
        Assert.Equal(53, first.AddedMappings);
        Assert.True(second.WasNoOp);
        Assert.Equal(53, await context.CareerDegreeFields.CountAsync());
        Assert.All(await context.CareerDegreeFields.ToListAsync(), mapping =>
        {
            Assert.Equal("In Review", mapping.ReviewStatus);
            Assert.Contains(mapping.ConfidenceLevel, new[] { "High", "Medium" });
            Assert.False(mapping.IsPublished);
            Assert.Null(mapping.ReviewedBy);
            Assert.Null(mapping.ApprovedAt);
            Assert.NotEmpty(mapping.EvidenceSummary);
            Assert.NotEmpty(mapping.SourceUrls);
            Assert.NotEmpty(mapping.SourceVersions);
            Assert.Matches(@"^\d{2}\.\d{4}$", mapping.OriginalCipCode);
            Assert.Matches(@"^\d{4}$", mapping.MalaysianNecCode);
        });
        Assert.Equal(47, await context.CareerDegreeFields.CountAsync(mapping => mapping.ConfidenceLevel == "High"));
        Assert.Equal(6, await context.CareerDegreeFields.CountAsync(mapping => mapping.ConfidenceLevel == "Medium"));
        Assert.Contains(await context.CareerDegreeFields.ToListAsync(), mapping => mapping.ProfessionalRegulationFlag);
        var source = Assert.Single(await context.RelatedFieldDatasetSources.ToListAsync());
        Assert.Equal("1.0", source.DatasetVersion);
        Assert.Contains("NCES-CIP-SOC-2020-2018", source.SourceMetadataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportRejectsCareersOutsideExistingApprovedCatalogueBeforeWriting()
    {
        await using var context = CreateContext();
        var importer = new RelatedFieldDatasetImporter(context);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => importer.ImportAsync(Paths()));

        Assert.Contains("outside the approved catalogue", exception.Message, StringComparison.Ordinal);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(await context.DegreeFields.ToListAsync());
        Assert.Empty(await context.CareerDegreeFields.ToListAsync());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"related-fields-{Guid.NewGuid()}").Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedPilotCareers(ApplicationDbContext context)
    {
        var rows = CareerCatalogCsv.Read(Paths().MappingCsvPath);
        var categories = rows.Select(row => row["SmartCareerCategory"]).Distinct().Select((name, index) =>
            new CareerCategory { CategoryId = index + 1, CategoryName = name, Slug = $"category-{index + 1}" }).ToDictionary(item => item.CategoryName);
        context.CareerCategories.AddRange(categories.Values);
        foreach (var row in rows.GroupBy(row => row["OnetOccupationCode"]).Select(group => group.First()))
        {
            context.Careers.Add(new Career
            {
                OccupationCode = row["OnetOccupationCode"], SourceTitle = row["CareerTitle"], Title = row["CareerTitle"],
                Category = categories[row["SmartCareerCategory"]], Description = "Test career", SourceName = "O*NET", SourceVersion = "31.0"
            });
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static RelatedFieldImportPaths Paths() => new(
        DataPath("smartcareer-related-fields-pilot-v1.0.csv"), DataPath("cip-soc-evidence-subset-v1.0.csv"), DataPath("source-metadata-v1.0.json"));

    private static string DataPath(string fileName)
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "dataset-research", "related-fields", fileName);
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException(fileName);
    }
}
