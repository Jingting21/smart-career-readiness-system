using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;

namespace SmartCareer.Web.Services.RelatedFields;

public sealed record RelatedFieldReviewCommand(int MappingId, string Decision, string Rationale, string ExpectedEvidenceFingerprint, int ExpectedRevision);
public sealed record RelatedFieldStateCommand(int MappingId, string Rationale, string ExpectedEvidenceFingerprint, int ExpectedRevision);

public sealed class RelatedFieldReviewService(ApplicationDbContext dbContext, TimeProvider timeProvider)
{
    public async Task SubmitReviewAsync(ClaimsPrincipal actor, RelatedFieldReviewCommand command, CancellationToken cancellationToken = default)
    {
        var userId = RequireAdministrator(actor);
        var decision = command.Decision?.Trim();
        if (decision is not ("Approve" or "Reject" or "Needs Revalidation")) throw new ArgumentException("An explicit supported review decision is required.");
        var rationale = RequireRationale(command.Rationale);
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var mapping = await LoadMappingAsync(command.MappingId, cancellationToken);
        ValidateExpected(mapping, command.ExpectedEvidenceFingerprint, command.ExpectedRevision);
        if (mapping.ReviewStatus != "In Review") throw new InvalidOperationException("Only mappings currently In Review can receive a review decision.");

        var prior = mapping.ReviewEvents.Where(item => item.EventType == "ReviewSubmitted" && item.EvidenceFingerprint == command.ExpectedEvidenceFingerprint).ToList();
        if (prior.Any(item => item.ActorUserId == userId)) throw new InvalidOperationException("This administrator has already reviewed the current evidence.");
        if (!mapping.ProfessionalRegulationFlag && prior.Count != 0) throw new InvalidOperationException("A non-regulated mapping already has a review decision.");
        if (mapping.ProfessionalRegulationFlag && prior.Count >= 2) throw new InvalidOperationException("Both independent review positions are already filled.");

        var previousStatus = mapping.ReviewStatus;
        var previousPublished = mapping.IsPublished;
        if (mapping.ProfessionalRegulationFlag && prior.Count == 1 && prior[0].Decision != decision)
            SetUnpublishedStatus(mapping, "Needs Revalidation");
        else if (decision == "Reject") SetUnpublishedStatus(mapping, "Rejected");
        else if (decision == "Needs Revalidation") SetUnpublishedStatus(mapping, "Needs Revalidation");
        mapping.ReviewRevision++;
        AddEvent(mapping, userId, "ReviewSubmitted", decision, prior.Count == 1, rationale, command.ExpectedEvidenceFingerprint, previousStatus, previousPublished);
        await SaveAsync(transaction, cancellationToken);
    }

    public async Task ApproveAsync(ClaimsPrincipal actor, RelatedFieldStateCommand command, CancellationToken cancellationToken = default)
    {
        var userId = RequireAdministrator(actor);
        var rationale = RequireRationale(command.Rationale);
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var mapping = await LoadMappingAsync(command.MappingId, cancellationToken);
        ValidateExpected(mapping, command.ExpectedEvidenceFingerprint, command.ExpectedRevision);
        if (mapping.ReviewStatus != "In Review" || mapping.IsPublished) throw new InvalidOperationException("The mapping is not eligible for approval.");
        var approvals = mapping.ReviewEvents.Where(item => item.EventType == "ReviewSubmitted" && item.Decision == "Approve" && item.EvidenceFingerprint == command.ExpectedEvidenceFingerprint).ToList();
        var required = mapping.ProfessionalRegulationFlag ? 2 : 1;
        if (approvals.Select(item => item.ActorUserId).Distinct(StringComparer.Ordinal).Count() < required)
            throw new InvalidOperationException("The required distinct reviewer approvals are not complete.");

        var previous = mapping.ReviewStatus;
        mapping.ReviewStatus = "Approved";
        mapping.ReviewedBy = approvals.OrderBy(item => item.OccurredAt).First().ActorUserId;
        mapping.ApprovedAt = timeProvider.GetUtcNow().UtcDateTime;
        mapping.ReviewRevision++;
        AddEvent(mapping, userId, "Approved", null, false, rationale, command.ExpectedEvidenceFingerprint, previous, false);
        await SaveAsync(transaction, cancellationToken);
    }

