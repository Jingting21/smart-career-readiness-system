using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Controllers;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services.RelatedFields;

namespace SmartCareer.Web.Tests;

public sealed class RelatedFieldReviewServiceTests
{
    [Fact]
    public void ControllerIsAdministratorOnlyAndPostsRequireAntiforgery()
    {
        var auth = Assert.Single(typeof(AdminRelatedFieldsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(AppRoles.Administrator, auth.Roles);
        foreach (var name in new[] { "Review", "Approve", "Publish", "Unpublish" })
        {
            var method = typeof(AdminRelatedFieldsController).GetMethod(name)!;
            Assert.Single(method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true));
            Assert.Single(method.GetCustomAttributes(typeof(HttpPostAttribute), true));
        }
    }

    [Fact]
    public async Task AnonymousAndStudentActorsAreDenied()
    {
        await using var fixture = await Fixture.Create(false);
        var command = fixture.Review("Approve", "Supported by the checked evidence.");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.SubmitReviewAsync(new ClaimsPrincipal(new ClaimsIdentity()), command));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.SubmitReviewAsync(Actor("student", AppRoles.Student), command));
        Assert.Empty(fixture.Context.CareerDegreeFieldReviewEvents);
    }

    [Fact]
    public async Task ExplicitReviewRequiresRationaleAndRejectsDuplicateDecision()
    {
        await using var fixture = await Fixture.Create(false);
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.SubmitReviewAsync(Actor("admin-1"), fixture.Review("Approve", " ")));
        await fixture.Service.SubmitReviewAsync(Actor("admin-1"), fixture.Review("Approve", "Evidence and Malaysian context checked."));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.SubmitReviewAsync(Actor("admin-1"), fixture.Review("Approve", "Again", revision: 1)));
        var audit = Assert.Single(fixture.Context.CareerDegreeFieldReviewEvents);
        Assert.Equal("admin-1", audit.ActorUserId);
        Assert.Equal("Approve", audit.Decision);
        Assert.False(audit.IsIndependentReview);
        Assert.Equal("In Review", fixture.Mapping.ReviewStatus);
    }

    [Fact]
    public async Task StaleFingerprintAndRevisionAreRejected()
    {
        await using var fixture = await Fixture.Create(false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.SubmitReviewAsync(Actor("admin-1"), fixture.Review("Approve", "Checked", fingerprint: new string('0', 64))));
        await fixture.Service.SubmitReviewAsync(Actor("admin-1"), fixture.Review("Approve", "Checked"));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => fixture.Service.ApproveAsync(Actor("admin-1"), fixture.State("Approve after review", revision: 0)));
    }

    [Fact]
    public async Task NonRegulatedApprovalDoesNotPublishAndCanBePublishedThenUnpublished()
    {
        await using var fixture = await Fixture.Create(false);
        await fixture.Service.SubmitReviewAsync(Actor("admin-1"), fixture.Review("Approve", "Checked"));
        await fixture.Service.ApproveAsync(Actor("admin-1"), fixture.State("Required review complete", revision: 1));
        Assert.Equal("Approved", fixture.Mapping.ReviewStatus);
        Assert.False(fixture.Mapping.IsPublished);
        Assert.Equal("admin-1", fixture.Mapping.ReviewedBy);

        await fixture.Service.PublishAsync(Actor("admin-1"), fixture.State("Approved content is ready for publication", revision: 2));
        Assert.True(fixture.Mapping.IsPublished);
        await fixture.Service.UnpublishAsync(Actor("admin-1"), fixture.State("Temporarily withdrawn", revision: 3));
        Assert.False(fixture.Mapping.IsPublished);
        Assert.Equal(["ReviewSubmitted", "Approved", "Published", "Unpublished"], fixture.Context.CareerDegreeFieldReviewEvents.OrderBy(item => item.OccurredAt).Select(item => item.EventType));
    }

    [Fact]
    public async Task PublicationBeforeApprovalIsRejected()
    {
        await using var fixture = await Fixture.Create(false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PublishAsync(Actor("admin-1"), fixture.State("Not yet approved")));
    }

    [Fact]
    public async Task RegulatedMappingRequiresTwoDistinctReviewers()
    {
        await using var fixture = await Fixture.Create(true);
        await fixture.Service.SubmitReviewAsync(Actor("admin-1"), fixture.Review("Approve", "Primary review complete"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ApproveAsync(Actor("admin-1"), fixture.State("Too early", revision: 1)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.SubmitReviewAsync(Actor("admin-1"), fixture.Review("Approve", "Duplicate", revision: 1)));
        await fixture.Service.SubmitReviewAsync(Actor("admin-2"), fixture.Review("Approve", "Independent review complete", revision: 1));
        await fixture.Service.ApproveAsync(Actor("admin-2"), fixture.State("Both reviews complete", revision: 2));
        Assert.Equal("Approved", fixture.Mapping.ReviewStatus);
        Assert.Equal(2, fixture.Context.CareerDegreeFieldReviewEvents.Count(item => item.EventType == "ReviewSubmitted"));
        Assert.True(fixture.Context.CareerDegreeFieldReviewEvents.Single(item => item.ActorUserId == "admin-2" && item.EventType == "ReviewSubmitted").IsIndependentReview);
    }

    [Fact]
    public async Task ConflictingIndependentDecisionRequiresRevalidation()
    {
        await using var fixture = await Fixture.Create(true);
        await fixture.Service.SubmitReviewAsync(Actor("admin-1"), fixture.Review("Approve", "Primary review complete"));
        await fixture.Service.SubmitReviewAsync(Actor("admin-2"), fixture.Review("Reject", "Independent reviewer found conflicting evidence", revision: 1));
        Assert.Equal("Needs Revalidation", fixture.Mapping.ReviewStatus);
        Assert.False(fixture.Mapping.IsPublished);
    }

    [Theory]
    [InlineData("Reject", "Rejected")]
    [InlineData("Needs Revalidation", "Needs Revalidation")]
    public async Task NegativeDecisionChangesStatusAndKeepsMappingUnpublished(string decision, string expectedStatus)
    {
        await using var fixture = await Fixture.Create(false);
        await fixture.Service.SubmitReviewAsync(Actor("admin-1"), fixture.Review(decision, "Evidence does not support approval."));
        Assert.Equal(expectedStatus, fixture.Mapping.ReviewStatus);
        Assert.False(fixture.Mapping.IsPublished);
        Assert.Null(fixture.Mapping.ApprovedAt);
    }

    [Fact]
    public async Task AuditEventsCannotBeModifiedOrDeleted()
    {
        await using var fixture = await Fixture.Create(false);
        await fixture.Service.SubmitReviewAsync(Actor("admin-1"), fixture.Review("Approve", "Checked"));
        var audit = Assert.Single(fixture.Context.CareerDegreeFieldReviewEvents);
        audit.Rationale = "Changed";
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Context.SaveChangesAsync());
        fixture.Context.Entry(audit).State = EntityState.Deleted;
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Context.SaveChangesAsync());
    }

    private static ClaimsPrincipal Actor(string id, string role = AppRoles.Administrator) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, role)], "Test"));

    private sealed class Fixture : IAsyncDisposable
    {
        public required ApplicationDbContext Context { get; init; }
        public required CareerDegreeField Mapping { get; init; }
        public required RelatedFieldReviewService Service { get; init; }
        public required string Fingerprint { get; init; }

        public static async Task<Fixture> Create(bool regulated)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase($"review-{Guid.NewGuid()}").Options;
            var context = new ApplicationDbContext(options);
            var category = new CareerCategory { CategoryName = "Test", Slug = Guid.NewGuid().ToString("N") };
            var mapping = new CareerDegreeField
            {
                Career = new Career { OccupationCode = "15-1252.00", SourceTitle = "Software Developers", Title = "Software Developers", Description = "Test", SourceName = "O*NET", SourceVersion = "31.0", Category = category },
                DegreeField = new DegreeField { CanonicalName = "Software Engineering", ClassificationSystem = "CIP", ClassificationCode = "14.0903", ClassificationVersion = "2020" },
                DatasetSource = new RelatedFieldDatasetSource { DatasetVersion = "1.0", MappingFileName = "test.csv", RetrievedOn = new DateOnly(2026, 10, 29), SourceMetadataJson = "{}" },
                RelationshipType = "Primary", ConfidenceLevel = "High", ReviewStatus = "In Review", EvidenceSummary = "Checked evidence",
                OriginalSocCode = "15-1252", OriginalCipCode = "14.0903", MalaysianNecCode = "0612", SourceUrls = "https://example.test",
                SourceVersions = "test", ReviewDate = new DateOnly(2026, 10, 29), ReviewerNotes = "Caveat", ProfessionalRegulationFlag = regulated
            };
            context.CareerDegreeFields.Add(mapping);
            await context.SaveChangesAsync();
            var service = new RelatedFieldReviewService(context, TimeProvider.System);
            return new Fixture { Context = context, Mapping = mapping, Service = service, Fingerprint = RelatedFieldReviewService.ComputeEvidenceFingerprint(mapping) };
        }

        public RelatedFieldReviewCommand Review(string decision, string rationale, int? revision = null, string? fingerprint = null) =>
            new(Mapping.CareerDegreeFieldId, decision, rationale, fingerprint ?? Fingerprint, revision ?? Mapping.ReviewRevision);
        public RelatedFieldStateCommand State(string rationale, int? revision = null) =>
            new(Mapping.CareerDegreeFieldId, rationale, Fingerprint, revision ?? Mapping.ReviewRevision);
        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
