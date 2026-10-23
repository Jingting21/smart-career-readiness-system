using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Services.CareerCatalogImport;

namespace SmartCareer.Web.Tests;

public sealed class CareerCatalogImportTests
{
    private static readonly Lazy<CareerCatalogImportPlan> Plan = new(() =>
        new OnetCareerCatalogImporter().BuildPlan(GetDatasetPaths()));

    [Fact]
    public void ApprovedDatasetBuildsExpectedDeterministicPlan()
    {
        var plan = Plan.Value;

        Assert.True(plan.IsValid, string.Join(Environment.NewLine, plan.Errors));
        Assert.Equal(10, plan.Categories.Count);
        Assert.Equal(175, plan.Careers.Count);
        Assert.Equal(35, plan.Skills.Count);
        Assert.Equal(10, plan.Skills.Count(skill => skill.SkillCategory == "Essential"));
        Assert.Equal(25, plan.Skills.Count(skill => skill.SkillCategory == "Transferable"));
        Assert.Equal(6125, plan.CareerSkills.Count);
        Assert.Single(plan.Sources);
        Assert.Equal(
            plan.Careers.OrderBy(career => career.OccupationCode, StringComparer.Ordinal),
            plan.Careers);
        Assert.Equal(
            plan.Skills.OrderBy(skill => skill.ExternalSkillCode, StringComparer.Ordinal),
            plan.Skills);
        Assert.Equal(
            plan.CareerSkills.OrderBy(link => link.OccupationCode, StringComparer.Ordinal)
                .ThenBy(link => link.ExternalSkillCode, StringComparer.Ordinal),
            plan.CareerSkills);
    }

    [Fact]
    public void MappingPreservesSourceAndApprovedDisplayTitles()
    {
        var plan = Plan.Value;
        var physiotherapist = Assert.Single(
            plan.Careers, career => career.OccupationCode == "29-1123.00");

        Assert.Equal("Physical Therapists", physiotherapist.SourceTitle);
        Assert.Equal("Physiotherapists", physiotherapist.Title);
        Assert.Equal("O*NET", physiotherapist.SourceName);
        Assert.Equal("31.0", physiotherapist.SourceVersion);
        Assert.Null(physiotherapist.SourceUpdatedOn);
        Assert.Equal(21, plan.Careers.Count(career => career.SourceTitle != career.Title));
        Assert.Equal(11, plan.Careers.Count(career => career.ReviewStatus != "Approved"));
        Assert.Equal(164, plan.Careers.Count(career => !career.RequiresLocalReview && career.LocalReviewNote is null));
        Assert.Equal(11, plan.Careers.Count(career => career.RequiresLocalReview && career.LocalReviewNote == career.ReviewStatus));
    }

    [Fact]
    public void SourceRecordContainsApprovedOnetProvenance()
    {
        var source = Assert.Single(Plan.Value.Sources);

        Assert.Equal("O*NET", source.SourceName);
        Assert.Equal("31.0", source.SourceVersion);
        Assert.Equal("https://www.onetcenter.org/dl_files/database/db_31_0_json.zip", source.OfficialSourceUrl);
        Assert.Equal("Creative Commons Attribution 4.0 International (CC BY 4.0)", source.LicenseName);
        Assert.Equal("https://creativecommons.org/licenses/by/4.0/", source.LicenseUrl);
        Assert.Equal(new DateOnly(2026, 10, 24), source.RetrievedOn);
        Assert.Equal(OnetCareerCatalogImporter.Attribution, source.AttributionText);
        Assert.Empty(OnetCareerCatalogImporter.ValidateSourceRecord(source));
    }

