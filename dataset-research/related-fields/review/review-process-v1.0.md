# Related Fields of Study Human Review Process v1.0

## Scope

This process reviews the 53 pilot career–field mappings imported from dataset version 1.0. Review does not publish a mapping. A mapping remains hidden until a separate publication action is authorised and completed.

## Approval criteria

A reviewer may select `Approved` only when every criterion below is satisfied and documented in the review notes:

1. **Career identity verified.** The O*NET occupation code and title identify the intended SmartCareer career.
2. **Study-field relationship supported.** The CIP–SOC evidence, occupational description and other cited evidence support the stated relationship type.
3. **Malaysian context checked.** The reviewer has checked whether the broad field is meaningful in Malaysian higher education. US CIP evidence alone is insufficient.
4. **Source links valid.** The links needed for the decision open and support the evidence summary. A replacement official link must be recorded if a source has moved.
5. **NEC alignment reviewed.** The NEC-2020 code is appropriate at the stated level of specificity. Uncertain or conflicting codes require `Needs Revalidation`.
6. **Professional requirements checked.** For regulated careers, the current official regulator material has been checked and the caveat correctly separates a related study field from registration or permission to practise.
7. **No compulsory-degree implication.** The mapping describes a related field, not a universal degree, employment, licensing or registration requirement.

Choose `Rejected` when credible evidence shows that the relationship is unsuitable. Choose `Needs Revalidation` when evidence is incomplete, conflicting, outdated or cannot be verified. Do not use a blank decision to imply approval.

## Regulated-career rule

Every row marked `Professional regulation flag = Yes` requires a second independent reviewer. The second reviewer must be a different person and must record their own decision, identity, date and notes. Approval is valid only when both reviewers explicitly approve and all regulator evidence is current. A disagreement results in `Needs Revalidation`; it must not be resolved automatically.

## Proposed controlled approval command

Implement a small administrative command rather than a reviewer UI for the pilot:

1. Accept the completed workbook exported to a controlled CSV format, the expected dataset version, and an explicit `--confirm-database SmartCareerDB` argument.
2. Validate all mapping identifiers against existing `CareerDegreeField` rows and require the current database status to match the worksheet's `Current review status`.
3. Accept only `Approved`, `Rejected` or `Needs Revalidation`. Require reviewer name, review date and a substantive rationale for every nonblank decision.
4. For regulated mappings, require a distinct independent reviewer and their explicit decision, date and rationale. Never infer the second decision from the first.
5. Reject duplicate identifiers, unknown mappings, changed source evidence, blank required evidence and stale status/version values before opening the write transaction.
6. Display a dry-run summary and require an explicit confirmation token before writing.
7. In one SQL Server transaction, append an immutable review event and update only the permitted review fields. Use an optimistic concurrency token so a previously reviewed or approved row cannot be overwritten accidentally.
8. Never change `IsPublished`. The approval command must finish with approved mappings still unpublished.

## Audit trail design

A later implementation should add an append-only `CareerDegreeFieldReviewEvent` record containing:

- Career–field mapping identifier
- Dataset version and evidence fingerprint
- Previous and new review status
- Reviewer identity and timestamp
- Reviewer decision and rationale
- Independent reviewer identity, timestamp, decision and rationale when required
- Command/import correlation identifier

Review events must not be edited or deleted through the approval command. Corrections should append a new event and move the mapping to `Needs Revalidation` where appropriate.

## Separate publication workflow

Publication must remain a separate command and permission. It may select only approved mappings with complete review evidence, complete independent review where required, and unchanged source fingerprints. It should provide its own dry run, confirmation and audit event. Approval alone must never set `IsPublished = true`.

## Worksheet handling

- Yellow columns are reviewer inputs.
- Leave reviewer fields blank until a real person completes the review.
- Preserve the mapping identifier and all source/evidence columns unchanged.
- Do not replace missing MASCO, MQR or programme evidence with assumptions.
- Store the completed worksheet in an access-controlled location before running any future approval command.
