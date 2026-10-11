# Activity-Based Sampling Age

## Status
Accepted planning direction; implementation requires M0012. Not current runtime behavior.

## Decision
Calendar time is not a defensible universal proxy for aging in bursty agentic repositories. Keep the existing statistical hazard and sampling algorithms, but replace the current wall-clock *rule policies* with abstract, activity-derived age units where aging is useful. Age is exposure, not a claim that existing code has deteriorated.

A reference cycle consists of **365 abstract age units**, not 365 days. A rule may use `H_age = ageUnits / 365`; the meaning of a cycle is calibrated empirically rather than tied to a year. First-encounter, changed-fingerprint, accepted-review, and rule-version-reset semantics remain distinct.

## Minimal 80/20 signal
Default to **project-scoped normalized source churn** rather than repository-wide commit counts or calendar time. For a trustworthy pair of Git snapshots, compute changed eligible source lines (additions plus deletions), divided by a stable reference project source-LOC denominator (with a documented floor). One project-equivalent churn volume maps to 365 age units. This is a proposed baseline to validate, not a claim that one churn-equivalent is the correct inspection frequency.

Count activity only once per transition between persisted observed states. Repeated checks of the same source state must not accrue age. When Git cannot establish reliable history (shallow checkout, missing base, rewritten ancestry, dirty worktree), record the limitation explicitly and use a defined conservative current-state/diff fallback or zero activity age; never fabricate changes from elapsed time. Exclude generated code and non-source churn; handle project moves, new projects, large deletions, and denominator changes deterministically. A bounded source snapshot or Git diff may be preferable to history traversal.

Project scope prevents unrelated monorepo work from aging subjects. A subject or aggregate unit inherits the activity exposure of its owning project; aggregate rules may apply population size if justified. Rules choose whether to use age at all, weight it modestly, or rely primarily on it. Avoid charging one physical edit twice through both change fingerprint and activity-age hazard without an explicit policy.

A shared small activity fact/cursor is sufficient. No external telemetry, NuGet history, ecosystem clock, agent-model generations, or general scoring framework. No hidden network dependencies.

## Lifecycle/version boundary
A .NET target-framework transition, a revised hygiene policy, or changed reviewer expectations can justify updating the relevant rule/model version and resetting its sampling state. These are explicit compatibility changes, **not** automatic age signals. New agent generations influence the capability of future reviews; they do not automatically degrade previously written code.

The sampling substrate's generic elapsed cursor is not itself wrong: a future explicitly time-driven rule may use it. M0012 changes current rule-owned policies rather than silently redefining milliseconds as abstract units. If a new generic activity cursor is needed, it must have an explicit unit/name, persistence compatibility, and tests.

## Evidence before claims
Do not claim that the 365-unit scale is statistically calibrated until the M0013 cross-repository experiment is complete. Validate selection behavior on several real repository histories, including long-idle/bursty projects, monorepos, small projects, and repeated no-op checks. Record inputs, versions, sampling seeds, limitations, measured workloads, and false-positive/review-value assessments. Publish both favorable and unfavorable findings.

## Non-goals
- Real-world age prediction, package obsolescence scoring, and continuously tracking ecosystem velocity.
- A universal maintainability score or defect probability.
- Guaranteed reinspection after calendar time for rules that opt out of wall-clock accrual.
- Replacing the two M0009 statistical sampling models.