    [Fact]
    public void ReviewMappingRejectsUnexpectedAndInconsistentStatuses()
    {
        var approved = OnetCareerCatalogImporter.ResolveReviewStatus("15-1211.00", "Approved");
        var flagged = OnetCareerCatalogImporter.ResolveReviewStatus(
            "29-9099.01",
            "Regulated profession - Malaysian midwifery requirements must be verified");

        Assert.False(approved.RequiresLocalReview);
        Assert.Null(approved.LocalReviewNote);
        Assert.True(flagged.RequiresLocalReview);
        Assert.Equal(
            "Regulated profession - Malaysian midwifery requirements must be verified",
            flagged.LocalReviewNote);
        Assert.Throws<InvalidDataException>(() =>
            OnetCareerCatalogImporter.ResolveReviewStatus("15-1211.00", "Needs review"));
        Assert.Throws<InvalidDataException>(() =>
            OnetCareerCatalogImporter.ResolveReviewStatus("29-9099.01", "Approved"));
    }

    [Fact]
    public void SourceValidationRejectsBlankAttributionInvalidUrlsAndExcessiveLengths()
    {
        var invalid = new CareerCatalogSourceImportRecord(
            new string('S', 51),
            "31.0",
            "not-a-url",
            "CC BY 4.0",
            "http://creativecommons.org/licenses/by/4.0/",
            default,
            "   ");

        var errors = OnetCareerCatalogImporter.ValidateSourceRecord(invalid);

        Assert.Contains(errors, error => error.Contains("SourceName exceeds 50", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("OfficialSourceUrl must be an absolute HTTPS URL", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("LicenseUrl must be an absolute HTTPS URL", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("AttributionText is required", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("RetrievedOn is required", StringComparison.Ordinal));
    }

    [Fact]
    public void SkillsUseOfficialElementIdsAndRawOnetRatings()
    {
        var plan = Plan.Value;
        var reading = Assert.Single(
            plan.Skills, skill => skill.ExternalSkillCode == "2.A.1.a");
        var systemsAnalystReading = Assert.Single(
            plan.CareerSkills,
            link => link.OccupationCode == "15-1211.00" && link.ExternalSkillCode == "2.A.1.a");

        Assert.Equal("Reading Comprehension", reading.SkillName);
        Assert.Equal("Essential", reading.SkillCategory);
        Assert.Equal(3.88m, systemsAnalystReading.Importance);
        Assert.Equal(4.50m, systemsAnalystReading.RequiredLevel);
        Assert.All(plan.CareerSkills, link => Assert.InRange(link.Importance, 1m, 5m));
        Assert.All(
            plan.CareerSkills.Where(link => link.RequiredLevel.HasValue),
            link => Assert.InRange(link.RequiredLevel!.Value, 0m, 7m));
        Assert.Equal(5368, plan.CareerSkills.Count(link => link.RequiredLevel.HasValue));
        Assert.Equal(757, plan.CareerSkills.Count(link => !link.RequiredLevel.HasValue));
    }

    [Fact]
    public void ProvenanceMatchesPinnedReleaseAndChecksums()
    {
        var provenance = Plan.Value.Provenance;

        Assert.Equal("O*NET Database 31.0", provenance.Dataset);
        Assert.Equal("August 2026", provenance.ReleaseDate);
        Assert.Equal(new DateOnly(2026, 10, 24), provenance.RetrievedOn);
        Assert.Equal("E88360B2D2375D15107B4F1C3DD7FEC780D792D051B37A6A81FE8F1328AF47AA", provenance.ArchiveSha256);
        Assert.Equal("609579E78F6CA96C9AC95F70A53817F830244AA5010319F5468624B7E454200A", provenance.ManifestSha256);
        Assert.Contains("used under the CC BY 4.0 licence", provenance.Attribution, StringComparison.Ordinal);
        Assert.Contains("has not approved, endorsed, or tested", provenance.Attribution, StringComparison.Ordinal);
    }

    [Fact]
    public void DryRunReportsNoDatabaseWritesAndNoRejectedRecords()
    {
        var report = CareerCatalogDryRun.Execute(GetDatasetPaths());

        Assert.Equal("PASS", report.Status);
        Assert.False(report.DatabaseWritesPerformed);
        Assert.Empty(report.RejectedRecords);
        Assert.Equal(6125, report.CareerSkills);
        Assert.Equal(11, report.ReviewFlags.Count);
        Assert.Equal(1, report.Sources);
        Assert.Equal("O*NET", report.SourceName);
        Assert.Equal("31.0", report.SourceVersion);
        Assert.Equal("Creative Commons Attribution 4.0 International (CC BY 4.0)", report.LicenseName);
        Assert.Equal("https://creativecommons.org/licenses/by/4.0/", report.LicenseUrl);
        Assert.Equal(164, report.NormalApprovedCareers);
        Assert.Equal(11, report.CareersRequiringLocalReview);
        Assert.True(report.ReviewNotesValid);
        Assert.NotEmpty(report.AttributionText);
    }

    [Fact]
    public async Task DatabaseImporterIsIdempotentAndRejectsConflictingSourceWithoutFurtherWrites()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"CareerImport-{Guid.NewGuid():N}")
            .Options;
        await using var context = new ApplicationDbContext(options);
        var importer = new CareerCatalogDatabaseImporter(context);

        Assert.Equal(CareerCatalogDatabaseImportOutcome.Imported, await importer.ApplyAsync(Plan.Value));
        Assert.Equal(1, await context.CareerCatalogSources.CountAsync());
        Assert.Equal(10, await context.CareerCategories.CountAsync());
        Assert.Equal(175, await context.Careers.CountAsync());
        Assert.Equal(35, await context.Skills.CountAsync());
        Assert.Equal(6125, await context.CareerSkills.CountAsync());
        Assert.Equal(CareerCatalogDatabaseImportOutcome.AlreadyCurrent, await importer.ApplyAsync(Plan.Value));

        var source = await context.CareerCatalogSources.SingleAsync();
        source.LicenseName = "Conflicting licence";
        await context.SaveChangesAsync();
        var countsBeforeRejectedImport = new[]
        {
            await context.CareerCatalogSources.CountAsync(),
            await context.CareerCategories.CountAsync(),
            await context.Careers.CountAsync(),
            await context.Skills.CountAsync(),
            await context.CareerSkills.CountAsync()
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ApplyAsync(Plan.Value));

        Assert.Equal(countsBeforeRejectedImport[0], await context.CareerCatalogSources.CountAsync());
        Assert.Equal(countsBeforeRejectedImport[1], await context.CareerCategories.CountAsync());
        Assert.Equal(countsBeforeRejectedImport[2], await context.Careers.CountAsync());
        Assert.Equal(countsBeforeRejectedImport[3], await context.Skills.CountAsync());
        Assert.Equal(countsBeforeRejectedImport[4], await context.CareerSkills.CountAsync());

        source.LicenseName = "Creative Commons Attribution 4.0 International (CC BY 4.0)";
        var career = await context.Careers.SingleAsync(entity => entity.OccupationCode == "15-1211.00");
        career.RequiresLocalReview = true;
        career.LocalReviewNote = "Conflicting review state";
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ApplyAsync(Plan.Value));
        Assert.Equal(175, await context.Careers.CountAsync());
        Assert.Equal(6125, await context.CareerSkills.CountAsync());
    }

    [Theory]
    [InlineData(null, "DESKTOP-N7746K8", "O*NET 31.0")]
    [InlineData("SmartCareerDB", null, "O*NET 31.0")]
    [InlineData("SmartCareerDB", "DESKTOP-N7746K8", null)]
    public void ImportGuardRejectsMissingConfirmations(string? database, string? server, string? dataset)
    {
        var errors = CareerCatalogImportGuard.ValidateConfirmationOnly(
            new CareerCatalogImportConfirmation(database, server, dataset));

        Assert.NotEmpty(errors);
    }

    [Theory]
    [InlineData("ProductionDB", "DESKTOP-N7746K8", "O*NET 31.0")]
    [InlineData("SmartCareerDB", "REMOTE-SQL", "O*NET 31.0")]
    [InlineData("SmartCareerDB", "DESKTOP-N7746K8", "O*NET 30.2")]
    public void ImportGuardRejectsWrongConfirmationValues(string database, string server, string dataset)
    {
        var errors = CareerCatalogImportGuard.ValidateConfirmationOnly(
            new CareerCatalogImportConfirmation(database, server, dataset));

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ImportGuardRejectsResolvedTargetMismatchAndPendingMigration()
    {
        var confirmation = ValidConfirmation();
        var wrongDatabase = ValidTarget() with { DatabaseName = "OtherDatabase" };
        var wrongServer = ValidTarget() with { ServerName = "REMOTE-SQL", DataSource = "REMOTE-SQL" };
        var pending = ValidTarget() with { PendingMigrations = ["PendingMigration"] };

        Assert.NotEmpty(CareerCatalogImportGuard.Validate(confirmation, wrongDatabase));
        Assert.NotEmpty(CareerCatalogImportGuard.Validate(confirmation, wrongServer));
        Assert.NotEmpty(CareerCatalogImportGuard.Validate(confirmation, pending));
        Assert.Empty(CareerCatalogImportGuard.Validate(confirmation, ValidTarget()));
    }

    [Fact]
    public async Task DatabaseImporterRejectsPartialCatalogueWithoutAddingRecords()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"CareerPartial-{Guid.NewGuid():N}")
            .Options;
        await using var context = new ApplicationDbContext(options);
        context.CareerCategories.Add(new CareerCategory
        {
            CategoryName = "Partial",
            Slug = "partial"
        });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CareerCatalogDatabaseImporter(context).ApplyAsync(Plan.Value));

