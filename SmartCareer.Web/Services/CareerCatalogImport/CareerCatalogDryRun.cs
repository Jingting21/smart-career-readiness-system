namespace SmartCareer.Web.Services.CareerCatalogImport;

public static class CareerCatalogDryRun
{
    public static CareerCatalogDryRunReport Execute(CareerCatalogDatasetPaths paths)
    {
        // Deliberately has no DbContext or database-writer dependency. A dry run cannot write to a database.
        var plan = new OnetCareerCatalogImporter().BuildPlan(paths);
        return OnetCareerCatalogImporter.CreateDryRunReport(plan);
    }
}
