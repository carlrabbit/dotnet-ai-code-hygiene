# Migration — Guide System v0.9.1 to v0.9.2 (draft)

## Purpose

Introduce project-local planning guidance and registered project-owned profiles as optional planning extensions. Preserve shared profile composition, scoped applicability, and the disconnected implementation boundary.

## Migration required

No compulsory repository restructuring. Updating recorded guide-system and selected shared-profile versions to `0.9.2` is sufficient when a project does not need new local guidance or profiles.

The new capabilities are opt-in. Existing `.guide-profile.json` schema version 2 remains valid; no conversion to a new schema version is required.

## Conditional adoption

When planning depends on project-specific agentic-development objectives or guide-methodology clarification:

- create or refine `docs/PROJECT-GUIDANCE.md` from actual project decisions, not from empty scaffolding;
- describe objectives, local planning/review obligations, and links to concrete engineering/specification authority;
- keep implementation-affecting policy and validation decisions in ordinary project authority or the ready milestone;
- do not force ordinary implementation agents to load this planning document.

When an uncovered project engineering shape requires multiple coordinated obligations:

- define `.guide/profiles/<id>/PROFILE.md`, with purpose, applicability, mandatory/conditional obligations, project-owned decisions, validation/release expectations, and composition;
- register it in `.guide-profile.json` using `"id": "local:<id>"`, a project-owned `version`, and existing schema-version-2 scopes;
- resolve semantic component/surface IDs through normal architecture/engineering authority;
- verify the registered definition exists, and resolve conflicts with applicable shared/local profiles before a milestone is ready.

Illustrative registration:

```json
{
  "id": "local:android-ime",
  "version": "0.1.0",
  "scopes": [
    { "kind": "component", "ids": ["keyboard-app"] }
  ]
}
```

A Kotlin library may merit a `local:kotlin-library` profile when it exposes a supported library/API surface. An Android input-method component may merit `local:android-ime` when lifecycle, input interaction, privacy, and device validation form a coherent cross-cutting engineering contract. Neither is required merely because Kotlin or Android is present.

## Compatibility and non-goals

- Shared unprefixed profile IDs and existing profile entries are unchanged.
- Profile order still has no semantic meaning, and local profiles cannot silently override base/shared obligations.
- Do not add feature-tag profiles for operating systems, individual packages, execution loci, or single test preferences.
- Do not move project-specific runtime configuration out of ordinary engineering authority into profile docs.
- Do not backfill historical milestones or research; no mandatory retroactive reconciliation.
- Local-profile promotion into the central guide is optional, not part of guide-system compliance.

## Planning and completion

Planning reads project guidance and any registered relevant local profile along with the shared guide and project authority. It resolves conflicting/conditional obligations before `ready`, names operative project-authority references, and includes affected obligations and validation/evidence gates in the handoff.

Completion of this migration can be classified as a metadata-only no-op for the new capabilities, or a conditional local-guidance/profile adoption when justified. No implementation source changes or runtime tests are required by this documentation-only guide version change.
