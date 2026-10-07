# Execution Ledger — M0009 Statistical Sampling Core

Primary milestone: `docs/milestones/M0009-statistical-sampling-core.md`

This file is operational implementation state. It is not project authority and does not amend the milestone.

Planning seeds the lossless obligation registry, evidence cases, and validation gates. Implementation owns work-package decomposition, concrete mechanics, evidence, status progression, and resume state.

The execution ledger may compress work. It must not compress obligations.

## Milestone Obligation Registry

Planner-owned columns are `ID`, `Type`, and `Obligation`. Implementation must not delete, merge, renumber, paraphrase, or replace planner-seeded obligation rows.

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | Statistical sampling is implemented as small shared internal mechanics consumed explicitly through the existing rule/context architecture; M0009 introduces no generalized rule pipeline, sampling-rule hierarchy, descriptor DSL, dependency graph, reflection discovery, or DI framework. | WP-03, WP-04 | Internal `Sampling` mechanics and explicit `RuleContext.Sampling` access; no catalog/framework changes. | VAL-01; VAL-03; VAL-07 | complete |
| AC-02 | acceptance | Sampling history is an explicit durable service separate from transient `RepositorySession` facts, is loaded lazily, and does not create/read/write sampling state when unused. | WP-04 | Lazy `RuleContext` property; SamplingTests lazy RuleContext and isolated SDK/Git fixture. | VAL-01; VAL-04 | complete |
| AC-03 | acceptance | Sampling state uses a versioned human-inspectable JSON file under `.hygiene/.state/sampling.json`, stages changes during execution, commits atomically after successful analysis, detects malformed/unsupported state, and leaves no partial mutation after failure/cancellation. | WP-04 | SamplingSession versioned JSON + atomic replace/concurrency check; rollback, corrupt-file, and explicit discard-boundary tests. | VAL-01; VAL-04 | complete |
| AC-04 | acceptance | A documented SHA-256-based derivation maps the repository sampling seed and stable sampling keys to reproducible uniform values strictly in `(0,1)` and exponential thresholds `T=-ln(U)`; fixed test vectors pin the encoding and numeric behavior. | WP-01, WP-02, WP-05 | SAMPLING.md canonical encoding/vector; SamplingTests exact U/T vector and endpoint test. | VAL-01; VAL-02 | complete |
| AC-05 | acceptance | Subject-state sampling accumulates non-negative rule-supplied hazard per stable subject and reports the current generation due exactly when cumulative hazard reaches its derived exponential threshold. | WP-03 | SubjectHazardSampler.AddHazard/Due; threshold boundary and subject ticket tests. | VAL-01 | complete |
| AC-06 | acceptance | A due subject remains due until an explicit matching observation is recorded; merely selecting/reporting it does not reset hazard, and stale/duplicate observation tickets cannot advance state. | WP-03 | Subject due remains pure until matching Observe; repeated due/ticket generation and stale duplicate tests. | VAL-01 | complete |
| AC-07 | acceptance | A valid subject observation resets accumulated hazard, advances generation exactly once, records permitted lightweight observation metadata, and derives the next independent threshold from the new generation. | WP-03 | Subject Observe resets hazard and increments generation once; stale observation test. | VAL-01 | complete |
| AC-08 | acceptance | When due subjects exceed a caller budget, the sampler selects deterministically by inspection debt/threshold overshoot with stable tie-breaking, while unselected subjects remain due. | WP-03 | SelectDue sorts by hazard minus threshold, stable ID tie-break; budget/debt test leaves other subject due. | VAL-01 | complete |
| AC-09 | acceptance | Aggregate sampling persists state only per structural population unit (scope plus optional cohort) and does not persist a row/index for each concrete subject. | WP-03 | PopulationHazardState is one aggregate unit record; aggregate test uses transient candidate list and asserts one persisted-state shape. | VAL-01; VAL-07 | complete |
| AC-10 | acceptance | Aggregate sampling accepts non-negative rule-supplied hazard mass and computes due inspection events using sequential generation-derived exponential thresholds; large residual hazard can make multiple events due without consuming them merely by calculation/selection. | WP-03 | DueEvents evaluates sequential generation thresholds without state mutation; large residual multi-event test. | VAL-01; VAL-02 | complete |
| AC-11 | acceptance | Each valid observed aggregate event consumes exactly its current generation threshold from residual hazard, advances generation exactly once, preserves remaining hazard, and rejects stale/duplicate event observations. | WP-03 | Population Observe consumes one threshold per matching ticket; sequential residual and duplicate ticket tests. | VAL-01 | complete |
| AC-12 | acceptance | A due aggregate generation can deterministically select from current concrete subjects without persisting their identities; multiple due events in one workload avoid duplicate concrete subjects while alternatives exist, and later independent samples may repeat subjects. | WP-03 | SelectSubjects hashes transient IDs by due generation, avoids workload duplicates, and retains no candidate index; replay/one-candidate repeat tests. | VAL-01 | complete |
| AC-13 | acceptance | Shared aggregate evidence mechanics support finite non-negative pass/fail effective counts, retention factors in `[0,1]`, pass/fail observation updates, and rule-owned Beta-prior calculations without claiming a strict stationary posterior after discounting. | WP-03 | DiscountedEvidence validates retention and updates effective pass/fail counts; retention 0/0.5/1 and Beta mean tests. | VAL-01 | complete |
| AC-14 | acceptance | Shared sampling code contains no product-wide age/change/complexity/risk weight model, cohort policy, prior values, or configurable sampling parameters; future rules calculate their own hazard and evidence-retention inputs in ordinary code. | WP-03 | Sampling APIs accept rule hazard/retention inputs only; source review confirms no feature weights, cohort policy, or prior configuration. | VAL-07 | complete |
| AC-15 | acceptance | Sampling state compatibility includes rule identity/version and sampling algorithm/model version; incompatible or deleted state resets conservatively and cannot manufacture historical confidence. | WP-04 | SamplingSession keys state by rule/model versions; incompatible/deleted state starts with a new epoch/seed, zero hazard/evidence, and no cursor; stale-ticket and reset-contract tests. | VAL-01; VAL-04 | complete |
| AC-16 | acceptance | Sampling state is not pruned or treated as absent merely because a subject/population is missing from a changed/explicit partial run; any reconciliation/pruning seam requires an explicitly complete population view. | WP-04 | No prune/reconciliation code exists; updates add/change only explicitly requested units; partial-view retention test. | VAL-01; VAL-04 | complete |
| AC-17 | acceptance | All existing rule IDs/versions, findings, configuration, summary-review SHA-256 ranking, exact five-item maximum sample, fingerprints, batch identities, expansion, handoff, profile behavior, and rewrite behavior remain regression-compatible. | WP-05 | Production semantic-review code untouched; existing LifecycleTests and CliProcessTests cover ranking, five-item sample, fingerprints, expansion, handoff, profile and rewrite behavior. | VAL-03; VAL-05; VAL-06 | complete |
| AC-18 | acceptance | Deterministic large-N tests with fixed inputs demonstrate the exponential threshold distribution and proportional aggregate event behavior within explicit non-flaky tolerances, in addition to exact state-machine/unit tests. | WP-02, WP-05 | Fixed 100,000-threshold tests at four H values and 50,000-unit 1:2 hazard-rate simulation use documented fixed tolerances. | VAL-02 | complete |
| AC-19 | documentation | `docs/specs/SAMPLING.md` becomes the authoritative sampling mechanics contract; `docs/ARCHITECTURE.md`, `docs/ENGINEERING.md`, `docs/TERMINOLOGY.md`, and milestone/index documentation consistently describe the implemented boundary while research remains rationale/provenance. | WP-01, WP-05 | SAMPLING.md plus Architecture/Engineering/Terminology/SPECS/Milestone index reviewed against source. | VAL-07 | complete |
| AC-20 | acceptance | M0009 adds no database/binary index, runtime statistics dependency, new rule, rule semantic change, public plugin API, generic scheduler, model client, broad repository index, or unrelated cleanup. | WP-01..WP-05 | Diff/source review: no dependency, rule, database, index, plugin, scheduler, model client, or current-rule integration added. | VAL-03; VAL-05; VAL-06; VAL-07 | complete |
| REV-01 | review | Project owner/delegate confirms that the sampling subsystem is mathematically defensible, BORING to consume from a future rule, clearly separates due work from observed evidence, preserves the normal-repository lightweight-state goal, and does not create a hidden rule framework. | WP-05 | Owner review package assembled; approval intentionally pending at REV-M0009-COMPLETION. | VAL-08 | pending |

