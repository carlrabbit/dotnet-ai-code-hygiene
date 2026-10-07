# M0009 — Statistical Sampling Core

**State:** ready  
**Mode:** AI-executed, human-reviewed  
**Depends on:** completed M0008 rule locality and agent routing hygiene

## Goal

Implement the two statistical sampling algorithms preserved in `docs/research/SAMPLING-RULES.md` as small shared internal mechanics that future rules can opt into without introducing a sampling framework, repository database, or per-rule configuration language.

The milestone must establish:

1. **subject-state hazard sampling** for rules that need individual inspection/reinspection guarantees;
2. **aggregate population hazard sampling** for dense populations where persistent subject identity is disproportionate, with optional cohorts represented as aggregate population units;
3. small, transparent, durable statistical state under `.hygiene/.state/`;
4. deterministic hash-derived pseudo-randomness and reproducible mathematical behavior;
5. explicit separation between a subject/population being **due** for inspection and an inspection result actually being **observed**;
6. a narrow lazy integration seam through the existing modular rule/shared-session architecture.

This milestone implements sampling mechanics only. It does not change any current production rule to use the new algorithms.

## Prerequisite

`REV-M0008-COMPLETION` is approved and durably recorded on `main`. M0008 is complete.

M0009 uses the accepted post-M0008 architecture as its baseline. Do not reopen or alter M0008 implementation semantics as part of M0009.

## Planning baseline

Planning inspected current `main` at:

```text
a89f4aa217b238f45c934a999282f775e37584da
```

The accepted architectural substrate is:

```text
CLI command
-> command policy
-> RepositorySession
   -> target/Git/project/workspace/document context
   -> lazy Roslyn context
   -> lazy immutable session facts

check
-> explicit RuleCatalog
-> independent rule modules
-> host-owned result materialization/persistence
```

M0007 established this architecture and M0008 made rule locality explicit. Sampling must fit into it rather than replacing it with a generalized pipeline.

The current semantic-review specification remains authoritative for existing summary review rules:

```text
population fingerprint
-> SHA-256 deterministic ranking
-> Take(5)
```

That behavior remains unchanged in M0009.

## Authority

Implementation starts from this milestone and reads:

- `docs/ARCHITECTURE.md`
- `docs/ENGINEERING.md`
- `docs/SPECS.md`
- `docs/TERMINOLOGY.md`
- `docs/specs/HYGIENE.md`
- `docs/specs/SEMANTIC-REVIEWS.md`
- `docs/research/SAMPLING-RULES.md`
- `docs/research/RULE-APPLICATION-ARCHITECTURE.md`
- completed M0007 architecture milestone only as compatibility/provenance where needed
- completed M0008 locality milestone only as compatibility/provenance where needed

For this milestone, the sampling research is explicit planning input. M0009 promotes the selected mechanics into authoritative documentation as part of implementation.

## Target architecture

Conceptually:

```text
RuleModule
  owns:
    population semantics
    subject/scope/cohort identity
    hazard/risk calculation
    evidence-retention policy
    rule-specific prior/interpretation

RuleContext
  -> existing RepositorySession
  -> lazy SamplingSession

SamplingSession
  -> SubjectHazardSampler
  -> PopulationHazardSampler
  -> deterministic sampling random source
  -> staged SamplingState
  -> atomic SamplingStateStore
```

The exact type names and file boundaries are implementation-owned.

A likely physical locality is:

```text
src/DotNetAiCodeHygiene.Core/
  Sampling/
    <deterministic random mechanics>
    <subject hazard sampler>
    <population hazard sampler>
    <sampling state/session/store>
    <discounted evidence helper>
```

Do not introduce an `ISamplingRule` hierarchy, sampling descriptor DSL, rule dependency graph, feature registry, scheduler framework, reflection discovery, or DI container.

## Core design principles

### Rules own policy; samplers own mathematics

