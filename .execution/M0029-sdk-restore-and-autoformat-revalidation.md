# Execution Obligation Ledger — M0029

> Planner-seeded material-outcome inventory. This is not a test matrix, evidence registry, or additional source of policy.

**Milestone:** `docs/milestones/M0029-sdk-restore-and-autoformat-revalidation.md`  
**Planning base:** M0028 implementation head `64781d65997c712e47665f47e5c1d7d10732d00f`  
**Status:** planner amendment accepted into ready contract; implementation PR #38 must reconcile the added correction before completion

## Material obligations

| ID | Outcome | Status | Result or blocker |
|---|---|---|---|
| REST-01 | Default SDK-native restore and explicitly forwarded `--no-restore`; real SDK `IDE0005` retains needed imports, removes unused imports, and never requires an Autoformat-invoked build | pending | |
| LAY-01 | First successful normalize pass is byte-stable on the next pass; normalized check-only is consistent and `format` stays whitespace-only | pending | |
| COLL-01 | Amended: prevent collection-expression rewrites without a valid target type, including the three M0029 target `foreach` failures, while retaining eligible typed conversions; focused regression and diagnostic target recheck | pending | |
| EXP-01 | Target Draft PR #14 preserves its policy/first-output commits and receives a new unmodified regenerated-output commit after the `COLL-01` fix; previous PR #14 and original PR #13 evidence, exact revisions, diagnostic build and convergence results are reported accurately | pending | |
| DOC-01 | Direct Autoformat docs/CLI plus the amended M0029 research, milestone index, completion note and PR descriptions accurately distinguish first and corrected M0029 experiment outputs and the result of `COLL-01` | pending | |

## Resume context

- Original M0029 implementation was submitted as workbench PR #38, head `7f2826bfb487862ba2f9bba642766c403b92a04b`, and generated target Draft PR #14, head `08781f4d3ae7e71bde39bc1e6af9271355ce6b12`.
- The newly discovered three `CS9176` collection-expression failures require `COLL-01` and a fresh output commit to **existing** Draft PR #14 before implementation can be considered complete. The correction has **not** yet been implemented.
- Preserve the original M0028 research and `dotnet-nuget-content` Draft PR #13 as historical evidence.
- The ready milestone governs scope and acceptance. Reconcile all four IDs at completion; no separate proof item per ID.

## Completion accounting

Do not mark an obligation `done` until the corresponding deliverable exists. In implementation PR #38, re-open `COLL-01`, `EXP-01` and `DOC-01` as appropriate; preserve earlier truthful evidence for REST-01 and LAY-01. Report failed restore/format operations and unverified outcomes truthfully; a diagnostic build is not a universal Autoformat runtime gate.