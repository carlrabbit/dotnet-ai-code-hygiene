# Migration — Guide System v0.5.2 to v0.6.0

## Purpose

Replace persistent or stale human-review approval semantics with milestone-scoped completion gates.

## Applies to

Repositories that adopted the v0.4.0 through v0.5.2 human-review model or implemented review commands based on it.

Repositories without human review may classify this migration as no-op.

## Required guide-repository changes

- replace `meta/HUMAN-REVIEW-MODEL.md`;
- update validation and milestone execution models;
- add ADR-0020;
- update review request and record templates;
- update human-review command guidance;
- update planning and execution prompts without abbreviating their existing operational content.

## Product-repository assessment

Inspect:

- `.review/pending/`;
- `.review/closed/`;
- `.review/records/`;
- human-review sections in active milestones;
- `docs/ENGINEERING.md`;
- review command implementation;
- `AGENTS.md` routing;
- `.guide-profile.json`.

## Required changes

When the review module is active:

- require every pending review to identify one owning milestone;
- make `review-check` validate an explicit milestone context;
- stop treating completed reviews as continuously current project approval;
- stop invalidating completed records because repository state changed;
- preserve canonical review IDs and decision history;
- preserve ephemeral interactive aliases;
- update `.guide-profile.json` guide version to `0.6.0` after migration completion.

## Conditional changes

### Legacy pending requests without a milestone

Assign each request to:

- its source milestone when unambiguous;
- a new focused migration milestone when it represents unfinished work;
- historical/closed state when the work already completed and a decision exists;
- manual review when ownership cannot be inferred safely.

### Existing approved records with stale flags

- retain the approval as historical evidence;
- remove generic stale state caused by later repository changes;
- preserve provenance fields;
- do not ask the human to reapprove merely to complete this migration.

### Existing re-review triggers

Remove generic future-change triggers from historical records.

A still-active milestone may retain evidence-refresh requirements until its completion, but those requirements end when the milestone completes.

### Review command implementation

Update:

```text
review check --milestone <id>
review list --milestone <id>
review request --milestone <id>
```

Keep canonical-ID and ephemeral-alias support.

## Deprecated

- repository-wide human approval state;
- generic `approved-but-stale` historical records;
- revalidating prior milestone approvals after later commits;
- `review-check` as a global perpetual project gate;
- reopening completed reviews because the reviewed subject later changed.

## Manual review

- classify ambiguous legacy pending requests;
- decide whether historical evidence files must be retained permanently;
- define waiver policy for blocking reviews;
- confirm which active milestones currently require review.

## No-op

- automated resumable validation receipts remain fingerprinted and may become stale;
- ephemeral review aliases remain unchanged;
- repositories without active human review require no review-system changes.

## Validation

- every pending review has one milestone owner;
- active milestones with blocking review pass only after `review-check --milestone <id>` succeeds;
- completed historical reviews are not invalidated by later commits;
- aliases remain ephemeral and canonical IDs remain durable;
- documentation and prompts describe milestone-scoped review consistently.
