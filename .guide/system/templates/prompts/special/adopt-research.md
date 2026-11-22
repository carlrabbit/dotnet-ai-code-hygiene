# Prompt — Adopt Historical Planning Research

Use this workflow to establish or improve a repository's durable planning-knowledge layer from historical material that predates, bypassed, or was not previously captured by the guide-system research model.

This is a planning/research workflow, not ordinary implementation.

Read `meta/RESEARCH-MODEL.md` from the pinned `.guide/system/` snapshot (or the upstream guide root when maintaining it). Keep project research in `docs/research/` and do not copy upstream methodology there; `.guide/system/` is the one intentional upstream snapshot.
Do not copy planning conversations wholesale into `docs/research/`.
Do not treat historical material as project authority merely because it influenced earlier decisions.

## Goal

Recover only planning knowledge whose future value justifies durable retention, while preserving the boundary:

```text
historical material
  -> extract candidate durable evidence/findings
  -> revalidate where materially necessary
  -> persist bounded research
  -> promote missing operative conclusions into project authority
```

A successful research adoption improves future planning context. It does not attempt to reconstruct every historical thought, conversation, or discarded idea.

## Source material

Use only material actually available to the planning task, for example:

- previous planning chats or exported conversations;
- uploaded documents or retained research files;
- project notes;
- prototypes, experiments, benchmarks, or generated evidence;
- issues, pull requests, commit history, or historical repository state;
- external documentation/references retained from earlier investigation;
- current project authority and source/tests needed to interpret historical evidence.

Historical chats and notes are source material, not research artifacts by default.

Do not claim recovery of evidence that is unavailable.

## Select adoption mode

Choose exactly one mode for the current adoption scope:

### `none`

Use when historical recovery has insufficient expected value, evidence is unavailable/too weak, or future rediscovery cost is low.

Do not create research scaffolding.

### `selective`

Recover a bounded set of particularly valuable findings/evidence.

Prefer this when only some historical subjects have meaningful future planning value.

### `reconstructive`

Deliberately establish a useful planning-knowledge baseline from a larger body of surviving evidence.

Use this only when expected future planning value justifies the investigation and curation cost. Reconstructive does not mean exhaustive transcript archival.

The mode is a workflow decision, not persistent `.guide-profile.json` configuration.

## Adoption decision surface

Consider, as applicable:

- availability of historical evidence;
- evidence quality and provenance;
- expected cost or unreliability of rediscovery;
- expected future planning value;
- project maturity;
- expected future rate of change;
- importance of unusual, fragile, external, or poorly documented constraints;
- cost and feasibility of revalidation;
- whether current project authority already preserves the operative conclusion adequately.

Do not map maturity mechanically to an adoption mode.

## Workflow

For `selective` or `reconstructive` adoption:

1. inventory the available historical sources relevant to the chosen scope;
2. identify candidate evidence/findings worth preserving;
3. discard conversational sequencing, generic brainstorming, and chain-of-thought-style material;
4. distinguish observed evidence, inference, and assumptions where material;
5. assess provenance and freshness;
6. revalidate only findings whose current use materially depends on freshness or whose evidence creates reasonable doubt;
7. write or update bounded research artifacts under `docs/research/` using the research model;
8. identify conclusions that currently constrain implementation, validation, compatibility, architecture, public behavior, or supported environments;
9. promote any such missing operative conclusions into normal project authority;
10. create/update `docs/RESEARCH.md` only when the retained corpus is large enough that an index materially improves retrieval;
11. remove or avoid preserving source material that has no continuing planning value.

Do not preserve material merely to make the historical record complete. Git history or external archives may remain the archive; `docs/research/` is curated planning knowledge.

## Authority boundary

Research answers what available evidence establishes.

Project authority answers what the project requires, supports, or guarantees.

A research document may explain why an authority decision was plausible or necessary, but ordinary implementation must be able to follow the operative rule without reading the research artifact.

If historical material exposes a material unresolved project decision rather than recoverable evidence, do not silently decide it as part of research adoption. Return it to normal planning.

## Relationship to guide migration

Research adoption may be triggered by a guide migration, especially when crossing from a guide version without a research model into one that defines it.

Guide compliance and historical research completeness are separate:

```text
guide migration complete
!=
historical research reconstructed
```

Missing historical chats/files do not block guide migration.

A small selective adoption may be embedded in a guide-adoption or version-update workflow. When reconstructive adoption is substantial, perform it through this dedicated workflow rather than making guide migration depend on a broad historical reconstruction.

This workflow may also be run independently later when historical material becomes available.

## Deliverable

Produce only repository-relative planning-knowledge and project-authority changes justified by the adoption.

Do not create implementation source/test changes.

Report:

1. selected adoption mode and rationale;
2. historical material actually available and used;
3. research artifacts created, updated, superseded, or removed;
4. findings revalidated and the evidence used;
5. operative conclusions promoted into project authority;
6. unresolved questions returned to normal planning;
7. material historical knowledge that could not be recovered because evidence was unavailable;
8. whether `docs/RESEARCH.md` was created/updated and why;
9. confirmation that ordinary implementation does not need the adopted research to recover project rules.

## Quality bar

The adoption is acceptable only if:

- retained research has concrete future planning value;
- source conversations/notes were curated rather than copied wholesale;
- provenance and uncertainty are preserved where material;
- stale evidence was not presented as current without appropriate qualification/revalidation;
- implementation-affecting conclusions are represented in project authority;
- research does not become shadow project authority;
- absence of unrecoverable historical material is reported rather than fabricated;
- no empty research scaffolding is introduced;
- the selected adoption mode is proportionate to expected future value.