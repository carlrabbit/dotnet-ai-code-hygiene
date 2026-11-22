# Planning Handoff Transport Model

## Scope and authority

This model governs delivery of repository-relative planning artifacts: ready milestones, referenced project authority, and planner-seeded obligation ledgers for substantive work. Git commits and ZIP overlays are **transports**, not additional sources of requirements or implementation authority. Both must preserve the same milestone contract, obligation completeness, review/validation boundaries, and implementation independence.

## Git-native handoff

When authorized target-repository Git access exists, prefer committing **planning-owned** changes to a dedicated planning branch from the intended repository base. Honor an explicitly assigned branch or authorized default-branch write. Do not infer such authorization from credentials. An empty Git repository has no commit from which to branch, but an authorized initialization package **may itself be the first (root) planning commit** on the designated branch; no placeholder commit or mandatory ZIP is needed. A **planning-only handoff must not include product implementation edits**—including changes to source, tests, build/CI, or runtime behavior—merely because they relate to the planned milestone. The selected planning workflow may still authorize its own planning artifacts and repository metadata/documentation edits. Product implementation changes require separate, explicit workflow authority; they must not be smuggled into or represented as a planning-only commit.

Declare a milestone `ready` only once the planner has seeded its material obligations where applicable. Record the **exact accepted planning commit SHA**, repository, branch, primary milestone path, and intended implementation-branch relationship in the *handoff report*. Do not attempt to record a commit's own SHA inside that same commit (self-reference). A branch name or PR link alone is not an immutable planning baseline.

Implementation may continue on the authorized planning branch **from that accepted commit** or branch separately **from the same exact commit**. If the branch has advanced before pickup, inspect changes affecting the milestone, seeded ledger, or referenced authority **and materially relevant source/test changes that could invalidate the planner's assumptions**. Explicitly reconcile an accepted baseline before relying on changed assumptions; do not silently treat the moving head as the agreed handoff. This is a focused divergence assessment, not an exhaustive comparison of unrelated repository changes.

**Planning PR merged before pickup:** A squash or rebase merge may produce a new commit SHA without retaining the original planning commit in reachable history. Inspect the resulting merged repository state. Use the original accepted commit if it is still accessible and appropriate; otherwise the merged commit may become the new implementation baseline **after confirming semantic equivalence of the ready milestone, seeded ledger, and referenced project authority, and confirming that materially relevant source/test changes have not invalidated its planning assumptions**. Material changes to the accepted contract or assumptions require planning reconciliation, not an implicit baseline substitution. Do not rewrite history, discard unrelated repository work, or demand preservation of one particular Git ancestry merely to recover the old SHA. Preserve unrelated existing work in all cases. Committing planning, opening a planning PR, approving/merging it, and beginning implementation are distinct decisions. None is implied by selecting Git transport.

## ZIP handoff

When Git write access is unavailable/inappropriate, or a ZIP is specifically requested, provide a repository-relative overlay containing only the files permitted by the selected workflow. Identify the intended target repository/base revision **when known**, the primary milestone path, and application instructions for file additions, moves, and deletions. Do not keep obsolete files merely because a ZIP cannot express deletion directly. Do not invent a Git commit SHA if no commit was created.

Before applying a ZIP overlay, compare the target repository with the intended base **when available**. If relevant files have diverged, reconcile overlapping changes instead of silently overwriting project authority or unrelated work. When the base is unknown, inspect existing target files before replacement and resolve any conflicts rather than treating absence of a base SHA as permission to overwrite. Move/deletion instructions must accompany the archive and be available to the person or agent applying it. Verify that the applied overlay leaves the intended ready milestone, obligation ledger, and referenced authority intact.

After the recipient safely applies the ZIP to the intended or reconciled repository state, the resulting ready milestone, referenced authority, and seeded ledger constitute the *same semantic contract* as a Git delivery. The archive and application notes do not become another source of requirements or replace the canonical implementation prompt.

## Direct changes without a milestone

Guide adoption/updates and documentation synchronization may be straightforward **direct changes**. They require no manufactured ready milestone, obligation ledger, or implementation handoff. For Git, report the actual resulting commit SHA and changed files; this is a change record, not a separate implementation baseline. For ZIP, report the applicable base if known and any application instructions.

## Verification

Confirm that delivered repository-relative files, required obligation rows, and project authority are consistent. Report Git repository, branch, **exact accepted commit**, milestone path and optional PR; or ZIP, intended base when known, and application instructions. The handoff contract does **not** introduce evidence-case mappings, receipts, additional approval gates, publication, or release qualification.