Shared sampling infrastructure must not know about comments, documentation, classes, complexity, Git semantics, AI review questions, or rule-specific risk factors.

Rules may calculate hazard from ordinary C# logic using their domain context.

The shared algorithms own only generic mechanics such as:

- cumulative hazard accounting;
- exponential thresholds;
- deterministic pseudo-random derivation;
- due-event calculation;
- observation advancement;
- stable bounded selection;
- aggregate event counting;
- discounted pass/fail evidence bookkeeping;
- state versioning/persistence.

Do not create a shared configuration object containing generic `AgeWeight`, `ChangeWeight`, `ComplexityWeight`, or similar feature weights.

### Durable sampling state is not a session fact

`RepositorySession` facts are command-local analysis/cache state and remain transient.

Sampling state intentionally survives commands and therefore has a separate explicit persistence boundary.

A rule may use normal session facts to cheaply derive current structural information, then pass rule-owned hazard/evidence inputs into a sampler.

### Due is not observed

Crossing a statistical threshold means inspection work is due.

It does **not** mean an inspection has occurred.

Sampling state must not consume/reset an inspection event merely because:

- a check command ran;
- a review batch was created;
- a subject was selected for a batch;
- a handoff file was created.

Only an explicit observation operation advances the observation-dependent state.

This distinction is mandatory for both algorithms.

## Deterministic sampling random source

Sampling uses a repository-local random seed plus stable inputs to derive pseudo-random values through SHA-256.

Conceptually:

```text
repository sampling seed
rule ID
rule version
sampling algorithm version
sampling model
subject or population-unit identity
generation
[transient candidate identity when ranking concrete population subjects]
```

The implementation must define one canonical byte/string encoding and fixed test vectors.

For exponential thresholds, derive a uniform value strictly inside `(0,1)`, then:

```text
T = -ln(U)
```

A suitable implementation may derive 53 bits and map them using a midpoint transform such as:

```text
U = (n + 0.5) / 2^53
```

The exact correct encoding/bit extraction is implementation-owned but becomes authoritative through `docs/specs/SAMPLING.md` and fixed test vectors.

Do not use process-random `Random` calls for threshold behavior once the repository seed exists.

## Model 1 — subject-state hazard sampling

### Purpose

Use when the rule needs meaningful state for each eligible subject and individual inspection/reinspection probability matters.

### State semantics

A subject record must retain only the small state required by the algorithm, conceptually including:

```text
stable subject identity
last/current content or structural fingerprint where needed
last evaluation time where needed
accumulated hazard
generation
last observation time/outcome where needed
```

Do not persist source text, syntax trees, Roslyn symbols, semantic models, or generic feature vectors.

### Hazard clock

For cumulative hazard `H` and generation-derived threshold `T`:

```text
due <=> H >= T
```

The sampler accepts rule-calculated non-negative hazard increments and accumulates them.

The shared sampler does not define what age/change/risk means.

A non-zero baseline hazard, creation hazard, change hazard, or other policy is rule-owned.

### Due persistence

A due subject stays due until an explicit observation is recorded.

Additional hazard may continue to accumulate while it is due.

A bounded workload must not silently clear subjects that were not selected because a batch budget was exhausted.

### Observation

After a valid observation of the current due generation:

```text
accumulated hazard := 0
generation := generation + 1
record observation metadata/outcome as applicable
```

The next threshold is derived from the new generation.

Stale/out-of-order observation tickets must not mutate newer state.

### Bounded selection

When more subjects are due than the caller's budget, selection must be deterministic.

Use threshold overshoot/inspection debt as the primary urgency measure:

```text
urgency = accumulated hazard - current threshold
```

Use a stable deterministic tie-breaker.

Unselected due subjects remain due.

## Model 2 — aggregate population hazard sampling

### Purpose

Use when the rule needs population-level surveillance and persistent identity for every concrete subject would be disproportionate.

The persistent unit is a structural scope plus an optional cohort.

