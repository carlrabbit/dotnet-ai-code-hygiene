# Meta Guide

## Status

Authoritative meta-level guide for creating, versioning, applying, executing, and migrating the guide system.

## Purpose

The meta guide defines how this guide system is structured and how guide artifacts are applied to concrete repositories.

It does not define a single universal repository template or maintain a catalog of concrete project types. Instead, it defines how to compose reusable profiles and generic models, then specialize them through project-local authority for a repository's actual technology, runtime, validation, and architectural constraints.

## Core doctrine

```text
Guide repository owns methodology and reusable engineering shapes.
Product repositories contain project truth and concrete specializations.
Product repositories may retain selective non-authoritative planning research.
Milestones separate rigorous planning from economical agent-scale implementation: planning investigates material change impacts and resolves intended outcomes, permitted consequences, protected invariants, and affected interactions before the handoff. Reversible implementation experimentation remains allowed; a small planner-seeded obligation ledger prevents dropped requirements.
Planning may consume durable research and promote conclusions into project authority.
Implementation agents read product repository authority and ready milestones, not planning context.
Milestone assurance is risk-proportionate; distribution qualification belongs to release readiness by default.
Planning and documentation-sync agents use this guide system through the product repository's pinned `.guide/system/` snapshot. The upstream guide repository uses its root directly for its own maintenance.
```

## Guide repository entrypoints

The root `README.md` is a compact assigned-task router. It is **not** an authoritative summary of guide rules or a chronological release narrative. Route directly to the selected canonical prompt or appropriate meta model; avoid requiring an agent to read unrelated workflow or historical context.

Implementation review and correction prompting uses `templates/prompts/review-milestone.md`; routine defect reviews must not create formal human-review machinery or ratchet up validation without demonstrated risk.

`templates/PROMPTS.md` catalogs prompt selection. The actual workflow contract belongs in its canonical prompt and applicable authoritative models. Avoid duplicating operational rules in either navigation entrypoint; duplicated descriptions cannot override the current contract.

Keep historical changes in `CHANGELOG.md`, version-to-version instructions in `migrations/`, and rationale in `decisions/`. Ordinary product Implementers use the product milestone handoff and repository authority, not these guide entrypoints.

## Product repository boundary

Product repositories intentionally vendor one **read-only, version-pinned full guide snapshot** under `.guide/system/`. This is their authoritative shared planning methodology. They must not copy or customize upstream guides elsewhere as active authority. The upstream guide repository itself uses its root rather than nesting a copy. See [Repository-Local Guide Snapshot Model](LOCAL-GUIDE-SNAPSHOT-MODEL.md).

A product repository records the upstream Git tag and peeled source commit SHA of `.guide/system/` in `.guide-profile.json`; `.guide-sync/pending/` remains optional for deferred documentation synchronization hints.

These files are coordination metadata, not ordinary implementation authority.

Concrete specializations and semantic component/surface definitions are ordinary project truth and belong in the repository's normal engineering, specification, architecture, or decision documents.

A project may optionally define `docs/PROJECT-GUIDANCE.md` for agentic-development meta objectives and local interpretations of methodology, and register locally owned engineering-shape profiles under `.guide/profiles/<id>/PROFILE.md` using `local:<id>` in `.guide-profile.json`. These are project-authored planning authority, not central guide copies or ordinary implementation required reading. The planning phase must account for applicable obligations and make the implementation handoff self-contained.

A product repository may also contain `docs/research/` and an optional `docs/RESEARCH.md` index as non-authoritative planning knowledge. Research preserves useful evidence and investigation context; it does not define project behavior.

## Project-local planning extensions

Planning must reconcile the base guide, applicable shared profiles, registered project-local profiles, optional project guidance, and concrete project authority for the affected scope.

Project guidance expresses objectives and local methodology or review obligations. Profiles express reusable engineering shapes and their mandatory/conditional obligations and project-owned decisions. Concrete engineering/specification/architecture authority supplies implementation-facing contracts. Material conflicts require explicit planning resolution; profile order never determines authority.

A project may create a local profile during planning without updating the shared guide. Do not invent a local profile for a single runtime choice or formatting preference. Before `ready`, material implementation-affecting consequences and exceptional validation obligations must be explicit in the milestone or normal project authority; ordinary implementation remains independent from this planning layer.

See `meta/PROFILE-MODEL.md` and `meta/DOCUMENTATION-LAYER-MODEL.md`.

## Planning knowledge and research

Planning often needs more information than should become implementation context or durable project authority.

Examples include:

- external documentation and source observations;
- experiments, compatibility probes, and benchmarks;
- comparison evidence;
- historical reconstruction;
- uncertain findings and known unknowns;
- vendor/runtime behavior whose provenance or version matters.

Persist only the subset that would materially help future planning and would be expensive, unreliable, or wasteful to rediscover.

The information flow is:

```text
external evidence / investigation
        -> durable research when justified
        -> planning interpretation
        -> project authority for implementation-affecting conclusions
        -> implementation
```

Research remains non-authoritative. If a conclusion constrains implementation, validation, compatibility, architecture, public behavior, supported environments, or another durable project contract, planning must promote it into the appropriate project-authority document.

Planning should load relevant research selectively rather than reading the complete research corpus by default.

Ordinary implementation agents should not require research to reconstruct planning decisions. An implementation or diagnostic milestone may explicitly inspect or produce research when investigation itself is part of the assigned work, but any material new decision still returns to planning.

### Historical research adoption

Projects may have valuable planning knowledge that predates the repository, predates the guide-system research model, or remained only in chats/files outside repository authority.

Historical material can be adopted using one of three planning modes:

```text
none
selective
reconstructive
```

