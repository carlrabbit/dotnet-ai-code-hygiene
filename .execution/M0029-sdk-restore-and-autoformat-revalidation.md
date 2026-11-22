# Execution Obligation Ledger — M0029

> Planner-seeded material-outcome inventory. This is not a test matrix, evidence registry, or additional source of policy.

**Milestone:** `docs/milestones/M0029-sdk-restore-and-autoformat-revalidation.md`  
**Planning base:** M0028 implementation head `64781d65997c712e47665f47e5c1d7d10732d00f`  
**Status:** pending implementation

## Material obligations

| ID | Outcome | Status | Result or blocker |
|---|---|---|---|
| REST-01 | Default SDK-native restore and explicitly forwarded `--no-restore`; real SDK `IDE0005` retains needed imports, removes unused imports, and never requires an Autoformat-invoked build | pending | |
| LAY-01 | First successful normalize pass is byte-stable on the next pass; normalized check-only is consistent and `format` stays whitespace-only | pending | |
| EXP-01 | New, isolated `dotnet-nuget-content` Draft PR from the original target baseline publishes unchanged new formatter output and separately committed policy; comparison to original Draft PR #13 and honest diagnostic build outcome | pending | |
| DOC-01 | Direct Autoformat documentation/CLI updates, concise M0029 research with provenance and both target PR links, milestone index and completion note | pending | |

## Resume context

- Planning only. No Autoformat fix, second experiment, or new target branch/PR has been executed.
- Preserve the original M0028 research and `dotnet-nuget-content` Draft PR #13 as historical evidence.
- The ready milestone governs scope and acceptance. Reconcile all four IDs at completion; no separate proof item per ID.

## Completion accounting

Do not mark an obligation `done` until the corresponding deliverable exists. Report failed restore/format operations and unverified outcomes truthfully; a diagnostic build is not a universal Autoformat runtime gate.