Examples:

```text
CustomerService / comments / all
CustomerService / comments / new
CustomerService / comments / affected
CustomerService / comments / established
```

Cohorts are ordinary aggregate population units, not a separate sampler implementation.

### No persistent concrete-subject index

Persistent aggregate state may contain population/cohort statistics and fingerprints, but must not contain an identity row for every concrete sampled subject.

Concrete subjects are materialized from current source only after an aggregate inspection event is selected.

### Aggregate hazard

The shared sampler accepts non-negative aggregate hazard increments calculated by the rule.

A rule may calculate an increment as:

```text
population count * expected per-subject hazard
```

or another documented rule-specific model.

The sampler does not own that formula.

### Sequential exponential events and residual hazard

Aggregate hazard represents inspection demand across a population.

Let residual cumulative hazard be `H`. For the current generation, derive threshold `T_g`.

Each threshold crossed represents one due inspection event.

For an **observed** event:

```text
H := H - T_g
generation := generation + 1
```

then evaluate the next generation-derived threshold against the residual hazard.

Large accumulated hazard can therefore make multiple inspections due.

Critically, merely reporting that multiple events are due does not consume them. Due-event calculation is pure with respect to observation advancement; events are consumed only as matching observations are recorded.

This is the aggregate analogue of a Poisson inspection process over integrated hazard.

### Due tickets

The sampler must expose enough stable identity for a caller to correlate an observation with the due generation it satisfies.

A ticket may conceptually include:

```text
rule/model identity
population-unit identity
generation
```

Exact types are implementation-owned.

Stale or duplicate observations must not double-consume hazard.

### Concrete subject selection

For each due aggregate generation, the caller can provide the current concrete subjects in that population unit.

The shared mechanics may deterministically rank candidates from:

```text
repository seed
rule identity/version
algorithm version
population-unit identity
due generation
transient candidate identity
```

and choose a subject uniformly by hash rank.

When several due generations are materialized in one workload, do not choose the same concrete subject twice while unused alternatives exist.

Transient candidate identities used for ranking are not persisted.

Repeats across later independent observations are valid.

## Discounted binary evidence

Aggregate population state may maintain lightweight effective pass/fail evidence for rules whose inspection outcome is binary.

Use a small helper rather than an estimator framework.

Conceptually:

```text
passEvidence
failEvidence
```

For retention factor `d` in `[0,1]`:

```text
passEvidence := d * passEvidence
failEvidence := d * failEvidence
```

A pass adds one pass-equivalent observation. A failure adds one failure-equivalent observation.

A rule may combine this evidence with its own Beta prior:

```text
defect probability ~ Beta(
    alpha0 + failEvidence,
    beta0 + passEvidence)
```

The prior and retention factor are rule policy, not sampler configuration.

After discounting, the result is intentionally described as **discounted/effective Beta evidence**, not as a strict stationary-population posterior.

M0009 does not require credible-interval/CDF machinery or an external statistics dependency.

## Persistence

### Location

Use transparent repository-local state:

```text
.hygiene/.state/sampling.json
```

The directory is already ignored by repository `.gitignore`.

Do not add SQLite, binary indexes, embedded databases, or a persistent Roslyn/repository index.

### Shape

The persisted file must be explicit and versioned, conceptually:

```text
schema version
repository-local sampling seed
per-rule/model state
  rule ID
  rule version
  algorithm/model version
  subject states OR population states
```

Exact JSON record structure is implementation-owned.

### Lazy use

If no enabled rule requests statistical sampling:

- the sampling state file is not required to exist;
- check behavior remains unchanged;
- sampling state must not force workspace/index work.

### Staging and commit

Sampling mutations are staged during command execution and committed atomically only after successful rule analysis/materialization at the host-owned persistence boundary.

If command execution fails or is cancelled before commit, no partial sampling-state mutation remains.

