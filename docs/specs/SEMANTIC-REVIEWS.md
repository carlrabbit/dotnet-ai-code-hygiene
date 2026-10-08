# Semantic Review Batches

## Model

The deterministic engine discovers eligible semantic-review populations; each rule owns the semantic contract and any statistical selection policy. The caller judges review work. The CLI performs no model/provider invocation.

A semantic-review rule owns:

- fixed rule ID/version;
- eligible population semantics;
- review questions/rubric;
- normal and escalated reviewer class;
- escalation condition;
- statistical hazard policy when it opts into sampling;
- the meaning of an accepted observation.

Shared host mechanics own:

- run/batch/item materialization;
- population fingerprinting for stale-run protection;
- latest-run persistence;
- expansion/handoff;
- durable sampling-state commit/discard boundaries;
- the `review accept` command surface.

Statistical selection remains rule-owned ordinary C# over the shared sampling substrate. There is no sampling descriptor DSL, sampling-rule hierarchy, scheduler, model client, or provider integration.

## Population, due workload, and normal selection

These concepts are distinct:

```text
eligible population
    all subjects that the rule can review in the current reporting scope

due workload
    eligible subjects/events whose sampling thresholds are currently due

normal selection
    bounded subset of due workload emitted by normal `hygiene check`
```

The public `PopulationCount` remains the eligible-population count. `SampleCount` is the number of selected due items emitted for the current normal batch and may be zero even when `PopulationCount` is non-zero.

The host must not independently rerank a statistical rule's normal selection. A rule supplies its selected due subjects in deterministic order while the host retains the full eligible population for population fingerprinting and expansion/handoff.

Every enabled semantic-review rule continues to emit exactly one normal batch on successful check, including `0/N` and `0/0` batches.

## Statistical observation protocol

Sampling state advances only after explicit accepted review evidence.

M0011 adds:

```text
hygiene review accept <batch-handle> <item-id>...
hygiene review accept <batch-handle> --all
```

`--all` means every item in that batch's current normal sample, never the full eligible population.

Acceptance means that the caller has answered every required rule question for each accepted item and the item satisfies the rule's non-escalation condition. The CLI does not infer this judgment.

Before mutating sampling state, `review accept` must:

1. resolve the latest-run batch handle using the existing bare/qualified rules;
2. revalidate the current eligible-population fingerprint;
3. resolve only items that belonged to the stored normal sample;
4. verify every selected sampling ticket is still current and observable;
5. reject the complete operation if any requested item is stale, unknown, non-selected, already consumed, or otherwise invalid.

A successful multi-item acceptance is one atomic sampling-state transaction. No requested item is consumed if any requested item fails validation.

Accepted subject-state items consume their matching current ticket as a successful observation. Accepted aggregate items consume their matching current event ticket. M0011 does not use aggregate pass/fail Beta evidence for `architecture.boringness.review`; its acceptance consumes the event without making a population-quality posterior claim.

There is no `fail` or `uncertain` observation command in M0011. A materially negative or uncertain review does not consume the due ticket. It remains due until the relevant code/documentation is changed and a later review is confidently acceptable.

`review accept` supports text and JSON output. Its JSON output is schema version 1 and exposes the source batch/rule plus accepted item IDs/count. Sampling tickets, state epochs, repository seed material, hazards, thresholds, and internal fingerprints are never public output.

## Expansion and handoff

The existing escalation surfaces remain:

```text
hygiene review expand <batch-handle>
hygiene review handoff <batch-handle> [--file <path>]
```

Expansion/handoff revalidate the full eligible population, not merely the normal due workload. This preserves the existing "inspect the complete current population when escalation is required" contract.

Expanded/handoff items need not themselves be due and therefore are not automatically observable. `review accept` only accepts items from the normal sampled batch that carry current due tickets.

Handoff artifacts remain transport-neutral review requests and expose no sampling ticket, hazard, state epoch, repository seed, or evidence-state metadata. A decoupled reviewer result is not automatically imported. After an external planning/review decision, the local caller applies any resulting change, reruns/revalidates as needed, and explicitly accepts current normal sampled items only when they satisfy the rule.

