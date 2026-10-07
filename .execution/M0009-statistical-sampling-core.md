# Execution Ledger — M0009 Statistical Sampling Core

Primary milestone: `docs/milestones/M0009-statistical-sampling-core.md`

This file is operational implementation state. It is not project authority and does not amend the milestone.

Planning seeds the lossless obligation registry, evidence cases, and validation gates. Implementation owns work-package decomposition, concrete mechanics, evidence, status progression, and resume state.

The execution ledger may compress work. It must not compress obligations.

## Milestone Obligation Registry

Planner-owned columns are `ID`, `Type`, and `Obligation`. Implementation must not delete, merge, renumber, paraphrase, or replace planner-seeded obligation rows.

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | Statistical sampling is implemented as small shared internal mechanics consumed explicitly through the existing rule/context architecture; M0009 introduces no generalized rule pipeline, sampling-rule hierarchy, descriptor DSL, dependency graph, reflection discovery, or DI framework. |  |  |  | pending |
| AC-02 | acceptance | Sampling history is an explicit durable service separate from transient `RepositorySession` facts, is loaded lazily, and does not create/read/write sampling state when unused. |  |  |  | pending |
| AC-03 | acceptance | Sampling state uses a versioned human-inspectable JSON file under `.hygiene/.state/sampling.json`, stages changes during execution, commits atomically after successful analysis, detects malformed/unsupported state, and leaves no partial mutation after failure/cancellation. |  |  |  | pending |
| AC-04 | acceptance | A documented SHA-256-based derivation maps the repository sampling seed and stable sampling keys to reproducible uniform values strictly in `(0,1)` and exponential thresholds `T=-ln(U)`; fixed test vectors pin the encoding and numeric behavior. |  |  |  | pending |
| AC-05 | acceptance | Subject-state sampling accumulates non-negative rule-supplied hazard per stable subject and reports the current generation due exactly when cumulative hazard reaches its derived exponential threshold. |  |  |  | pending |
| AC-06 | acceptance | A due subject remains due until an explicit matching observation is recorded; merely selecting/reporting it does not reset hazard, and stale/duplicate observation tickets cannot advance state. |  |  |  | pending |
| AC-07 | acceptance | A valid subject observation resets accumulated hazard, advances generation exactly once, records permitted lightweight observation metadata, and derives the next independent threshold from the new generation. |  |  |  | pending |
| AC-08 | acceptance | When due subjects exceed a caller budget, the sampler selects deterministically by inspection debt/threshold overshoot with stable tie-breaking, while unselected subjects remain due. |  |  |  | pending |
| AC-09 | acceptance | Aggregate sampling persists state only per structural population unit (scope plus optional cohort) and does not persist a row/index for each concrete subject. |  |  |  | pending |
| AC-10 | acceptance | Aggregate sampling accepts non-negative rule-supplied hazard mass and computes due inspection events using sequential generation-derived exponential thresholds; large residual hazard can make multiple events due without consuming them merely by calculation/selection. |  |  |  | pending |
| AC-11 | acceptance | Each valid observed aggregate event consumes exactly its current generation threshold from residual hazard, advances generation exactly once, preserves remaining hazard, and rejects stale/duplicate event observations. |  |  |  | pending |
| AC-12 | acceptance | A due aggregate generation can deterministically select from current concrete subjects without persisting their identities; multiple due events in one workload avoid duplicate concrete subjects while alternatives exist, and later independent samples may repeat subjects. |  |  |  | pending |
| AC-13 | acceptance | Shared aggregate evidence mechanics support finite non-negative pass/fail effective counts, retention factors in `[0,1]`, pass/fail observation updates, and rule-owned Beta-prior calculations without claiming a strict stationary posterior after discounting. |  |  |  | pending |
| AC-14 | acceptance | Shared sampling code contains no product-wide age/change/complexity/risk weight model, cohort policy, prior values, or configurable sampling parameters; future rules calculate their own hazard and evidence-retention inputs in ordinary code. |  |  |  | pending |
| AC-15 | acceptance | Sampling state compatibility includes rule identity/version and sampling algorithm/model version; incompatible or deleted state resets conservatively and cannot manufacture historical confidence. |  |  |  | pending |
| AC-16 | acceptance | Sampling state is not pruned or treated as absent merely because a subject/population is missing from a changed/explicit partial run; any reconciliation/pruning seam requires an explicitly complete population view. |  |  |  | pending |
| AC-17 | acceptance | All existing rule IDs/versions, findings, configuration, summary-review SHA-256 ranking, exact five-item maximum sample, fingerprints, batch identities, expansion, handoff, profile behavior, and rewrite behavior remain regression-compatible. |  |  |  | pending |
| AC-18 | acceptance | Deterministic large-N tests with fixed inputs demonstrate the exponential threshold distribution and proportional aggregate event behavior within explicit non-flaky tolerances, in addition to exact state-machine/unit tests. |  |  |  | pending |
| AC-19 | documentation | `docs/specs/SAMPLING.md` becomes the authoritative sampling mechanics contract; `docs/ARCHITECTURE.md`, `docs/ENGINEERING.md`, `docs/TERMINOLOGY.md`, and milestone/index documentation consistently describe the implemented boundary while research remains rationale/provenance. |  |  |  | pending |
| AC-20 | acceptance | M0009 adds no database/binary index, runtime statistics dependency, new rule, rule semantic change, public plugin API, generic scheduler, model client, broad repository index, or unrelated cleanup. |  |  |  | pending |
| REV-01 | review | Project owner/delegate confirms that the sampling subsystem is mathematically defensible, BORING to consume from a future rule, clearly separates due work from observed evidence, preserves the normal-repository lightweight-state goal, and does not create a hidden rule framework. | Human review |  | VAL-08 | pending |

