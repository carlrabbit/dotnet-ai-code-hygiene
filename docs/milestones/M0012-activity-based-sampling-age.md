# M0012 — Activity-Based Sampling Age

**State:** planned
**Mode:** AI-executed, human-reviewed
**Depends on:** M0011 completion

## Goal
Replace current elapsed-days hazard in the three sampled semantic rules with a cheap, project-scoped **abstract activity-age** approximation. Do not change exponential thresholds, observation tickets, statistical guarantees, or deterministic review selection semantics outside the new hazard contributions.

## Accepted design
- 365 age units represent a nominal *activity review cycle*, **never** 365 calendar days.
- Use Git/source churn on eligible non-generated C# source per owning project. Proposed baseline: `ageUnits = 365 * (addedLOC + deletedLOC) / max(referenceProjectLOC, floor)`. Choose/document a small deterministic denominator floor and reference snapshot convention at implementation planning time; no per-repository configuration.
- Record observed Git/source baseline and idempotent project activity deltas in transparent lightweight state; exact representation is implementation freedom. Handle replay, no-op runs, staged/uncommitted edits, new/deleted projects, renames, missing Git, shallow history, and rewritten ancestry without inventing activity. Define and test a conservative fallback.
- Provide activity exposure through shared repository/session facts. Rule policy remains located with its rule; do not introduce a generic risk DSL, background polling, network APIs, or per-commit state database.
- Do not reinterpret the existing sampler's Unix-millisecond cursor as age units. Preserve generic elapsed-accrual mechanics for future rules that explicitly want time.
- Summary-quality, German-summary, and BORINGness rules each own the inclusion/weight of age and retain existing first-evaluation, fingerprint-change, and accepted-observation semantics where appropriate. Explicitly test and document no double counting.
- Bump affected rule/model versions and invalidate incompatible accumulated evidence in the existing supported reset mechanism. Runtime specs must be updated *with implementation*, not in advance.

## Acceptance criteria
1. A year of inactivity alone does not increase hazard for these three rules.
2. Identical repeated checks, including after restart, do not add activity age.
3. Changes in project A do not age project B, including within one repository.
4. A bounded known source diff produces reproducible age units and hazard; tests cover changed LOC, normalization, excluded/generated sources, and large/small projects.
5. Rule-specific policies are tested: no-age, age-contributing, and age-primary behavior are possible without mandatory policy machinery; only production rule behavior approved for this milestone is implemented.
6. New/deleted/renamed files/projects, uncommitted edits, shallow/no Git, rebases, baseline loss, and concurrent state updates have documented conservative results, and no invented wall-clock exposure.
7. Existing due/observation/ticket persistence and rollback-safety tests remain green; migration/reset behavior is tested.
8. Deterministic benchmarks establish overhead is reasonable for normal project and monorepo scans, without a history-indexing service.
9. Help, specifications, rule rationale, and examples accurately distinguish abstract age units from elapsed days.
10. The implementation records a stable way for M0013 to replay and compare policies without production telemetry or a public policy-configuration DSL.

## Boundaries
The formula and 365-unit scale are provisional 80/20 engineering choices. This milestone establishes working behavior, not empirical proof of optimal review frequency. No dependency/agent-generation streams, NuGet queries, external services, or automatic framework-version clock. Existing sampler mechanism stays unchanged.

## Handoff
After implementation is validated and merged, execute M0013 as a separate **experiment** with reproducible evidence. Do not claim that M0013 has already been run.