Existing atomic/concurrent-state protection conventions should be reused or factored only as far as justified.

### Missing/corrupt state

Missing state starts conservatively with a new local seed and no historical evidence.

Malformed/unsupported state must fail clearly rather than being silently interpreted as valid evidence.

Deleting sampling state is supported as a conservative reset: it may cause additional future inspection but must never fabricate prior confidence.

### Rule/model invalidation

Persisted sampling evidence must not silently survive an incompatible semantic change.

At minimum, state compatibility is keyed by:

- rule ID;
- rule version;
- sampling model/algorithm version.

An incompatible version resets that rule/model state conservatively.

## Partial reporting scopes and population reconciliation

The existing distinction between reporting scope and readable context remains.

Sampling must not infer that a persisted subject/population disappeared merely because it was absent from:

- a changed-file run;
- an explicit file target;
- an explicit directory target;
- another known partial population view.

If pruning/reconciliation is implemented, it must require an explicitly authoritative complete population view.

Time-based hazard does not require scanning the population every day. A rule may calculate elapsed hazard from persisted evaluation time when the subject/population is next encountered.

## Existing semantic review remains unchanged

M0009 must preserve the accepted behavior of:

- `docs.summary.quality.review`;
- `docs.summary.language.german.review`;
- all review population fingerprints;
- rule-specific SHA-256 ranking;
- exact maximum sample of five;
- batch identities;
- expansion;
- handoff.

The new statistical samplers are not wired into these rules in this milestone.

A later rule-policy milestone may explicitly migrate a rule and advance its semantic contract/version as required.

## Scope

In scope:

- deterministic hash-derived sampling random mechanics;
- subject-state hazard sampler;
- aggregate population/cohort hazard sampler;
- due-ticket/observation distinction;
- aggregate residual-hazard/multiple-event behavior;
- deterministic bounded due selection;
- deterministic transient concrete-subject ranking for aggregate events;
- discounted binary evidence helper;
- lazy sampling session/state store;
- transparent JSON persistence under `.hygiene/.state/`;
- atomic staged commit and corruption/version handling;
- optional safe reconciliation seam for an explicitly complete population;
- focused mathematical/statistical tests;
- architecture/engineering/terminology updates;
- new authoritative `docs/specs/SAMPLING.md`;
- milestone/index/evidence updates.

## Non-goals

Out of scope:

- migrating any existing production rule to statistical sampling;
- changing current semantic-review sample size/ranking;
- adding a new hygiene rule;
- user-configurable hazard weights, thresholds, priors, cohorts, sample size, or sampling model;
- rule descriptor sampling metadata;
- generic risk-feature definitions;
- repository-wide type index;
- Git-history mining beyond what a future rule explicitly chooses;
- Horvitz-Thompson or other design-based population reporting;
- repository-wide defect percentages or confidence claims;
- credible-interval/CDF library work;
- learned/ML sampling policy;
- model/provider invocation;
- database or binary-state backend;
- public plugin/extensibility API;
- general scheduler/DAG;
- DI/reflection framework;
- parallel sampling execution as a design goal;
- broad cleanup unrelated to sampling.

## Acceptance criteria

**AC-01 — Existing architecture preserved.** Statistical sampling is implemented as small shared internal mechanics consumed explicitly through the existing rule/context architecture; M0009 introduces no generalized rule pipeline, sampling-rule hierarchy, descriptor DSL, dependency graph, reflection discovery, or DI framework.

**AC-02 — Lazy durable-state boundary.** Sampling history is an explicit durable service separate from transient `RepositorySession` facts, is loaded lazily, and does not create/read/write sampling state when unused.

**AC-03 — Transparent atomic persistence.** Sampling state uses a versioned human-inspectable JSON file under `.hygiene/.state/sampling.json`, stages changes during execution, commits atomically after successful analysis, detects malformed/unsupported state, and leaves no partial mutation after failure/cancellation.

