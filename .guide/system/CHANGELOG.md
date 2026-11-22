# Changelog

## 1.0.0 (stable)

Migration required: yes for product repositories adopting the repository-local pinned planning methodology; active substantive milestones also need lossless material-obligation accounting. Existing completed milestone history is not rewritten.

Affected areas: planning authority, guide installation/upgrade, profile resolution and routing, bootstrap/adoption/update/special prompts, execution obligation handoff, execution ledger template, and version traceability.

### Changed — self-contained planning

- Vendor one **complete, unmodified, pinned upstream Git tree** at `.guide/system/` in each product repository; normal planning uses this local authority instead of drifting upstream `main`. The upstream guide itself stays at its root.
- Record source Git tag, peeled commit SHA, version and local path in schema-version-2 `.guide-profile.json`. Shared profiles resolve from the snapshot; `.guide/profiles/` and project guidance remain project-owned. Research remains non-authoritative.
- Use Git subtree with squashed upstream history or an exact tracked-tree copy, with explicit target-state-aware upgrades; no guide package, GitHub Release, automatic synchronization, or custom packaging infrastructure is required.

### Changed — completeness without validation overengineering

- Restore **planner-seeded, stable material-obligation IDs** and a lightweight `.execution/<milestone-id>.md` ledger as the default for substantive implementation, documentation, migration and release-readiness milestones. Permit omission for genuinely trivial indivisible work.
- The implementer preserves every planned obligation and reconciles delivered outcomes before completion. This prevents forgotten work; it does **not** mandate one test per requirement, evidence-case registries, proof maps, receipts, or stronger milestone validation.
- Preserve coherent agent-sized milestones, reversible internal experiments, focused real integration, review/correction economy, and independent final-artifact release qualification.

### Clarified — rigorous planning and authorized change boundaries

- Substantive milestone planners investigate materially affected existing contracts, distinguish intended changes from tolerated consequences and protected invariants, and resolve consequential interactions before `ready`. This shifts intellectual effort upstream to reduce implementer ambiguity and correction cycles; planning effort/length is not a target for minimization.
- Plan high-information checks against plausible boundary violations and cross-mode inconsistencies where material, rather than primarily happy-path existence tests. Maintain proportionate implementation validation and no mandatory per-obligation tests, proof registries, or blanket semantics-preservation requirement.

### Clarified — transport-independent planning handoffs

- An authorized Git planning commit or repository-relative ZIP overlay carries the same ready contract; no mandatory archive or PR.
- Git handoffs record the exact accepted planning commit SHA, milestone path and implementation-branch relationship; moving branches require explicit baseline reconciliation. ZIP overlays identify target revision when known and application steps for moves/deletions.
- Planning-only commits, approval/merge, implementation and direct guide/documentation updates remain distinct. No evidence-case or proof machinery returns. See ADR-0034.

### Migration

Use `migrations/guide-system-v1.0.0-rc.1-to-v1.0.0.md` for RC1 adopters; earlier adopters should inspect the applicable historical chain but implement the effective stable 1.0 target state. See ADR-0032 and ADR-0033.


## 1.0.0-rc.1 (release candidate)

Migration required: yes for projects that adopted mandatory 0.9.x AI-executed ledger, evidence-case, validation-receipt, or consumer-at-milestone machinery; otherwise mostly metadata/routing alignment.

Affected areas: milestone scope and autonomy, planning/implementation prompts, acceptance/evidence, validation and release boundaries, constrained execution, special workflows, profiles, templates and guide versioning.

### Changed — agent-scaled planning

- Prefer the largest coherent capability economical to implement, evaluate and discard; do not force human-sized vertical slices.
- Distinguish reversible implementation experiments from high-consequence product risks.
- Make execution decomposition and progress ledgers executor-owned and optional rather than planning-seeded.

### Changed — risk-proportionate assurance

