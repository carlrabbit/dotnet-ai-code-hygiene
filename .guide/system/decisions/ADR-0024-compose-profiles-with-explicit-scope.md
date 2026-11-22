# 0024 — Compose Profiles with Explicit Applicability Scope

## Status

Accepted.

## Context

Mixed repositories may contain several first-class engineering shapes at once. A package suite can contain ordinary reusable libraries and source-generator packages, while only selected components need generator-specific packaging and validation rules.

Repository-wide profile selection alone is too coarse for these cases. Introducing primary/secondary profile precedence would make composition order-dependent and hide material conflicts.

## Decision

Profiles compose additively and have explicit applicability.

A profile may apply repository-wide or to bounded project-defined components/surfaces.

Profile order has no semantic meaning. No applied profile silently overrides another.

Compatible obligations are combined. Materially incompatible obligations must be resolved explicitly in project-local authority before implementation proceeds.

`.guide-profile.json` schema version 2 records applicability as semantic `repository`, `component`, or `surface` scopes. Component/surface identifiers are resolved by normal project authority rather than path-glob metadata.

## Consequences

- Mixed repositories can apply reusable guidance only where it actually belongs.
- Profile composition does not become a tag system or inheritance hierarchy.
- Refactoring paths does not silently change profile applicability.
- Planning must detect profile conflicts and preserve their resolution in durable project authority.
- Ordinary implementation agents continue to work from project-local authority and ready milestones rather than `.guide-profile.json`.
