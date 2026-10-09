# M0011 — Statistical Semantic Review Adoption

**State:** awaiting human review
**Mode:** AI-executed, human-reviewed
**Depends on:** completed M0010 Rule-Owned Remediation & Policy Projection

## Goal

Make M0009 statistical sampling the production selection mechanism for semantic review, without making sampling universal.

M0011 has three product outcomes:

```text
docs.summary.quality.review
docs.summary.language.german.review
    -> subject-state hazard sampling
    -> explicit accepted-review observations

architecture.boringness.review
    -> aggregate document/type sampling
    -> simple structural questions
    -> planner escalation for architectural pressure
```

The CLI continues to select work and persist transparent local statistical state; external agents/humans perform semantic judgment. The tool invokes no model.

## Target state

```text
check
-> rule discovers complete eligible population
-> rule updates its fixed sampling policy
-> shared sampler reports due work
-> rule selects bounded due workload
-> host emits normal review batch
-> caller answers fixed questions

acceptable
-> hygiene review accept ...
-> revalidate population/tickets
-> atomic explicit observation
-> sampling state advances

negative / uncertain
-> do not observe
-> review expand / handoff
-> frontier or planner decides next action
```

For statistical rules, the host no longer owns an independent SHA-256 `Take(5)` semantic policy. Rules own selection through M0009 sampling mechanics; the host owns generic batch/result persistence and revalidation.

## Planning baseline

Planning was performed against `main` commit:

```text
92825010a5afce11646857b2b20e1e0f5ec20174
Implement M0010: Rule-Owned Remediation & Policy Projection (#13)
```

M0001-M0010 are complete. `REV-M0009-COMPLETION` and `REV-M0010-COMPLETION` are durable on `main`.

One pre-existing documentation inconsistency is resolved by this milestone: current `docs/ARCHITECTURE.md` contains both the implemented M0009 sampling boundary and an older paragraph saying statistical sampling is future-only. M0011 treats M0009 as completed authority and requires the stale paragraph to be corrected.

## Execution profile and guide applicability

| Field | Value |
|---|---|
| Lifecycle state | awaiting-human-review |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Guide system | `carlrabbit/agentic-project-guides` 0.9.1 |
| Applicable profiles | `base` + `cli-tool`, repository scope |
| Repository role | developer-tool |
| Maturity | greenfield |
| Validation locus | local Windows 11 + .NET 11 SDK + Git + PowerShell |
| Distribution boundary | exact locally packed/installed `dotnet tool` |
| Execution ledger | `.execution/M0011-statistical-semantic-review-adoption.md` |
| Human review | required/blocking at completion |

No material profile conflict exists. The `cli-tool` profile makes the new `review accept` command, structured output, exit behavior, help/discovery, filesystem mutation safety, and exact installed-tool validation compatibility-sensitive surfaces.

## Required authority

Implementation starts from this milestone and reads:

- `docs/specs/SEMANTIC-REVIEWS.md` from this package;
- `docs/SPECS.md` from this package;
- `docs/specs/SAMPLING.md`;
- `docs/specs/RULE-CAPABILITIES.md`;
- `docs/specs/HYGIENE.md`;
- `docs/specs/REVIEW-HANDOFFS.md`;
- `docs/specs/DOCUMENTATION.md`;
- `docs/specs/READABILITY.md`;
- `docs/specs/AGENT-HELP.md`;
- `docs/ARCHITECTURE.md`;
- `docs/ENGINEERING.md`;
- `docs/TERMINOLOGY.md`;
- `docs/PUBLIC-DOCS.md`;
- `AGENTS.md`;
- current rule/sampling/review/CLI source and relevant tests.

`docs/research/SAMPLING-RULES.md` is planning provenance only. Implementation must not need it to reconstruct the contract.

M0009 and M0010 are compatibility/provenance baselines, not substitutes for the M0011 target decisions below.

## Resolved decisions

### Sampling remains exceptional

Mechanical, exhaustive rules remain exhaustive. M0011 migrates only semantic rules whose judgment is fuzzy/expensive.

No deterministic documentation/profile/readability rule begins sampling in this milestone.

### Semantic review owns eligible population and selected workload separately

A statistical semantic rule must provide enough information for the host to retain:

```text
complete eligible population
selected normal due workload
source context
sampling ticket association for selected items
```

The exact internal types are implementation freedom.

