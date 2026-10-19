# Agent Instructions

## Default implementation path

For implementation work, read in this order:

1. The ready milestone or task that governs the work.
2. Its planning-seeded `.execution/<milestone-id>.md` ledger for operational progress and evidence.
3. The operative authority documents referenced by that milestone.
4. Relevant source and test files.

The milestone defines scope and acceptance; the ledger records execution and evidence. Repository-local authority is sufficient. Do not require an external guide repository or planning conversation to reconstruct the project contract.

Use repository-defined `eng/` commands for declared project validation gates. Choose focused checks for development feedback when appropriate. Do not invent commands, claim aggregate success from partial logs, or treat investigative runs as acceptance evidence unless the milestone says they are a gate.

## Coordination metadata

During ordinary implementation, ignore unless explicitly in scope:

- `.guide-profile.json`;
- `.guide-sync/`;
- `.review/`.

Read `.review/` when the active milestone requires human-review evidence or review validation.

## Repository-local authority

Treat `docs/research/` as non-authoritative planning knowledge unless the active milestone explicitly references it as evidence or investigation input.

Treat `.execution/<milestone-id>.md` as operational progress/evidence state, never as authority over the milestone.

## Constrained execution

When a validation command exposes a resumable or sharded plan, use the repository-defined plan/shard/verify flow rather than attempting to escape execution limits. A focused run is only evidence for the checks it actually performs; do not claim aggregate success from partial logs.

## Environment and permissions

Executing existing repository tools and restoring project dependencies within the established development environment is allowed when relevant to the task. This does not authorize persistent host provisioning, administrative installation, CI/workflow changes, or remote infrastructure changes; those require explicit project authority.

## Do not

- broaden scope;
- perform broad documentation synchronization unless requested;
- put milestone-specific instructions into this file;
- treat external guide documents as repository authority;
- put complex project semantics into shell launchers.
