# Semantic Review Batches

## Model

The deterministic engine selects a small semantic-review workload; the caller judges it.

M0003 does not persist answers/history or introduce periodic/history-based triggers. Every enabled semantic review rule evaluates every run.

A semantic review rule defines:

```text
rule ID/version
eligible population
sample-size maximum
stable subject identity/content fingerprint
deterministic ranking
reviewer class
fixed questions/rubric
escalation condition
expanded reviewer class
```

## Deterministic sampling

For each rule:

1. enumerate eligible subjects in selected target scope;
2. derive stable subject identity and review-relevant content fingerprint;
3. derive a population fingerprint from the ordered eligible subject identities/content fingerprints;
4. rank subjects with SHA-256 over `rule ID + version + population fingerprint + subject identity + subject content fingerprint`;
5. take the first rule-defined maximum count.

Identical review-relevant state => identical sample. Population/content change => changed population fingerprint and potentially rotated sample.

M0003 sample maximum for `docs.summary.quality.review` is exactly 5 and is not configurable.

## Public batch shape

Normal check batch exposes:

```text
id / handle
ruleId / ruleVersion
mode = sample
reviewerClass = implementer
populationCount / sampleCount
questions[]
escalation { condition, command, reviewerClass=frontier }
items[]
```

Each item exposes:

```text
item ID
repository-relative path
1-based line/column
symbol
summary text
declaration display/signature
```

Internal hashes, ranking values, semantic anchors, and population fingerprints are not public output.

Text output must be concise but sufficient to execute the rubric. Empty sample must be explicit (`sample 0/0`).

## Caller protocol

The tool does not collect answers.

For a sample:

```text
all required answers confidently acceptable
    -> no expansion

any materially negative answer
    -> expand

implementer cannot confidently answer any required question
    -> expand
```

The caller hands expanded output to a frontier-capability reviewer. The frontier reviewer identifies problematic items; the implementation agent fixes them; then normal `hygiene check` runs again.

## Expansion

`review expand` accepts bare latest-run or fully qualified latest-run batch handles.

Before expansion, recompute current eligible population/fingerprint. If changed, fail with exit 3 and request new check.

Expanded batch:

```text
mode = expanded
reviewerClass = frontier
sampleCount == populationCount
items == complete eligible population, including previously sampled items
same questions/rubric
```

Expansion does not create a new run and does not mutate latest-run state.

## `docs.summary.quality.review`

Version 1. Output kind `review-batch`. Normal reviewer `implementer`. Expanded reviewer `frontier`. Max sample 5.

### Population

Source-declared C# symbols in the same public/internal categories governed by `docs.summary.required` that currently have a valid non-empty XML `<summary>`.

Missing/empty/invalid summaries are excluded and remain the responsibility of `docs.summary.required`.

Subjects must belong to selected target files. Broader project context may be read.

### Subject identity

Prefer Roslyn documentation-comment ID as semantic symbol identity. Review-relevant content fingerprint includes normalized XML summary content.

### Fixed questions

**Q1 — German quality**  
Is the summary natural, comprehensible German rather than awkward literal translation or merely German-looking text?

**Q2 — Technical correctness**  
Is it consistent with the declaration and relevant implementation/API context, without inventing behavior?

**Q3 — Information value**  
Does it add useful caller-relevant meaning rather than simply repeat/paraphrase the symbol name/type/signature?

**Q4 — Clarity and scope**  
Is it concise, specific, and clear enough to communicate responsibility without irrelevant implementation detail?

A concise summary is acceptable only when it still communicates useful purpose/domain meaning.

### Escalation

Expand when any sampled summary materially fails Q1-Q4 or the implementer is not confident enough to answer any required question.

The full expanded population is reviewed by a frontier-capability reviewer using the same questions.

## Relationship to required-summary rule

```text
docs.summary.required
    -> deterministic missing-summary finding

docs.summary.quality.review
    -> bounded semantic sample of summaries that exist
```

Adding a missing summary resolves Rule A. A later run may sample it under Rule B according to deterministic sampling.

## Non-goals

No language heuristic is treated as sufficient quality judgment. No automatic model call, repair, history, schedule, random sampling, or review-answer persistence.