The generic host may fingerprint/materialize/revalidate these structures, but it must not silently apply a second semantic ranking policy after the rule has selected due work.

### Explicit acceptance is the observation boundary

The current product has no semantic answer collection. M0011 adds only the minimum observation contract necessary for statistical sampling:

```text
hygiene review accept <batch-handle> <item-id>...
hygiene review accept <batch-handle> --all
```

Acceptance is a caller assertion that all fixed required questions for those normal sampled items are confidently acceptable.

There is intentionally no `fail`, `uncertain`, handoff-result import, or automatic model-result protocol. Negative/uncertain items remain due and use the existing expansion/handoff workflow.

Sampling state therefore never advances merely because review work was emitted, expanded, handed off, or read.

### Generic evaluation metadata is part of the shared sampling substrate

M0009 already owns generic durable sampling state. M0011 extends that state narrowly so rule code can apply change-sensitive hazard idempotently:

- subject and population state persist an opaque current evaluation fingerprint/cursor value sufficient to distinguish first/unchanged/changed evaluation;
- aggregate state persists the previously observed candidate count used by a rule's next elapsed-hazard calculation;
- repeated identical evaluation must not create hazard merely because `check` ran again;
- the core stores metadata; it does not decide fingerprint contents or hazard weights.

Do not introduce a generic risk-feature framework or policy DSL.

### Summary rules use subject-state sampling

`docs/specs/SEMANTIC-REVIEWS.md` is authoritative for the exact fixed policy.

The migration intentionally changes selection semantics, so versions advance:

```text
docs.summary.quality.review          v3 -> v4
docs.summary.language.german.review  v1 -> v2
```

The normal work budget remains five, but five is now a workload cap over persistent due work rather than the sampling model itself.

### BORINGness uses aggregate population sampling

Add:

```text
architecture.boringness.review v1
```

It is configurable, enabled by default, diagnosis/review only, and appended to canonical rule order.

Its persistent aggregate unit is one source document. Its concrete type declarations remain transient candidates. This deliberately spends state per file rather than per type.

The rule does not claim individual type coverage or repository defect prevalence.

### BORINGness is a planner-escalation detector, not a style oracle

The five questions and escalation threshold in `docs/specs/SEMANTIC-REVIEWS.md` are fixed rule interface text.

The implementer answers observable structural questions. The rule does not ask the implementer to decide whether the architecture should be redesigned.

When the threshold is crossed, the caller escalates to the planner. The planner may conclude the complexity is justified.

Raw line count, method count, type count, or similar size alone is not a BORINGness failure.

### Expansion/handoff remains complete-population escalation

Normal statistical selection is bounded, but explicit escalation keeps the existing full eligible-population semantics.

Summary rules retain `frontier` as escalated reviewer class. BORINGness uses `planner`.

The durable handoff format must generalize its wording/validation to the rule-owned reviewer class without becoming provider-specific.

### Partial reporting stays safe

A changed/explicit run may accrue only encountered rule units/subjects. It must never infer deletion from omission.

When an encountered persisted unit has an elapsed cursor, the fixed rule policy catches up elapsed hazard lazily.

### Product/version boundary

M0011 is a public semantic/CLI change and advances the package to:

```text
0.7.0
```

The hygiene profile remains `dotnet-11 v2`.

## Scope

In scope:

- summary quality v4 subject-state sampling;
- German summary v2 independent subject-state sampling;
- narrow generic evaluation-fingerprint metadata in sampling state;
- previous aggregate candidate-count metadata;
- separation of full semantic-review population and selected due workload;
- durable internal association from normal selected review item to sampling ticket;
- public `hygiene review accept`;
- atomic accepted-observation persistence;
- stale/source/ticket revalidation;
- new `architecture.boringness.review` v1;
- aggregate document/type population;
- fixed BORINGness questions and planner escalation threshold;
- planner reviewer class in expansion/handoff;
- package version 0.7.0;
- rule catalogue/capability/help/public documentation updates;
- correction of stale architecture sampling wording;
- Architecture source-family locality;
- exact policy/regression/installed-tool validation.

## Non-goals

Out of scope:

- `readability.type-responsibility.review`;
- test-quality sampling;
- comment-quality sampling;
- exception/logging review;
- automatic semantic remediation;
- automatic application of planner recommendations;
- importing planner/frontier result files;
- model/provider SDKs or network calls;
- repository-configurable hazard/sample/rubric values;
- generic sampling metadata on rule descriptors;
- generic risk-feature/cohort DSL;
- persistent concrete type candidate identities for BORINGness;
- prevalence/confidence percentage reporting;
- new database/binary sampling backend;
- external statistics dependency;
- public plugin API;
- scheduler/DAG/DI/reflection discovery;
- broad Git-history mining;
- unrelated deterministic-rule or rewrite changes.

