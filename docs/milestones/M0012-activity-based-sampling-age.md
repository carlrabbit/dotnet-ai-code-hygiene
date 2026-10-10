# M0012 — Activity-Based Sampling Age

**State:** implementation complete; awaiting human review
**Mode:** AI-executed, human-reviewed  
**Depends on:** M0011 completion

## Goal

Replace elapsed-day hazard in the currently sampled semantic rules with **activity-derived age where useful**, without changing the statistical samplers. Only BORINGness initially consumes project activity age; the two documentation-summary rules retain first-encounter and relevant-fingerprint-change hazard but cease wall-clock accrual.

An age unit is an abstract measure of codebase exposure, **not a day**. The nominal convention is 365 units per project-equivalent source churn; this is an uncalibrated 80/20 assumption awaiting M0013 validation.

## Fixed rule policies

| Rule | First evaluation | Changed rule-relevant fingerprint | Age |
| --- | --- | --- | --- |
| `docs.summary.quality.review` | H += 1 | H += 1 once per observed change | None |
| `docs.summary.language.german.review` | H += 1 | H += 1 once per observed change | None |
| `architecture.boringness.review` | aggregate H += 1 per new source-document unit | aggregate H += 1 once per observed document change | aggregate H += prior observed eligible type count × activityAge / 365 |

Only BORINGness uses activity age in M0012; no configurable coefficients or generic no-age/age-primary mode is required. Its independently measured project activity may include the same underlying change that altered a document fingerprint: these are intentionally distinct contributions (direct evidence invalidation versus background project exposure), and the report/validation must show both contributions separately. Do not count the same activity delta twice through repeated executions.

An accepted review consumes the current sampling ticket according to existing semantics. No failed/uncertain review advances a sampling generation. Retain bounded selection and escalation logic. Increase affected rule versions with the existing compatibility/reset behavior; this is a sampling-policy migration, not reuse of historical elapsed-day hazard.

## Activity measurement and accounting

### Observable project source transitions

For a successful, complete project source observation, compare the current **eligible, non-generated C# source state** with its last persisted observation, independent of commit boundaries. Determine added plus deleted logical source lines from a deterministic line-diff algorithm. A new file contributes added lines, a removed file deleted lines, and a rename with unchanged content contributes zero; renames with edits contribute actual edits. Include tracked, staged, and uncommitted current source changes. Exclude non-source files and recognized generated output using the same documented eligibility convention consistently across runs.

A run observing multiple intervening commits measures the **net observable source transition** between baselines once; it does not claim to recover squashed, reverted, or otherwise unobserved intermediate work. Git can assist mapping/optimization, but the rule must not depend on a full commit-history walk. Missing Git, shallow clones, or rebases do not block operation when the two observed source states remain comparable.

At the **first observation** of a project, record its eligible-source baseline and initialize project activity to zero; do not infer earlier history. For a completely new project, first-encounter rule hazard handles its new candidates. If the previous source baseline is missing or irreconcilable, establish a new baseline without fabricated churn, record the reset, and do not add activity for the unmeasurable interval. A removed project is not evaluated further. Persist project identity and source membership deterministically; moves between projects count as deletion/addition in the affected project snapshots only when both transitions are observed.

### Normalization and counters

For each observed project transition:

```text
changedLOC = addedEligibleSourceLines + deletedEligibleSourceLines
referenceLOC = max(previousEligibleProjectLOC, 100)
deltaAgeUnits = 365 * changedLOC / referenceLOC
projectAgeTotal += deltaAgeUnits
```

Use a fixed denominator floor of **100 eligible C# LOC**, not a repository parameter. Use finite non-negative numeric values and a stable canonical project identity. A project-equivalent normalized churn yields `H_age = 1` before BORINGness population weighting. `365` is a presentation scale; the actual baseline BORINGness per-type hazard contribution equals `changedLOC/referenceLOC`. No time-dependent accrual.

The project maintains a single monotonic activity total and an observed source baseline. Each BORINGness aggregate unit retains an independent last-accounted project-age position. Upon its next **eligible evaluation**, accrue only the positive delta since its previous position, then update its position, including when no review is due. First encounter initializes its position to the current project total, without retrospective exposure. An accepted review changes the sampling generation as usual; the position remains current, so old exposure cannot be charged to the new generation.

