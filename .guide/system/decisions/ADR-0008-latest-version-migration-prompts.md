# 0008 — Use Generic Latest-Version Migration Prompts

## Status

Accepted; terminology normalized in guide system v0.7.0.

## Decision

Use stable adoption/update prompts that resolve the latest guide-system version from the guide repository, changelog, and migration documents, while keeping version-specific migrations as source material.

The generic entry points are:

- `templates/prompts/adopt-guide-system.md` for repositories not yet on a recognized guide-system version;
- `templates/prompts/update-guide-system.md` for repositories already using the guide system.

Version-specific prompts remain historical or exact-target workflows under `templates/prompts/version-specific/`.

## Consequences

- the guide system keeps methodology outside product repository truth;
- migration is a special planning workflow, not an execution mode;
- the resulting migration milestone still follows the same planning-to-ready-to-implementation lifecycle where coding changes are required;
- current users do not need to choose among version-pair prompts for normal upgrades.