        Assert.Equal(1, await context.CareerCategories.CountAsync());
        Assert.Equal(0, await context.Careers.CountAsync());
        Assert.Equal(0, await context.CareerCatalogSources.CountAsync());
    }

    [Fact]
    public async Task DatabaseImporterFailurePersistsNoPartialCatalogue()
    {
        var databaseName = $"CareerRollback-{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        await using (var failingContext = new FailingSaveApplicationDbContext(options))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new CareerCatalogDatabaseImporter(failingContext).ApplyAsync(Plan.Value));
        }

        await using var verificationContext = new ApplicationDbContext(options);
        Assert.Equal(0, await verificationContext.CareerCatalogSources.CountAsync());
        Assert.Equal(0, await verificationContext.CareerCategories.CountAsync());
        Assert.Equal(0, await verificationContext.Careers.CountAsync());
        Assert.Equal(0, await verificationContext.Skills.CountAsync());
        Assert.Equal(0, await verificationContext.CareerSkills.CountAsync());
    }

    [Fact]
    public void CsvParserHandlesQuotedCommasAndEscapedQuotes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"smartcareer-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, "Code,Title\r\n\"1\",\"Analysts, \"\"Specialist\"\"\"\r\n");

            var row = Assert.Single(CareerCatalogCsv.Read(path));

            Assert.Equal("1", row["Code"]);
            Assert.Equal("Analysts, \"Specialist\"", row["Title"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ValidationRejectsMissingSourceFilesWithoutProducingRecords()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"smartcareer-missing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var plan = new OnetCareerCatalogImporter().BuildPlan(new CareerCatalogDatasetPaths(directory));

            Assert.False(plan.IsValid);
            Assert.NotEmpty(plan.Errors);
            Assert.Empty(plan.Careers);
            Assert.Empty(plan.CareerSkills);
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    private static CareerCatalogDatasetPaths GetDatasetPaths() =>
        new(Path.Combine(FindRepositoryRoot(), "dataset-research", "onet-31.0"));

    private static CareerCatalogImportConfirmation ValidConfirmation() =>
        new("SmartCareerDB", "DESKTOP-N7746K8", "O*NET 31.0");

    private static CareerCatalogDatabasePreflightResult ValidTarget() =>
        new(
            "localhost",
            "DESKTOP-N7746K8",
            "SmartCareerDB",
            ["20261023181916_AddCareerCatalogProvenanceAndReview"],
            [],
            0, 0, 0, 0, 0);

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SmartCareer.Web", "Program.cs")) &&
                File.Exists(Path.Combine(directory.FullName, "SmartCareer.Web", "SmartCareer.Web.csproj")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the SmartCareer repository root.");
    }

    private sealed class FailingSaveApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated persistence failure.");
    }
}
