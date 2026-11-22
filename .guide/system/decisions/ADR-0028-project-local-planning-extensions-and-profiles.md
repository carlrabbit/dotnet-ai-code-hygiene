# 0028 — Support Project-Local Planning Guidance and Profiles

## Status

Proposed for v0.9.2.

## Context

The shared guide can be specialized through project engineering authority, but planning lacks an explicit home for project-specific agentic-development meta objectives. Additionally, new profile shapes currently appear to require adding them centrally, even when only one consumer project needs them.

## Decision

Introduce optional `docs/PROJECT-GUIDANCE.md` as project-authored planning authority for objectives, methodology clarifications, and additional planning/review expectations.

Allow selected project-owned engineering-shape profiles under `.guide/profiles/<id>/PROFILE.md`. Register them via the `local:<id>` ID namespace in existing schema-version-2 `.guide-profile.json`; maintain project-owned versions and existing repository/component/surface applicability. Unprefixed IDs continue to identify shared profiles.

Local and shared profiles compose additively. Local profile authors may specify mandatory/conditional obligations and project-owned decisions; no profile order or source grants implicit override power. Planner discovery is mandatory for registered applicable profiles and existing project guidance.

Keep technology-specific implementation, validation, and runtime decisions in ordinary engineering/specification/architecture/decision authority. Before `ready`, promote implementation-affecting outcomes into operative authority or the milestone. The disconnected executor need not read planning-only local profile definitions.

## Consequences

- No mandatory new project files and no metadata schema migration.
- Projects can add coherent local engineering profiles without changing the shared guide.
- Planning discovery and handoff requirements change; ordinary implementation inputs do not.
- Local profiles may later be promoted selectively when broadly reusable, but promotion is not required.
- Profiles must not proliferate into feature tags or copy the central guide.