- Distinguish focused feedback, milestone validation, and release-candidate qualification.
- Preserve real-runtime integration where relevant, but move packaged/installed consumer and broad compatibility qualification to release by default.
- Remove default acceptance/evidence-case one-to-one mapping, fingerprinted receipts, sharding, immutable proof records and exhaustive reconciliation.
- Prohibit correction-cycle validation-infrastructure expansion without demonstrated material risk; provide a canonical implementation-review and correction prompt.
- Permit PR-based human acceptance without a dedicated review command subsystem; structured human review remains optional.
- Allow bounded reversible internal architecture alternatives with fixed invariants and evaluation criteria; record brief durable milestone completion notes.
- Require final-artifact consumer checks for actively supported distribution mechanisms at release readiness; keep execution modes independent of scope size.
- Clarify authorization for destructive mutations against persistent/external targets; apply target-state-aware guide migrations and allow straightforward direct updates without milestones.
- Separate genuine project research from copied legacy guides, normalize human-review completion semantics, and clarify source-generator/library profile composition.

### Migration

Use `migrations/guide-system-v0.9.3-to-v1.0.0-rc.1.md`. ADR-0030 and ADR-0031 supersede conflicting defaults from historical decisions. This release candidate is intentionally breaking; it is not a stable 1.0.0 release.

## 0.9.3 (draft)

Migration required: conditional for repositories with materially expensive validation suites or simulations that currently leak into routine tests; otherwise metadata-only/no-op.

Affected areas: validation model, constrained execution, planning/implementation prompts, milestone and engineering templates, guide version metadata.

### Added — validation intent and cost discipline

- Separate development feedback, investigative execution, and authoritative acceptance validation without adding a test tier or profile.
- During iteration prefer cheapest reliable affected checks; at closure verify current complete gate evidence rather than unconditionally rerun every expensive stage.
- Treat independent stages, dependency fingerprints, safe receipt reuse, and measurement of costly setup/execution as proportional optimizations, including suites without timeouts.
- Exclude exploratory simulations, profiling and probes from default regression/CI unless explicitly required; promote durable findings to research/authority or bounded regression deliberately.
- Preserve all target/locus, release/consumer, evidence-case and completion-audit obligations.

### Clarified — agent entrypoint and migration reconciliation

- Keep `AGENTS.md` as a minimal repository-routing entrypoint, with the milestone package and referenced operative authority supplying the implementation contract.
- Bootstrap generates the entrypoint; adoption and version migration check existing routing and edit it only when necessary, preserving project-specific instructions.
- Avoid duplicating validation policy in `AGENTS.md` and do not let its reading order or command restrictions conflict with the canonical milestone execution prompt.

### Changed — guide repository README routing

- Replace the version-by-version README narrative with an assigned-task router, including direct routes for planning, bootstrap, adoption/update, specialized planning, documentation sync, and guide maintenance.
- Mark README and the prompt catalog as navigation rather than competing normative contracts. The selected prompt and authoritative models define current behavior.
- Keep version history in the changelog/migrations and design rationale in ADRs. Ordinary product Implementers continue to use the milestone handoff without reading this repository.

### Migration

See `migrations/guide-system-v0.9.2-to-v0.9.3.md`. No mandatory regeneration of existing milestones or evidence.


### Clarified — execution environment authority

- Separate permission to execute validation from permission to install/modify host software and to create/change CI/CD or remote infrastructure.
- Planning and new-project bootstrap now resolve material toolchain prerequisites, provisioning scope, accepted alternative loci, and blocked behavior before milestone handoff.
- Implementation may use authorized isolated setup and declared dependencies, but may not substitute newly created CI workflows for unavailable local validation; blocked evidence remains explicit.
- No compulsory retrofit of existing completed milestones. The clarification applies when planning new or revising active work.

## 0.9.2 (draft)

Migration required: no by default; conditional for projects that elect to introduce local planning guidance or project-local profiles.

Affected areas:

- project-local planning objectives and discovery;
- local profile registration, scope, and composition;
- guide profile schema-version-2 compatibility;
- planning-to-implementation authority boundary;
- optional project documentation layers and templates;
- planning/new-project/adoption/update prompts.

### Added — project-local planning guidance

Optional `docs/PROJECT-GUIDANCE.md` captures project-specific meta objectives, methodology clarifications, and additional planning/review expectations. Planning must consider it when present. Implementation-affecting rules still belong in ordinary engineering/specification/architecture/decision authority or a ready milestone. Do not make implementation load broad planning rationale.

### Added — project-owned profiles

Projects may register `local:<id>` in `.guide-profile.json` and define its contract in `.guide/profiles/<id>/PROFILE.md`. Existing shared profile IDs remain unchanged. Schema version 2, repository/component/surface scopes, additive composition, and explicit conflict resolution remain in force. Local profile versions are owned by the project, not coupled to the guide version.

