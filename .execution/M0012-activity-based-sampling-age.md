# Execution Ledger — M0012

Primary milestone: `docs/milestones/M0012-activity-based-sampling-age.md`

This file records implementation and validation evidence. It does not replace the milestone or claim experimental validation of the age model.

## Work packages

| ID | Work package | Status | Evidence |
|---|---|---|---|
| WP-01 | Versioned deterministic project source-activity snapshots, line diff, normalization, persistence, bounded costs, and report-scope decoupling | done | `Sampling/ProjectActivity.cs`, `SamplingSession.ObserveProjectActivity`, `RuleContext.TryReadEligibleProjectSources`; caps and fallback vectors in `SamplingTests`; changed-target lifecycle evidence below |
| WP-02 | Apply the fixed no-age summary and activity-aged BORINGness policies with explicit rule-version resets | done | Summary rule versions 5/3, BORINGness v2; existing rule-owned hazard diagnostics and acceptance behavior retained |
| WP-03 | Preserve atomic publication, isolation, stale-ticket, acceptance, and public-output behavior | done | Activity and sampler state share `sampling.json`; existing two-file publish/rollback remains; 105-test regression and installed-tool checks pass |
| WP-04 | Update authority, public docs, validation, and criterion-specific execution evidence | done | `SAMPLING.md`, `SEMANTIC-REVIEWS.md`, architecture and README describe report-scope behavior, resource bounds, and conservative rename/diff policy; ledger evidence reconciled |

## Acceptance evidence