## Evidence Case Registry

Planner-owned columns are `ID`, `Parent obligation`, and `Required evidence case`. Implementation must not delete, merge, renumber, paraphrase, or replace planner-seeded evidence cases.

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-02a | AC-02 | running the accepted current production rule set without a statistical-sampling consumer does not create/load `.hygiene/.state/sampling.json` or otherwise add sampling work | VAL-01, VAL-04 |  | pending |
| EC-03a | AC-03 | injected failure/cancellation after staged sampling mutation but before commit leaves the original sampling file byte-for-byte unchanged | VAL-01, VAL-04 |  | pending |
| EC-03b | AC-03 | malformed/unsupported sampling JSON fails clearly and is not replaced with fresh evidence unless explicitly reset | VAL-01, VAL-04 |  | pending |
| EC-04a | AC-04 | fixed repository seed/key/generation vectors produce exact documented uniform values and exponential thresholds across repeated runs | VAL-01 |  | pending |
| EC-04b | AC-04, AC-18 | fixed large-N deterministic vectors approximate `P(T <= H) = 1 - exp(-H)` at multiple H values within documented tolerances | VAL-02 |  | pending |
| EC-06a | AC-06 | subject crosses threshold, is selected/reported repeatedly without observation, and remains due with unchanged generation until a matching observation arrives | VAL-01 |  | pending |
| EC-07a | AC-07 | valid subject observation resets hazard and advances one generation; duplicate/stale ticket cannot advance again | VAL-01 |  | pending |
| EC-08a | AC-08 | budget smaller than due-subject count selects the highest overshoot deterministically and leaves every non-selected due subject due | VAL-01 |  | pending |
| EC-09a | AC-09 | an aggregate population containing many transient concrete subjects persists only one population/cohort record and no concrete-subject identity list | VAL-01, VAL-07 |  | pending |
| EC-10a | AC-10 | aggregate hazard large enough for several sequential thresholds reports several due generations without mutating generation/hazard merely by calculation | VAL-01 |  | pending |
| EC-11a | AC-11 | observing sequential aggregate tickets consumes their exact thresholds one at a time, retains residual hazard, and rejects duplicate/out-of-order advancement | VAL-01 |  | pending |
| EC-12a | AC-12 | deterministic concrete-subject ranking is stable for the same seed/unit/generation, uses no persistent candidate index, avoids within-workload duplicates when alternatives exist, and permits later-generation repeats | VAL-01 |  | pending |
| EC-13a | AC-13 | retention 1 preserves evidence, retention 0 returns to prior-only evidence before the next observation, intermediate retention scales both pass/fail evidence, and posterior mean arithmetic matches the documented formula | VAL-01 |  | pending |
| EC-15a | AC-15 | rule-version or algorithm-version mismatch discards/inactivates incompatible evidence conservatively; deleting state initializes no historical pass/fail confidence | VAL-01, VAL-04 |  | pending |
| EC-16a | AC-16 | a partial changed/explicit view omitting previously tracked units does not delete them; explicit complete-population reconciliation can remove truly absent state if that seam is implemented | VAL-01, VAL-04 |  | pending |
| EC-17a | AC-17 | existing quality/German semantic-review check -> expand -> handoff behavior retains exact deterministic population ranking/sample/fingerprint semantics and five-item maximum | VAL-03, VAL-05 |  | pending |
| EC-18a | AC-18 | fixed deterministic aggregate simulation with hazard rates in a known ratio produces event counts in the corresponding ratio within a documented statistical tolerance | VAL-02 |  | pending |
| EC-19a | AC-19 | sampling specification, architecture, engineering, terminology, milestone, and live source agree on due/observation semantics, persistence boundary, and the two supported algorithms | VAL-07 |  | pending |