## Evidence Case Registry

Planner-owned columns are `ID`, `Parent obligation`, and `Required evidence case`. Implementation must not delete, merge, renumber, paraphrase, or replace planner-seeded evidence cases.

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-02a | AC-02 | running the accepted current production rule set without a statistical-sampling consumer does not create/load `.hygiene/.state/sampling.json` or otherwise add sampling work | VAL-01, VAL-04 | SamplingTests: rule context remains `HasSampling=false`, no state file; isolated SDK/Git fixture loads no sampling until explicit access; full current production catalog has no consumer. | complete |
| EC-03a | AC-03 | injected failure/cancellation after staged sampling mutation but before commit leaves the original sampling file byte-for-byte unchanged | VAL-01, VAL-04 | SamplingTests: injected exception immediately before atomic replacement leaves `sampling.json` byte-for-byte equal to the pre-commit baseline; internal Check/ExpandReview/ignore validation/listing use discard and preserve persisted sampling bytes. | complete |
| EC-03b | AC-03 | malformed/unsupported sampling JSON fails clearly and is not replaced with fresh evidence unless explicitly reset | VAL-01, VAL-04 | SamplingTests: malformed `{` raises ProductException and file bytes remain `{`; deletion then initializes zero evidence. | complete |
| EC-04a | AC-04 | fixed repository seed/key/generation vectors produce exact documented uniform values and exponential thresholds across repeated runs | VAL-01 | SamplingTests and SAMPLING.md: seed 00..1F, key test, generation 7 => digest D4EC04956522E039638C20F9E75D089F2CBDA28D75E4BF085802ADFCDFF1A7B2, U 0.8317263474210779, T 0.18425180161313817; repeated calls match. | complete |
| EC-04b | AC-04, AC-18 | fixed large-N deterministic vectors approximate `P(T <= H) = 1 - exp(-H)` at multiple H values within documented tolerances | VAL-02 | SamplingTests: 100,000 fixed hash-derived thresholds at H=0.1, 0.5, 1, 2; each observed probability is within 0.006 of 1-exp(-H). | complete |
| EC-06a | AC-06 | subject crosses threshold, is selected/reported repeatedly without observation, and remains due with unchanged generation until a matching observation arrives | VAL-01 | SamplingTests: due ticket reports same generation until observed; selected subject remains due with unchanged generation; only explicit matching observation advances it. | complete |
| EC-07a | AC-07 | valid subject observation resets hazard and advances one generation; duplicate/stale ticket cannot advance again | VAL-01 | SamplingTests: subject observation resets hazard, increments generation once, and duplicate ticket returns false. | complete |
| EC-08a | AC-08 | budget smaller than due-subject count selects the highest overshoot deterministically and leaves every non-selected due subject due | VAL-01 | SamplingTests: budget 1 selects b with larger threshold overshoot; a non-selected due subject remains due; stable key order is the tie-breaker in source. | complete |
| EC-09a | AC-09 | an aggregate population containing many transient concrete subjects persists only one population/cohort record and no concrete-subject identity list | VAL-01, VAL-07 | SamplingTests: a population unit with several transient candidates has exactly one `PopulationHazardState`; serialized model stores only population states, never candidate IDs. | complete |
| EC-10a | AC-10 | aggregate hazard large enough for several sequential thresholds reports several due generations without mutating generation/hazard merely by calculation | VAL-01 | SamplingTests: hazard 12 yields multiple sequential due generations while persisted generation remains 0 and residual hazard remains 12. | complete |
| EC-11a | AC-11 | observing sequential aggregate tickets consumes their exact thresholds one at a time, retains residual hazard, and rejects duplicate/out-of-order advancement | VAL-01 | SamplingTests: sequential aggregate tickets each consume the current threshold, retain residual hazard/evidence, and reject duplicate advancement. | complete |
| EC-12a | AC-12 | deterministic concrete-subject ranking is stable for the same seed/unit/generation, uses no persistent candidate index, avoids within-workload duplicates when alternatives exist, and permits later-generation repeats | VAL-01 | SamplingTests: repeated same unit/generation inputs return the same ranking; multi-event selection avoids duplicate IDs while alternatives exist; a single candidate can recur across generations; no candidate index exists in state. | complete |
| EC-13a | AC-13 | retention 1 preserves evidence, retention 0 returns to prior-only evidence before the next observation, intermediate retention scales both pass/fail evidence, and posterior mean arithmetic matches the documented formula | VAL-01 | SamplingTests: retention 1 preserves prior counts before adding, 0 returns to prior-only state before the next observation, 0.5 discounts both pass/fail, and Beta mean is checked arithmetically. | complete |
| EC-15a | AC-15 | rule-version or algorithm-version mismatch discards/inactivates incompatible evidence conservatively; deleting state initializes no historical pass/fail confidence | VAL-01, VAL-04 | SamplingTests: incompatible version creates a new epoch with zero hazard/evidence/cursor and rejects the old ticket; deletion creates a new seed/epoch, rejects the old ticket at matching generation, and persists only new-state values. Final contract explicitly promises no inherited confidence, not increased near-term inspection pressure. | complete |
| EC-16a | AC-16 | a partial changed/explicit view omitting previously tracked units does not delete them; explicit complete-population reconciliation can remove truly absent state if that seam is implemented | VAL-01, VAL-04 | SamplingTests and isolated fixture: updating scope-two preserves scope-one; implementation has no pruning/reconciliation seam, so absence from partial views is not interpreted as deletion. | complete |
| EC-17a | AC-17 | existing quality/German semantic-review check -> expand -> handoff behavior retains exact deterministic population ranking/sample/fingerprint semantics and five-item maximum | VAL-03, VAL-05 | Full Core + CLI regression passed; LifecycleTests exact sample/fingerprint/repeat/expand and CliProcessTests check/expand/handoff remain unchanged; installed current CLI workflow passed. | complete |
| EC-18a | AC-18 | fixed deterministic aggregate simulation with hazard rates in a known ratio produces event counts in the corresponding ratio within a documented statistical tolerance | VAL-02 | SamplingTests: deterministic 50,000-unit populations at hazards 0.01 and 0.02; event-count ratio must remain within 0.18 of 2.0 (fixed-seed Bernoulli sampling tolerance). | complete |
| EC-19a | AC-19 | sampling specification, architecture, engineering, terminology, milestone, and live source agree on due/observation semantics, persistence boundary, and the two supported algorithms | VAL-07 | Direct source/document review confirms SAMPLING.md and ARCHITECTURE/ENGINEERING/TERMINOLOGY/SPECS/index agree with state, due/observation, and two-model semantics. | complete |

