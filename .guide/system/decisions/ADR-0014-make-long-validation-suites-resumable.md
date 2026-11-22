# ADR-0014 — Make Long Validation Suites Resumable

> **1.0.0-rc.1 applicability:** Historical rationale for an **optional advanced** validation mechanism. ADR-0031 and current models govern its risk/cost-justified use; it is not default milestone compliance.


## Status

Accepted.

## Decision

Validation suites that may exceed agent runtime limits must expose bounded shards and a fast verifier.