A local profile must activate a meaningful engineering shape with cross-cutting obligations; one technology choice, test preference, runtime, or packaging setting alone remains ordinary project specialization. Promotion into a shared profile is optional and requires separate assessment.

### Migration

Use `migrations/guide-system-v0.9.1-to-v0.9.2.md`. No empty project-guidance or local-profile scaffolding is required.

## 0.9.1

Migration required: no by default; conditional when a repository chooses to adopt the new `cli-tool` profile.

Affected areas:

- reusable profile catalog;
- new-project profile selection;
- CLI/process contract guidance;
- consumer/distribution validation;
- project-local specialization boundary;
- guide-profile version metadata.

### Added — CLI tool profile

0.9.1 adds the technology-neutral `cli-tool` profile for repositories or repository scopes that expose a supported command-line executable as a first-class product surface.

The profile activates recurring planning/engineering concerns including:

- command and invocation semantics;
- non-interactive automation;
- stdout/stderr ownership;
- structured output;
- stable exit semantics;
- configuration/input precedence;
- path/filesystem behavior;
- environment/process/cancellation behavior;
- diagnostics;
- help/version behavior;
- compatibility-sensitive CLI surfaces;
- representative process-boundary validation;
- distributed-artifact consumer validation.

### Clarified — profile versus distribution specialization

A concrete distribution mechanism such as `dotnet tool` is not a profile.

A .NET tool normally composes:

```text
base + cli-tool
+ project-local .NET/dotnet-tool specialization
```

Do not add `dotnet-library` merely because the CLI is transported through a NuGet package. Apply `dotnet-library` only when a genuine reusable library/API surface also exists.

Likewise, apply `artifact-first-runtime` only when artifact production/provenance is materially part of the product shape rather than merely because the CLI writes files.

### Migration

Use:

```text
migrations/guide-system-v0.9.0-to-v0.9.1.md
```

Existing repositories do not need to adopt `cli-tool` unless the profile genuinely matches a supported CLI product surface.

## 0.9.0

Migration required: recommended for repositories using AI-executed milestones and persistent execution ledgers; otherwise metadata-only/no-op may be sufficient.

Affected areas:

- milestone obligation identity;
- planning/implementation ledger ownership;
- execution-ledger template;
- planning prompt deliverables;
- work-package decomposition;
- validation-to-criterion traceability;
- final reconciliation and completion evidence;
- guide-profile version metadata.

### Changed — execution ledger ownership

Planning now initializes the execution ledger for AI-executed milestones before `ready`.

Planning seeds only the lossless contract-derived coverage structure:

```text
stable milestone obligation IDs
+ one pending ledger row per independently verifiable obligation
+ required validation gates and intended obligation coverage
```

Implementation continues to own work packages, concrete implementation mapping, evidence, status, and resume state.

This does not return implementation decomposition to planning.

### Added — lossless obligation invariant

The execution model now states:

```text
the execution ledger may compress work
the execution ledger must not compress obligations
```

Several milestone obligations may map to one coherent work package, but the planner-seeded obligation rows remain individually traceable.

Implementation must not delete, merge, renumber, paraphrase, or replace seeded obligation rows merely to simplify tracking.

### Added — criterion-specific validation traceability

Required validation gates identify the milestone obligation IDs they are intended to prove.

Aggregate validation success establishes only the obligations whose required behavior the executed scenario actually exercises.

A passing broad suite or integration scenario does not implicitly prove separately specified behaviors that were not exercised.

### Added — acceptance evidence topology

An obligation may define stable `EC-*` evidence cases when it spans materially different implementation paths, state mechanisms, compatibility surfaces, target surfaces, or failure behaviors.

Examples include scalar/enum dispatch, Root/Batch scope, inherited/direct behavior, provider-specific implementations, or source-project/packaged-consumer surfaces.

Evidence cases describe what must be proven, not how tests are implemented.

Planning must not generate a Cartesian product of every dimension. Evidence cases are used only where one path would not provide reliable evidence for another.

Validation gates map to exact obligation/evidence-case IDs.

### Added — durable completion evidence

Before an AI-executed milestone transitions to `done`, implementation preserves a compact durable completion trace:

```text
obligation/evidence case
-> concrete evidence
-> validation gate/target
-> result
```