## Acceptance criteria and completion obligations

**AC-01 — Acceptance.** M0011 preserves the explicit static rule/catalog architecture: statistical selection remains ordinary rule-owned C# over the shared sampling service, and the change introduces no sampling-rule hierarchy, descriptor DSL, dependency graph, reflection discovery, DI framework, scheduler, model/provider client, or second semantic rule catalogue.

**AC-02 — Acceptance.** Semantic-review execution distinguishes the complete eligible population from the rule-selected normal due workload: population fingerprinting/expansion retain the complete current population, normal `hygiene check` emits only the rule-selected due items, `PopulationCount` remains eligible-population size, and `SampleCount` may be zero for a non-empty population.

**AC-03 — Acceptance.** The sampling substrate persists generic evaluation metadata sufficient for rule-owned change-sensitive hazard without per-run double counting: subject and population state can distinguish first evaluation, unchanged fingerprint, and changed fingerprint, and aggregate state retains the previous observed candidate count needed by the M0011 elapsed-hazard policy.

**AC-04 — Acceptance.** `docs.summary.quality.review` advances to v4 and uses subject-state sampling with the fixed M0011 policy: stable project-qualified documentation subject identity, review-relevant evaluation fingerprint, `H += 1` on first evaluation, `H += 1` exactly once per changed fingerprint, `H += elapsed/365 days`, and a normal budget of at most five due subjects selected by sampler debt/overshoot.

**AC-05 — Acceptance.** `docs.summary.language.german.review` advances to v2 and independently uses the same subject-state identity/fingerprint/hazard shape under its own rule ID/version while preserving its fixed German-language question and repository toggle behavior.

**AC-06 — Acceptance.** For both summary rules, merely emitting/selecting review work never consumes sampling state; a materially negative or uncertain item remains due, and only explicit accepted review consumes the current subject ticket and advances its sampling generation.

**AC-07 — Acceptance.** The public CLI adds `hygiene review accept <batch-handle> <item-id>...` and `hygiene review accept <batch-handle> --all`; `--all` means the current normal sample only, accepted items must belong to that stored normal sample, and text/JSON output reports the accepted batch/rule/items without exposing internal sampling metadata.

**AC-08 — Acceptance.** `review accept` revalidates the latest-run eligible-population fingerprint and every requested sampling ticket before mutation, rejects stale/unknown/non-selected/already-consumed requests with product exit 3 and rerun guidance, and applies multi-item observation as one all-or-nothing sampling-state transaction.

**AC-09 — Acceptance.** Sampling tickets, state epochs, repository seed material, hazards, thresholds, evaluation fingerprints, and aggregate evidence state remain engine-internal: normal check JSON/text, expanded review output, and durable handoff JSON expose none of them.

**AC-10 — Acceptance.** Existing `review expand` and `review handoff` continue to revalidate and expose the complete eligible population; summary escalations retain `frontier` reviewer semantics, while `architecture.boringness.review` escalates to reviewer class `planner`, and handoff remains transport-neutral with no result import or model/network invocation.

**AC-11 — Acceptance.** A new configurable `architecture.boringness.review` v1 semantic-review rule uses aggregate population sampling with repository-relative C# source document as the persistent unit and current source-backed class/struct/record/interface declarations as transient candidates; enums/delegates are excluded and concrete type candidate identities are not persisted.

**AC-12 — Acceptance.** `architecture.boringness.review` uses the fixed aggregate hazard policy from `docs/specs/SEMANTIC-REVIEWS.md`: first document evaluation adds aggregate `H=1`, each new document evaluation fingerprint adds aggregate `H=1` exactly once, elapsed baseline adds `previousCandidateCount * elapsed/365 days`, accepted review consumes one current aggregate event, and failed/uncertain review consumes none.

**AC-13 — Acceptance.** Normal BORINGness selection is deterministic and bounded: consider only the current event from each due document unit, order due units by current event debt/overshoot then repository-relative path, take at most five units, and use the aggregate sampler's transient deterministic ranking to select exactly one current type from each selected unit; unselected due events remain due.