## Validation Gates

| ID | Required validation | Target/locus | Proves evidence units | Status | Evidence |
|---|---|---|---|---|---|
| VAL-01 | focused Core unit/state-machine tests for deterministic random derivation, subject sampler, population sampler, tickets, discounted evidence, version/reset behavior, lazy sampling session, and persistence | Tier 1 / Windows 11 + .NET 11 | AC-02..AC-16; EC-02a..EC-16a | passed | Passed: full Windows/.NET 11 Release suite; Core 61/61 includes deterministic vectors, state machines, elapsed-cursor idempotence, reset/version stale-ticket rejection, multi-version rejection, read-only revalidation, persistence/corruption, and isolated SDK/Git fixture. |
| VAL-02 | deterministic large-N statistical tests with fixed seeds/vectors and explicit tolerances | Tier 1 / Windows 11 + .NET 11 | AC-04, AC-10, AC-18; EC-04b, EC-18a | passed | Passed: deterministic fixed-seed 100,000-sample exponential tests at four hazards and 50,000-unit aggregate hazard-rate ratio test. |
| VAL-03 | existing Core + built CLI regression, including exact semantic-review ranking/sample/expand/handoff behavior | Tier 1 / Windows 11 + .NET 11 | AC-01, AC-17, AC-20; EC-17a | passed | Passed: Core 61/61 and built CLI 22/22; review fingerprints/ranking/five-item cap/expand/handoff regression coverage retained. |
| VAL-04 | isolated SDK-style Git fixture covering lazy unused state, successful staged persistence, failure rollback, partial-scope non-pruning, and state reset/corruption behavior | Tier 3 / Windows 11 + .NET 11 + Git | AC-02, AC-03, AC-15, AC-16 | passed | Passed: SamplingTests isolated `.git` + SDK project fixture covers lazy use, successful staged persistence, partial unit retention; state fixture covers pre-replace rollback, corruption, delete/new-seed stale-ticket rejection, incompatible-version reset, and zero-hazard/evidence/cursor contract. LifecycleTests confirms ExpandReview and ignore validation/listing leave sampling bytes unchanged. |
| VAL-05 | exact locally packed/installed current tool representative workflow | Tier 4 / isolated Windows consumer repository | AC-17, AC-20 | passed | Passed in eng/validate.ps1: exact 0.5.0 package packed, installed into isolated tool path, and representative format/normalize/bootstrap/update/rules/check/review/expand/handoff workflows passed. |
| VAL-06 | `./eng/validate.ps1` plus final `git diff --check` | Tier 2 / complete repository | AC-17..AC-20 | passed | Passed: `eng/validate.ps1` completed successfully (Release build, Core 61/61, CLI 22/22, exact installed-tool workflow and M0006 self-host); final `git diff --check` recorded after reconciliation. |
| VAL-07 | direct source/document review of subsystem simplicity, rule-policy separation, state shape, and authority consistency | repository review | AC-01, AC-09, AC-14, AC-19, AC-20; EC-09a, EC-19a | passed | Freshly reviewed Sampling source, RuleContext/host commit placement, epoch/cursor JSON shape, explicit read-only boundary, unchanged rule catalog/review hashing and `Take(5)`, milestone/spec and repository authority; exact planner registries retained. |
| VAL-08 | `REV-M0009-COMPLETION` | Human / project owner or delegate | REV-01 | pending | Pending: stop at required REV-M0009-COMPLETION human review. |