The trace lives in the milestone or another immutable completion record explicitly referenced by it.

Mutable `.execution/` state may still be removed after completion according to repository policy. The durable trace preserves reviewability without retaining work-package tasks, resume state, or execution scratch detail.

### Strengthened — final reconciliation

Before `COMPLETE`, implementation verifies exact set equality:

```text
set(applicable milestone obligation IDs)
==
set(ledger obligation IDs)
```

It then verifies concrete implementation and validation evidence for every individual obligation.

Missing, grouped, duplicated, stale, indirect-without-justification, substitute-only, or merely asserted evidence cannot establish completion.

### Migration

Use:

```text
migrations/guide-system-v0.8.0-to-v0.9.0.md
```

Draft/planning milestones should adopt stable obligation IDs and a seeded ledger before becoming ready.

Ready or active AI-executed milestones should be reconciled to the lossless obligation model. Existing work packages and valid evidence can be preserved.

Completed historical milestones do not require retroactive ledger rewriting unless reopened.

## 0.8.0

Migration required: recommended for repositories using integration validation, concrete project-type guidance, mixed/scoped profiles, external/runtime-bound validation, integration-first testing, durable planning research, or historical planning knowledge worth adopting; otherwise metadata/template review may be sufficient.

Affected areas:

- validation model and tier terminology;
- validation targets and execution loci;
- integration-ready maturity semantics;
- profile model and scoped profile composition;
- guide-profile schema version 2;
- reusable source-generator profile;
- project-local specialization;
- planning research and durable evidence;
- historical research adoption and optional transition capabilities;
- planning/implementation context boundary;
- .NET library test-strategy guidance;
- milestone planning/template semantics;
- bootstrap/adoption/version-migration prompts;
- platform-support guidance;
- concrete `project-types/` layer.

### Changed — validation topology

Validation depth is now independent of execution location.

Tier 2 is `standard repository validation` rather than `standard local validation`.

Tier 3 is `integration validation` rather than `PR integration validation`.

Material validation obligations may separately declare:

```text
depth
+ target
+ execution locus
+ platform/capability requirements
+ evidence
```

Local, CI, remote-service, and mixed execution are valid according to project authority. CI is not part of the generic definition of integration depth or integration-ready maturity.

### Added — validation targets and integration-bound projects

The validation model now names the real boundary being exercised as a validation target.

For external/runtime-bound validation, project authority should resolve applicable availability, locus/platform, provisioning, isolation/reset, identity, invocation, evidence, failure-semantics, fallback, release, and consumer-surface concerns.

Concrete target rules remain project-local.

### Added — integration-first policy

Projects may explicitly adopt integration-first testing.

The generic `.NET library` profile no longer assumes a test pyramid or requires unit tests as the default correctness mechanism. Unit tests remain appropriate when isolated validation is materially cheaper, more exhaustive, or more diagnostic.

### Changed — profiles and specialization

Profiles are broad reusable engineering shapes, not feature tags.

Do not create profiles merely for operating system, local/CI execution, integration-first testing, one packaging mechanism, one named external product/service, or one concrete project architecture.

`meta/SPECIALIZATION-MODEL.md` defines project-local specialization: the guide system defines reusable decision surfaces; product repositories contain the concrete answers needed by implementation.

### Added — scoped profile composition

Repositories may combine a small number of profiles when multiple broad engineering shapes genuinely apply.

Profile applicability may be repository-wide, component-scoped, or surface-scoped. Applicable obligations compose additively; profile order has no semantic meaning and no profile silently overrides another.

Material conflicts between applicable profiles must be resolved into project-local authority before implementation.

`.guide-profile.json` schema version 2 records semantic profile scopes for planning/traceability. Component and surface IDs are project-defined semantic identifiers whose meaning comes from ordinary project authority rather than path globs in metadata.

### Added — source-generator profile

0.8 adds a reusable `source-generator` profile for Roslyn source-generator/analyzer-style compile-time surfaces.

It may compose with `dotnet-library`, including in mixed package suites where only selected generator components/surfaces receive the additional source-generator obligations.

Concrete compiler/MSBuild compatibility, generated-source, diagnostic, packaging, and version-alignment rules remain project-local where project-specific.

### Added — planning research and durable evidence

`meta/RESEARCH-MODEL.md` makes research a first-class non-authoritative planning-knowledge layer.

