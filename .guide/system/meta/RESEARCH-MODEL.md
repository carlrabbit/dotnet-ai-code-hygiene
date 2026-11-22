# Research Model

## Status

Authoritative for persistent planning knowledge, research lifecycle, historical research adoption, promotion into project authority, and the planning/implementation research boundary.

## Purpose

Research gives planning a durable place to preserve evidence that would be expensive, unreliable, or wasteful to rediscover later.

It exists to externalize useful planning context without turning planning conversation, exploratory reasoning, or evidence notes into implementation authority.

The core rule is:

```text
persist expensive-to-rediscover evidence
-> use it during planning
-> promote implementation-affecting conclusions into project authority
-> keep research itself non-authoritative
```

## Knowledge layers

Keep these layers distinct:

| Layer | Purpose | Authority | Normal consumer |
|---|---|---|---|
| Pinned `.guide/system/` (upstream root when maintaining the guide) | Reusable methodology and engineering shapes | Shared guide authority for this repository | Planning/migration/sync work |
| Planning conversation | Transient investigation and reasoning | No | Current planner |
| `docs/research/` | Durable planning knowledge and evidence | No | Planning/research work |
| Project specs/architecture/decisions/engineering docs | Concrete project truth | Yes | Planning and implementation |
| `.execution/` | Planner-seeded obligation-completeness inventory and mutable implementation progress | No (milestone remains authoritative) | Planning handoff and implementation |

Research is therefore project-local knowledge, but it is not ordinary project authority.

Investigative execution (simulation, exploration, profiling, temporary diagnostic tests) is not itself a requirement to persist research or add regression tests. Preserve controlled inputs/seeds, methods and results when findings are expensive to rediscover or explicitly required by a milestone; otherwise discard temporary probes. A discovered invariant becomes an operative requirement or bounded regression test only through deliberate promotion. Do not add exploratory workloads to canonical validation by accident.

## When to persist research

Persist research when future planning would materially benefit from retaining the evidence rather than rediscovering it.

Typical cases include:

- external product/runtime/service behavior that required non-trivial investigation;
- experiments, prototypes, benchmarks, compatibility probes, or reverse-engineering observations;
- historical reconstruction needed to understand a current decision surface;
- comparison of materially different approaches where the evidence may be useful again;
- limitations or uncertain behavior that may affect later planning;
- external documentation or source-code findings whose provenance/version matters;
- unresolved questions whose existing evidence should survive a planning-session boundary.

Do not persist research merely because information was encountered during planning.

## What not to persist

Research is not a dumping ground for planning context.

Do not persist:

- conversational transcripts or chain-of-thought style reasoning;
- generic brainstorming with no durable evidence value;
- easily rediscovered generic documentation unless a specific version/interpretation matters;
- implementation task lists or execution progress;
- conclusions that already exist completely as current project authority and have no remaining evidence value;
- copied guide-system methodology;
- copied setup/engineering guides used as project-local authority.

Git history is the normal archive for deleted/superseded repository material. Do not retain research only to create an archive.

## Durable research contract

A durable research artifact should make the following recoverable to a future planner, as applicable:

- **question** — what was being determined;
- **context** — why the question mattered;
- **scope** — what was and was not investigated;
- **evidence/provenance** — documentation, code, versions, experiments, commands, measurements, or other sources inspected;
- **findings** — what the evidence establishes;
- **uncertainty** — what remains unknown, conditional, inferred, or unverified;
- **implications** — which project decision surfaces the findings may affect;
- **freshness** — when the evidence was verified and what change would trigger revalidation;
- **authority impact** — project-authority documents created or changed because of the research, when any.

Use explicit evidence classes when ambiguity matters:

```text
observed  = directly verified from a source, repository state, or experiment
inferred  = reasoned conclusion from observed evidence
assumed   = unverified premise that planning currently depends on
```

Do not invent confidence percentages when evidence does not support quantitative confidence.

## Provenance

Preserve enough provenance to reproduce or reassess a material finding.

Depending on the evidence, this may include:

- authoritative document/page identity and publication/version date;
- repository, commit, file, or source version;
- runtime/tool version;
- experiment setup and command;
- input fixture or dataset identity;
- relevant environment/capability constraints.

Prefer concise references and summarized findings over copying large external documents into the repository.

## Freshness and revalidation

Research does not become invalid merely because time passes.

Record concrete revalidation triggers when known, such as:

- upgrading a runtime, framework, SDK, service, or external application;
- a vendor changing the relevant contract or artifact format;
- changing a project assumption that the finding depends on;
- reopening a decision that the research supported;
- contradictory new evidence.

`Last verified` is evidence metadata, not a guarantee of present truth.

A planner should revalidate only the findings material to the current decision when a trigger has occurred or current evidence creates reasonable doubt.

## Promotion into project authority

Research answers:

```text
What does the available evidence establish?
```

Project authority answers:

```text
What does this project require, support, or guarantee?
```

If a planning conclusion constrains implementation, validation, compatibility, architecture, public behavior, supported environments, or another durable project contract, promote that conclusion into the appropriate project authority, for example:

```text
docs/SPECS.md or docs/specs/<topic>.md
docs/ARCHITECTURE.md or docs/architecture/<topic>.md
docs/DECISIONS.md or docs/decisions/<decision>.md
docs/ENGINEERING.md or docs/engineering/<topic>.md
```

