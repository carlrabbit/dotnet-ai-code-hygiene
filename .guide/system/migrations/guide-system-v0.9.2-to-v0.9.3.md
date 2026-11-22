# Migration — Guide System v0.9.2 to v0.9.3 (draft)

## Purpose

Add cost-conscious validation cadence, safe incremental evidence reuse, and an explicit investigative-execution category while preserving existing validation tiers, required gates and project-local planning authority.

## Migration required

Conditional. No repository restructuring or compulsory test-runner change. Repositories whose current validation is inexpensive, already has safe stage freshness, and keeps exploratory work separate may update guide/profile metadata only.

For repositories with expensive repeated validation or simulations mixed into regular test/CI runs, review active engineering authority and applicable ready/implementing milestones. Completed milestones and historical receipts do not need retroactive rewriting unless reopened.

## Conditional adoption

- Distinguish **development feedback** (affected, cheap checks), **investigative execution** (diagnostic probes, simulations, parameter sweeps), and **acceptance validation** (mandatory current milestone/release evidence). These are not new tiers, modes or profiles.
- In project engineering authority, identify normal focused/complete validation commands and separate experimental entry points. Exclude exploratory workloads from default test discovery, canonical validation and CI unless deliberately required.
- When stages are materially expensive, inspect measured runtime/setup cost, repeated process/restore/fixture work and the value of independent resumable stages. Choose an appropriately simple solution; do not require a generic cache graph for small suites.
- Reuse passing stage evidence only with trustworthy dependency scope covering relevant code, tests, fixtures, toolchain, configuration, environment and upstream artifacts. If no such contract exists, conservatively rerun. Whole-repository fingerprints remain a safe, potentially over-invalidating fallback.
- At the milestone completion boundary, confirm that every required gate has current, target/locus-correct, criterion-specific evidence. Do not interpret complete verification as mandatory unconditional re-execution, nor bypass fresh final-artifact/consumer evidence.
- Preserve experimental methodology/results as non-authoritative research only when useful for future planning or explicitly required. Promote established invariants deliberately into engineering/specification authority and bounded regression checks; remove disposable diagnostics.

## Planning and implementation boundary

Optional `docs/PROJECT-GUIDANCE.md` may state project-specific priorities for feedback latency or research/simulation work. Registered local profiles remain reserved for real engineering shapes, not isolated test strategies. The Planner resolves required evidence and any cost/freshness constraints into the milestone or project engineering authority. Implementers own affected-check selection, bounded diagnostic runs and incremental execution mechanics within that contract.

## AGENTS.md entrypoint reconciliation

The 0.9.3 `AGENTS.md` template is a minimal navigation entrypoint, not a second Implementer guide. It routes milestone execution first to the ready milestone, its applicable planning-seeded ledger, referenced project authority, and then relevant code/tests. Non-milestone tasks follow their assigned workflow.

During this migration inspect existing `AGENTS.md` for contradictory reading order, duplicated test/receipt/simulation policy, requirements to consult external guide or planning-only sources, or overly rigid command restrictions. Preserve useful project-specific instructions and move detailed validation rules to operative engineering authority or the milestone. Edit `AGENTS.md` only when necessary; an already-sound entrypoint is a no-op. Treat a correction as direct migration work when the current routing would misdirect the executor, not as optional later documentation sync.

## Completion

Confirm the project's normal test/CI entry points do not accidentally include exploratory workloads; acceptance evidence remains complete and fresh; existing `AGENTS.md` routing was evaluated and reconciled only when needed; no new workflows, infrastructure, mandatory receipts or profile changes were introduced merely to conform. No runtime tests are required for a guide-metadata-only update.