The generic flow is:

```text
investigation/evidence
  -> durable research when future planning value justifies retention
  -> planning conclusion
  -> project authority when implementation depends on the conclusion
  -> implementation
```

Research may preserve external documentation findings, experiments, compatibility probes, historical reconstruction, comparisons, uncertainty, provenance, and revalidation triggers.

Research must not become shadow project authority. Implementation-affecting conclusions must be promoted into ordinary project authority before a ready milestone depends on them.

Planning should load research selectively. Ordinary implementation must remain executable from the ready milestone and referenced project authority without reconstructing decisions from the research corpus.

A lightweight `templates/research/research-template.md` is provided for durable investigations. Projects should not create empty research folders, indexes, or placeholder artifacts merely because the guide supports research.

Existing `docs/research/` content should be classified rather than blindly retained or deleted: useful evidence may be retained/revalidated/superseded, operative rules should be promoted, obsolete notes may be removed, and copied guide documents remain legacy guide leakage rather than project research.

### Added — historical research adoption

Projects whose planning history predates the 0.8 research model may optionally recover durable planning knowledge from surviving chats, uploaded documents, notes, experiments, repository history, or other evidence.

Research adoption uses one of three planning modes:

```text
none
selective
reconstructive
```

The mode is chosen from evidence availability/quality, rediscovery cost, expected future planning value, maturity and expected change, unusual/fragile constraints, and revalidation cost. Maturity does not mechanically determine the mode.

Historical conversations/files are source material, not research artifacts. They must be curated into bounded evidence/findings rather than copied wholesale into `docs/research/`.

Guide-system compliance and historical research completeness are independent. Missing historical material does not block migration and must not be fabricated. A repository may fully adopt 0.8 with `research adoption: none`.

Small selective adoption may occur as part of project creation, guide adoption, or the pre-0.8 -> 0.8 migration. Substantial reconstructive adoption should normally use `templates/prompts/special/adopt-research.md` and may occur later when historical material becomes available.

The selected adoption mode is workflow history, not stable `.guide-profile.json` configuration.

### Changed — bootstrap and migration handling

`new-project.md` uses research only when bootstrap investigation produces evidence worth retaining and can evaluate pre-project planning material when it is actually available; it does not create research scaffolding by default.

`adopt-guide-system.md` distinguishes genuine project research from copied legacy guide material, explicitly evaluates `none`/`selective`/`reconstructive` historical adoption when the target model supports research, and promotes any de-facto project rules before implementation handoff.

`update-guide-system.md` derives research and optional transition semantics from the applicable version-to-version migration chain rather than hard-coding 0.8 behavior into every future guide update.

`meta/MIGRATION-MODEL.md` now distinguishes required guide migration from optional transition capabilities whose historical adoption may be useful but is not required for compliance.

### Removed — concrete project-type catalog

The central `project-types/agentic-2d-game-engine/` guide is removed.

Concrete game-engine architecture/role/runtime rules belong in the engine repository. Reusable concepts remain covered by profiles and generic validation/engineering models.

The guide repository should not grow a parallel catalog of concrete projects or vendor integrations.

### Platform/local-first clarification

Platform support is established by declared representative validation evidence, not by CI alone.

A repository may intentionally use a Windows-local authoritative integration environment while GitHub provides portable checks, release automation, or publication.

### Migration

Use:

```text
migrations/guide-system-v0.7.5-to-v0.8.0.md
```

## 0.7.5

Migration required: recommended for repositories using disconnected planning/implementation handoff; otherwise no-op when durable implementation authority already preserves all resolved material planning decisions.

Affected areas:

- planning-to-implementation information boundary;
- decision completeness and preservation;
- ready-milestone audit;
- baseline implementation model handoff;
- guide-profile version metadata.

### Strengthened

Planning readiness now has two separate obligations:

```text
decision completeness
+ decision preservation
```

Planning may compress investigation history, rejected alternatives, and speculative implementation ideas, but must preserve every resolved material decision required by the baseline executor in durable milestone or referenced project authority.

Before `ready`, planning applies a counterfactual audit: if the planning conversation disappeared, the baseline executor must still be able to recover every material decision without repeating project-level reasoning.

The v0.7.3 execution-ledger/closure model and v0.7.4 consumer-surface validation model remain unchanged.

### Migration

Use:

