# 0002 — Use `.guide-profile.json` for Traceability

## Status

Accepted.

## Decision

Use an optional `.guide-profile.json` file in product repositories to record guide-system metadata, selected profiles, profile applicability scopes, repository role, maturity stage, and active/inactive documentation layers.

The file is planning and traceability metadata. It is not ordinary implementation authority and must not become the canonical architecture map for the repository.

Guide-profile schema version 2 records each applied profile with one or more semantic scopes:

- `repository`;
- `component`;
- `surface`.

Component and surface IDs are project-defined semantic identifiers whose meaning is established by normal project authority. Do not use `.guide-profile.json` path globs as the authoritative definition of project architecture.

Profile order has no semantic meaning and does not establish precedence.

## Consequences

- The guide system keeps methodology outside product repository truth.
- Planning can identify which reusable profile obligations apply to which durable project surfaces.
- Mixed repositories can compose broad profiles without pretending every profile applies everywhere.
- Material profile conflicts must be resolved in ordinary project authority, not by metadata ordering.
- Planning, execution, documentation synchronization, and migration remain separate task modes.
