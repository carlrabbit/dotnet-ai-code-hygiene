# ADR-0017 — Do Not Attempt to Escape Agent Harness Lifetimes

## Status

Accepted.

## Decision

Backgrounding, `nohup`, `setsid`, and shell timeout changes are not accepted solutions to hard external process-group lifetimes.
