# Agent Instructions

## Default implementation path

Read:

- `docs/ENGINEERING.md`;
- the relevant milestone or task;
- authority documents listed by that milestone;
- relevant source and test files.

Use canonical `eng/` commands only.

## Coordination metadata

During ordinary implementation, ignore unless explicitly in scope:

- `.guide-profile.json`;
- `.guide-sync/`;
- `.review/`.

Read `.review/` only when the active milestone requires human-review evidence or review validation.

## Repository-local authority

Implementation agents use repository-local authority. Do not require the external guide repository or a planning conversation to reconstruct the project contract.

Treat `docs/research/` as non-authoritative planning knowledge unless the active milestone explicitly references it as evidence or investigation input.

Treat `.execution/<milestone-id>.md` as operational progress/evidence state, never as authority over the milestone.

## Constrained execution

When a validation command exposes a resumable/sharded plan, use the repository-defined plan/shard/verify flow rather than attempting to escape execution limits.

Do not claim aggregate success from partial logs.

## Do not

- invent commands;
- broaden scope;
- perform broad documentation synchronization unless requested;
- put milestone-specific instructions into this file;
- treat external guide documents as repository authority;
- put complex project semantics into shell launchers.