```text
migrations/guide-system-v0.7.4-to-v0.7.5.md
```

## 0.7.4

Migration required: recommended for repositories that produce distributable artifacts; otherwise version-metadata-only/no-op.

Affected areas:

- validation model;
- distributable artifact acceptance;
- milestone validation template;
- guide-profile version metadata.

### Added

Distributable artifacts now require representative consumer-surface validation through their intended consumption mechanism.

Testing only source projects, internal assemblies, command handlers, unpackaged executables, or successful packaging is not sufficient to prove the distributable boundary.

The generic rule is:

```text
build/package current artifact
-> consume/install/publish through intended mechanism
-> exercise representative public behavior
```

Specific guidance now covers:

- NuGet libraries consumed from the current packed package by an isolated consumer;
- `dotnet tool` packages installed from the current local package through `dotnet tool` and invoked through the installed command/shim;
- standalone executables invoked from the current published/package artifact.

For `dotnet tool`, packaged-tool acceptance should normally prove installation, command resolution, representative successful behavior, applicable failure/exit semantics, and absence of repository-relative/global-install assumptions.

Consumer-surface acceptance should remain small and representative. Detailed behavior continues to belong primarily in lower-level tests.

### Clarified

- consumer-surface validation uses the artifact produced by the current build;
- validation must avoid accidental reliance on globally installed, stale, cached, or repository-internal substitutes;
- milestones that materially affect a distributable surface must include the applicable artifact-level validation gate;
- this is a validation capability, not a new project type, lifecycle phase, or engineering-command requirement.

### Migration

Use:

```text
migrations/guide-system-v0.7.3-to-v0.7.4.md
```

## 0.7.3

Migration required: recommended for repositories using AI-executed coding milestones; otherwise no-op for workflows that do not use the canonical AI implementation prompt.

Affected areas:

- implementation execution shape;
- baseline execution tractability;
- persistent implementation progress;
- final completion reconciliation;
- canonical planning and execution prompts;
- milestone execution model and template;
- planning-to-implementation handoff;
- guide-profile version metadata.

### Corrected

The v0.7.1 completion audit made milestone closure the executor objective, but long runs could still rely too heavily on conversational working memory until the final audit.

v0.7.3 makes requirement coverage and implementation progress explicit throughout execution rather than reconstructing them retrospectively after implementation and validation.

### Added

AI-executed coding milestones now begin with implementation-owned execution decomposition.

Before production edits, the executor creates or reconciles:

```text
.execution/<milestone-id>.md
```

The execution ledger maps milestone obligations and acceptance criteria to bounded work packages, tracks progress, records required validation, and stores concrete evidence/resume state.

A small milestone may use one work package. AI executors do not skip the ledger merely because work appears simple.

The ledger is operational implementation state, not project authority. It cannot amend or reinterpret the ready milestone.

After each coherent work package and relevant validation, the executor updates the ledger.

On context compaction, interruption, or resume, the executor rereads the milestone and ledger before continuing.

### Strengthened completion

Before `COMPLETE`, the executor must freshly reread the primary milestone from disk and reconcile:

```text
milestone obligations
<-> execution ledger
<-> live repository and concrete evidence
```

A checked ledger row is not proof by itself. Unsupported or stale completion claims must be reopened and resolved.

The existing completion audit and terminal outcomes remain in force after this reconciliation.

### Planning and readiness

Baseline-model readiness now includes execution tractability in addition to decision completeness.

Large coherent milestones may remain single semantic milestones when:

- material project-level decisions are settled;
- obligations are explicit and observable;
- implementation can derive bounded coherent work packages without reopening planning;
- long-running progress can be recovered from repository-local operational state;
- validation/evidence can establish completion reliably.

Planning does not pre-author the execution ledger or detailed implementation task list.

### Handoff cleanup

Planning must not generate bespoke per-milestone `EXECUTE-Mxxx.md` or equivalent files that duplicate the canonical execution prompt, milestone acceptance criteria, or referenced project authority.

The normal disconnected handoff is the ready milestone plus referenced authority and the stable `templates/prompts/execute-milestone.md` methodology. Overlay application instructions and a concise milestone-path handoff remain valid transport artifacts.

### Not added

v0.7.3 does not add:

- a new durable milestone lifecycle phase;
- generic milestone/ledger/checklist engineering commands;
- model-strength execution tiers;
- planner-authored line-by-line implementation plans.