**AC-14 — Acceptance.** `architecture.boringness.review` owns exactly the five fixed observable questions in `docs/specs/SEMANTIC-REVIEWS.md`: speculative abstraction, indirection, change locality, hidden machinery, and unclear ownership, including the explicit allowance for legitimate real boundaries.

**AC-15 — Acceptance.** BORINGness escalation is fixed and mechanical at the rubric level: Q4=yes escalates to the planner, two or more yes answers among Q1/Q2/Q3/Q5 escalate to the planner, zero signals or exactly one non-Q4 signal do not; items requiring escalation are not accepted into sampling history until later review is acceptable.

**AC-16 — Acceptance.** Disabled semantic-review rules emit no batch and accrue/commit no sampling state; changed/explicit partial runs neither prune nor reset omitted subject/population state, and when a persisted unit is encountered later its elapsed cursor advances according to the fixed rule policy without fabricating observations.

**AC-17 — Acceptance.** Canonical rule order appends `architecture.boringness.review` after all existing rules, advances only the two migrated summary rule versions, and M0011 advances the product/package version from 0.6.0 to 0.7.0 without changing the dotnet-11 v2 hygiene profile.

**AC-18 — Acceptance.** All pre-M0011 deterministic finding rules, rule-owned format/normalize remediation, profile/bootstrap/update behavior, finding/ignore semantics, summary eligible-population semantics, expansion/handoff filesystem behavior, target resolution, and exit-code meanings remain regression-compatible except for the explicitly versioned semantic-review selection/acceptance changes.

**AC-19 — Acceptance.** M0011 adds no repository-configurable sampling/rubric parameters, automatic fail/uncertain result import, semantic auto-remediation, repository-wide defect percentage/confidence claim, persistent concrete-candidate index for aggregate sampling, external statistical dependency, or model/provider/network review execution.

**AC-20 — Acceptance.** Deterministic fixed-seed validation proves the M0011 rule policies are composed correctly over M0009 mechanics: first/change/elapsed hazard is applied exactly once as specified, summary due selection preserves backlog under the five-item budget, aggregate event pressure scales with the persisted previous candidate count, and acceptance consumes only matching current tickets.

**AC-21 — Acceptance.** Repository-local authority is internally consistent after M0011: `docs/specs/SEMANTIC-REVIEWS.md` is authoritative for the three semantic-review rule policies, `docs/specs/SAMPLING.md` reflects active production consumers and the generic evaluation metadata extension, `docs/ARCHITECTURE.md` no longer claims statistical sampling is future-only, `docs/ENGINEERING.md` admits the Architecture rule family, and rule-capability/catalogue documentation matches the live rule order and versions.

**DOC-01 — Documentation.** README/public docs and `docs/specs/AGENT-HELP.md` describe statistical semantic review, zero-due batches, explicit `review accept`, the difference between accept versus expand/handoff, BORINGness planner escalation, local sampling-state ownership, and the fact that the CLI still invokes no model/provider.

**REV-01 — Review.** Project owner/delegate confirms that summary review rotation is useful and comprehensible, `review accept` is safe/usable for coding agents, the BORINGness questions identify architectural pressure without treating every abstraction as a defect, planner escalation thresholds are appropriate, aggregate state remains lightweight, and M0011 introduces no hidden model or rule framework.

## Required evidence cases

