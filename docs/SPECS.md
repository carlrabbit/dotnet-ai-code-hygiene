# Specifications

## Product principles

- Deterministic tooling performs what does not require model intelligence.
- Rules remain opinionated, parameterless, enabled/disabled, and engine-ordered.
- Rules may produce deterministic findings or semantic review batches.
- Semantic judgment belongs to the calling agent/model, never to the M0003 CLI.

## CLI

```text
hygiene format
hygiene normalize
hygiene check
hygiene explain
hygiene ignore
hygiene unignore
hygiene ignores
hygiene rules
hygiene review expand
```

`format`/`normalize` remain non-functional until M0004.

## Canonical M0003 rule order

```text
1. docs.summary.required                    v1  finding
2. docs.summary.quality.review              v1  review-batch
3. readability.long-line.review             v1  finding/review-candidate
4. readability.control-flow.visual-block    v1  finding
```

`hygiene rules` exposes output kind in text/JSON. Existing enable/disable configuration applies to the new rule.

## Review batches

Every enabled semantic-review rule evaluates on every successful `check`. One such rule produces exactly one batch, including an explicit empty batch when no eligible subjects exist.

Review batches:

- do not count as findings;
- do not affect `ignoredCount`;
- do not affect finding IDs/ignore matching;
- cannot be passed to `ignore` as findings;
- do not change successful exit code 0.

Detailed contract: `docs/specs/SEMANTIC-REVIEWS.md`.

## Check JSON

Keep `schemaVersion: 1` and add an additive always-present field:

```json
{
  "schemaVersion": 1,
  "runId": "R-...",
  "findings": [],
  "ignoredCount": 0,
  "reviewBatches": []
}
```

Existing M0002 finding fields/semantics remain unchanged.

## Batch identities

```text
R-7K2M9P/B-1
```

Bare `B-1` means latest locally persisted run only. Old/unavailable qualified run handles fail with exit code 3.

## Expansion

```text
hygiene review expand <batch-handle> [--output text|json]
```

The CLI does not infer pass/fail. Callers expand according to the rule's fixed escalation condition.

Expansion revalidates the latest batch population against current source. If review-relevant population changed, fail with exit 3 and instruct rerunning `hygiene check`.

Successful expansion returns the complete eligible population, sets `reviewerClass=frontier`, keeps the same rubric, does not create a new run, and does not mutate latest-run state.

## Model boundary

M0003 metadata may use:

```text
implementer
frontier
```

These are orchestration instructions only. No model API key, SDK, provider, network call, model selection, answer collection, automatic escalation, or review history exists.

## Persistence

Existing `.hygiene/config.json` and `.hygiene/decisions.json` remain deterministic finding state.

Semantic review creates no committed state. Latest-run internal state may retain batch population/fingerprint data needed for expansion and remains under `.hygiene/.state/`.

## Platform

Windows 11 + .NET 11 remains authoritative. No GitHub Actions/workflows.