### Migration

Use:

```text
migrations/guide-system-v0.7.2-to-v0.7.3.md
```

## 0.7.2

Migration required: recommended for repositories using AI implementation agents; otherwise no-op when existing milestone planning already guarantees baseline-model executability.

Affected areas:

- `ready` milestone semantics;
- planning responsibility;
- baseline implementation model metadata;
- canonical planning prompt;
- milestone template;
- milestone execution model;
- prompt index;
- guide-profile version metadata.

### Added

Milestone readiness now includes a baseline-executability requirement.

For the default ChatGPT-focused guide profile, the baseline implementation model is GPT-5.6 Luna.

Planning must make the milestone executable by the configured baseline implementation model without requiring that executor to invent a new material decision about:

- architecture;
- semantics or behavior;
- compatibility;
- scope or non-goals;
- acceptance criteria;
- validation policy;
- human-review policy;
- project-level invariants.

The baseline model is planning metadata. Ordinary implementation agents still work from the ready milestone and referenced project authority rather than `.guide-profile.json`.

### Clarified

- difficult project-level reasoning belongs in planning;
- a stronger implementation model is not the normal remedy for an under-specified milestone;
- `strong`, `frontier`, and similar model-capability execution tiers are not part of the generic guide model;
- execution profiles continue to describe autonomy and review expectations rather than model strength;
- baseline executability requires decision completeness, not line-by-line implementation prescription;
- large coherent milestones remain valid when decisions are settled and execution or validation can be bounded or resumed;
- if material decisions require new evidence, planning may create a diagnostic milestone first and then return to planning for the final implementation milestone.

### Retained

The v0.7.1 execution contract remains unchanged:

```text
implement -> validate -> completion audit -> continue or terminate
```

Implementation still terminates only as:

- `COMPLETE`;
- `AWAITING HUMAN REVIEW`;
- `BLOCKED`.

The milestone-scoped human-review model and ephemeral interactive review aliases remain unchanged.

### Migration

Use:

```text
migrations/guide-system-v0.7.1-to-v0.7.2.md
```

## 0.7.1

Migration required: recommended for repositories using AI or disconnected implementation agents; otherwise no-op.

Affected areas:

- milestone execution objective;
- completion audit;
- terminal execution outcomes;
- executor scope and reading boundaries;
- canonical execution prompt;
- milestone execution model;
- guide-profile version metadata.

### Corrected

The implementation agent now owns milestone closure, not only implementation and validation.

Execution follows:

```text
implement -> validate -> completion audit -> continue or terminate
```

Passing tests, successful compilation, or completion of listed focus areas is not sufficient by itself to claim milestone completion.

The executor must continue resolving every unsatisfied milestone obligation that is agent-resolvable in the current execution context.

### Added

A mandatory completion audit checks all applicable milestone obligations, including:

- goal and target state;
- acceptance criteria;
- required validation and evidence;
- required artifacts or generated outputs;
- direct documentation obligations;
- migrations, cleanup, and compatibility work;
- human-review gates;
- constraints and invariants;
- supporting work discovered during implementation that is necessary for completion.

Implementation runs terminate only as:

- `COMPLETE`;
- `AWAITING HUMAN REVIEW`;
- `BLOCKED`.

`COMPLETE` requires actual milestone completion, not merely implementation or validation success.

`BLOCKED` is intentionally narrow and is not used for work the agent can continue fixing itself.

### Clarified

- implementation success, validation success, and milestone completion are distinct concepts;
- scope guardrails prohibit unrelated product expansion but permit supporting work necessary to satisfy the milestone contract;
- focus areas and workstreams are guidance rather than edit allowlists unless explicitly contractual;
- implementation may inspect additional repository-local material when necessary to implement or prove a milestone requirement;
- `AWAITING HUMAN REVIEW` and `BLOCKED` are terminal outcomes for the current execution run, not new durable milestone lifecycle states.

### Not added

v0.7.1 does not add generic `milestone-show`, `milestone-check`, or `milestone-complete` engineering commands. Closure remains part of the executor contract and may use repository-specific commands when they already exist.

### Migration

Use:

```text
migrations/guide-system-v0.7.0-to-v0.7.1.md
```

## 0.7.0

Migration required: recommended for repositories using milestone-driven coding workflows; otherwise no-op.

