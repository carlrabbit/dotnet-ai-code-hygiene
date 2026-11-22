# ADR-0034 — Reproducible, Transport-Independent Planning Handoffs

**Status:** Accepted for 1.0.0 before its Git version tag.

## Context

The guide permits Git and ZIP delivery but a moving planning-branch name does not uniquely identify the milestone, planner-seeded ledger, and project authority accepted for implementation. Historical PR #11 proposed an exact Git planning baseline but mixed that improvement with superseded 0.9.x proof machinery and a conflicting historical ADR number.

## Decision

For authorized Git handoffs, preserve the accepted **planning-baseline commit SHA** and the implementation branch relationship; implementation may continue on the planning branch or branch from that exact commit. Reconcile relevant changes if a branch advances before pickup. After squash/rebase merge, a semantically equivalent merged commit may replace an inaccessible or unsuitable original SHA as the accepted implementation baseline; material changes require planning reconciliation. An authorized initialization package may be the repository's first root commit. An optional planning PR, approval/merge, and actual implementation are independent actions. For disconnected ZIP delivery, preserve paths, intended base when known, and move/deletion instructions; reconcile divergent target authority instead of overwriting it.

Transport cannot change the ready milestone, substantive planner-seeded obligation completeness, or risk-proportionate validation. Straightforward direct repository changes do not create artificial milestones. The normative contract is in `meta/HANDOFF-TRANSPORT-MODEL.md`.

## Consequences

Reproducible Git handoff without mandatory ZIP or PR workflow; equivalent disconnected transport; no new evidence registry, packaging system, or release/publication mechanism.