| ID | Obligation | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|
| AC-01 | Unchanged source and repeated checks add no age, including process restart | Project totals advance only on complete observable source transitions; summary subjects no longer use elapsed accrual | `M0012ProjectActivitySurvivesRestartsIgnoresPartialSnapshotsAndStaysProjectScoped` repeats checks after persisted restart and asserts unchanged totals; canonical Core suite passes | done |
| AC-02 | Complete transitions accrue once; repeated, partial, failed, and revalidation scans do not duplicate or drop activity | Activity observation reads all eligible source documents in the project independently from report targets; baseline, total, and sampler state stay staged in one session | Changed-target lifecycle test edits a requested and an unrequested file, observes both through activity, asserts report population stays 1, repeats the partial scan twice without increasing age, then runs full scope with no second charge. Existing failed publication, rollback, stale-state, and revalidation-discard tests pass in canonical suite | done |
| AC-03 | Activity is project isolated; deferred units receive only unaccounted exposure | Stable project-qualified aggregate unit IDs and project totals; each unit stores `LastActivityAgeUnits` and previous count | Two-project lifecycle fixture asserts project 0 ages while project 1 stays unchanged; zero-candidate transition test checks deferred/count-zero behavior; repeated unit accrual remains idempotent | done |
| AC-04 | Added/deleted LOC, normalization floor/previous size, rename/edit, and large-source vectors | Deterministic bounded Myers diff plus fingerprint/line-count fallback; exact-content rename pairing only; unrelated path pairs count as independent addition/deletion | `ProjectActivityUsesExactSourceDiffAndPreviousSizeNormalization` asserts first baseline, denominator floor and prior size, exact/approximate transitions, exact-content rename zero churn, unrelated delete/add counts, 6,000-line single edit, 4,000-line rewrite fallback, 131,073-character file changes, and 600-Ki-character project fallback | done |
| AC-05 | Staged/uncommitted source, missing Git/history, baseline loss, project moves, generated, linked, and multi-target behavior are deterministic | Reads current Roslyn project source contents without consulting Git history; stable `.csproj` identity; groups duplicate physical paths; generated path/suffix/header exclusion; metadata-only states continue activity after restart | Lifecycle test modifies working-tree files and excludes generated source; `M0012LinkedSourcesAreDeduplicatedAndMultiTargetProjectsShareActivityIdentity` constructs duplicate linked paths and two target project instances with one `.csproj`, asserting one physical source and same project activity key. Cross-project moves and legacy unavailable-baseline recovery have fixed vectors | done |
| AC-06 | Summary rules have first/change hazards only; BORINGness gets prior-count-weighted age; evidence distinguishes both contributions | Versions 5/3 remove summary elapsed accrual; BORINGness v2 calls `AccrueActivity`; fingerprint and activity hazard contributions are returned separately | `BoringnessZeroCandidatePopulationTransitionsResetTheAggregateBaseline` asserts 1 fingerprint + 4 activity hazard separately and population transitions; lifecycle benchmark asserts no repeated age | done |
| AC-07 | Observation acceptance and stale-ticket semantics remain intact | Ticket code and acceptance protocol unchanged; new rule versions invalidate old current-rule tickets/state as before | Core/CLI regression suites; installed accept/accept-all workflow in canonical validation | done |
| AC-08 | Atomic state, concurrency, rollback, and reset behavior | Activity snapshots share the existing sampler file and concurrent-change check; v3 source metadata upgrades v1/v2 state; three rule versions and sampler algorithms remain unchanged | Oversized metadata publication failure leaves prior JSON byte-for-byte unchanged; concurrent modification is rejected; v1 JSON with source text upgrades and preserves age; existing rule reset, latest-run rollback, stale-ticket and acceptance tests pass | done |
| AC-09 | Cold/steady/changed checks and activity-state size are measured on one/two-project fixtures, including large and oversized files | Lifecycle benchmark records cold/steady/changed-target runtime plus state/snapshot size; direct project and repository-budget benchmarks record metadata size and comparison overhead | Windows 11/.NET 11: one project (8 eligible files) cold 3,724 ms, steady 1,560 ms, changed-target 1,523 ms; state 14,231 B/activity section 3,301 chars. Two projects (16 files) cold 2,851 ms, steady 2,917 ms, changed-target 2,796 ms; state 27,507 B/activity section 6,529 chars. Direct diff: 6,000-line single edit 1.55 ms / 107,133 B state; 4,000-line rewrite fallback 12.69 ms / 59,134 B state. Five-file 600-Ki-character project: 491,520 exact characters retained, 492,377 B serialized state, metadata-only changed-file comparison 2.67 ms. One 131,073-character file persisted as metadata in an 813 B sampling state. Three projects × five 120-Ki-character files each compacted to 4,470 B; two reverse-order all-project changed scans took 70.55 ms total. Timings include Roslyn/MSBuild startup where applicable and are environment samples, not age-model experiments | done |
| AC-10 | Authorities match implementation; M0013 remains separate and unclaimed | Sampling, semantic review, engineering, architecture and public README describe report-scope decoupling, resource bounds, and conservative diff/rename behavior; M0013 remains separate | Canonical `eng/validate.ps1` and final documentation/source review passed; no M0013 replay or effectiveness claim | done |

## Validation record

- `eng/validate.ps1` passed on Windows 11 with .NET SDK `11.0.100-rc.1.26425.128`.
- Release build: 0 warnings, 0 errors.
- TUnit: 82 Core tests and 23 CLI tests passed; 105 total, 0 failed.
- Tier 4 exact locally packed/installed 0.7.0 consumer validation passed; installed rule versions and repository M0006 self-host behavior passed.
- `git diff --check` passed.
- No M0013 execution, experimental replay, or calibration claim was performed.

## Deviations and unresolved items

- Final correction retains SHA-256 and LOC metadata for every eligible file. Exact source content is bounded at 128 Ki characters per file, 512 Ki characters per project, and 1 Mi characters repository-wide; crossing the repository limit drops exact content for all projects at publication, independent of evaluation order. Changed files without exact comparable content count full-file replacement; unchanged fingerprints add zero. Diff fallback thresholds remain 12,000 combined lines, 2,000,000 retained frontier cells, and edit distance 1,024.
- Edited rename recognition is intentionally conservative: without reliable rename evidence, deleted and added paths are independent; exact-content moves retain zero churn. No M0012 policy, sampler algorithm, acceptance protocol, or rule version changes were made by this correction.
- The nominal 365 age scale and its effectiveness remain unvalidated by design; this is reserved for M0013.
- PR review remains pending.
