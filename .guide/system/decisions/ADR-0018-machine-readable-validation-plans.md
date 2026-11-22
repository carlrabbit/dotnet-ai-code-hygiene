# ADR-0018 — Expose Machine-Readable Validation Plans

> **1.0.0-rc.1 applicability:** Historical rationale for an **optional advanced** validation mechanism. ADR-0031 and current models govern its risk/cost-justified use; it is not default milestone compliance.


## Status

Accepted.

## Decision

Resumable suites intended for agents expose a `--plan-json` command describing shard commands and final verification.
