using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Data;
using SmartCareer.Web.Security;
using SmartCareer.Web.Services.RelatedFields;
using SmartCareer.Web.ViewModels;

namespace SmartCareer.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
[Route("Admin/RelatedFields")]
public sealed class AdminRelatedFieldsController(ApplicationDbContext dbContext, RelatedFieldReviewService reviewService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await dbContext.CareerDegreeFields
        .AsNoTracking().Include(item => item.Career).Include(item => item.DegreeField)
        .OrderBy(item => item.Career.Title).ThenBy(item => item.DegreeField.CanonicalName).ToListAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var mapping = await dbContext.CareerDegreeFields.AsNoTracking().Include(item => item.Career).Include(item => item.DegreeField)
            .Include(item => item.DatasetSource).Include(item => item.ReviewEvents).ThenInclude(item => item.ActorUser)
            .SingleOrDefaultAsync(item => item.CareerDegreeFieldId == id, cancellationToken);
        return mapping is null ? NotFound() : View(new RelatedFieldReviewDetailsViewModel(mapping, RelatedFieldReviewService.ComputeEvidenceFingerprint(mapping)));
    }

    [HttpPost("Review"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(RelatedFieldReviewInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await reviewService.SubmitReviewAsync(User, new(input.MappingId, input.Decision, input.Rationale, input.EvidenceFingerprint, input.ExpectedRevision), cancellationToken);
        return RedirectToAction(nameof(Details), new { id = input.MappingId });
    }

    [HttpPost("Approve"), ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(RelatedFieldStateInputModel input, CancellationToken cancellationToken) => Execute(input, reviewService.ApproveAsync, cancellationToken);

    [HttpPost("Publish"), ValidateAntiForgeryToken]
    public Task<IActionResult> Publish(RelatedFieldStateInputModel input, CancellationToken cancellationToken) => Execute(input, reviewService.PublishAsync, cancellationToken);

    [HttpPost("Unpublish"), ValidateAntiForgeryToken]
    public Task<IActionResult> Unpublish(RelatedFieldStateInputModel input, CancellationToken cancellationToken) => Execute(input, reviewService.UnpublishAsync, cancellationToken);

    private async Task<IActionResult> Execute(RelatedFieldStateInputModel input,
        Func<System.Security.Claims.ClaimsPrincipal, RelatedFieldStateCommand, CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await operation(User, new(input.MappingId, input.Rationale, input.EvidenceFingerprint, input.ExpectedRevision), cancellationToken);
        return RedirectToAction(nameof(Details), new { id = input.MappingId });
    }
}
