# ADR-0029 — Separate Validation Intent and Reuse Current Evidence Safely

> **1.0.0-rc.1 applicability:** Historical rationale for an **optional advanced** validation mechanism. ADR-0031 and current models govern its risk/cost-justified use; it is not default milestone compliance.


## Status

Accepted for guide-system v0.9.3 (draft).

## Context

AI executors can spend more time repeating full build/test/package/consumer suites than implementing the product. Existing focused validation and resumable receipts primarily address short execution lifetimes, while exploratory simulations can accidentally leak into ordinary regression tests.

## Decision

Distinguish development feedback, investigative execution and acceptance validation independently of validation tier and execution locus. Prefer affected feedback during implementation, independent stages for expensive validation, and verification of complete current evidence at milestone closure. Reuse passing evidence only when its relevant inputs and dependencies are tracked reliably; use conservative invalidation otherwise. Exploratory probes are excluded from default regression/CI unless explicitly required and are promoted deliberately.

Keep cost priorities in optional project planning guidance, concrete stage commands/freshness and investigative isolation in operative project engineering authority, and required proof obligations in ready milestones. Do not introduce a profile or mandatory caching framework for a test strategy.

## Consequences

- A late-stage failure need not repeat unrelated successful expensive stages.
- The aggregate verifier checks freshness and completeness, not every stage by rerunning it.
- Missing/uncertain evidence still fails closed; release and consumer evidence remain mandatory when contracted.
- Cheap suites can continue to use a single direct validation command.
- Simulations can remain disposable or non-authoritative research instead of becoming permanent expensive regression tests.
