# Semantic Review Batches

## Model

The deterministic engine selects a bounded semantic-review workload; the caller judges it.

Every enabled configurable semantic-review rule evaluates during the normal rule set. Bootstrap/update do not run source semantic-review rules.

A semantic review rule retains fixed rule ID/version, population semantics, deterministic ranking, reviewer class, questions, and escalation condition.

## Deterministic sampling

Existing SHA-256 population fingerprint/ranking behavior remains unchanged.

Each summary review rule ranks independently using its own fixed rule ID/version and population fingerprint. Each has a maximum sample of exactly five; this is not configurable. Batch IDs are deterministic by canonical rule order (`B-1` quality, `B-2` German), and each batch can be expanded or handed off by its own qualified or bare handle.

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

## Shared summary population

Both rules use the covered API subject and summary-carrier population described in `docs/specs/DOCUMENTATION.md`: ordinary subjects use explicit non-empty `<summary>` prose; synthesized positional-record properties use matching `<param>` prose; inheritdoc-only subjects are excluded. Optional parameter documentation is not sampled as a separate subject.

## `docs.summary.quality.review`

Version: `3`
Output kind: `review-batch`
Normal reviewer: `implementer`
Expanded reviewer: `frontier`
Maximum sample: `5`

Rationale: correctness, information value, and clarity are semantic prose questions that deterministic syntax and structure checks cannot establish reliably without brittle proxies.

### Questions

Q1 Technical correctness — consistent with declaration and relevant implementation/API context without invented behavior.

Q2 Information value — useful caller-relevant meaning beyond merely repeating symbol/type/signature.

Q3 Clarity/scope — concise, specific responsibility without irrelevant implementation detail.

### Escalation

Escalate when any sampled summary materially fails Q1-Q3 or the implementer cannot confidently answer any required question.

## `docs.summary.language.german.review`

Version: `1`
Output kind: `review-batch`
Normal reviewer: `implementer`
Expanded reviewer: `frontier`
Maximum sample: `5`

Rationale: some consuming repositories deliberately require German documentation quality. Keeping this as its own optional fixed rule lets those projects apply that policy without making German intrinsic to generic summary quality.

This independently enabled rule asks whether each selected summary is natural, comprehensible German rather than awkward literal translation or merely German-looking text. German is fixed in the rule identity; there is no language parameter. English summaries are eligible and fail this question. Escalate if a sampled summary fails or the implementer is uncertain.

The rule uses the shared summary population above, independently ranks and fingerprints it under its own rule ID/version, and follows the same expansion/handoff protocol.

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
    -> bounded language-neutral semantic judgment of local summary prose

docs.summary.language.german.review
    -> bounded judgment of natural, comprehensible German prose
```

Deterministic structural failures are not replaced by semantic review.
