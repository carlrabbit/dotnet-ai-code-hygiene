# Documentation Layer Model

Documentation layers should be activated by selected profiles, maturity, repository role, and concrete project authority needs.

They are not all mandatory.

Project-local specialization normally uses these ordinary authority layers rather than introducing a separate central-style specialization hierarchy.

## Repository-local guide methodology

`.guide/system/` is a pinned, upstream-owned snapshot supplying *planning methodology*, not project specifications. It is authoritative for shared guide rules in the consuming repository and must not be edited to express local policies. Resolve shared profiles there and project-local profiles in `.guide/profiles/`; see [Repository-Local Guide Snapshot Model](LOCAL-GUIDE-SNAPSHOT-MODEL.md). Root `AGENTS.md` routes planning tasks to the snapshot; implementation tasks remain independent of it.

## Authority layers

Common project-authority layers:

- `README.md`
- `AGENTS.md`
- `docs/TERMINOLOGY.md`
- `docs/SPECS.md`
- `docs/ARCHITECTURE.md`
- `docs/DECISIONS.md`
- `docs/ENGINEERING.md`
- `docs/MILESTONES.md`
- `docs/PUBLIC-DOCS.md`
- `public-docs/`
- `docs/SCENARIOS.md`
- `docs/ARTIFACTS.md`
- `docs/HUMAN-REVIEW.md`

These documents contain project truth according to their defined authority roles.

`AGENTS.md` is a thin repository entrypoint for agent navigation. For milestone implementation it points first to the ready milestone, an optional execution progress note when present, referenced operative authority, and relevant code/tests. For non-milestone work it routes to the assigned workflow. Do not turn it into a second execution guide, repeat validation algorithms there, or use it to amend a milestone's contract. It may contain small enduring repository-specific boundaries, but substantive engineering policy belongs in ordinary engineering/specification authority. Bootstrap normally creates this entrypoint; migrations reconcile it when the routing/authority contract changes and preserve valid project-specific instructions.

## Optional project planning authority

```text
docs/PROJECT-GUIDANCE.md             project-specific agentic objectives and methodology clarifications
.guide/profiles/<id>/PROFILE.md      registered project-local engineering-shape profile
```

Both are **optional project-authored planning authority**, unlike non-authoritative research. Create them only when they improve planning decisions or define meaningful new obligations.

`docs/PROJECT-GUIDANCE.md` may capture project objectives, local interpretations of generic guidance, additional planning/review requirements, and references to operative policy. It must not become a second copy of the shared guide or a replacement for normal engineering/specification authority.

A local profile is loaded only when referenced through `.guide-profile.json` as `local:<id>`. Its applicability may be repository/component/surface scoped. The profile defines an engineering shape and obligations; concrete specialization remains in normal project authority.

Planning evaluates these sources and projects implementation-affecting requirements into the ready milestone or normal project authority. Routine implementation agents do not have to read project-planning guidance or local profile definitions to reconstruct a contract.

## Planning-knowledge layer

Research is an optional non-authoritative planning-knowledge layer:

```text
docs/RESEARCH.md        optional compact discovery/index surface
docs/research/          durable research/evidence artifacts
```

Activate it only when the project has evidence that is materially useful to future planning and costly, unreliable, or wasteful to rediscover.

Research may preserve observations, provenance, experiments, comparisons, uncertainty, and revalidation triggers. It must not become shadow project authority.

Implementation-affecting conclusions must be promoted into the appropriate specification, architecture, decision, engineering, milestone, or other project-authority document before ordinary implementation depends on them.

Planning should load research selectively rather than treating the complete research corpus as required context.

See `meta/RESEARCH-MODEL.md`.

## Default discouraged layers/usages

- `docs/TBPS.md`;
- `docs/tbps/`;
- `.github/ISSUE_TEMPLATE/`;
- copied guide documents under `docs/research/` or elsewhere outside the deliberate `.guide/system/` snapshot;
- research files that merely archive planning conversations or duplicate current project authority.
