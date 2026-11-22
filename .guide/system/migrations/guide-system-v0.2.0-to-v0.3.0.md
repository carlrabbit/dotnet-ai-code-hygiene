# Migration — Guide System v0.2.0 to v0.3.0

## Historical scope

This document records the v0.2.0 to v0.3.0 migration as it existed at that time. Filenames below that use the old `*-latest-*` naming are historical names, not current v0.7.0 entry points.

Current equivalents are:

```text
templates/prompts/adopt-guide-system.md
templates/prompts/update-guide-system.md
templates/prompts/execute-milestone.md
```

## Purpose

Introduce execution handoff prompts and generic latest-version adoption/update prompts.

## Applies to

Repositories or workflows that use disconnected planning and implementation agents.

## Required changes in the guide repository at v0.3.0

- Add execution prompt templates.
- Add historical `adopt-latest-guide-system.md`.
- Add historical `update-to-latest-guide-system.md`.
- Update `templates/PROMPTS.md`.
- Add ADR-0007 and ADR-0008.

## Conditional changes in product repositories

No product-repository change is required merely because this version exists.

At v0.3.0, repositories using planning packages and disconnected implementation agents were instructed to:

- provide an execution handoff for the disconnected implementation agent;
- ensure implementation agents were not instructed to read the external guide repository;
- ensure migration prompts resolved the latest guide version explicitly rather than using fixed version-pair prompts as the default.

In v0.7.0, the ready milestone plus referenced project authority is the durable handoff and `templates/prompts/execute-milestone.md` supplies canonical execution methodology; a bespoke generated execution prompt is no longer required.

## Deprecated at v0.3.0

- Planning packages that provided a milestone ZIP but no usable disconnected execution handoff.
- Implementation prompts that said only "implement the ZIP" without execution constraints.
- Direct implementation instructions such as "upgrade this repository to the latest guide".

## Manual review

- Keep version-specific migration documents as structured historical source material.
- Use current top-level prompt entry points for current work rather than historical filenames in this migration.

## Validation

For historical v0.3.0 validation, the prompt index listed execution and latest-version prompts and implementation prompts prevented executors from reading the external guide repository.

For current work, validate against `templates/PROMPTS.md` and the canonical v0.7.0 prompt paths above.