**AC-04 — Canonical deterministic random derivation.** A documented SHA-256-based derivation maps the repository sampling seed and stable sampling keys to reproducible uniform values strictly in `(0,1)` and exponential thresholds `T=-ln(U)`; fixed test vectors pin the encoding and numeric behavior.

**AC-05 — Subject hazard semantics.** Subject-state sampling accumulates non-negative rule-supplied hazard per stable subject and reports the current generation due exactly when cumulative hazard reaches its derived exponential threshold.

**AC-06 — Subject due/observation separation.** A due subject remains due until an explicit matching observation is recorded; merely selecting/reporting it does not reset hazard, and stale/duplicate observation tickets cannot advance state.

**AC-07 — Subject observation advancement.** A valid subject observation resets accumulated hazard, advances generation exactly once, records permitted lightweight observation metadata, and derives the next independent threshold from the new generation.

**AC-08 — Deterministic bounded subject selection.** When due subjects exceed a caller budget, the sampler selects deterministically by inspection debt/threshold overshoot with stable tie-breaking, while unselected subjects remain due.

**AC-09 — Aggregate state granularity.** Aggregate sampling persists state only per structural population unit (scope plus optional cohort) and does not persist a row/index for each concrete subject.

**AC-10 — Aggregate hazard/event semantics.** Aggregate sampling accepts non-negative rule-supplied hazard mass and computes due inspection events using sequential generation-derived exponential thresholds; large residual hazard can make multiple events due without consuming them merely by calculation/selection.

**AC-11 — Aggregate observation/residual advancement.** Each valid observed aggregate event consumes exactly its current generation threshold from residual hazard, advances generation exactly once, preserves remaining hazard, and rejects stale/duplicate event observations.

**AC-12 — Transient aggregate subject selection.** A due aggregate generation can deterministically select from current concrete subjects without persisting their identities; multiple due events in one workload avoid duplicate concrete subjects while alternatives exist, and later independent samples may repeat subjects.

**AC-13 — Discounted binary evidence.** Shared aggregate evidence mechanics support finite non-negative pass/fail effective counts, retention factors in `[0,1]`, pass/fail observation updates, and rule-owned Beta-prior calculations without claiming a strict stationary posterior after discounting.

**AC-14 — Policy remains rule-owned.** Shared sampling code contains no product-wide age/change/complexity/risk weight model, cohort policy, prior values, or configurable sampling parameters; future rules calculate their own hazard and evidence-retention inputs in ordinary code.

**AC-15 — Safe state compatibility/reset.** Sampling state compatibility includes rule identity/version and sampling algorithm/model version; incompatible or deleted state resets conservatively and cannot manufacture historical confidence.

**AC-16 — Partial-scope safety.** Sampling state is not pruned or treated as absent merely because a subject/population is missing from a changed/explicit partial run; any reconciliation/pruning seam requires an explicitly complete population view.

**AC-17 — Existing rule behavior unchanged.** All existing rule IDs/versions, findings, configuration, summary-review SHA-256 ranking, exact five-item maximum sample, fingerprints, batch identities, expansion, handoff, profile behavior, and rewrite behavior remain regression-compatible.

**AC-18 — Statistical behavior is evidenced.** Deterministic large-N tests with fixed inputs demonstrate the exponential threshold distribution and proportional aggregate event behavior within explicit non-flaky tolerances, in addition to exact state-machine/unit tests.

**AC-19 — Authority promoted.** `docs/specs/SAMPLING.md` becomes the authoritative sampling mechanics contract; `docs/ARCHITECTURE.md`, `docs/ENGINEERING.md`, `docs/TERMINOLOGY.md`, and milestone/index documentation consistently describe the implemented boundary while research remains rationale/provenance.

**AC-20 — No unrelated expansion.** M0009 adds no database/binary index, runtime statistics dependency, new rule, rule semantic change, public plugin API, generic scheduler, model client, broad repository index, or unrelated cleanup.