## Validation Gates

| ID | Required validation | Target/locus | Proves evidence units | Status | Evidence |
|---|---|---|---|---|---|
| VAL-01 | focused Core unit/state-machine tests for deterministic random derivation, subject sampler, population sampler, tickets, discounted evidence, version/reset behavior, lazy sampling session, and persistence | Tier 1 / Windows 11 + .NET 11 | AC-02..AC-16; EC-02a..EC-16a | pending |  |
| VAL-02 | deterministic large-N statistical tests with fixed seeds/vectors and explicit tolerances | Tier 1 / Windows 11 + .NET 11 | AC-04, AC-10, AC-18; EC-04b, EC-18a | pending |  |
| VAL-03 | existing Core + built CLI regression, including exact semantic-review ranking/sample/expand/handoff behavior | Tier 1 / Windows 11 + .NET 11 | AC-01, AC-17, AC-20; EC-17a | pending |  |
| VAL-04 | isolated SDK-style Git fixture covering lazy unused state, successful staged persistence, failure rollback, partial-scope non-pruning, and state reset/corruption behavior | Tier 3 / Windows 11 + .NET 11 + Git | AC-02, AC-03, AC-15, AC-16 | pending |  |
| VAL-05 | exact locally packed/installed current tool representative workflow | Tier 4 / isolated Windows consumer repository | AC-17, AC-20 | pending |  |
| VAL-06 | `./eng/validate.ps1` plus final `git diff --check` | Tier 2 / complete repository | AC-17..AC-20 | pending |  |
| VAL-07 | direct source/document review of subsystem simplicity, rule-policy separation, state shape, and authority consistency | repository review | AC-01, AC-09, AC-14, AC-19, AC-20; EC-09a, EC-19a | pending |  |
| VAL-08 | `REV-M0009-COMPLETION` | Human / project owner or delegate | REV-01 | pending |  |

## Work Packages

Implementation-owned. Derive bounded work packages after reading the milestone and live post-M0008 source.

## Resume Point

Current state:

```text
READY
```

Prerequisite status:

`REV-M0008-COMPLETION` is approved and durable on `main` at planning baseline `a89f4aa217b238f45c934a999282f775e37584da`.

Next action:

1. reread M0009 and verify obligation/evidence-case equality;
2. inspect the live post-M0008 architecture;
3. derive implementation work packages;
4. begin the statistical-sampling core implementation.

Known agent-resolvable gaps:

None.

External blockers or planning escalations:

None.