The mode is chosen from evidence availability/quality, rediscovery cost, expected future planning value, maturity and expected change, unusual/fragile constraints, and revalidation cost. It is not mechanically derived from maturity and is not persistent `.guide-profile.json` configuration.

Historical chats/files are source material, not research artifacts. Curate durable evidence/findings, preserve provenance and uncertainty, revalidate where material, and promote operative conclusions into project authority.

Guide-system compliance is independent from historical research completeness. Missing historical evidence does not block adoption or migration. Substantial reconstruction should use `templates/prompts/special/adopt-research.md` and may occur later when material becomes available.

See `meta/RESEARCH-MODEL.md`.

## Guide composition principle

Guides compose; repositories do not inherit everything.

A repository should be planned from an explicit small profile selection such as:

```text
base + dotnet-library + public-preview maturity
```

or:

```text
base + artifact-first-runtime + capability-provider role + implementation-ready maturity
```

or, for a first-class command-line product:

```text
base + cli-tool
+ project-local distribution specialization
```

For example, a .NET global/local tool uses `cli-tool` for reusable command/process expectations while `dotnet-tool` packaging remains project-local specialization. Do not add `dotnet-library` solely because the tool is transported as a NuGet package.

Profiles describe broad reusable engineering shapes. They are not feature tags for operating systems, test strategies, packaging choices, execution locations, named vendor products, or concrete project architectures.

### Scoped composition

Multiple broad engineering shapes may coexist in one repository.

Profile applicability may be:

```text
repository-wide
component-scoped
surface-scoped
```

For example, a package suite containing ordinary .NET libraries and source-generator packages can use:

```text
base                [repository]
dotnet-library       [repository]
source-generator     [component: generator-packages]
```

Profiles compose additively. Profile order has no semantic meaning and no profile silently overrides another.

If applicable profile obligations conflict materially, planning resolves the conflict into project-local authority before implementation proceeds.

`.guide-profile.json` records profile applicability for planning/traceability. Component and surface IDs are semantic project-defined identifiers; their meaning comes from normal project authority, not path globs in metadata.

Project-local authority then specializes the reusable model where concrete answers are required.

Example:

```text
profile:
  dotnet-library

project policy:
  integration-first

validation target:
  installed external desktop runtime

project-local specialization:
  concrete runtime, Windows requirements, invocation,
  isolation/reset, evidence, and failure semantics
```

See `meta/PROFILE-MODEL.md`, `meta/SPECIALIZATION-MODEL.md`, and `meta/VALIDATION-MODEL.md`.

## Planning/implementation separation

Milestone work has two distinct phases independent of the human, model, interface, or tool used for either phase.

Planning agents read the task-specific guidance from the product repository's pinned `.guide/system/` snapshot (or this repository's root when maintaining the guide itself). They inspect product-repository truth and relevant planning research to resolve implementation-affecting uncertainty and produce a ready milestone.

Planning owns decisions that materially affect:

- architecture;
- semantics, including intended behavioral changes and permitted secondary consequences;
- protected invariants and existing contracts, including those that must survive a broad rewrite;
- compatibility and cross-component or cross-mode interactions;
- scope and non-goals;
- acceptance criteria;
- validation and review policy;
- applicable profiles and their semantic scopes;
- conflicts between applicable profile obligations;
- required project-local specializations.

Planning also decides whether investigation evidence should be persisted for future planning, revalidated, promoted into authority, superseded, deleted, or adopted from historical material according to `meta/RESEARCH-MODEL.md`.

Planning may create or update durable project-truth documents when those decisions need repository authority.

For substantive AI-executed milestones, planning preserves every independently deliverable material obligation under a stable ID and seeds a compact `.execution/<milestone-id>.md` ledger before `ready`; a trivial indivisible task may explicitly omit it. This prevents dropped work, **not** a one-to-one criterion/test/evidence/gate registry. Release qualification of distributable artifacts belongs to the release boundary unless the milestone explicitly requires otherwise for material risk.

Planning should investigate material product and behavioral consequences thoroughly rather than shifting unresolved semantics to implementation. **Do not optimize for reduced planner effort or short planning documents.** Deep investigation and precise handoff authority may be valuable even when implementation and validation are deliberately inexpensive. Planning should not prescribe concrete implementation mechanics that a capable executor can derive from the live repository unless those mechanics are themselves part of the required contract.

Implementation agents execute a ready milestone. They inspect the live source and tests, derive concrete edits, implement, validate, and produce evidence. They should not load the vendored methodology or external guide repository unless explicitly assigned planning, guide migration, documentation synchronization, or release documentation work.

Ordinary implementation must not depend on `docs/research/` to recover operative project rules. Research may be read only when explicitly relevant to an investigation/evidence obligation or when the ready milestone deliberately references it for non-authoritative evidence.

The implementation handoff must not depend on the planning conversation. Everything required to execute must exist in the ready milestone or its referenced project authority.

Implementation may explore and replace reversible approaches within the accepted goal and invariants. If it discovers a material unresolved architectural, semantic, compatibility, scope, acceptance, validation, profile-applicability/conflict, or specialization decision, that decision returns to planning. Implementation must not silently create new project policy from research or other evidence.

Planning may deliver repository-relative milestone, documentation, or migration work using **authorized Git commits** or **ZIP overlays** under [Planning Handoff Transport Model](HANDOFF-TRANSPORT-MODEL.md). A Git milestone handoff identifies the exact accepted planning commit rather than only a mutable branch name. A ZIP handoff identifies its target base when known and provides move/deletion instructions. Neither transport changes the milestone, obligation ledger, or permission to implement. A planning PR or merge is not implied by using Git.
