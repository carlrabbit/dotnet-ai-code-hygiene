# Execution Ledger — M0012

Primary milestone: `docs/milestones/M0012-activity-based-sampling-age.md`

This file records implementation and validation evidence. It does not replace the milestone or claim experimental validation of the age model.

## Work packages

| ID | Work package | Status | Evidence |
|---|---|---|---|
| WP-01 | Versioned deterministic project source-activity snapshots, line diff, normalization, persistence, bounded costs, and report-scope decoupling | done | `Sampling/ProjectActivity.cs`, `SamplingSession.ObserveProjectActivity`, `RuleContext.TryReadEligibleProjectSources`; caps and fallback vectors in `SamplingTests`; changed-target lifecycle evidence below |
| WP-02 | Apply the fixed no-age summary and activity-aged BORINGness policies with explicit rule-version resets | done | Summary rule versions 5/3, BORINGness v2; existing rule-owned hazard diagnostics and acceptance behavior retained |
| WP-03 | Preserve atomic publication, isolation, stale-ticket, acceptance, and public-output behavior | done | Activity and sampler state share `sampling.json`; existing two-file publish/rollback remains; 103-test regression and installed-tool checks pass |
| WP-04 | Update authority, public docs, validation, and criterion-specific execution evidence | done | `SAMPLING.md`, `SEMANTIC-REVIEWS.md`, architecture and README describe report-scope behavior, resource bounds, and conservative rename/diff policy; ledger evidence reconciled |

## Acceptance evidence

| ID | Obligation | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|
| AC-01 | Unchanged source and repeated checks add no age, including process restart | Project totals advance only on complete observable source transitions; summary subjects no longer use elapsed accrual | `M0012ProjectActivitySurvivesRestartsIgnoresPartialSnapshotsAndStaysProjectScoped` repeats checks after persisted restart and asserts unchanged totals; canonical Core suite passes | done |
| AC-02 | Complete transitions accrue once; repeated, partial, failed, and revalidation scans do not duplicate or drop activity | Activity observation reads all eligible source documents in the project independently from report targets; baseline, total, and sampler state stay staged in one session | Changed-target lifecycle test edits a requested and an unrequested file, observes both through activity, asserts report population stays 1, repeats the partial scan twice without increasing age, then runs full scope with no second charge. Existing failed publication, rollback, stale-state, and revalidation-discard tests pass in canonical suite | done |
| AC-03 | Activity is project isolated; deferred units receive only unaccounted exposure | Stable project-qualified aggregate unit IDs and project totals; each unit stores `LastActivityAgeUnits` and previous count | Two-project lifecycle fixture asserts project 0 ages while project 1 stays unchanged; zero-candidate transition test checks deferred/count-zero behavior; repeated unit accrual remains idempotent | done |
| AC-04 | Added/deleted LOC, normalization floor/previous size, rename/edit, and large-source vectors | Deterministic bounded Myers diff; exact-content rename pairing only; unrelated path pairs count as independent deletion/addition; extensive rewrite falls back to whole-file add/delete counts | `ProjectActivityUsesExactSourceDiffAndPreviousSizeNormalization` asserts first baseline, denominator floor and prior size, additions/deletions, exact-content rename 0 churn, unrelated delete/add independent counts, changed stable-path edit, 6,000-line one-line edit, 4,000-line rewrite fallback, and oversized-file rebaseline | done |
| AC-05 | Staged/uncommitted source, missing Git/history, baseline loss, project moves, generated, linked, and multi-target behavior are deterministic | Reads current Roslyn project source contents without consulting Git history; stable `.csproj` identity; groups duplicate physical paths; generated path/suffix/header exclusion; unavailable baseline recovery adds no fabricated churn | Lifecycle test modifies working-tree files and excludes generated source; `M0012LinkedSourcesAreDeduplicatedAndMultiTargetProjectsShareActivityIdentity` constructs duplicate linked paths and two target project instances with one `.csproj`, asserting one physical source and same project activity key. Cross-project moves and bounded baseline rebase have fixed vectors | done |
| AC-06 | Summary rules have first/change hazards only; BORINGness gets prior-count-weighted age; evidence distinguishes both contributions | Versions 5/3 remove summary elapsed accrual; BORINGness v2 calls `AccrueActivity`; fingerprint and activity hazard contributions are returned separately | `BoringnessZeroCandidatePopulationTransitionsResetTheAggregateBaseline` asserts 1 fingerprint + 4 activity hazard separately and population transitions; lifecycle benchmark asserts no repeated age | done |
| AC-07 | Observation acceptance and stale-ticket semantics remain intact | Ticket code and acceptance protocol unchanged; new rule versions invalidate old current-rule tickets/state as before | Core/CLI regression suites; installed accept/accept-all workflow in canonical validation | done |
| AC-08 | Atomic state, concurrency, rollback, and reset behavior | Activity snapshots share the existing sampler file and concurrent-change check; three rule versions advance; no elapsed cursor is translated | Activity publication failure/concurrency test; check rollback lifecycle test; old snapshot JSON without the additive availability field deserializes compatibly; canonical installed rule-version assertions pass | done |
| AC-09 | Cold/steady/changed checks and activity-state size are measured on one/two-project fixtures, including large files and rewrites | Lifecycle benchmark records cold, steady and changed-target runtime plus state/snapshot size; direct activity benchmark times bounded large-file diff and fallback | Windows 11/.NET 11: one project (8 eligible files) cold 3,742 ms, steady 1,558 ms, changed-target 1,537 ms; state 12,991 B/activity section 2,061 chars. Two projects (16 eligible files) cold 2,930 ms, steady 2,831 ms, changed-target 3,004 ms; state 25,027 B/activity section 4,049 chars. Direct diff: 6,000-line single edit 1.52 ms, snapshot 107,011 B; 4,000-line rewrite fallback 10.35 ms, snapshot 59,013 B. Fixture checks also exclude generated source. Timings include Roslyn/MSBuild startup where applicable and are environment samples, not age-model experiments | done |
| AC-10 | Authorities match implementation; M0013 remains separate and unclaimed | Sampling, semantic review, engineering, architecture and public README describe report-scope decoupling, resource bounds, and conservative diff/rename behavior; M0013 remains separate | Canonical `eng/validate.ps1` and final documentation/source review passed; no M0013 replay or effectiveness claim | done |

## Validation record

- `eng/validate.ps1` passed on Windows 11 with .NET SDK `11.0.100-rc.1.26425.128`.
- Release build: 0 warnings, 0 errors.
- TUnit: 80 Core tests and 23 CLI tests passed; 103 total, 0 failed.
- Tier 4 exact locally packed/installed 0.7.0 consumer validation passed; installed rule versions and repository M0006 self-host behavior passed.
- `git diff --check` passed.
- No M0013 execution, experimental replay, or calibration claim was performed.

## Deviations and unresolved items

- Corrections add fixed snapshot bounds of 128 Ki characters per file, 512 Ki characters per project, and 1 Mi characters across retained snapshots. A project exceeding a storage bound remains at its prior age with an unavailable baseline until a bounded baseline can be re-established. Diff fallback thresholds are 12,000 combined lines, 2,000,000 retained frontier cells, and edit distance 1,024; exceeding a diff bound counts whole-file additions and deletions.
- Edited rename recognition is intentionally conservative: without reliable rename evidence, deleted and added paths are independent; exact-content moves retain zero churn. No M0012 policy, sampler algorithm, acceptance protocol, or rule version changes were made by this correction.
- The nominal 365 age scale and its effectiveness remain unvalidated by design; this is reserved for M0013.
- PR review remains pending.