A partial scan may advance project activity when the complete eligible project source state is independently available and validated, even while reporting remains restricted to requested targets (including `--changed`). Do not infer a deleted file merely because it was outside project source inspection. If any eligible source cannot be inspected, retain the prior baseline and total. Persist activity baseline/total and sampling positions in an atomic or rollback-safe operation consistent with existing two-file sampling/latest-run publication; concurrent modifications must be detected, never silently overwritten. Failed runs and internal revalidation discard staged activity changes.

Bound persisted source snapshots and diff work. If a project snapshot exceeds the documented fixed storage budget, conservatively rebaseline without adding churn until a comparable bounded snapshot is available. If a file diff exceeds its fixed work budget, use whole-file deletion plus addition as a conservative upper bound and record that fallback in test evidence. Exact-content moves may contribute zero churn. Without reliable rename evidence, count deleted and added paths independently; do not infer an edited rename from an arbitrary path pair.

If a BORINGness unit has no eligible type candidates on its current evaluation, retain whatever historic unit state is needed for safe later accounting; avoid retroactive application of activity to newly created candidates. Document exact reconciliation/reset behavior.

### Scope and compatibility

Project activity is shared across rules through repository/session services, but rule-local logic decides how to use it. In a multi-project repository, activity in project A must not age a type in project B. Multi-target builds and shared/linked source files must not double count activity within a project, and each eligible rule unit must have a deterministic owning project or documented conservative exclusion.

Do not reinterpret existing Unix-millisecond elapsed cursors as abstract age units. Retain generic elapsed-accrual sampler support for future explicitly time-based rules, while removing its use from the three current rules. The shared sampling algorithms, exponential thresholds, ticket formats, explicit accept protocol, and public output contracts stay intact. Store new project-age positions and source baselines with explicit versioned semantics rather than disguising them as time cursors.

No package queries, external network, ecosystem clocks, agent-release clocks, background polling, policy DSL, or Git commit-history database.

## Acceptance criteria

1. A year of unchanged source and repeated identical checks (including process restart) add **zero** activity-age hazard.
2. Project activity is accrued once per complete observable source transition. Repeated, failed, partial, and revalidation scans never double charge or drop previously unaccounted exposure.
3. Changes in project A do not age project B. Deferred evaluation of a unit in A receives precisely its as-yet-unaccounted project exposure.
4. Fixed test vectors prove added/deleted LOC, the 100-LOC floor, previous-size normalization, large projects, renamed/moved/generated files, and first-observation baseline behavior.
5. Staged/uncommitted edits, missing/shallow Git, rebases, baseline loss, new/deleted projects, shared source and multi-target projects have deterministic conservative behavior.
6. The two summary rules accrue **no age**, retaining first/changed-fingerprint hazards; BORINGness uses its fixed prior-count-weighted project activity hazard. Tests show independent source-fingerprint and age contributions.
7. Accepted/failed/uncertain observations, selected versus unselected due work, generation transitions, persistence and stale-ticket protection remain correct.
8. Atomic state publishing, concurrent update detection, rollback after failed checks, and explicit policy-version resets are covered by automated tests; historic elapsed-day hazard is not carried over as if it were activity exposure.
9. Benchmark cold/steady/changed checks on representative single-project and multi-project fixtures. Record runtime and state-size overhead; normal checks must not require replaying Git history.
10. Update the authoritative sampling/semantic review specifications, engineering notes and rule rationale to match executed behavior. Provide reproducible activity/hazard diagnostics or internal test fixtures usable by M0013 without adding a public risk DSL.

## Implementation scope

The implementation agent may select compact state representation and deterministic line-diff mechanics compatible with the above observable behavior. No general activity or telemetry platform. M0012 establishes the mechanism, **not proof of its effectiveness**. M0013 performs comparative historical replay and publishes real evidence.

## Handoff

After M0012 passes implementation review and is merged, run M0013 independently. Its findings may confirm, adjust, or reject activity aging; do not pre-claim success.
