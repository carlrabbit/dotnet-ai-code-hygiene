# Semantic Review Batches

## Model

The deterministic engine selects a bounded semantic-review workload; the caller judges it.

Every enabled configurable semantic-review rule evaluates during the normal rule set. Bootstrap/update do not run source semantic-review rules.

A semantic review rule retains fixed rule ID/version, population semantics, deterministic ranking, reviewer class, questions, and escalation condition.

## Deterministic sampling

Existing SHA-256 population fingerprint/ranking behavior remains unchanged.

For `docs.summary.quality.review` v2, sample maximum remains exactly five and is not configurable.

## Caller protocol

The CLI does not collect semantic answers.

```text
all required answers confidently acceptable -> no expansion
materially negative answer                  -> expand/escalate
uncertain required answer                   -> expand/escalate
```

Existing:

```text
hygiene review expand <batch-handle>
hygiene review handoff <batch-handle> [--file <path>]
```

contracts remain unchanged.

## `docs.summary.quality.review`

Version: `2`
Output kind: `review-batch`
Normal reviewer: `implementer`
Expanded reviewer: `frontier`
Maximum sample: `5`

### Population

Population consists of covered API subjects with explicit, non-empty local documentation-summary prose under the M0005 summary-carrier model:

```text
ordinary subject
  -> <summary>

synthesized positional-record property
  -> matching record <param>
```

Inheritdoc-only subjects are excluded because no local prose exists.

Optional parameter/type-parameter/returns/value/exception prose is not sampled merely because it exists.

### Subject identity/content

Subject identity is the documented API subject, preferably its Roslyn documentation-comment identity or stable equivalent.

Review-relevant content fingerprint uses normalized prose from that subject's resolved local summary carrier.

### Questions

Q1 German quality — natural, comprehensible German rather than awkward literal translation.

Q2 Technical correctness — consistent with declaration and relevant implementation/API context without invented behavior.

Q3 Information value — useful caller-relevant meaning beyond merely repeating symbol/type/signature.

Q4 Clarity/scope — concise, specific responsibility without irrelevant implementation detail.

### Escalation

Escalate when any sampled summary materially fails Q1-Q4 or the implementer cannot confidently answer any required question.

The CLI still performs no model call and stores no semantic review history/acceptance.

## Relationship to deterministic documentation rules

```text
docs.summary.required
    -> whether the covered subject has a required summary carrier/inheritdoc

docs.xml.consistent
    -> whether explicitly present optional/structural XML documentation is coherent

docs.text.sentence
    -> mechanical sentence-ending requirement for selected explicit prose

docs.summary.quality.review
    -> bounded semantic judgment of local summary prose
```

Deterministic structural failures are not replaced by semantic review.
