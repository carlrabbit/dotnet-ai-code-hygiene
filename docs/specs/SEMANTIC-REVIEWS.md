# Semantic Review Batches

## Model

The deterministic engine selects a small semantic-review workload; the caller judges it.

M0003 does not persist semantic answers/history or introduce periodic/history-based triggers. Every enabled semantic review rule evaluates every run.

A semantic review rule defines rule ID/version, eligible population, sample-size maximum, stable subject identity/content fingerprint, deterministic ranking, reviewer class, fixed questions/rubric, escalation condition, and expanded reviewer class.

## Deterministic sampling

For each rule:

1. enumerate eligible subjects in selected target scope;
2. derive stable subject identity and review-relevant content fingerprint;
3. derive a population fingerprint from the ordered eligible subject identities/content fingerprints;
4. rank subjects with SHA-256 over `rule ID + version + population fingerprint + subject identity + subject content fingerprint`;
5. take the first rule-defined maximum count.

Identical review-relevant state yields the same sample. Population/content change changes the population fingerprint and may rotate the sample.

M0003 sample maximum for `docs.summary.quality.review` is exactly 5 and is not configurable.

## Public batch shape

Normal check batch exposes id/handle, rule ID/version, `mode=sample`, `reviewerClass=implementer`, population/sample counts, questions, escalation guidance, and items.

Each item exposes item ID, repository-relative path, 1-based line/column, symbol, summary text, and declaration display/signature.

Internal hashes, ranking values, semantic anchors, and population fingerprints are not public check output.

Empty sample must be explicit (`sample 0/0`).

## Caller protocol

The tool does not collect answers.

```text
all required answers confidently acceptable -> no escalation
any materially negative answer             -> escalate
uncertain required answer                   -> escalate
```

Escalation preparation has two forms:

```text
hygiene review expand <batch-handle>
    -> expanded frontier batch on stdout

hygiene review handoff <batch-handle> [--file <path>]
    -> durable expanded frontier-review request file
```

The latter is the portable handoff for either a colocated frontier/planner or a completely decoupled reviewer.

## Expansion

`review expand` accepts bare latest-run or fully qualified latest-run batch handles.

Before expansion, recompute current eligible population/fingerprint. If changed, fail with exit 3 and request a new check.

Expanded batch has `mode=expanded`, `reviewerClass=frontier`, `sampleCount == populationCount`, and contains the complete eligible population including previously sampled items with the same questions/rubric.

Expansion does not create a new run and does not mutate latest-run state.

## Handoff

`review handoff` uses the same resolution/revalidation/expanded population as `review expand`; it must not have a divergent semantic implementation.

Default repository-backed destination:

```text
.hygiene/reviews/<handoff-id>/request.json
```

Explicit external destination:

```text
hygiene review handoff B-1 --file <absolute-or-relative-file-path>
```

The artifact contract is defined in `docs/specs/REVIEW-HANDOFFS.md`.

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

Escalate when any sampled summary materially fails Q1-Q4 or the implementer is not confident enough to answer any required question.

For direct same-process use, `review expand` is sufficient. For a durable handoff, PR-visible evidence, or a decoupled frontier/planner, use `review handoff`.

## Relationship to required-summary rule

`docs.summary.required` remains the deterministic missing-summary rule; `docs.summary.quality.review` samples summaries that exist.

## Non-goals

No language heuristic is treated as sufficient quality judgment. No automatic model call, repair, engine-managed review history, schedule, random sampling, or review-answer persistence.
