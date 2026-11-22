# ADR-0020 — Scope Human Review to Milestone Completion

## Status

Accepted in guide system v0.6.0.

## Context

The earlier human-review model risked treating approval as a persistent project-wide quality gate. If approval validity was checked against later repository state, unrelated commits could make historical approvals appear stale and require unnecessary reapproval.

The intended use is narrower: a human reviews evidence during milestone implementation when automation cannot determine acceptance.

## Decision

Human review is owned by one milestone and acts only as a completion gate for that milestone.

A completed review record is immutable historical evidence.

Later repository changes do not invalidate it.

Later milestones declare new review requirements when necessary.

`review-check` validates the reviews required by one explicit active milestone and does not revalidate historical approvals from earlier milestones.

## Consequences

- humans review uncertain evidence during the milestone where it matters;
- completed milestones do not create ongoing review maintenance;
- later commits do not force repeated approval;
- review classes remain useful for selecting evidence and reviewer expertise;
- repository revision and evidence fingerprints may be recorded for provenance without becoming future invalidation authority;
- automated validation receipts may still use staleness because they serve a different purpose.

## Rejected alternatives

### Repository-wide approval validity

Rejected because it creates perpetual maintenance and invalidates reviews after unrelated changes.

### Subject fingerprint staleness for all historical reviews

Rejected as the generic model because future work should declare a new milestone review instead of reopening historical completion evidence.

### Chat-only approval

Rejected because disconnected agents and durable milestone completion require repository-local records.