Affected areas:

- milestone lifecycle;
- planning/implementation boundary;
- milestone template;
- canonical planning and execution prompts;
- prompt directory structure;
- meta-guide and migration terminology;
- migration guidance.

### Changed

Coding milestones now have an explicit two-phase lifecycle:

```text
draft/planning -> ready -> implementing -> done
```

Planning resolves material architectural, semantic, compatibility, scope, acceptance, and validation uncertainty.

Implementation begins from a ready milestone, re-inspects the live repository, derives concrete implementation mechanics, implements, tests, and validates.

Execution profile is orthogonal to lifecycle phase. The guide system does not encode particular model tiers as durable execution modes.

Migration, documentation synchronization, and release readiness are workflow types, not execution modes.

### Simplified

The generic milestone contract now emphasizes:

- goal and target state;
- scope and non-goals;
- resolved decisions and constraints;
- required project authority;
- acceptance criteria;
- validation;
- milestone-owned human review when applicable;
- an explicit escalation boundary.

Exhaustive file lists, predicted class changes, and detailed edit sequences are no longer generic milestone requirements unless they are themselves contractually significant.

The normal prompt surface is now:

```text
templates/prompts/
  plan-milestone.md
  execute-milestone.md
  new-project.md
  adopt-guide-system.md
  update-guide-system.md
  special/
  version-specific/
```

For normal development, use `plan-milestone.md` followed by `execute-milestone.md`.

The old human-led, AI-assisted, broad-AI, and AI-executed-human-reviewed planning prompt variants are no longer separate user-facing entry points. Execution profile is selected during planning.

Special planning workflows live under `templates/prompts/special/`. Historical and exact-target migration/repair prompts live under `templates/prompts/version-specific/`.

### Handoff model

The ready milestone plus referenced project authority is the durable planning-to-implementation handoff.

`templates/prompts/execute-milestone.md` is the canonical execution methodology. Planning may provide a concise handoff note or overlay application instructions, but it does not need to regenerate a bespoke execution prompt for each milestone.

### Added

- explicit `ready` milestone semantics;
- implementation ownership of local code/test mechanics;
- return-to-planning behavior for material unresolved decisions;
- `migrations/guide-system-v0.6.0-to-v0.7.0.md`.

### Retained

- external guide authority and localized project truth;
- existing execution profiles;
- deferred documentation synchronization;
- milestone-scoped human review;
- constrained execution and resumable validation.

## 0.6.0

Migration required: conditional.

Affected areas:

- human-review model;
- milestone completion gates;
- review request and record templates;
- review command semantics;
- milestone planning prompts;
- implementation execution prompts;
- validation guidance.

### Changed

Human review is now milestone-scoped.

A review answers:

```text
Was the evidence produced for this milestone acceptable when the milestone completed?
```

It does not establish ongoing project-wide approval.

Approved review records:

- remain historical evidence;
- do not become stale after later commits;
- are not revalidated against future repository state;
- do not require repeated human approval.

`review-check` now validates required reviews for a named active milestone.

### Retained

- review classes;
- repository-local `.review/` state;
- canonical review IDs;
- ephemeral numeric aliases for interactive use;
- thin `eng/` launchers over shared tested implementation;
- constrained-execution and resumable-validation models.

### Removed from the generic model

- perpetual approval validity;
- repository-wide fingerprint-based review staleness;
- re-review triggers caused by later unrelated commits;
- treating historical approvals as current project quality gates;
- requiring completed reviews to be reopened after subsequent work.

### Conditional migration

Repositories that already implemented the previous review model should:

- bind every pending review to one active milestone;
- stop invalidating approved records because repository state changed;
- change `review-check` to require `--milestone <id>` or an equivalent explicit milestone context;
- preserve completed records as historical evidence;
- remove generic stale/re-review behavior from review commands and templates;
- decide how to classify legacy pending requests that do not identify a milestone.

### No-op

Repositories that do not use human review require no product-repository change.

## 0.5.2

Added ephemeral numeric aliases for interactive human-review commands while retaining canonical review IDs as durable identity.

## 0.5.1

Restored full operational prompt templates after abbreviated replacements were introduced in v0.5.0.

## 0.5.0

Added constrained agent execution and resumable validation suites.

## 0.4.0

Added repository-local human review and cross-platform engineering command architecture.