## Shared summary population

Both summary rules use the covered API subject and summary-carrier population described in `docs/specs/DOCUMENTATION.md`:

- ordinary subjects use explicit non-empty `<summary>` prose;
- synthesized positional-record properties use matching `<param>` prose;
- inheritdoc-only subjects are excluded;
- optional parameter documentation is not a separate review subject.

A summary sampling subject uses a stable project-qualified documentation anchor rather than source position alone.

The rule-owned evaluation fingerprint must change when review-relevant local evidence changes. For M0011 it includes the summary carrier and the corresponding source declaration text needed to judge the fixed rubric. The shared sampling store persists the last evaluation fingerprint so the rule can distinguish first evaluation, unchanged evaluation, and a new fingerprint without using run count as a proxy for change.

For each summary rule independently:

```text
first evaluation of subject          -> add H = 1
new evaluation fingerprint           -> add H = 1 exactly once
elapsed baseline                     -> add H = elapsed / 365 days
accepted review                      -> consume/reset current subject event
failed/uncertain review              -> do not consume
normal batch budget                  -> at most 5 due subjects
```

The elapsed cursor and fingerprint update are persisted together with sampling state. Repeating the same effective evaluation time/fingerprint must not add duplicate hazard.

`H=1` is deliberately interpretable rather than tuned pseudo-precision: one independent unit of integrated hazard corresponds to `1-exp(-1)` (about 63%) probability of having crossed the current exponential inspection threshold. Three accumulated units correspond to about 95%.

## `docs.summary.quality.review`

Version: `4`
Output kind: `review-batch`
Normal reviewer: `implementer`
Escalated reviewer: `frontier`
Sampling model: `subject-state`
Normal batch budget: `5`

Rationale: correctness, information value, and clarity are semantic prose questions that deterministic syntax and structure checks cannot establish reliably without brittle proxies. Stable API documentation subjects justify per-subject revisit state.

Questions:

- **Q1 — Technical correctness:** Is it consistent with the declaration and relevant implementation/API context, without inventing behavior?
- **Q2 — Information value:** Does it add useful caller-relevant meaning rather than simply repeat/paraphrase the symbol name/type/signature?
- **Q3 — Clarity and scope:** Is it concise, specific, and clear enough to communicate responsibility without irrelevant implementation detail?

A sampled item is acceptable only when Q1-Q3 are all confidently acceptable.

Escalate when any sampled summary materially fails a required question or the implementer cannot confidently answer one. Failed/uncertain items are not accepted into sampling history.

## `docs.summary.language.german.review`

Version: `2`
Output kind: `review-batch`
Normal reviewer: `implementer`
Escalated reviewer: `frontier`
Sampling model: `subject-state`
Normal batch budget: `5`

Rationale: some consuming repositories deliberately require German documentation quality. Keeping this as its own optional fixed rule lets those projects apply that policy without making German intrinsic to generic summary quality.

Question:

- **Q1 — German language:** Is the summary natural, comprehensible German rather than awkward literal translation or merely German-looking text?

German is fixed in the rule identity; there is no language parameter. English summaries are eligible and fail this question.

The rule uses the same population/fingerprint/hazard shape as the quality rule but owns independent sampling state under its own rule ID/version. A sampled item is acceptable only when Q1 is confidently acceptable.

## `architecture.boringness.review`

Version: `1`
Output kind: `review-batch`
Normal reviewer: `implementer`
Escalated reviewer: `planner`
Sampling model: `aggregate population`
Aggregate unit: repository-relative C# source document
Candidate: source-backed class, struct, record, or interface declaration
Normal batch budget: `5`

Purpose: detect architectural pressure that may justify planner attention without asking the implementer to make a vague global judgment about "BORINGness."

Enums and delegates are not candidates. Nested types are candidates. Candidate identities are transient and are not persisted in aggregate sampling state.

The aggregate unit persists only source-document state. Its evaluation fingerprint represents the current ordered eligible type-declaration source for that document. The shared aggregate state also persists the previously observed eligible-candidate count so elapsed hazard can use the prior observed population size rather than pretending the current count existed for the entire unseen interval.