| ID | Parent | Required evidence case |
|---|---|---|
| EC-02a | AC-02 | eligible population is non-empty but no sampling ticket is due, producing a successful `0/N` normal batch without losing the full population needed for expansion |
| EC-02b | AC-02 | more work is due than the normal budget, proving normal selection is a bounded subset while expansion still has the complete eligible population |
| EC-03a | AC-03 | first evaluation initializes persisted fingerprint/cursor metadata without retroactive elapsed hazard |
| EC-03b | AC-03 | repeating the same fingerprint and effective evaluation time adds no duplicate change or elapsed hazard |
| EC-03c | AC-03 | a new fingerprint adds the rule-owned change hazard exactly once and then becomes the persisted baseline |
| EC-03d | AC-03 | aggregate evaluation persists the previous candidate count and uses that previous count for the next elapsed interval |
| EC-04a | AC-04 | a first-seen quality-review subject receives exactly one unit of initial hazard |
| EC-04b | AC-04 | an unchanged quality-review subject receives exactly elapsed/365-days baseline hazard across persisted sessions |
| EC-04c | AC-04 | review-relevant summary/declaration fingerprint change receives exactly one additional unit of hazard without run-count accumulation |
| EC-04d | AC-04 | more than five due quality subjects select the five greatest debts deterministically and leave all unselected subjects due |
| EC-05a | AC-05 | quality and German rules over the same summary population retain independent subject-state generations/hazards and disabling German prevents German-state accrual |
| EC-06a | AC-06 | selected summary item is re-emitted and generation remains unchanged when caller does not accept it |
| EC-06b | AC-06 | accepted summary item consumes exactly its matching ticket once and duplicate/stale acceptance cannot advance generation again |
| EC-07a | AC-07 | explicit item-ID acceptance succeeds for a subset of the current normal sample and leaves other sampled items due |
| EC-07b | AC-07 | `--all` accepts exactly every item in the current normal sample and never expanded-only/non-sampled population items |
| EC-07c | AC-07 | text and JSON accept output expose the documented public fields while retaining schema version 1 structured output |
| EC-08a | AC-08 | review-relevant source/population change after check causes accept to reject the stale batch without changing sampling bytes |
| EC-08b | AC-08 | already-consumed/stale ticket causes accept to fail with no further state advancement |
| EC-08c | AC-08 | multi-item accept containing one invalid item performs no partial observations and leaves the sampling file byte-for-byte unchanged |
| EC-09a | AC-09 | check/expand/handoff/accept public payloads contain no sampling ticket, epoch, seed, hazard, threshold, or evaluation-fingerprint fields |
| EC-10a | AC-10 | summary expansion/handoff retains the full current eligible summary population and frontier reviewer class after statistical normal selection |
| EC-10b | AC-10 | BORINGness handoff uses planner reviewer class, embeds the complete current eligible type-review population, and contains no automatic result-import contract |
| EC-11a | AC-11 | class/struct/record/interface declarations including nested types are eligible transient candidates |
| EC-11b | AC-11 | enum/delegate declarations are excluded and serialized aggregate state contains document units only, never concrete type candidate IDs |
| EC-12a | AC-12 | first encounter of an eligible document adds aggregate H=1 and no retroactive elapsed component |
| EC-12b | AC-12 | unchanged document accrues `previousCandidateCount * elapsed/365 days` and repeated effective evaluation adds zero |
| EC-12c | AC-12 | document evaluation fingerprint change adds aggregate H=1 exactly once while updating the current candidate-count baseline |
| EC-13a | AC-13 | six or more due document units produce at most five normal items, no two from one document, deterministic debt/path ordering, and persistent due state for unselected units |
| EC-13b | AC-13 | aggregate candidate choice for one due document is stable for the same seed/unit/generation/current candidates and no candidate identity is persisted |
| EC-15a | AC-15 | all five BORINGness answers are no: no planner escalation and item is eligible for acceptance |
| EC-15b | AC-15 | exactly one yes among Q1/Q2/Q3/Q5 with Q4=no: no planner escalation and item is eligible for acceptance |
| EC-15c | AC-15 | two yes answers among Q1/Q2/Q3/Q5 with Q4=no: planner escalation required and item is not accepted |
| EC-15d | AC-15 | Q4=yes by itself: planner escalation required and item is not accepted |
| EC-16a | AC-16 | disabled summary/BORINGness rule neither emits a batch nor changes that rule's sampling-state entry |
| EC-16b | AC-16 | partial run omitting previously tracked subjects/documents preserves them and a later encounter accrues elapsed hazard from the persisted cursor |
| EC-18a | AC-18 | existing deterministic documentation/readability/profile rules and ignore lifecycle remain compatible with statistical semantic-review rules enabled |
| EC-18b | AC-18 | existing format/normalize rule selection and transactional rewrite behavior remain compatible with M0011 |
| EC-20a | AC-20 | fixed-seed subject-policy simulation demonstrates the exact H=1 initial/change and elapsed composition plus deterministic five-item debt selection |
| EC-20b | AC-20 | fixed-seed aggregate-policy simulation demonstrates first/change event mass, previous-count elapsed scaling, one-event-per-unit normal selection, and matching-ticket consumption |

## Validation gates

All executable validation is local on the repository's supported Windows 11/.NET 11 path. `./eng/validate.ps1` remains the canonical engineering entrypoint and must exercise the specific scenarios named below; broad green output is not evidence for an unexercised case.

