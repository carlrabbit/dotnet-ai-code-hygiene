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

Formatting/normalization move to M0004.

## Boundaries

`DotNetAiCodeHygiene.Cli` remains public process boundary. `DotNetAiCodeHygiene.Core` remains internal application/domain implementation.

CLI owns parsing/rendering/exits. Core owns rule semantics, sampling, batch identities, population revalidation, and existing deterministic behavior.

## Rule output model

M0003 supports first-class output kinds:

```text
finding
review-batch
```

`ReviewBatch` is not a pseudo-finding and cannot be ignored.

Built-ins:

```text
docs.summary.required                    finding
docs.summary.quality.review              review-batch
readability.long-line.review             finding
readability.control-flow.visual-block    finding
```

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

Batch handles are run-local (`R-.../B-1`). Latest-run state expands internally to retain enough data to resolve/revalidate/expand batches. It remains Git-ignored and engine-owned.

No committed review-history file exists.

## Expansion boundary

```text
batch handle
-> latest-run lookup
-> recompute current population
-> verify population fingerprint unchanged
-> emit complete expanded population
```

Expansion prepares data only. It performs no model call and no mutation.

## Model boundary

Core/CLI know only reviewer-class metadata (`implementer`, `frontier`). No provider/API/credential/model-routing concern exists in M0003.

## Summary quality first use case

Summary quality is semantic: presence and language heuristics alone cannot establish useful documentation. Deterministic tooling selects a bounded sample; semantic judgment checks natural German, technical correctness, information value, and clarity.
