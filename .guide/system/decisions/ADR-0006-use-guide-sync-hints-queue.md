# 0006 — Use a Guide Sync Hints Queue

## Status

Accepted.

## Decision

Target repositories may contain `.guide-sync/pending/` as a versioned, non-authoritative coordination queue for deferred documentation synchronization hints.

## Consequences

- The guide system keeps methodology outside product repository truth.
- Planning, execution, documentation synchronization, and migration remain separate task modes.