| ID | Depth | Required validation | Target/locus | Proves |
|---|---|---|---|---|
| VAL-01 | Tier 1 | `./eng/validate.ps1` Core focused/state-machine coverage for evaluation fingerprints/count cursors, subject/aggregate rule hazard composition, ticket acceptance, and atomic failure paths | Windows 11 + .NET 11; Core test target | AC-03..AC-08, AC-12, AC-16, AC-20; EC-03a..EC-08c, EC-12a..EC-12c, EC-16a..EC-16b, EC-20a..EC-20b |
| VAL-02 | Tier 1 | `./eng/validate.ps1` Core semantic-review population/selection/expansion/handoff coverage for all three rules and BORINGness rubric thresholds | Windows 11 + .NET 11; Core semantic-review target | AC-02, AC-04..AC-06, AC-09..AC-15; EC-02a..EC-02b, EC-04a..EC-06b, EC-09a..EC-15d |
| VAL-03 | Tier 1 | `./eng/validate.ps1` built CLI process coverage for `check`, `review accept`, `review expand`, `review handoff`, rules/help, text/JSON stdout, invalid/stale requests, and exit codes | Windows 11 + .NET 11; built CLI process | AC-07..AC-10, AC-15, AC-17, AC-19, DOC-01; EC-07a..EC-10b, EC-15a..EC-15d |
| VAL-04 | Tier 3 | `./eng/validate.ps1` isolated SDK-style Git fixture lifecycle across repeated commands, persisted sampling state, changed/explicit scopes, source changes, rule disablement, acceptance, expansion, and handoff | Windows 11 + .NET 11 + Git; isolated fixture repositories | AC-02..AC-18; EC-02a..EC-18b |
| VAL-05 | Tier 1 | `./eng/validate.ps1` deterministic fixed-seed policy/statistical simulation with explicit exact/tolerance assertions derived from M0009 exponential mechanics | Windows 11 + .NET 11; deterministic test target | AC-04, AC-12, AC-13, AC-20; EC-04a..EC-04d, EC-12a..EC-13b, EC-20a..EC-20b |
| VAL-06 | Tier 4 | `./eng/validate.ps1` exact locally packed/installed 0.7.0 tool workflow exercising bootstrap/update/rules/check/review accept/expand/handoff/format/normalize from an isolated consumer repository | Tier 4 local consumer on Windows 11 + .NET 11 + Git | AC-07..AC-10, AC-17..AC-19, DOC-01; relevant EC-07*, EC-08*, EC-09a, EC-10*, EC-18* |
| VAL-07 | Tier 2 | `./eng/validate.ps1` complete repository validation plus final `git diff --check` and repository self-host run with the new default BORINGness rule | complete repository on Windows 11 + .NET 11 | AC-01, AC-16..AC-21, DOC-01, and regression evidence not isolated above |
| VAL-08 | Documentation | direct source/document authority review of rule locality, static catalogue, sampling metadata shape, no candidate index/framework, canonical rule order/version, architecture/engineering/spec consistency, and public docs | repository review | AC-01, AC-09, AC-11, AC-17, AC-19, AC-21, DOC-01; EC-09a, EC-11b |
| VAL-09 | Human | `REV-M0011-COMPLETION` milestone-scoped project-owner/delegate review | human / project owner or delegate | REV-01 |

The deterministic statistical/policy tests must use fixed seeds/times/fingerprints and explicit expected values or justified fixed tolerances. Do not weaken tolerances until a failing run passes.

The exact installed-tool gate must prove the command came from the just-packed 0.7.0 artifact rather than repository build output, PATH leakage, or a stale global tool.

## Direct documentation impact

Implementation must reconcile the target state across at least:

- `docs/specs/SAMPLING.md`;
- `docs/specs/RULE-CAPABILITIES.md`;
- `docs/specs/HYGIENE.md`;
- `docs/specs/REVIEW-HANDOFFS.md`;
- `docs/specs/AGENT-HELP.md`;
- `docs/ARCHITECTURE.md`;
- `docs/ENGINEERING.md`;
- `docs/TERMINOLOGY.md`;
- `docs/PUBLIC-DOCS.md`;
- `README.md`;
- milestone/index/package-version surfaces.

`docs/ARCHITECTURE.md` must remove the stale statement that statistical sampling is future-only. `docs/ENGINEERING.md` must make the new `Rules/Architecture/` family an ordinary locality destination. Do not perform unrelated documentation cleanup.

