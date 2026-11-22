# Migration — Guide System 1.0.0-rc.1 → 1.0.0

**Classification:** Major planning-authority and execution-handoff correction. Required for consuming repositories adopting the stable 1.0 guide. No retroactive changes to completed milestones.

## New planning authority: pinned local snapshot

**Transition precedence:** For an explicitly authorized upgrade, use the target tagged version's adoption/update instructions and migration contract to govern the change; the old guide describes the current state but cannot forbid installation of the new snapshot. Repositories without `.guide/system/` may temporarily load that target tag upstream to bootstrap. After installing the pinned tree, updating its metadata and reconciling project authority, normal planning uses only the local snapshot.

1. Resolve the stable upstream Git tag `v1.0.0` to its **peeled commit SHA** after that tag exists. Install the entire tagged upstream guide tree unchanged at `.guide/system/` in the target repository. Prefer `git subtree add --prefix=.guide/system <guide-remote> v1.0.0 --squash`; an exact tracked tree copy is acceptable if subtree cannot be used. Do not copy a moving upstream `main` branch, synthesize a subset, or modify shared guides for local customization.
2. Record `guideSystem.repository`, `version: 1.0.0`, `tag: v1.0.0`, `revision: <peeled upstream commit SHA>`, and `localPath: .guide/system` in the existing schema-version-2 `.guide-profile.json`. Update applicable shared profile versions to 1.0.0; preserve project-owned local profile versions and scopes.
3. Route planning, research curation, guide migration, documentation synchronization, and release planning from root `AGENTS.md` to `.guide/system/README.md`. Keep ordinary implementation routed to the ready milestone, its planner-seeded ledger when applicable, and referenced project truth. Planning no longer requires an external guide repository after snapshot installation.
4. Preserve project-owned local profiles at `.guide/profiles/`, project guidance and operative authority in their existing files, and genuinely useful research under `docs/research/`. Only `.guide/system/` is a deliberate copy of shared methodology; treat old copied guides in other paths as legacy leakage, not as the new snapshot. Resolve material conflicts through planning instead of editing vendored files.

## Restore obligation completeness, not old proof requirements

5. For **new substantive implementation milestones**, assign stable IDs to all independently deliverable material acceptance obligations, including required documentation/migration deliverables, and seed one matching pending row per ID in `.execution/<milestone-id>.md` **before** `ready`. A genuinely trivial indivisible task may explicitly omit it. The ledger is operational state, subordinate to the milestone.
6. For **active** milestones, retain existing material acceptance and review it for lossless obligation transfer; add or reconcile a small ledger if required. Preserve genuinely useful existing ledger rows and current progress. An executor cannot silently remove, collapse, or renumber planner-seeded obligations. Do not retroactively rewrite completed milestones.
7. Require ledger-to-milestone obligation-ID reconciliation and a truthful disposition before declaring a substantive milestone complete. Do **not** recreate 0.9 evidence-case IDs, per-obligation test evidence, validation-gate mapping, immutable completion proof, fingerprint receipts, or exhaustive installed-consumer validation for ordinary milestones. Focused milestone checks, real integrations where consequential, and separate final-artifact release qualification remain unchanged.

## Target-state upgrade and completion

Read intermediate migrations when upgrading from older guide versions, but implement the **effective 1.0 target requirements**. In particular, do not create 0.9 proof machinery only to remove it; do preserve the lightweight obligation ledger required by 1.0. Existing 0.9 ledgers may be simplified instead of discarded.

This transition, including first-time adoption, can be a direct repository update when local snapshot installation, metadata/routing alignment, and limited document changes are straightforward. Use one ready milestone and its seeded obligation ledger only when coordinated implementation or material project decisions warrant it. Verify the installed snapshot/pin consistency, router, local profile resolution, and any active-milestone obligation coverage with focused inspection. No live planner/dogfood scenario suite is required.

The guide is **distributed by Git tags and repository-local copies**, not packages, published releases, or an automatic deployment channel. Do not create a GitHub Release or publish an artifact as part of this migration.