Rule-owned aggregate hazard policy:

```text
first evaluation of a document       -> add aggregate H = 1
new document evaluation fingerprint  -> add aggregate H = 1 exactly once
elapsed baseline                     -> add H = previousCandidateCount * elapsed / 365 days
accepted review                      -> consume one current aggregate event
failed/uncertain review              -> do not consume
```

On first evaluation there is no retroactive elapsed hazard. The current candidate count becomes the baseline for the next elapsed interval.

Normal selection:

1. enumerate current due aggregate units;
2. consider only the current generation/event from each unit, so one normal batch never selects two events from the same file;
3. order due units deterministically by current event debt/overshoot, then repository-relative path;
4. take at most five units;
5. use the aggregate sampler's deterministic transient candidate ranking to choose one current type from each selected unit.

Unselected due units/events remain due. Concrete type identities are not persisted.

The rule makes no defect-rate, repository-percentage, or individual-type revisit claim. It is population surveillance plus an escalation signal.

### BORINGness questions

Each question is intentionally observable and narrow. "Yes" means the signal is present.

- **Q1 — Speculative abstraction:** Is there an interface, provider, factory, strategy, generic mechanism, extension point, or similar abstraction with only one meaningful production use/implementation and no current requirement for variability?
- **Q2 — Indirection:** To understand what this code actually does, must you follow several forwarding/delegation layers before reaching the behavior?
- **Q3 — Change locality:** Would a small behavioral change require coordinated edits across multiple plumbing types, registrations, adapters, or mappings rather than mostly changing the code that owns the behavior?
- **Q4 — Hidden machinery:** Does the implementation rely on reflection, dynamic discovery, convention-based registration, a custom DSL/framework, or other implicit machinery where ordinary explicit C# could satisfy the current requirement?
- **Q5 — Unclear ownership:** After reading the relevant types, is it difficult to identify the single place that owns the behavior or state being reviewed?

Legitimate boundaries are not failures merely because they currently have one implementation. External-service, persistence, platform, interoperability, or other real boundaries may justify abstraction. The reviewer evaluates the current requirement rather than hypothetical future extensibility.

Escalation rule:

```text
Q4 = yes                         -> escalate to planner
2 or more of Q1/Q2/Q3/Q5 = yes -> escalate to planner
otherwise                        -> acceptable for this rule
```

One non-Q4 signal alone is review evidence, not a planner escalation.

When escalation is required, do not accept the sampled event. Use expansion/handoff as appropriate. The planner decides whether complexity is justified or whether simplification is a project-level change; the implementer does not redesign architecture merely to satisfy the sampled rule.

## Relationship to deterministic rules

```text
docs.summary.required
    -> required documentation carrier exists

docs.xml.consistent
    -> present XML documentation is structurally coherent

docs.text.sentence
    -> selected explicit documentation prose has mechanical sentence punctuation

docs.summary.quality.review
    -> statistically scheduled semantic quality review of documentation subjects

docs.summary.language.german.review
    -> statistically scheduled German-language review of documentation subjects

architecture.boringness.review
    -> statistically scheduled architectural-pressure review with planner escalation
```

Deterministic structural failures remain exhaustive and are not replaced by semantic sampling.

## Compatibility and boundaries

M0011 changes the semantic contracts of the two summary review rules, therefore their rule versions advance. Old v3/v1 sampling/ranking behavior is not preserved under the new versions.

Batch IDs remain fixed by semantic-review order:

```text
B-1 docs.summary.quality.review
B-2 docs.summary.language.german.review
B-3 architecture.boringness.review
```

Disabled rules do not emit a batch or accrue sampling state. Omitted subjects/units in partial reporting scope are not pruned or treated as absent. When next encountered, persisted elapsed cursors account for elapsed time according to the rule policy.

M0011 adds no repository-configurable sample size, hazard, time horizon, language, cohort, prior, reviewer threshold, or BORINGness threshold.

The CLI still performs no model/provider invocation.
