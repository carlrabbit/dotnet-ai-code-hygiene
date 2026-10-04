# Specifications

## Product principles

- Deterministic tooling performs what does not require model intelligence.
- Rules remain opinionated, parameterless, enabled/disabled, and engine-ordered.
- Rules may produce deterministic findings or semantic review batches.
- Semantic judgment belongs to the calling agent/model, never to the M0003 CLI.
- Escalation preparation is deterministic and may create a durable transport/review artifact.
- Durable handoff artifacts are explicit work products, not engine-managed semantic-review history.

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
hygiene review handoff
```

`format`/`normalize` remain non-functional until M0004.

## Canonical M0003 rule order

```text
1. docs.summary.required                    v1  finding
2. docs.summary.quality.review              v1  review-batch
3. readability.long-line.review             v1  finding/review-candidate
4. readability.control-flow.visual-block    v1  finding
```

`hygiene rules` exposes output kind in text/JSON. Existing enable/disable configuration applies to the semantic review rule.

## Review batches

Every enabled semantic-review rule evaluates on every successful `check`. One such rule produces exactly one batch, including an explicit empty batch when no eligible subjects exist.

Review batches do not count as findings, do not affect `ignoredCount`, finding IDs, or ignore matching, cannot be passed to `ignore` as findings, and do not change successful exit code `0`.

Detailed contract: `docs/specs/SEMANTIC-REVIEWS.md`.

## Check JSON

Keep `schemaVersion: 1` and retain the additive always-present `reviewBatches` field. Existing M0002 finding fields/semantics remain unchanged.

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

## Durable escalation handoff

M0003 additionally supports:

```text
hygiene review handoff <batch-handle> [--file <path>]
```

`handoff` performs the same latest-run resolution and population revalidation required by `review expand`, then persists the expanded frontier-review request as one JSON artifact.

Without `--file`, the artifact is written into the repository-owned namespace:

```text
.hygiene/reviews/<handoff-id>/request.json
```

This location is intentionally suitable for inclusion in a branch/PR and is not product-Git-ignored.

With `--file`, the caller chooses the exact destination file, including a path outside the repository such as a shared Transfer folder. This is the transport for a completely decoupled planner/frontier reviewer.

The artifact format is transport-neutral. See `docs/specs/REVIEW-HANDOFFS.md`.

The command does not commit files, invoke Git mutation, invoke a model, or persist review answers.

## Model boundary

M0003 metadata may use `implementer` and `frontier`. These are orchestration instructions only. No model API key, SDK, provider, network call, model selection, answer collection, or automatic escalation exists.

## Persistence

Existing `.hygiene/config.json` and `.hygiene/decisions.json` remain deterministic finding state.

`.hygiene/.state/` remains ephemeral engine-owned latest-run state and is Git-ignored.

`.hygiene/reviews/` is a product-namespaced durable review-artifact area. Artifacts appear there only when explicitly requested through the handoff command. They are not automatically interpreted as acceptance/history state and are not automatically deleted or committed.

There is no engine-managed semantic-review history database/state in M0003.

## Platform

Windows 11 + .NET 11 remains authoritative. No GitHub Actions/workflows.
