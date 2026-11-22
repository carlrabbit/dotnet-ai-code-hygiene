# Repository-Local Guide Snapshot Model

## Status

Authoritative for installing, pinning, reading, and upgrading the shared guide methodology in product repositories. The upstream `carlrabbit/agentic-project-guides` repository remains the source of published *Git history and tags*, not a live runtime dependency of each planner.

## Source and authority

Each adopting product repository **vendors one complete, version-pinned snapshot** of the upstream guide at `.guide/system/`. This is the **shared planning-methodology authority for that repository**; normal planning reads the local snapshot and relevant project documents, not upstream `main` or an external chat's recalled rules. The upstream guide repository itself uses its root directly and does not vendor itself.

The following remain distinct:

- `.guide/system/`: upstream-owned, **read-only by policy**, version-pinned shared methodology (including its relative references and historical files); never customize in place.
- `.guide/profiles/<id>/PROFILE.md`: project-owned local profiles registered as `local:<id>`.
- `docs/PROJECT-GUIDANCE.md` and project specifications/engineering/decisions: project-owned planning objectives and operative implementation contracts.
- `docs/research/`: selectively retained **non-authoritative** project planning evidence, not copied guide documents.

Only `.guide/system/` is an intentional guide copy. Other copied guide files under `docs/` are legacy leakage unless deliberately promoted into genuine project-owned authority. Shared profiles are resolved from `.guide/system/profiles/<id>/`; project-local profiles cannot edit or implicitly override them. Resolve material conflicts during planning and promote implementation-affecting decisions into project truth or the ready milestone.

## Pin and install

A stable upstream Git tag (`vX.Y.Z`) identifies a complete snapshot; it must be resolved to its **peeled commit SHA**. Use the actual tagged commit, not a branch tip. Historical `snapshot/*` draft tags are archival and not default adoption versions.

Store the source repository, guide version, Git tag, peeled commit SHA, and snapshot path in `.guide-profile.json`:

```json
{
  "guideSystem": {
    "repository": "carlrabbit/agentic-project-guides",
    "version": "1.0.0",
    "tag": "v1.0.0",
    "revision": "<full-peeled-commit-sha>",
    "localPath": ".guide/system"
  }
}
```

These fields extend the existing schema-version-2 metadata object; local profile versions remain independent. The recorded pin must match the vendored contents. A mismatch is a migration/authority defect; do not silently update from upstream or treat an arbitrary local edit as shared methodology.

Use `git subtree add --prefix=.guide/system <guide-remote> v1.0.0 --squash` for initial installation, and `git subtree pull --prefix=.guide/system <guide-remote> <new-tag> --squash` when upgrading if the repository already uses subtree. A tracked file copy of the exact tagged tree is acceptable where subtree tooling is unavailable; **do not introduce a package manager, generator, or live-submodule requirement** just to install Markdown. For either method, verify the source tag/commit and resulting tree, update the metadata, and commit the snapshot and direct project-authority migration together. An upgrade must not override unrelated local content.

The snapshot includes the upstream guide tree unchanged and should retain relative Markdown links. No attempt to filter active profiles, rewrite internal references, or synthesize a partial guide distribution is required. Avoid loading the entire snapshot into model context: read only the task router, selected prompt, applicable models/profiles, and relevant project research.

## Execution and update boundary

Planning, migration, documentation synchronization, and guide-specific review route to `.guide/system/README.md` when operating in a product repository. Ordinary **implementation** still uses root `AGENTS.md`, the ready milestone, planner-seeded obligation ledger where applicable, project authority, and live source/tests; it does not recompose methodology from `.guide/system/`.

**Upgrade-time authority:** For routine work, the installed pin governs. During an **explicitly requested** guide installation or upgrade, the currently pinned version describes the starting state, while the selected **target Git tag's** adoption/update prompt and migration contract govern the transition and its effective target-state requirements. A historical prompt cannot veto a newer target's transition merely because the old rules forbid its new structure. Read the necessary intermediate migrations for preservation requirements, not superseded process obligations. If there is no local snapshot yet, temporarily load the target tagged guide upstream solely to plan and install that first snapshot. Do not consult moving upstream `main` as authority. Once the target snapshot, matching metadata and necessary repository-authority reconciliation are committed, the new local snapshot governs routine planning.

Network access to the upstream guide is needed only for initial installation or an explicitly requested upgrade, never for routine planning. Prefer direct changes for straightforward updates and milestones for substantial coordinated changes. Never automatically pull new methodology. The existing pin remains routine-work authority until the explicit upgrade is completed.

No package publishing, GitHub Releases, background synchronization, cross-repository release automation, or planner/dogfood scenario gate is introduced by this model.
