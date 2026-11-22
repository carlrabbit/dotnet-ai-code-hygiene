# Prompt — Documentation Synchronization Pass

Our task is to perform a documentation synchronization pass for the target repository.

Use the product repository's pinned `.guide/system/` snapshot as shared planning methodology (or the upstream guide's root when maintaining that repository). Project-specific authority and research stay local. Do not edit the pinned guide, duplicate it elsewhere, or make implementers read it. See `meta/LOCAL-GUIDE-SNAPSHOT-MODEL.md` within the guide root.

Target repository documentation must contain project truth only.

If the target repository contains old copied setup or engineering guides, treat them as legacy/non-authoritative unless the repository explicitly marks them as active project documentation.

If the target repository contains `.guide-profile.json`, use it as guide-selection metadata. Ordinary implementation agents must not be required to read `.guide-profile.json`.

If the target repository contains `.guide-sync/`, treat it as deferred documentation synchronization metadata. Ordinary implementation agents must not be required to read `.guide-sync/`.

## Work state

This direct synchronization pass does not automatically create a milestone. Track existing `.guide-sync/pending/` hints. If executing an already ready substantive milestone, read and preserve its planner-seeded `.execution/<milestone-id>.md` obligation ledger. No new evidence-case registries or validation-gate maps.

## Goal

Synchronize repository documentation using the target repository's project truth and the deferred hints under:

```text
.guide-sync/pending/
```

This is a documentation task, not an implementation task.

## Required inputs

Inspect:

- `.guide-profile.json` if present;
- `.guide-sync/pending/`;
- `README.md`;
- `AGENTS.md`;
- `docs/TERMINOLOGY.md`;
- `docs/SPECS.md`;
- relevant `docs/specs/*`;
- `docs/ENGINEERING.md`;
- `docs/MILESTONES.md`;
- relevant milestones, decisions, architecture docs, public docs, and engineering docs.

Do not treat old copied guides under `docs/research/` as authoritative unless the repository explicitly says otherwise.

## Work rules

- Resolve each pending sync hint whose completion criteria can be satisfied.
- Delete resolved hint files from `.guide-sync/pending/`.
- If a hint cannot be completed, update it with current status, completed parts, remaining blockers, and next recommended action.
- Keep durable project truth in `docs/`, `public-docs/`, `README.md`, `AGENTS.md`, and relevant project docs.
- Do not duplicate upstream guide documents in project documentation; `.guide/system/` changes only through explicit guide-version updates.
- Do not add TBPs or issue templates unless explicitly requested.
- Do not perform implementation source changes unless explicitly required for documentation metadata.

## Deliverable

Follow `meta/HANDOFF-TRANSPORT-MODEL.md` to deliver authorized direct documentation changes on a Git branch, or as a repository-relative ZIP overlay. Report the **actual resulting Git commit SHA** (not an artificial milestone baseline) or the ZIP target revision when known and clear application instructions for changed, moved, or deleted files. Do not leave obsolete files because ZIP cannot encode deletion; no new milestone is implied.

In chat, report:

1. included file list;
2. deleted or resolved `.guide-sync/pending/` hints;
3. pending hints that remain and why;
4. documentation areas synchronized;
5. validation recommendations.