**REV-01 — Human completion review.** Project owner/delegate confirms that the sampling subsystem is mathematically defensible, BORING to consume from a future rule, clearly separates due work from observed evidence, preserves the normal-repository lightweight-state goal, and does not create a hidden rule framework.

## Required evidence cases

| ID | Parent | Required evidence case |
|---|---|---|
| EC-02a | AC-02 | running the accepted current production rule set without a statistical-sampling consumer does not create/load `.hygiene/.state/sampling.json` or otherwise add sampling work |
| EC-03a | AC-03 | injected failure/cancellation after staged sampling mutation but before commit leaves the original sampling file byte-for-byte unchanged |
| EC-03b | AC-03 | malformed/unsupported sampling JSON fails clearly and is not replaced with fresh evidence unless explicitly reset |
| EC-04a | AC-04 | fixed repository seed/key/generation vectors produce exact documented uniform values and exponential thresholds across repeated runs |
| EC-04b | AC-04, AC-18 | fixed large-N deterministic vectors approximate `P(T <= H) = 1 - exp(-H)` at multiple H values within documented tolerances |
| EC-06a | AC-06 | subject crosses threshold, is selected/reported repeatedly without observation, and remains due with unchanged generation until a matching observation arrives |
| EC-07a | AC-07 | valid subject observation resets hazard and advances one generation; duplicate/stale ticket cannot advance again |
| EC-08a | AC-08 | budget smaller than due-subject count selects the highest overshoot deterministically and leaves every non-selected due subject due |
| EC-09a | AC-09 | an aggregate population containing many transient concrete subjects persists only one population/cohort record and no concrete-subject identity list |
| EC-10a | AC-10 | aggregate hazard large enough for several sequential thresholds reports several due generations without mutating generation/hazard merely by calculation |
| EC-11a | AC-11 | observing sequential aggregate tickets consumes their exact thresholds one at a time, retains residual hazard, and rejects duplicate/out-of-order advancement |
| EC-12a | AC-12 | deterministic concrete-subject ranking is stable for the same seed/unit/generation, uses no persistent candidate index, avoids within-workload duplicates when alternatives exist, and permits later-generation repeats |
| EC-13a | AC-13 | retention 1 preserves evidence, retention 0 returns to prior-only evidence before the next observation, intermediate retention scales both pass/fail evidence, and posterior mean arithmetic matches the documented formula |
| EC-15a | AC-15 | rule-version or algorithm-version mismatch discards/inactivates incompatible evidence conservatively; deleting state initializes no historical pass/fail confidence |
| EC-16a | AC-16 | a partial changed/explicit view omitting previously tracked units does not delete them; explicit complete-population reconciliation can remove truly absent state if that seam is implemented |
| EC-17a | AC-17 | existing quality/German semantic-review check -> expand -> handoff behavior retains exact deterministic population ranking/sample/fingerprint semantics and five-item maximum |
| EC-18a | AC-18 | fixed deterministic aggregate simulation with hazard rates in a known ratio produces event counts in the corresponding ratio within a documented statistical tolerance |
| EC-19a | AC-19 | sampling specification, architecture, engineering, terminology, milestone, and live source agree on due/observation semantics, persistence boundary, and the two supported algorithms |

## Validation gates

