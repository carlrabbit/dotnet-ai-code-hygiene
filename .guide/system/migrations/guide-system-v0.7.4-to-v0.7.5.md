# Guide System Migration — v0.7.4 to v0.7.5

## Purpose

Version 0.7.5 adds an explicit decision-preservation invariant at the planning-to-implementation boundary.

Planning may compress investigation and reasoning, but it must not compress a resolved material decision into a weaker requirement that forces the implementation agent to rediscover or reinterpret project-level policy.

## Required Guide Updates

Adopt:

- `decisions/ADR-0020-preserve-planning-decisions.md` as the decision-preservation authority;
- the v0.7.5 milestone template, including its `Decision Preservation` readiness section;
- guide-profile version metadata.

The canonical execution-ledger and consumer-surface validation rules remain unchanged.

## Decision-Preservation Rule

Before a milestone becomes `ready`, verify both:

```text
decision completeness
+ decision preservation
```

Decision completeness means all material project-level decisions required for implementation have been made.

Decision preservation means every material decision already made during planning survives in the ready milestone or referenced project authority with enough precision that the baseline executor does not need to repeat the same project-level reasoning.

## Authority Placement

Do not make milestones self-contained by copying all project authority into them.

Use:

```text
project-wide durable decision
  -> appropriate spec / ADR / architecture / terminology authority
  -> milestone references it

milestone-specific decision
  -> milestone
```

Preserve negative requirements and decision-enabling rationale when they are necessary to prevent plausible semantic reinterpretation or to let the executor apply the decision consistently to implementation surfaces discovered later.

Do not preserve exploratory analysis, rejected alternatives, or speculative implementation mechanics unless their omission would lose a material decision or constraint.

## Existing Ready Milestones

Do not automatically replan existing ready milestones.

Before implementation or when resuming an active milestone, inspect whether known planning decisions exist only in planning conversation/history or were weakened during package construction.

If a material resolved decision is missing from durable project truth, return the milestone to planning long enough to record that decision in the appropriate authority and restore `ready` status.

Do not use the execution ledger to repair missing planning authority. `.execution/<milestone-id>.md` remains implementation-owned operational state.

## Planning Audit

Before `ready`, apply this counterfactual:

> Assume the planning conversation and planning model are unavailable. Can the configured baseline executor recover every material decision already made during planning from the milestone and referenced project authority, and implement the decided target state without repeating project-level reasoning?

If no, the milestone is not ready.

## Guide Profile

Update the guide-system and applied base profile versions to `0.7.5`.

## Completion Check

Migration is complete when:

- the repository points to guide system v0.7.5;
- milestone planning applies ADR-0020 and distinguishes decision completeness from decision preservation;
- resolved material decisions are durable in the milestone or referenced project authority;
- project-wide authority is referenced rather than duplicated into milestones;
- planning transcripts and scratch work are not required implementation inputs;
- implementation freedom remains limited only by actual resolved project-contract decisions;
- v0.7.3 execution-ledger behavior and v0.7.4 consumer-surface validation remain intact.
