namespace SmartCareer.Web.Services.RelatedFields;

public sealed record RelatedFieldImportResult(int MappingCount, int AddedDegreeFields, int AddedMappings, bool WasNoOp);