No `.guide-sync/pending/` hint is required: all documentation affected by the public rule/CLI contract is direct M0011 implementation scope.

## Research

No new durable research artifact is required.

The statistical mechanics are already promoted by completed M0009. The BORINGness rubric is a resolved product policy promoted directly into `docs/specs/SEMANTIC-REVIEWS.md`; preserving the brainstorming conversation would add no reusable evidence.

## Human review

Canonical review ID:

```text
REV-M0011-COMPLETION
```

Applicability: required and blocking.

Reviewer: project owner/delegate.

Review subject:

- at least one real generated summary-quality normal batch;
- German rule behavior with the rule explicitly enabled in a fixture;
- at least one real generated BORINGness normal batch from a representative repository state;
- `review accept` text/JSON UX and stale-safety behavior;
- one BORINGness case below the escalation threshold and one case that crosses it;
- resulting `.hygiene/.state/sampling.json` shape;
- rule catalogue/help/public documentation;
- complete automated validation evidence.

Acceptance questions:

1. Do the summary rules rotate/revisit work in a way that is more useful than arbitrary per-run `Take(5)` while keeping normal review bounded?
2. Is it obvious that only accepted semantic work advances sampling history?
3. Is `review accept` explicit enough that an agent is unlikely to record uncertain/failed work accidentally?
4. Do the five BORINGness questions identify observable architectural pressure rather than vague taste?
5. Does the Q4-or-two-signals threshold escalate plausible architectural decisions without escalating every harmless abstraction?
6. Is it clear that a legitimate external/persistence/platform boundary can remain acceptable?
7. Does aggregate BORINGness state remain lightweight and avoid per-type history?
8. Did the implementation preserve the no-model boundary and avoid creating a hidden sampling/rule framework?

Acceptable decisions:

```text
approved
corrections required
waived by explicit project-owner decision
```

There is no separate `.review/` automation in this repository. The milestone-scoped review check is the explicit `REV-M0011-COMPLETION` decision after VAL-01..VAL-08 are current; record the decision in this milestone's Completion Evidence and the active execution ledger.

## Constrained execution

No external service, credential, CI environment, or remote capability is required.

Validation is expected to fit the existing local `./eng/validate.ps1` workflow. If the suite becomes too long for the implementation environment, implementation may add repository-local resumable validation mechanics only if that does not change product semantics; otherwise return to planning rather than claiming partial aggregate success.

## Escalation boundary

Return to planning if implementation would require any of the following:

- a generic sampling-rule descriptor/framework to express M0011;
- an answer/result import protocol broader than explicit acceptance;
- a provider/model integration;
- a persistent per-type index for BORINGness;
- a materially different BORINGness population unit or escalation threshold;
- a change to the dotnet-11 v2 profile;
- a public schema/exit compatibility break beyond the additive surfaces resolved here;
- weakening full-population expansion/handoff semantics.

Local type names, exact internal records/interfaces, test decomposition, file edits, and work-package sequencing remain implementation-owned.

## Baseline-executability audit

Planning confirms:

- architecture, sampling policy, public CLI observation semantics, rule versions/order, BORINGness population/rubric/escalation, package version, partial-scope behavior, validation locus, and human review are resolved;
- applicable `base` + `cli-tool` profile obligations are represented;
- implementation does not need the planning conversation or external guide repository;
- M0009 research is not required as operative authority;
- the milestone and planning-seeded ledger have lossless obligation/evidence coverage;
- remaining decisions are local implementation mechanics;
- no known external blocker prevents the milestone from being `ready`.

## Completion expectation

Implementation must:

```text
read milestone + listed authority + planning-seeded ledger
-> verify exact obligation/evidence-case equality
-> inspect live post-M0010 source
-> derive bounded implementation work packages
-> implement without changing resolved policy
-> run criterion-specific validation
-> update ledger evidence/status
-> freshly reread milestone
-> reconcile milestone <-> ledger <-> live evidence
-> perform completion audit
-> write compact durable Completion Evidence
-> stop at REV-M0011-COMPLETION
```

Passing the test suite alone is insufficient.

## Completion Evidence

Agent-resolvable implementation and correction completed 2026-10-09. The package was unpacked at repository-relative paths and implementation followed the local milestone, execution ledger, listed authority documents, `AGENTS.md`, and `docs/ENGINEERING.md`.

