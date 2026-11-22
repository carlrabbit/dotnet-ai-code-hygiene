# Agent Instructions

## Repository entrypoint

This file routes agents to repository authority; it is not a milestone contract or a copy of the pinned guide methodology.

For a ready implementation milestone, start with:

1. the primary milestone document;
2. its planner-seeded `.execution/<milestone-id>.md` obligation ledger (required for substantive milestones);
3. the project authority explicitly referenced by the milestone;
4. relevant source, tests, and other repository context needed for the work.

For **planning**, guide adoption/upgrade, documentation synchronization, or release-readiness planning, start at the pinned `.guide/system/README.md` and follow the selected task's canonical prompt and applicable local project authority. For other tasks, follow the assigned task and relevant repository authority. Do not assume every invocation is milestone implementation.

Use documented repository engineering commands (`eng/` when applicable). The milestone and project engineering authority define validation requirements; do not invent command contracts.

## Authority boundaries

- The ready milestone and referenced project authority define implementation scope and obligations. For substantive milestones, the planner-seeded `.execution/<milestone-id>.md` tracks all material obligations through completion and must be read. It never overrides the milestone or requires evidence-case/test-gate registries.
- Planning metadata (`.guide-profile.json`, `.guide/profiles/`, `docs/PROJECT-GUIDANCE.md`, `.guide-sync/`) is not routine implementation authority. Consult it only when the assigned work requires it.
- `docs/research/` is non-authoritative evidence, not a substitute for project decisions. Read it when an assigned investigation/evidence obligation calls for it.
- `.review/` is relevant when the active work requires milestone-scoped review, migration, or synchronization.
- Implementation never needs `.guide/system/`, the external guide repository, or the planning conversation to reconstruct the contract. Planning normally needs neither the external repository nor live upstream access: use the pinned local snapshot.

## Execution boundaries

Preserve existing project work and remain within the assigned scope. Do not install persistent host software or change shared CI/CD infrastructure merely to make validation pass; follow the authorization in the milestone and project authority. Escalate material unresolved decisions to planning rather than silently changing project policy. Keep detailed test cadence, evidence-reuse mechanics, and simulation execution rules in the milestone or project engineering authority, not this entrypoint.
