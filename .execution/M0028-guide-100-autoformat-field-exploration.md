# Execution Obligation Ledger — M0028

> Planner-seeded completeness inventory for M0028; subordinate to the ready milestone, not a test matrix, evidence registry, or additional policy source.

**Milestone:** `docs/milestones/M0028-guide-100-autoformat-field-exploration.md`  
**Updated:** 2026-11-22 (planning baseline)  
**Guide target:** `carlrabbit/agentic-project-guides` `v1.0.0`, commit `6d1ca180d6573decb5c71d96bb6383e2073d342b`

## Planned obligations

| ID | Material outcome (same meaning as milestone) | Status | Brief result / blocker |
|---|---|---|---|
| MIG-01 | Vendor the complete unmodified pinned 1.0.0 guide under `.guide/system/` and add matching schema-v2 `.guide-profile.json` metadata and applicable profile selection | pending | |
| MIG-02 | Reconcile workbench agent routing and directly affected project authority with stable 1.0.0, specifically removing unconditional criterion-specific evidence and evidence-case mandates from active `docs/ENGINEERING.md` while preserving release validation | pending | |
| EXP-01 | Run the actual workbench Autoformat normalize/check workflow on an isolated disposable checkout of `carlrabbit/dotnet-nuget-content`, preserving target `main` and recording exact revisions/commands | pending | |
| EXP-02 | Critically inspect representative source changes, non-changes, source integrity, and convergence/check-only behavior; classify observations without silently fixing either product | pending | |
| EXP-03 | Open an explicitly non-mergeable-by-policy Draft PR in `carlrabbit/dotnet-nuget-content` containing only the raw Autoformat source diff and separately committed policy setup from recorded `main`, linked to M0028 | pending | |
| DOC-01 | Record a concise, reproducible, non-authoritative exploratory report linking the target Draft PR, distinguishing observed/inferred/unverified outcomes, uncertainties and revalidation triggers; reflect M0028 status in the workbench milestone index | pending | |

## Implementation progress / resume point

- Planning only; no guide snapshot installed, target branch or PR opened, or experiment run as part of M0028.
- Begin by verifying the exact guide tag/revision, target/workbench HEADs, and this ledger; migrate from the workbench's mixed external-only 0.9.x/rc.1 guidance to effective stable 1.0, and use the target experiment branch and Draft PR exclusively for raw formatter output.
- Use focused checks; no acceptance-to-test matrix or receipt bookkeeping.

## Completion accounting

Before `COMPLETE`, verify that all six seeded IDs remain present with truthful statuses and the corresponding outcomes exist. This is obligation completeness only: do not attach one proof artifact, test, or validation gate per row.

Record any material decisions affecting scope with planning rather than silently deleting obligations.