## Work Packages

### Derived work packages

| ID | Scope | Completion evidence |
|---|---|---|
| WP-01 | Promote the selected mechanics into `docs/specs/SAMPLING.md`; align architecture, engineering, terminology, and specification routing with the lazy, rule-owned boundary. | Authoritative specification and direct documentation consistency review. |
| WP-02 | Implement canonical versioned SHA-256 encoding, uniform `(0,1)` conversion, exponential thresholds, and fixed vectors. | Exact vector test and deterministic large-N exponential probability test. |
| WP-03 | Implement subject and aggregate hazard state machines, due tickets, observation advancement, deterministic debt selection, transient candidate ranking, and discounted evidence. | Focused state-machine, ticket, selection, retention, and aggregate-rate tests. |
| WP-04 | Implement lazy rule-context access, versioned JSON state, conservative compatibility/reset, staged atomic commit, and partial-scope preservation. | Lazy access, persistence round-trip, corruption, rollback, and partial-view tests. |
| WP-05 | Run all required focused, statistical, regression, installed-tool, aggregate, and source/document validation; reconcile planner records and record the required completion review package. | VAL-01..VAL-08 evidence, final source/diff review, and durable completion ledger. |

## Resume Point

Current state:

```text
AWAITING REV-M0009-COMPLETION
```

