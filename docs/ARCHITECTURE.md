# Architecture

## System shape

```text
source targets
-> project-aware Roslyn context
-> enabled rules
   -> deterministic occurrences/findings
   -> semantic review populations/samples
-> ignore matching for findings
-> ordered findings + review batches
-> latest-run snapshot
-> text/JSON presentation
-> caller remediation/review
```

Semantic escalation additionally supports:

```text
latest review batch
-> population revalidation
-> complete expanded frontier batch
-> optional durable review handoff artifact
```

Formatting/normalization move to M0004.

## Boundaries

`DotNetAiCodeHygiene.Cli` remains the public process boundary. `DotNetAiCodeHygiene.Core` remains internal application/domain implementation.

CLI owns parsing/rendering/exits and destination-path surface. Core owns rule semantics, sampling, batch identities, population revalidation, handoff construction, and existing deterministic behavior.

## Rule output model

M0003 supports `finding` and `review-batch`. `ReviewBatch` is not a pseudo-finding and cannot be ignored.

## Generic semantic sampler

```text
eligible subjects
-> stable subject/content fingerprints
-> population fingerprint
-> deterministic SHA-256 ranking
-> fixed-size sample
-> ReviewBatch
```

Infrastructure is generic; population, content fingerprint, sample maximum, questions, and escalation condition remain rule-owned.

## Batch/run state

Batch handles are run-local (`R-.../B-1`). Latest-run state retains enough data to resolve/revalidate/expand batches. It remains Git-ignored and engine-owned.

No engine-managed semantic review history exists.

## Expansion boundary

```text
batch handle
-> latest-run lookup
-> recompute current population
-> verify population fingerprint unchanged
-> emit complete expanded population
```

Expansion prepares data only. It performs no model call and no mutation.

## Durable handoff boundary

```text
batch handle
-> same latest-run lookup/revalidation as expansion
-> expanded frontier batch
-> transport envelope
-> atomic request.json write
```

Handoff creation must reuse expansion/revalidation semantics rather than duplicate a weaker path.

Default repository path:

```text
.hygiene/reviews/<handoff-id>/request.json
```

Explicit file paths can point outside the repository.

`.hygiene/reviews/` is a durable product namespace, distinct from ephemeral `.hygiene/.state/`.

The product does not automatically commit, delete, import, or treat these files as semantic acceptance/history.

## Two reviewer topologies

### Colocated reviewer

The frontier/planner can access the same repository/machine. The handoff contains stable source references plus embedded baseline source context; the reviewer may inspect additional repository context directly.

### Decoupled reviewer

The frontier/planner cannot access the repository/machine. The same request artifact embeds the complete expanded item set, rubric, and full text of source files containing review subjects. It can be transported through any external mechanism, including an implementer-side shared Transfer folder.

No transport provider is built into the CLI.

## Model boundary

Core/CLI know only reviewer-class metadata (`implementer`, `frontier`). No provider/API/credential/model-routing concern exists in M0003.

## Version-control boundary

`.hygiene/.state/` is ephemeral and ignored.

`.hygiene/reviews/` is intentionally not ignored so a handoff/result may be included in a PR when it is material engineering evidence.

Version-control choice remains caller/team policy; the CLI never invokes Git mutation for handoff artifacts.