The promoted authority must stand on its own. Ordinary implementation must not need to reconstruct the decision from research.

Research may link to the resulting authority for provenance. Authority may reference research when historical/evidence traceability is useful, but it must still state the operative project rule itself.

## Research lifecycle

Use the smallest lifecycle that preserves planning value:

```text
question
  -> investigate
  -> persist durable evidence when justified
  -> planning interprets evidence
  -> promote material project conclusions into authority
  -> retain, revalidate, supersede, or delete research according to remaining planning value
```

A research artifact may be marked `current`, `needs-revalidation`, or `superseded` when an explicit status is useful. Do not require status machinery for trivial research.

Delete research when its evidence is no longer useful to future planning and all durable project conclusions have been promoted. Mark it superseded rather than deleting only when the old evidence still has concrete planning/historical value.

## Historical research adoption

A project may have valuable planning knowledge that predates this research model or was never captured under it.

Possible historical source material includes:

- previous planning chats or exported conversations;
- uploaded documents and personal/project notes;
- prototypes, experiments, benchmarks, or retained evidence artifacts;
- issues, pull requests, commit history, or historical repository state;
- external references retained from earlier investigation;
- existing research folders created without a defined methodology.

Historical material is input evidence. It is not copied wholesale into `docs/research/` and does not become project authority merely because it influenced earlier planning.

The adoption flow is:

```text
historical material
  -> extract candidate durable evidence/findings
  -> assess provenance and freshness
  -> revalidate where materially necessary
  -> persist bounded research when future value justifies it
  -> promote missing operative conclusions into project authority
```

Research adoption may happen while introducing/upgrading the guide system or independently later when useful historical material becomes available.

### Adoption modes

Choose one mode for the current adoption scope:

```text
none
selective
reconstructive
```

`none` means historical planning knowledge is not reconstructed. Use it when evidence is unavailable or weak, rediscovery is cheap, future planning value is low, or the project is sufficiently settled that recovery would not justify the cost.

`selective` means recover only bounded high-value evidence/findings. This is normally appropriate when a few historical subjects contain costly, unusual, fragile, or likely-to-recur planning knowledge.

`reconstructive` means deliberately establish a broader useful planning-knowledge baseline from surviving historical evidence. It is not exhaustive transcript archival and must still discard conversational sequencing, generic brainstorming, and low-value material.

The selected mode is a planning/workflow decision, not stable project configuration. Do not add it to `.guide-profile.json` merely to record that an adoption occurred.

### Adoption decision surface

Determine the mode from the evidence and expected planning value rather than from a mechanical maturity rule.

Consider, as applicable:

- availability of historical evidence;
- evidence quality and provenance;
- cost or unreliability of rediscovery;
- expected future planning value;
- project maturity;
- expected rate of future change;
- importance of unusual, fragile, external, or poorly documented constraints;
- cost and feasibility of revalidation;
- whether current project authority already preserves the operative conclusion adequately.

Maturity is one input, not a mapping. A mature system with obscure external constraints may justify reconstruction; an early project with little reusable evidence may justify none.

### Adoption completeness

Historical completeness is not the goal.

Missing historical chats, notes, or artifacts must not be fabricated and normally do not block guide adoption/version migration.

Keep these concepts distinct:

```text
guide-system compliance
research-adoption completeness
historical evidence availability
```

A project may be fully migrated to a guide version that defines research while having no recovered historical research.

A small/selective research adoption may be embedded in a project-creation, guide-adoption, or version-migration workflow. If reconstructive adoption is substantial, prefer the dedicated `templates/prompts/special/adopt-research.md` workflow so guide compliance does not depend on broad historical reconstruction.

## Retrieval and planning context

Planning should load research selectively.

Read research when the current milestone or decision surface overlaps its subject, when project authority references it for provenance, or when a previous investigation is likely to prevent material rediscovery.

Do not load the entire research corpus merely because it exists.

When a repository accumulates enough research that discovery becomes difficult, it may maintain a compact `docs/RESEARCH.md` planning index. Such an index is non-authoritative and should identify only information useful for retrieval, for example:

```text
subject
status/freshness
revalidation trigger
related authority
research path
```

The index should not duplicate research findings or project authority.

## Planning and implementation boundary

Ordinary implementation agents should not use `docs/research/` as project authority and should not need research to understand a ready milestone.

Planning must promote every implementation-affecting conclusion required for execution into the ready milestone or referenced project authority.

An implementation or diagnostic milestone may explicitly require inspecting or producing research/evidence when investigation itself is part of the assigned work. Even then, research findings do not silently amend project authority; any material new decision returns to planning.

This preserves the boundary:

```text
planning may consume research
implementation consumes ready milestone + project authority
```

## Research during diagnostic/investigation milestones

When a material planning decision cannot be resolved without new evidence, planning may create a focused diagnostic or investigation milestone.

The milestone should define the evidence to obtain and how success is observed without pre-deciding the conclusion.

After the evidence exists:

1. persist reusable findings under `docs/research/` when justified;
2. return to planning;
3. promote resulting project decisions into authority;
4. produce or revise the implementation milestone.

Do not use an investigation milestone as a mechanism for an implementation agent to invent project policy.

## No generic planning ledger

The guide system does not define a persistent planning ledger analogous to `.execution/`.

Research preserves reusable evidence, not every planning step. Ready milestones and project authority preserve resolved decisions. Conversational scratch reasoning remains disposable.