| ID | Required validation | Target/locus | Proves |
|---|---|---|---|
| VAL-01 | focused Core unit/state-machine tests for deterministic random derivation, subject sampler, population sampler, tickets, discounted evidence, version/reset behavior, lazy sampling session, and persistence | Tier 1 / Windows 11 + .NET 11 | AC-02..AC-16; EC-02a..EC-16a |
| VAL-02 | deterministic large-N statistical tests with fixed seeds/vectors and explicit tolerances | Tier 1 / Windows 11 + .NET 11 | AC-04, AC-10, AC-18; EC-04b, EC-18a |
| VAL-03 | existing Core + built CLI regression, including exact semantic-review ranking/sample/expand/handoff behavior | Tier 1 / Windows 11 + .NET 11 | AC-01, AC-17, AC-20; EC-17a |
| VAL-04 | isolated SDK-style Git fixture covering lazy unused state, successful staged persistence, failure rollback, partial-scope non-pruning, and state reset/corruption behavior | Tier 3 / Windows 11 + .NET 11 + Git | AC-02, AC-03, AC-15, AC-16 |
| VAL-05 | exact locally packed/installed current tool representative workflow | Tier 4 / isolated Windows consumer repository | AC-17, AC-20 |
| VAL-06 | `./eng/validate.ps1` plus final `git diff --check` | Tier 2 / complete repository | AC-17..AC-20 |
| VAL-07 | direct source/document review of subsystem simplicity, rule-policy separation, state shape, and authority consistency | repository review | AC-01, AC-09, AC-14, AC-19, AC-20; EC-09a, EC-19a |
| VAL-08 | `REV-M0009-COMPLETION` | human / project owner or delegate | REV-01 |

Statistical tests must be deterministic and non-flaky. Their tolerances must be justified by the fixed sample size/distribution being tested rather than loosened until a run passes.

## Implementation guidance

A likely sequence is:

```text
confirm M0008 human approval is durable
-> promote sampling mechanics into docs/specs/SAMPLING.md
-> implement canonical deterministic hash/random primitive with fixed vectors
-> implement subject hazard state machine
-> implement aggregate hazard state machine
-> implement deterministic transient candidate selection
-> implement discounted binary evidence helper
-> implement lazy SamplingSession + JSON state store
-> integrate lazy SamplingSession into RuleContext/host commit boundary
-> add exact state-machine tests
-> add fixed large-N statistical tests
-> prove existing semantic-review behavior unchanged
-> run focused/integration/installed-tool validation
-> reconcile docs/evidence
-> human review
```

This sequence is guidance, not a required work-package decomposition.

Prefer plain records/classes and explicit methods over generic policy abstractions.

If implementing the requested behavior requires a broad scheduler, general feature DSL, persistent repository index, or current-rule semantic migration, stop and return to planning.

## Human review

Canonical review ID:

```text
REV-M0009-COMPLETION
```

Review subject:

- authoritative sampling specification;
- deterministic random primitive and fixed vectors;
- subject-state sampler;
- aggregate population sampler;
- discounted evidence helper;
- persisted JSON state example;
- lazy RuleContext/host integration;
- statistical validation evidence;
- regression evidence for existing semantic-review behavior.

Acceptance questions:

1. Can a future rule use either sampler with ordinary rule-owned C# hazard logic and without learning a framework?
2. Is it obvious which claims subject-state sampling can make that aggregate sampling cannot?
3. Does state advance only when evidence actually justifies advancement?
4. Can the aggregate model handle dense populations without persisting each concrete subject?
5. Is persisted state small, transparent, disposable, and conservative when lost?
6. Are the random/exponential mechanics pinned by deterministic vectors and statistically validated?
7. Did M0009 leave current product rule semantics unchanged?

Waiver policy: explicit project-owner decision only.

## Completion expectations

Implementation must:

```text
read milestone + planning-seeded ledger
-> verify obligation/evidence-case equality
-> verify M0008 completion approval is durable
-> inspect live post-M0008 architecture
-> derive bounded work packages
-> implement sampling core without current rule migration
-> run exact + statistical + regression validation
-> freshly reread milestone
-> reconcile every obligation/evidence case <-> ledger <-> repository/evidence
-> durable completion evidence
-> required human review
```

Passing ordinary unit tests alone is insufficient. The milestone requires exact deterministic vectors, deterministic statistical-distribution evidence, persistence/failure evidence, and source/document review.

## Completion evidence

Not yet implemented.

Current milestone outcome:

```text
READY
```
