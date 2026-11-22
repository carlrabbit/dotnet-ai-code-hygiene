# ADR-0033 — Restore Lossless Planner-Seeded Obligation Completeness

**Status:** Accepted for 1.0.0

**Revises:** ADR-0030's optional-only execution ledger default. Retains ADR-0031's economical assurance; restores the useful obligation-preservation purpose of 0.9 without its evidence topology.

## Context

AI implementation agents may overlook, merge, or forget independently required outcomes even when a ready milestone clearly lists them. The same implementer can overlook those omissions in its final general review. The earlier 0.9 planner-seeded ledger solved this *completeness* problem but combined it with expensive evidence cases, validation-gate mappings, receipts, and immutable proof tracking. RC1 removed those costs but also weakened the useful handoff invariant.

## Decision

For a **substantive implementation milestone**, planning assigns stable IDs to all independently deliverable material obligations and seeds a compact `.execution/<milestone-id>.md` ledger before declaring `ready`. This includes required non-code deliverables such as documentation and migration. A genuinely trivial indivisible task may omit it with a brief rationale. The choice is about omission risk, not elapsed implementation time.

The implementer owns internal tasks, implementation approach, and progress updates, but must not silently delete, merge, renumber, or reinterpret planner-seeded obligation rows. At completion it reconciles IDs against the milestone and accounts for every actual delivered outcome. A newly invalid material obligation goes back to planning.

This ledger is operational state subordinate to the milestone. It is **not an evidence registry**. Do not mandate evidence-case IDs, one test per obligation, gate-to-requirement mappings, receipt fingerprints, or immutable proof maps. Validation still targets representative real behavior and risks, with final distribution qualification at release readiness.

See `meta/MILESTONE-EXECUTION-MODEL.md` and `meta/VALIDATION-MODEL.md` for the operative rules.

## Consequences

A few extra ledger rows and minimal ID reconciliation reduce dropped work during agent-scale milestones without recreating the 0.9 validation cost inversion. Completeness and correctness are managed independently.
