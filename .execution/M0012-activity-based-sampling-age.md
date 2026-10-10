# Execution Ledger — M0012

Primary milestone: `docs/milestones/M0012-activity-based-sampling-age.md`

This file records implementation and validation evidence. It does not replace the milestone or claim experimental validation of the age model.

## Work packages

| ID | Work package | Status | Evidence |
|---|---|---|---|
| WP-01 | Versioned deterministic project source-activity snapshots, line diff, normalization, persistence, and partial-scope handling | done | `Sampling/ProjectActivity.cs`, `SamplingSession.ObserveProjectActivity`, `RuleContext.ProjectActivityAge`; fixed vectors and persistence/rollback tests |
| WP-02 | Apply the fixed no-age summary and activity-aged BORINGness policies with explicit rule-version resets | done | Summary rule versions 5/3, BORINGness v2; rule-owned hazard diagnostics in `BoringnessReview` |
| WP-03 | Preserve atomic publication, isolation, stale-ticket, acceptance, and public-output behavior | done | Activity and sampler state share `sampling.json`; existing two-file publish/rollback remains; full regression and installed-tool checks pass |
| WP-04 | Update authority, public docs, validation, and evidence ledger | done | Sampling/semantic-review/engineering/architecture/rule capability/docs index updated; `eng/validate.ps1` version assertions updated |

## Acceptance evidence

| ID | Obligation | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|
| AC-01 | Unchanged source and repeated checks add no age, including process restart | Project totals advance only on complete observable source transitions; summary subjects no longer use elapsed accrual | `M0012ProjectActivitySurvivesRestartsIgnoresPartialSnapshotsAndStaysProjectScoped`; same-state repeated checks retain age; Core sampling persistence tests | done |
| AC-02 | Complete transitions accrue once; repeated, partial, failed, and revalidation scans do not duplicate or drop activity | Baseline and total are staged with the sampler state; partial views only read age; CheckCore discard boundary retains revalidation rollback semantics | Activity partial/rollback/concurrency tests; existing `CheckPublishesLatestRunAndSamplingStateTogetherOnFailures`; canonical validation | done |
| AC-03 | Activity is project isolated; deferred units receive only unaccounted exposure | Stable project-qualified aggregate unit IDs and project totals; each unit stores `LastActivityAgeUnits` and previous count | Two-project lifecycle fixture asserts project 0 ages while project 1 stays at zero; zero-candidate transition test checks deferred/count-zero behavior | done |
| AC-04 | Added/deleted LOC, normalization floor/previous size, rename/edit, and large-source vectors | Deterministic Myers line diff, exact-content rename pairing, edited single-pair rename diff, fixed 100 LOC floor | `ProjectActivityUsesExactSourceDiffAndPreviousSizeNormalization` covers 100+ LOC normalization, floor, additions, deletion, same-content and edited renames, cross-project move as deletion/addition, and 10k-line source | done |
| AC-05 | Staged/uncommitted source, missing Git/history, baseline loss, project moves, generated, linked, and multi-target behavior are deterministic | Uses current MSBuild project document contents; ignores Git history; stable `.csproj` identity, distinct-path grouping, common generated suffix/path/header exclusion; first observation starts at zero | Lifecycle fixture covers current disk edits, generated-file exclusion, partial baseline preservation, and project A/B isolation; source review confirms same-project linked paths are grouped and multi-target instances share the `.csproj` identity. Git shallow/rebase cases are inapplicable to the history-free algorithm | done |
| AC-06 | Summary rules have first/change hazards only; BORINGness gets prior-count-weighted age; evidence distinguishes both contributions | Versions 5/3 remove summary elapsed accrual; BORINGness v2 calls `AccrueActivity`; fingerprint and activity hazard contributions are returned separately | `BoringnessZeroCandidatePopulationTransitionsResetTheAggregateBaseline` asserts 1 fingerprint + 4 activity hazard separately and population transitions; lifecycle benchmark asserts no repeated age | done |
| AC-07 | Observation acceptance and stale-ticket semantics remain intact | Ticket code and acceptance protocol unchanged; new rule versions invalidate old current-rule tickets/state as before | Core/CLI regression suites; installed accept/accept-all workflow in canonical validation | done |
| AC-08 | Atomic state, concurrency, rollback, and reset behavior | Activity snapshots share the existing sampler file and concurrent-change check; three rule versions advance; no elapsed cursor is translated | Activity publication failure/concurrency test; check rollback lifecycle test; rule version installed assertions pass | done |
| AC-09 | Cold/steady/changed checks and activity-state size are measured on one/two-project fixtures | Lifecycle benchmark test records cold, steady, changed runtime and state/activity snapshot size | Windows 11/.NET 11 observed: one project (8 eligible source files) cold 14.9s, steady 19.0s, changed 18.6s; state 12,957 B/activity snapshot 1,993 chars. Two projects (16 eligible source files) cold 42.3s, steady 31.5s, changed 8.6s; state 24,959 B/activity snapshot 3,947 chars. Each fixture also had an excluded generated source. Environment timings are noisy and include Roslyn/MSBuild startup; this is implementation overhead evidence, not an age-model experiment | done |
| AC-10 | Authorities match implementation; M0013 remains separate and unclaimed | Sampling, semantic review, engineering, architecture, rule-capability, documentation, and public docs describe current behavior; M0012 is implementation-complete and M0013 remains proposed | `eng/validate.ps1` and final documentation/source review; no M0013 replay or effectiveness claim | done |

## Validation record

- `eng/validate.ps1` passed on Windows 11 with .NET SDK `11.0.100-rc.1.26425.128`.
- Release build: 0 warnings, 0 errors.
- TUnit: 79 Core tests and 23 CLI tests passed; 102 total, 0 failed.
- Tier 4 exact locally packed/installed 0.7.0 consumer validation passed; installed rule versions and repository M0006 self-host behavior passed.
- `git diff --check` passed.
- No M0013 execution, experimental replay, or calibration claim was performed.

## Deviations and unresolved items

- No implementation deviations from M0012. The canonical installed-tool validation assertion was updated from summary versions v4/v2 to v5/v3 to reflect the explicitly versioned migration.
- The nominal 365 age scale and its effectiveness remain unvalidated by design; this is reserved for M0013.
- PR review remains pending.