    public Task PublishAsync(ClaimsPrincipal actor, RelatedFieldStateCommand command, CancellationToken cancellationToken = default) =>
        ChangePublicationAsync(actor, command, true, cancellationToken);

    public Task UnpublishAsync(ClaimsPrincipal actor, RelatedFieldStateCommand command, CancellationToken cancellationToken = default) =>
        ChangePublicationAsync(actor, command, false, cancellationToken);

    public static string ComputeEvidenceFingerprint(CareerDegreeField mapping)
    {
        var payload = string.Join('\n', mapping.OriginalSocCode, mapping.OriginalCipCode, mapping.MalaysianNecCode,
            mapping.RelationshipType, mapping.EvidenceSummary, mapping.SourceUrls, mapping.SourceVersions,
            mapping.DatasetSource.DatasetVersion, mapping.ProfessionalRegulationFlag.ToString());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private async Task ChangePublicationAsync(ClaimsPrincipal actor, RelatedFieldStateCommand command, bool publish, CancellationToken cancellationToken)
    {
        var userId = RequireAdministrator(actor);
        var rationale = RequireRationale(command.Rationale);
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var mapping = await LoadMappingAsync(command.MappingId, cancellationToken);
        ValidateExpected(mapping, command.ExpectedEvidenceFingerprint, command.ExpectedRevision);
        if (mapping.ReviewStatus != "Approved") throw new InvalidOperationException("Only approved mappings can change publication state.");
        if (mapping.IsPublished == publish) throw new InvalidOperationException(publish ? "The mapping is already published." : "The mapping is already unpublished.");
        var previousPublished = mapping.IsPublished;
        mapping.IsPublished = publish;
        mapping.ReviewRevision++;
        AddEvent(mapping, userId, publish ? "Published" : "Unpublished", null, false, rationale,
            command.ExpectedEvidenceFingerprint, mapping.ReviewStatus, previousPublished);
        await SaveAsync(transaction, cancellationToken);
    }

    private async Task<CareerDegreeField> LoadMappingAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.CareerDegreeFields.Include(item => item.DatasetSource).Include(item => item.ReviewEvents)
            .SingleOrDefaultAsync(item => item.CareerDegreeFieldId == id, cancellationToken)
        ?? throw new KeyNotFoundException("Related-field mapping was not found.");

    private static void ValidateExpected(CareerDegreeField mapping, string fingerprint, int revision)
    {
        if (mapping.ReviewRevision != revision) throw new DbUpdateConcurrencyException("The mapping changed after it was loaded.");
        if (fingerprint?.Length != 64 || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(ComputeEvidenceFingerprint(mapping)), Encoding.ASCII.GetBytes(fingerprint)))
            throw new InvalidOperationException("The evidence fingerprint is stale or invalid.");
    }

    private void AddEvent(CareerDegreeField mapping, string userId, string eventType, string? decision, bool independent,
        string rationale, string fingerprint, string previousStatus, bool previousPublished) =>
        mapping.ReviewEvents.Add(new CareerDegreeFieldReviewEvent
        {
            ActorUserId = userId, EventType = eventType, Decision = decision, IsIndependentReview = independent,
            Rationale = rationale, EvidenceFingerprint = fingerprint, PreviousReviewStatus = previousStatus,
            NewReviewStatus = mapping.ReviewStatus, PreviousIsPublished = previousPublished, NewIsPublished = mapping.IsPublished,
            OccurredAt = timeProvider.GetUtcNow().UtcDateTime, CorrelationId = Guid.NewGuid()
        });

    private static void SetUnpublishedStatus(CareerDegreeField mapping, string status)
    {
        mapping.ReviewStatus = status;
        mapping.IsPublished = false;
        mapping.ReviewedBy = null;
        mapping.ApprovedAt = null;
    }

    private static string RequireAdministrator(ClaimsPrincipal actor)
    {
        if (actor.Identity?.IsAuthenticated != true || !actor.IsInRole(AppRoles.Administrator)) throw new UnauthorizedAccessException("Administrator authentication is required.");
        return actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("The authenticated administrator has no Identity user identifier.");
    }

    private static string RequireRationale(string rationale) =>
        !string.IsNullOrWhiteSpace(rationale) ? rationale.Trim() : throw new ArgumentException("A non-empty review rationale is required.");

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken) =>
        dbContext.Database.IsRelational() ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;

    private async Task SaveAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }
}