Prerequisite status:

`REV-M0008-COMPLETION` is approved and durable on `main` at planning baseline `a89f4aa217b238f45c934a999282f775e37584da`.

Next action:

1. project owner/delegate reviews the completion package against REV-M0009-COMPLETION acceptance questions;
2. record the explicit review decision and final milestone outcome.

Known agent-resolvable gaps:

None.

External blockers or planning escalations:

None.

## Completion Evidence

WP-01..WP-04 are implemented. WP-05 validation passed except for the deliberately pending human review gate VAL-08. `eng/validate.ps1` completed on Windows 11 / .NET 11.0.100-rc.1.26425.128 with a clean Release build, Core 61/61 and CLI 22/22 tests, exact locally packed/installed tool validation, and repository self-host checks. Fixed deterministic tests cover the documented hash vector, 100,000 exponential threshold draws at four hazard values, and a 50,000-unit 1:2 aggregate hazard simulation. Regression evidence also covers stale ticket rejection across delete and incompatible reset, elapsed-hazard cursor idempotence, discarded internal revalidation for Check/ExpandReview/ignore validation/listing, multi-version session rejection, and zero-hazard/evidence/cursor reset semantics. `git diff --check` passed. All 21 AC/REV registry rows and all 18 EC rows were compared losslessly with the milestone; no planner-owned ID or wording changed.

Review package: `docs/specs/SAMPLING.md`; `src/DotNetAiCodeHygiene.Core/Sampling/SamplingMechanics.cs`; `src/DotNetAiCodeHygiene.Core/Sampling/SamplingSession.cs`; `RuleContext` and host commit placement; sampling tests; regression evidence in `LifecycleTests` and `CliProcessTests`; and this ledger's AC/EC/VAL mappings.

Current milestone outcome:

```text
AWAITING REV-M0009-COMPLETION
```
