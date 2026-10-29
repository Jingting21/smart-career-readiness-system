using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Data;
using SmartCareer.Web.Services.RelatedFields;

var options = Parse(args);
if (options.GetValueOrDefault("confirm-database") != "SmartCareerDB" ||
    options.GetValueOrDefault("confirm-dataset-version") != "1.0")
{
    Console.Error.WriteLine("Import refused. Confirm --confirm-database SmartCareerDB and --confirm-dataset-version 1.0.");
    return 2;
}

var datasetRoot = Required(options, "dataset-root");
var connection = Required(options, "connection");
var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
await using var context = new ApplicationDbContext(contextOptions);
if (!string.Equals(context.Database.GetDbConnection().Database, "SmartCareerDB", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Import refused: resolved database is not SmartCareerDB.");
    return 2;
}

var importer = new RelatedFieldDatasetImporter(context);
var result = await importer.ImportAsync(new RelatedFieldImportPaths(
    Path.Combine(datasetRoot, "smartcareer-related-fields-pilot-v1.0.csv"),
    Path.Combine(datasetRoot, "cip-soc-evidence-subset-v1.0.csv"),
    Path.Combine(datasetRoot, "source-metadata-v1.0.json")));

Console.WriteLine(result.WasNoOp
    ? $"AlreadyCurrent: {result.MappingCount} mappings validated; no rows changed."
    : $"Imported: mappings={result.AddedMappings}, degreeFields={result.AddedDegreeFields}, validated={result.MappingCount}.");
return 0;

static Dictionary<string, string> Parse(string[] arguments)
{
    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var index = 0; index < arguments.Length; index += 2)
    {
        if (index + 1 >= arguments.Length || !arguments[index].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException("Arguments must be supplied as --name value pairs.");
        values[arguments[index][2..]] = arguments[index + 1];
    }
    return values;
}

static string Required(IReadOnlyDictionary<string, string> values, string name) =>
    values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : throw new ArgumentException($"Missing required --{name} argument.");
