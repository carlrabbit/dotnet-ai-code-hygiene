# ADR-0015 — Use Fingerprinted Validation Receipts

> **1.0.0-rc.1 applicability:** Historical rationale for an **optional advanced** validation mechanism. ADR-0031 and current models govern its risk/cost-justified use; it is not default milestone compliance.


## Status

Accepted.

## Decision

Successful shards write atomic receipts tied to current repository, suite, command, and input fingerprints.
