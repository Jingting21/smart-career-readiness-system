using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Services.CareerCatalogImport;

namespace SmartCareer.Web.Services.RelatedFields;

public sealed class RelatedFieldDatasetImporter(ApplicationDbContext dbContext)
{
    private static readonly HashSet<string> RelationshipTypes = ["Primary", "Related", "Alternative", "Specialised"];
    private static readonly HashSet<string> ConfidenceLevels = ["High", "Medium", "Low"];
    private static readonly HashSet<string> ReviewStatuses = ["Draft", "In Review", "Approved", "Rejected", "Needs Revalidation"];

    public async Task<RelatedFieldImportResult> ImportAsync(RelatedFieldImportPaths paths, CancellationToken cancellationToken = default)
    {
        var plan = await BuildPlanAsync(paths, cancellationToken);
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var source = await dbContext.RelatedFieldDatasetSources
            .SingleOrDefaultAsync(item => item.DatasetVersion == plan.DatasetVersion, cancellationToken);
        if (source is null)
        {
            source = new RelatedFieldDatasetSource
            {
                DatasetVersion = plan.DatasetVersion,
                MappingFileName = Path.GetFileName(paths.MappingCsvPath),
                RetrievedOn = plan.RetrievedOn,
                SourceMetadataJson = plan.MetadataJson
            };
            dbContext.RelatedFieldDatasetSources.Add(source);
        }
        else if (source.MappingFileName != Path.GetFileName(paths.MappingCsvPath) ||
                 source.RetrievedOn != plan.RetrievedOn || source.SourceMetadataJson != plan.MetadataJson)
        {
            throw new InvalidDataException($"Dataset version '{plan.DatasetVersion}' already exists with different provenance.");
        }

        var fields = await dbContext.DegreeFields.ToDictionaryAsync(
            field => $"{field.ClassificationSystem}|{field.ClassificationCode}|{field.ClassificationVersion}", cancellationToken);
        var existingMappings = await dbContext.CareerDegreeFields.Include(item => item.DegreeField).Include(item => item.Career).Include(item => item.DatasetSource)
            .ToDictionaryAsync(item => $"{item.Career.OccupationCode}|{item.DegreeField.ClassificationCode}", cancellationToken);
        var addedFields = 0;
        var addedMappings = 0;

        foreach (var row in plan.Rows)
        {
            const string system = "CIP";
            const string version = "2020";
            var fieldKey = $"{system}|{row.CipCode}|{version}";
            if (!fields.TryGetValue(fieldKey, out var field))
            {
                field = new DegreeField { CanonicalName = row.FieldName, ClassificationSystem = system, ClassificationCode = row.CipCode, ClassificationVersion = version };
                fields.Add(fieldKey, field);
                dbContext.DegreeFields.Add(field);
                addedFields++;
            }
            else if (!string.Equals(field.CanonicalName, row.FieldName, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"CIP {row.CipCode} already has a different canonical name.");
            }

            var mappingKey = $"{row.OccupationCode}|{row.CipCode}";
            if (existingMappings.TryGetValue(mappingKey, out var existing))
            {
                EnsureIdentical(existing, row, plan.DatasetVersion);
                continue;
            }

            var mapping = new CareerDegreeField
            {
                CareerId = plan.CareerIds[row.OccupationCode], DegreeField = field, DatasetSource = source,
                RelationshipType = row.RelationshipType, ConfidenceLevel = row.ConfidenceLevel,
                ReviewStatus = row.ReviewStatus, EvidenceSummary = row.EvidenceSummary,
                OriginalSocCode = row.OccupationCode[..7], OriginalCipCode = row.CipCode,
                MalaysianNecCode = row.NecCode, SourceUrls = row.SourceUrls,
                SourceVersions = row.SourceVersions, ReviewDate = row.ReviewDate,
                ProfessionalRegulationFlag = row.Regulated, ReviewerNotes = row.ReviewerNotes,
                ReviewedBy = null, ApprovedAt = null, IsPublished = false
            };
            dbContext.CareerDegreeFields.Add(mapping);
            existingMappings.Add(mappingKey, mapping);
            addedMappings++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new(plan.Rows.Count, addedFields, addedMappings, addedMappings == 0 && addedFields == 0);
    }

    private async Task<ImportPlan> BuildPlanAsync(RelatedFieldImportPaths paths, CancellationToken cancellationToken)
    {
        var metadataJson = await File.ReadAllTextAsync(paths.SourceMetadataJsonPath, cancellationToken);
        using var metadata = JsonDocument.Parse(metadataJson);
        var root = metadata.RootElement;
        var datasetVersion = Required(root, "datasetVersion");
        if (!DateOnly.TryParse(Required(root, "retrievedOn"), out var retrievedOn)) throw new InvalidDataException("Source metadata has an invalid retrievedOn date.");

        var evidence = CareerCatalogCsv.Read(paths.CipSocEvidenceCsvPath)
            .Select(row => $"{Value(row, "Soc2018Code")}|{Value(row, "Cip2020Code")}").ToHashSet(StringComparer.Ordinal);
        var rows = CareerCatalogCsv.Read(paths.MappingCsvPath).Select(ParseRow).ToList();
        var duplicate = rows.GroupBy(row => $"{row.OccupationCode}|{row.CipCode}", StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) throw new InvalidDataException($"Duplicate career-field relationship: {duplicate.Key}.");
        foreach (var row in rows)
        {
            if (!RelationshipTypes.Contains(row.RelationshipType) || !ConfidenceLevels.Contains(row.ConfidenceLevel) || !ReviewStatuses.Contains(row.ReviewStatus))
                throw new InvalidDataException($"Mapping {row.OccupationCode}/{row.CipCode} has an unsupported controlled value.");
            if (!evidence.Contains($"{row.OccupationCode[..7]}|{row.CipCode}")) throw new InvalidDataException($"Mapping {row.OccupationCode}/{row.CipCode} is absent from pinned CIP-SOC evidence.");
            if (row.ReviewStatus == "Approved") throw new InvalidDataException("Dataset import cannot create Approved mappings; approval requires a separate human-review workflow.");
            if (string.IsNullOrWhiteSpace(row.EvidenceSummary) || string.IsNullOrWhiteSpace(row.SourceUrls) || string.IsNullOrWhiteSpace(row.SourceVersions))
                throw new InvalidDataException($"Mapping {row.OccupationCode}/{row.CipCode} lacks required provenance.");
        }

        var codes = rows.Select(row => row.OccupationCode).Distinct().ToList();
        var careers = await dbContext.Careers.Where(career => codes.Contains(career.OccupationCode))
            .ToDictionaryAsync(career => career.OccupationCode, career => career.CareerId, cancellationToken);
        var missing = codes.Where(code => !careers.ContainsKey(code)).ToList();
        if (missing.Count != 0) throw new InvalidDataException($"Mapping contains career codes outside the approved catalogue: {string.Join(", ", missing)}.");
        return new(datasetVersion, retrievedOn, metadataJson, rows, careers);
    }

    private static MappingRow ParseRow(IReadOnlyDictionary<string, string> row) => new(
        Value(row, "OnetOccupationCode"), Value(row, "RelatedFieldName"), Value(row, "NecCode"), Value(row, "CipCode"),
        Value(row, "RelationshipType"), Value(row, "ConfidenceLevel"), Value(row, "ReviewStatus"), Value(row, "EvidenceSummary"),
        Value(row, "SourceUrls"), Value(row, "SourceVersions"), DateOnly.Parse(Value(row, "ReviewDate")),
        Value(row, "ProfessionalRegulationFlag") == "Yes", Value(row, "ReviewerNotes"));

    private static string Value(IReadOnlyDictionary<string, string> row, string name) =>
        row.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new InvalidDataException($"Required CSV value '{name}' is missing.");
    private static string Required(JsonElement root, string name) => root.TryGetProperty(name, out var value) && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString()! : throw new InvalidDataException($"Required metadata value '{name}' is missing.");

    private static void EnsureIdentical(CareerDegreeField existing, MappingRow row, string datasetVersion)
    {
        var identical = existing.DatasetSource.DatasetVersion == datasetVersion && existing.RelationshipType == row.RelationshipType &&
            existing.ConfidenceLevel == row.ConfidenceLevel && existing.ReviewStatus == row.ReviewStatus && existing.EvidenceSummary == row.EvidenceSummary &&
            existing.OriginalCipCode == row.CipCode && existing.MalaysianNecCode == row.NecCode && existing.SourceUrls == row.SourceUrls &&
            existing.SourceVersions == row.SourceVersions && existing.ReviewDate == row.ReviewDate && existing.ProfessionalRegulationFlag == row.Regulated &&
            existing.ReviewerNotes == row.ReviewerNotes && existing.ReviewedBy is null && existing.ApprovedAt is null && !existing.IsPublished;
        if (!identical) throw new InvalidDataException($"Existing mapping {row.OccupationCode}/{row.CipCode} differs from the import; human-reviewed data was not overwritten.");
    }

    private sealed record MappingRow(string OccupationCode, string FieldName, string NecCode, string CipCode, string RelationshipType,
        string ConfidenceLevel, string ReviewStatus, string EvidenceSummary, string SourceUrls, string SourceVersions,
        DateOnly ReviewDate, bool Regulated, string ReviewerNotes);
    private sealed record ImportPlan(string DatasetVersion, DateOnly RetrievedOn, string MetadataJson, IReadOnlyList<MappingRow> Rows, IReadOnlyDictionary<string, int> CareerIds);
}
