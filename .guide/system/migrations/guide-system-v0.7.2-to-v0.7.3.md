# Guide System Migration — v0.7.2 to v0.7.3

## Purpose

Version 0.7.3 strengthens long-running implementation reliability without adding a new lifecycle phase or new engineering commands.

The semantic planning contract remains the ready milestone plus referenced project authority. The main change is that AI implementation now externalizes execution coverage and progress instead of relying on conversational memory until the final completion audit.

## Required Guide Updates

Adopt the v0.7.3 versions of:

- `templates/prompts/plan-milestone.md`;
- `templates/prompts/execute-milestone.md`;
- `templates/PROMPTS.md`;
- `templates/milestones/milestone-template.md`;
- `templates/milestones/execution-ledger-template.md`;
- `meta/MILESTONE-EXECUTION-MODEL.md`;
- `decisions/ADR-0007-generate-execution-prompts.md`;
- `decisions/ADR-0019-persist-execution-ledgers.md`;
- guide-profile version metadata.

## Execution Change

For AI-executed coding milestones, implementation now begins with:

```text
read milestone and authority
-> execution decomposition
-> create/reconcile .execution/<milestone-id>.md
-> implement/validate/update ledger by work package
```

Before `COMPLETE`, the executor must:

1. freshly reread the primary milestone from disk;
2. reconcile every applicable acceptance criterion and completion obligation against the ledger;
3. verify ledger claims against live repository state and concrete evidence;
4. confirm required validation is current;
5. continue fixing every agent-resolvable gap;
6. perform the existing completion audit and use the existing terminal outcomes.

The ledger is operational state only. It must not alter or reinterpret milestone authority.

## Existing Ready Milestones

Do not replan an existing ready milestone merely to adopt v0.7.3 when its project-level decisions are already settled.

Before implementation, create the execution ledger from the current milestone and live repository.

For a milestone already in progress, reconstruct the ledger from:

- the ready milestone;
- current repository state;
- current validation/evidence;
- known remaining work.

Do not mark items `done` merely because an earlier agent message claimed completion. Re-establish evidence from the repository.

## Planning Change

Planning must now check both:

- decision completeness; and
- execution tractability for the configured baseline implementation model.

Large coherent milestones may remain single milestones. They do not need to be split merely because implementation is large, provided their obligations can be decomposed by the executor into bounded work packages with observable completion evidence.

Planning still must not prescribe exhaustive implementation task lists. The executor derives the concrete work packages from the live repository.

## Handoff Change

Stop generating bespoke per-milestone `EXECUTE-Mxxx.md` or equivalent execution documents that duplicate canonical methodology, milestone acceptance criteria, or referenced project authority.

The normal disconnected handoff should contain only what transport requires, typically:

- overlay/archive application instructions when applicable;
- the primary ready-milestone path;
- a reminder to use `templates/prompts/execute-milestone.md`.

Existing bespoke execution files may remain as historical artifacts. Do not treat them as current methodology. If such a file is only redundant current handoff material and repository policy permits cleanup, it may be removed.

## No New Engineering Commands

v0.7.3 does not add milestone, ledger, checklist, or completion commands.

The ledger is maintained directly by the executor as repository-local Markdown.

## Guide Profile

Update:

```json
"guideSystem": {
  "version": "0.7.3"
}
```

and the applied base profile version to `0.7.3`.

## Completion Check

Migration is complete when:

- the repository points to guide system v0.7.3;
- normal planning no longer emits bespoke execution methodology files;
- AI-executed milestones create/reconcile `.execution/<milestone-id>.md` before production edits;
- execution updates the ledger as coherent work packages complete;
- `COMPLETE` requires a fresh milestone reread and milestone-to-ledger-to-repository/evidence reconciliation;
- the existing `COMPLETE`, `AWAITING HUMAN REVIEW`, and `BLOCKED` semantics remain intact;
- no new generic engineering commands were introduced solely for this migration.
