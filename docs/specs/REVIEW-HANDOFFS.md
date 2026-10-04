# Semantic Review Handoffs

## Purpose

A semantic review handoff is a durable, transport-neutral request for frontier/planner review.

It supports both operating modes:

```text
colocated
    implementer and frontier/planner can access the same machine/repository

decoupled
    frontier/planner has no direct access to the implementer's repository/machine
```

The same artifact schema is used in both modes.

A handoff is an explicit engineering/review work product. It is not an engine-managed acceptance record, semantic-review history database, or source of rule state.

## Command

```text
hygiene review handoff <batch-handle> [--file <path>]
```

The command:

1. resolves only the latest available batch;
2. performs the same population revalidation as `review expand`;
3. fails with exit 3 and rerun guidance when stale;
4. constructs the complete expanded frontier review;
5. writes exactly one JSON request artifact;
6. reports the resulting path on stdout.

The command does not invoke a model and does not modify source/latest-run state.

## Default repository destination

Without `--file`:

```text
.hygiene/reviews/<handoff-id>/request.json
```

Example:

```text
.hygiene/reviews/HR-R7K2M9P-B1/request.json
```

The exact safe encoding of run/batch components is implementation-maintained, but the handoff ID must be deterministic for one source run/batch and filesystem-safe.

`.hygiene/reviews/` is intentionally **not Git-ignored** by product defaults. A caller may therefore include the request in the same branch/PR as the work being reviewed.

The CLI does not run `git add`, commit, or otherwise decide whether the artifact is versioned.

## Explicit external destination

With `--file <path>` the exact requested destination file is used after normal filesystem normalization. The destination may be outside the repository.

Example implementer transport:

```text
G:\My Drive\Transfer\HR-R7K2M9P-B1-request.json
```

This path is an orchestration concern, not a hard-coded product dependency.

Parent directories may be created when missing.

The command must refuse to silently overwrite an existing request file. Existing-file conflict is product error exit 3 with clear guidance.

Writes use atomic temp-and-move/replace semantics so a reviewer never observes a partial request file.

## Request schema

Top-level shape:

```json
{
  "schemaVersion": 1,
  "kind": "semantic-review-request",
  "handoffId": "HR-R7K2M9P-B1",
  "createdAtUtc": "2026-10-04T10:00:00Z",
  "source": {
    "runId": "R-7K2M9P",
    "batchHandle": "R-7K2M9P/B-1"
  },
  "rule": {
    "id": "docs.summary.quality.review",
    "version": 1
  },
  "mode": "expanded",
  "reviewerClass": "frontier",
  "populationCount": 8,
  "questions": [],
  "items": [],
  "sources": []
}
```

Required semantics:

- `schemaVersion` is `1` for M0003.
- `kind` is exactly `semantic-review-request`.
- `handoffId` is deterministic for the source run/batch.
- `createdAtUtc` records artifact creation, not semantic acceptance.
- `source.runId` and `source.batchHandle` tie the artifact to latest-run evidence.
- `mode` is exactly `expanded`.
- `reviewerClass` is exactly `frontier`.
- `populationCount` equals `items.length`.
- `questions` are exactly the rule's fixed rubric.
- `items` represent the complete revalidated eligible population.
- engine-internal ranking hashes and persistence objects are not exposed.

## Item identity

Each item preserves the expanded review fields: item ID, repository-relative path, 1-based line/column, symbol, summary, and declaration.

For durable handoff artifacts, item IDs use a review-specific namespace and must not collide visually with persistent ignore IDs:

```text
RI-1
RI-2
...
```

Existing check/expand review-item identifiers may be migrated to the same `RI-*` convention in this amendment because M0003 has not yet received human completion approval.

## Embedded source context

The handoff must be useful when the frontier/planner is completely decoupled from the repository.

The request therefore includes a deduplicated `sources` array containing the full current text of every repository-relative source file that contains at least one handoff item:

```json
{
  "path": "src/Foo.cs",
  "content": "..."
}
```

This is bounded to files that actually contain review subjects, not the entire repository/project.

For a colocated reviewer, paths allow direct repository inspection for additional context.

For a decoupled reviewer, embedded source files provide baseline context. If that context is still insufficient for semantic judgment, the reviewer must mark the item uncertain rather than invent behavior.

## Repository/versioning semantics

A repository-local request is deliberately PR-friendly and may be committed when the team wants the escalation request visible/reproducible in the PR.

A decoupled request can instead be written to an external shared/transfer path.

These are transports of the same artifact. The product does not maintain separate local-vs-remote schemas.

## Result files

M0003 does not require CLI import or persistence of frontier answers.

A frontier/planner may write a sibling result artifact for human/agent use, but result ingestion, durable acceptance semantics, and review-history behavior remain outside this amendment.

When a repository-local result is intentionally kept as PR evidence, it belongs under the same product namespace, for example:

```text
.hygiene/reviews/<handoff-id>/result.json
```

The CLI does not interpret that file in M0003.

## Security/scope

The request intentionally contains source text. The caller chooses the destination and is responsible for ensuring that destination is appropriate for repository source.

No network transfer is performed by `hygiene`.
