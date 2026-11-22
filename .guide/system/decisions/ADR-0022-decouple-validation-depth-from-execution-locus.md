# 0022 — Decouple Validation Depth from Execution Locus

## Status

Accepted.

## Context

The previous validation tiers coupled integration validation to PR/CI execution and described standard repository validation as local. That made execution location part of validation depth and treated CI as the natural home of integration testing.

Projects may instead require authoritative integration validation on developer machines, installed desktop runtimes, native environments, remote services, real databases, browsers, or mixed topologies.

## Decision

Validation tiers describe depth and purpose only.

Validation obligations separately declare, as applicable:

- validation target;
- execution locus;
- platform/capability requirements;
- concrete command/check;
- evidence.

Tier 3 is generic integration validation, not PR integration validation.

Local and CI are execution loci, not validation tiers or maturity stages.

## Consequences

- integration-first local workflows are first-class;
- CI is no longer required by the generic definition of `integration-ready`;
- a Windows-local runtime may be the authoritative integration target while GitHub performs portable checks or publication;
- milestones must make target/locus constraints explicit when material;
- profiles should not encode local/CI execution as project categories.