Final registry audit freshly reread this milestone and the ledger: the 23 obligation IDs match exactly and the 39 required `EC-*` IDs match exactly. No planner-owned wording or registry row was changed. Per-ID implementation and evidence-case proofs are recorded in the implementation-owned columns of `.execution/M0011-statistical-semantic-review-adoption.md`.

Proof reconciliation:

| Obligation/evidence group | Concrete proof |
|---|---|
| AC-01..AC-03; EC-03a..EC-03d | Static rule modules/catalogue; persisted generic evaluation metadata and previous aggregate count; `SamplingTests.AcceptedSubjectObservationStartsElapsedAgingAtObservationTime` proves acceptance resets the next generation cursor to t1, so only t1..t2 accrues; `SamplingTests.BoringnessZeroCandidatePopulationTransitionsResetTheAggregateBaseline` proves tracked empty populations update fingerprint/count/cursor and exact aggregate residuals; VAL-01/05/08. |
| AC-04..AC-06; EC-02a..EC-06b | Migrated independent summary samplers, due selection, no-observation-on-emission, explicit ticket acceptance. `LifecycleTests.SemanticBatchRetainsFullPopulationWhenNoTicketsAreDue`, `SemanticReviewSamplingAndExpansionAreDeterministicAndRunScoped`, `SummaryQualityAndGermanReviewsAreIndependentFixedBatches`, `SemanticReviewAcceptanceConsumesOnlyValidatedCurrentTicketsAtomically` and `SamplingTests.AcceptedSubjectObservationStartsElapsedAgingAtObservationTime`; VAL-01/02/04/05. |
| AC-07..AC-10; EC-07a..EC-10b | CLI subset and `--all` acceptance, mixed-form invalid invocation with unchanged sampling state, check-state rollback on both injected failure points, full-population expansion and frontier/planner handoffs. `CliProcessTests.ReviewAcceptReportsOnlyCurrentNormalSampleItems`, `LifecycleTests.CheckPublishesLatestRunAndSamplingStateTogetherOnFailures`; exact installed consumer workflow; VAL-03/04/06. |
| AC-11..AC-15; EC-11a..EC-15d | Aggregate per-document sampling, transient type candidates, fixed rubric and planner threshold. `LifecycleTests.BoringnessIncludesNestedTypeDeclarationsAndExcludesEnumsAndDelegates`, `BoringnessRubricEscalatesOnlyAtTheFixedThreshold`, `SamplingTests.BoringnessZeroCandidatePopulationTransitionsResetTheAggregateBaseline` (empty fingerprint/count/cursor, zero interval accrual, one reappearance increment, and no candidate identity in state); VAL-01/02/04/05/06/08. |
| AC-16..AC-21; EC-16a..EC-20b | Disablement/partial-scope and regression behavior; tracked BORINGness units advance through zero-candidate evaluations while omitted units remain retained; package/catalogue/docs consistency including corrected `docs/ENGINEERING.md` and `docs/specs/SAMPLING.md`; deterministic sampling and transactional publication evidence; full repository and self-host validation. VAL-01/04/05/06/07/08. |
| DOC-01 | README, agent help, and public/spec docs updated; engineering guidance reflects the three active consumers and exceptional sampling boundary; CLI help and installed workflow exercised; VAL-03/06/07/08. |

Validation: `./eng/validate.ps1` passed on 2026-10-09 on the declared Windows/.NET 11 locus: Release build, 75 Core tests, 23 CLI tests, exact packed/installed 0.7.0 consumer workflow (bootstrap/update/rules/check/accept/expand/handoff/format/normalize), repository self-host check, and `git diff --check`. VAL-01 through VAL-08 are passed in the ledger.

Correction audit: tracked BORINGness document units now record empty-population fingerprints, zero candidate counts, and the current evaluation cursor; never-eligible paths remain untracked. The deterministic N→0→0→N regression asserts residuals 1→6→6→7 and transient candidate identity exclusion. Subject observations reset their elapsed cursor at acceptance; the two check-owned state files roll back together on sampling-preparation or latest-run publication failure; mixed explicit IDs and `--all` fail with CLI exit 2 before state access; `docs/ENGINEERING.md` and `docs/specs/SAMPLING.md` reflect active M0011 consumers and paired check-state publication. Focused tests passed for all corrected paths.

No agent-resolvable gap or unresolved implementation policy decision remains. `REV-M0011-COMPLETION` / VAL-09 is the required project-owner/delegate review and remains pending. Stop here for human review; do not mark M0011 complete until that gate is decided.
