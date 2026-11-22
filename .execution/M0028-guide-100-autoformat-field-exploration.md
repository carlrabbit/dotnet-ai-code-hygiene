# Execution Obligation Ledger — M0028

> Planner-seeded completeness inventory for M0028; subordinate to the ready milestone, not a test matrix, evidence registry, or additional policy source.

**Milestone:** `docs/milestones/M0028-guide-100-autoformat-field-exploration.md`  
**Updated:** 2026-11-22
**Guide target:** `carlrabbit/agentic-project-guides` `v1.0.0`, commit `6d1ca180d6573decb5c71d96bb6383e2073d342b`

## Planned obligations

| ID | Material outcome (same meaning as milestone) | Status | Brief result / blocker |
|---|---|---|---|
| MIG-01 | Vendor the complete unmodified pinned 1.0.0 guide under `.guide/system/` and add matching schema-v2 `.guide-profile.json` metadata and applicable profile selection | done | Exact tagged tree installed; profile pin, base/cli-tool repository scopes and project metadata added. |
| MIG-02 | Reconcile workbench agent routing and directly affected project authority with stable 1.0.0, specifically removing unconditional criterion-specific evidence and evidence-case mandates from active `docs/ENGINEERING.md` while preserving release validation | done | Router and both named engineering paragraphs reconciled; release-grade validation text preserved. |
| EXP-01 | Run the actual workbench Autoformat normalize/check workflow on an isolated disposable checkout of `carlrabbit/dotnet-nuget-content`, preserving target `main` and recording exact revisions/commands | done | Disposable target baseline `25b28310c7de04426de02e4ac8037b32ed9dbd9e`; policy and two raw normalize passes executed locally. No remote target refs changed. |
| EXP-02 | Critically inspect representative source changes, non-changes, source integrity, and convergence/check-only behavior; classify observations without silently fixing either product | done | Second pass converged; check-only normalize and format reported zero changes. Diagnostic build confirmed five unresolved NuGet symbols caused by removed imports; evidence recorded without repairs. |
| EXP-03 | Open an explicitly non-mergeable-by-policy Draft PR in `carlrabbit/dotnet-nuget-content` containing only the raw Autoformat source diff and separately committed policy setup from recorded `main`, linked to M0028 | done | [Draft PR #13](https://github.com/carlrabbit/dotnet-nuget-content/pull/13) is open against `main`, marked DO NOT MERGE, and contains only `.editorconfig` plus the nine raw-formatted C# files. |
| DOC-01 | Record a concise, reproducible, non-authoritative exploratory report linking the target Draft PR, distinguishing observed/inferred/unverified outcomes, uncertainties and revalidation triggers; reflect M0028 status in the workbench milestone index | done | Research report links PR #13; M0028 index and completion note record the compile-breaking finding and deferred correction work. |

## Implementation progress / resume point

- Guide snapshot, metadata, routing, and direct authority reconciliation are complete. Exact guide/tag pin and relevant authority changes are in the milestone completion note.
- Target experiment used recorded baseline `25b28310c7de04426de02e4ac8037b32ed9dbd9e`, disposable branch `experiment/m0028-autoformat-canonicalization`, and commits `900f73e`, `c20f395`, `741c795`. Only that branch was pushed; [Draft PR #13](https://github.com/carlrabbit/dotnet-nuget-content/pull/13) remains open and unmerged with a DO NOT MERGE compile-failure warning.
- The target baseline project built successfully; the normalized project failed on five missing NuGet type imports removed by Autoformat. The report links the PR and records commands, findings, uncertainty, and follow-up in `docs/research/M0028-autoformat-dotnet-nuget-content-exploration.md`.
- Focused verification only; no aggregate suite or release qualification was run.

## Completion accounting

Before `COMPLETE`, verify that all six seeded IDs remain present with truthful statuses and the corresponding outcomes exist. This is obligation completeness only: do not attach one proof artifact, test, or validation gate per row.

Record any material decisions affecting scope with planning rather than silently deleting obligations.
