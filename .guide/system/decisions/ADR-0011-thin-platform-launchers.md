# ADR-0011 — Keep Platform Launchers Thin

## Status

Accepted.

## Decision

Bash and PowerShell launchers forward arguments and exit codes but do not own project or workspace semantics.
