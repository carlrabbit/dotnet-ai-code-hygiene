# Prompt — Adopt Latest Agentic Project Guide System

Our task is to adopt the selected stable `carlrabbit/agentic-project-guides` model in a repository that currently uses an older local setup/engineering guide model. Apply straightforward adoption directly when authorized; prepare a ready migration milestone/package only when material decisions or coordinated implementation justify it.

During initial adoption, **temporarily load the selected target tagged guide from upstream** for the adoption workflow and migration contract; no local snapshot is available yet. Its target-version instructions govern this explicit transition, even when older local guides forbid shared guide copies. Install the selected tag's full pinned tree at `.guide/system/`, then record its tag and peeled commit SHA in `.guide-profile.json`. The installed snapshot becomes the repository's authoritative shared planning methodology. This is the **one intentional guide copy**. Never customize it or treat old copies in `docs/research/` as the snapshot. See `meta/LOCAL-GUIDE-SNAPSHOT-MODEL.md`.

Target repository documentation must contain project truth and concrete project specialization only, plus optional non-authoritative planning research when durable evidence genuinely warrants retention.

If the target repository contains old copied setup or engineering guides, treat them as legacy/non-authoritative unless the repository explicitly marks them as active project documentation.

If the target repository contains `.guide-profile.json`, use it as guide-selection metadata. Ordinary implementation agents must not be required to read `.guide-profile.json`.

If the target repository contains `.guide-sync/`, treat it as deferred documentation synchronization metadata. Ordinary implementation agents must not be required to read `.guide-sync/`.

## Resolve latest guide version

Before creating files, inspect upstream tags and choose the target **stable** guide-system version; resolve its peeled commit SHA. An archival draft tag or unpinned branch tip is not a default adoption version.

Read at minimum:

- `README.md`;
- `CHANGELOG.md`;
- `meta/VERSIONING.md`;
- `meta/MIGRATION-MODEL.md`;
- `meta/PROFILE-MODEL.md`;
- `meta/SPECIALIZATION-MODEL.md` when present;
- `meta/RESEARCH-MODEL.md` when present;
- `meta/VALIDATION-MODEL.md`;
- only relevant current ADRs needed to resolve migration decisions, not the entire historical `decisions/` directory;
- only the applicable migration chain, not every historical migration;
- `templates/PROMPTS.md`.

State the resolved target version, tag, and commit in the final report and in a migration milestone only if one is warranted.

Do not assume a hard-coded latest version.

## Target use case

Use this prompt when the target repository has one or more of:

- copied setup/engineering guides under `docs/research/`;
- old AGENTS required-reading rules;
- TBPs, guardrails, workflows, or issue templates from an older default model;
- no `.guide-profile.json`;
- no `.guide-sync/`;
- guide documents treated as repository-local authority;
- old central project-type references;
- validation rules that equate integration depth with PR/CI execution;
- mixed repository shapes whose profile applicability is currently implicit;
- research/notes that may contain useful planning evidence or de-facto implementation rules;
- surviving historical planning chats, documents, experiments, or other evidence that may be worth adopting as durable planning knowledge.

## Install the pinned methodology

Include a tracked, unmodified copy of the full selected upstream tagged tree at `.guide/system/` as part of the adoption package or direct repository changes. Prefer `git subtree add --prefix=.guide/system <guide-remote> <tag> --squash`; an exact tracked copy is acceptable when necessary. Keep shared profiles inside the snapshot; preserve separate project-owned `.guide/profiles/`. Populate `.guide-profile.json` with `guideSystem.repository`, `version`, `tag`, `revision` (peeled commit SHA), and `localPath`. The target state must be self-contained for subsequent planning without upstream access. Do not create synchronization tooling or fetch upstream for every planning task.

## Repository inspection

Inspect the target repository before writing files.

Usually read:

- `README.md`;
- `AGENTS.md`;
- `.guide-profile.json` if present;
- `docs/PROJECT-GUIDANCE.md` if present;
- any registered `.guide/profiles/<id>/PROFILE.md` documents;
- old `docs/research/project-setup-guide-*.md` and `docs/research/engineering-guide-*.md` only to understand legacy state;
- `docs/TERMINOLOGY.md`;
- `docs/SPECS.md`;
- `docs/ENGINEERING.md` and focused engineering/specialization docs;
- `docs/ARCHITECTURE.md` when component/surface boundaries matter;
- `docs/MILESTONES.md`;
- relevant existing milestones and decisions.

If `docs/RESEARCH.md` exists, inspect it as a non-authoritative discovery surface and open only research subjects relevant to the migration decisions being made.

Treat genuine project research as non-authoritative planning knowledge. Treat old copied guides as legacy/non-authoritative guide leakage; do not reclassify them as project research merely because they are stored under `docs/research/`.

## Research-adoption decision

When the target guide version defines a research model, explicitly evaluate whether historical planning knowledge should be adopted.

Available source material may include repository-local notes/research and any previous chats, uploaded documents, experiments, prototypes, issue/PR history, or external references actually available to the planning task.

Choose one research-adoption mode for the current adoption scope:

```text
none
selective
reconstructive
```

Use `meta/RESEARCH-MODEL.md` as the authority for the decision surface. Consider evidence availability/quality, rediscovery cost, future planning value, project maturity and expected change, importance of unusual/fragile constraints, revalidation cost, and how much current project authority already preserves.

Do not map maturity mechanically to a mode.

`none` is valid. Historical material may be unavailable or not worth curating. Do not fabricate missing evidence and do not block guide adoption merely because planning history cannot be reconstructed.

`selective` recovers only bounded high-value findings/evidence.

`reconstructive` deliberately establishes a broader useful planning-knowledge baseline from surviving evidence; it is still curated research rather than conversation archival.

Historical chats/files are source material. Do not copy them wholesale into `docs/research/`. Extract durable evidence/findings, preserve provenance/uncertainty, revalidate where material, and promote operative project rules into normal project authority.

A bounded selective adoption may be included in this workflow. If reconstructive adoption would materially expand or delay guide adoption, prefer `templates/prompts/special/adopt-research.md` as a separate planning/research workflow. Guide adoption may complete independently unless a recovered conclusion is actually required to establish the current project contract correctly.

Do not persist the selected adoption mode in `.guide-profile.json`.

## 0.8 profile composition, specialization, research, and validation review

When the resolved target version uses the 0.8 model or later:

- keep profiles broad and reusable rather than turning project details into feature-tag profiles;
- allow a small set of profiles to compose when several broad engineering shapes genuinely apply;
- determine whether each applied profile is repository-wide, component-scoped, or surface-scoped;
- use semantic component/surface identifiers defined by normal project authority rather than path globs as architecture metadata;
- treat profile order as non-semantic and do not invent primary/secondary precedence;
- resolve material conflicts between applicable profile obligations into project-local authority before implementation;
- migrate `.guide-profile.json` to schema version 2 when profile applicability metadata is adopted;
- treat legacy schema-version-1 profile entries as repository-wide unless repository evidence demonstrates narrower intended applicability;
- migrate concrete project-type/vendor/runtime rules into normal project-local authority;
- separate validation depth from validation target and execution locus;
- preserve authoritative local/runtime integration validation when CI cannot exercise the real target;
- record external/runtime-bound validation target, locus, platform/capability, invocation/evidence, and fallback constraints when material;
- preserve an intentional integration-first test strategy without manufacturing unit-test obligations from a generic test-pyramid assumption;
- classify existing planning notes/research as retain, revalidate, promote, supersede, remove, or legacy-guide where that classification is needed for migration;
- promote implementation-affecting conclusions that exist only in research/notes into normal project authority;
- retain research only when it continues to provide material future planning value;
- do not create research scaffolding when no durable planning evidence exists;
- keep ordinary implementation independent of `docs/research/` for operative project rules.

## 0.9.2 local planning-extension review

When adopting 0.9.2 or later, preserve existing project planning objectives in normal authority and optionally consolidate durable agentic-development meta objectives or planning-methodology clarifications into `docs/PROJECT-GUIDANCE.md`. Create neither project guidance nor local profile scaffolding without an actual need.

If the project owns a distinct reusable engineering shape not covered by shared profiles, it may register `local:<id>` in schema-version-2 `.guide-profile.json` and define `.guide/profiles/<id>/PROFILE.md`. Verify each reference, scope, and project-owned version. Do not convert one-off technology choices into profiles.

Reconcile applicable central/local profile obligations and project guidance into operative authority or implementation-ready milestones. Do not require implementation to read planning-only guidance.

## AGENTS.md entrypoint reconciliation

Inspect existing root `AGENTS.md` as a routing document, not as a second guide or a place to paste the latest implementation methodology. If absent and the project needs an agent entrypoint, create a concise project-specific one. Otherwise preserve valid existing instructions while removing outdated required-reading lists, copied generic execution rules, or routing that conflicts with the adopted milestone handoff.

For an assigned substantive implementation milestone, `AGENTS.md` should route first to the ready milestone, its planner-seeded obligation ledger, then explicitly referenced operative authority and relevant source/tests. It must not force the Implementer to reconstruct planning from guide documents, planning-only metadata, or research. Non-milestone agents should use their assigned workflows. Keep material validation requirements, optional evidence-reuse policy, and concrete commands in the execution contract or project engineering authority; do not blindly replace a customized `AGENTS.md` with the template.

Classify an `AGENTS.md` edit as required only when existing routing is incorrect or incompatible; otherwise record no-op. Include necessary entrypoint changes as direct, in-scope migration documentation rather than deferring a broken routing contract to broad documentation sync.

## Deliverable selection

**Direct adoption:** If installing the full tagged snapshot, recording its pin, and reconciling existing project authority/routing are straightforward and require neither a material design decision nor coordinated implementation, apply the changes directly in the authorized repository or branch. Do **not** create a migration milestone, obligation ledger, or implementation handoff merely to administer this work. Verify the installed snapshot and recorded pin, routing, and any modified project authority; report the actual changes.

**Substantive adoption:** When material architecture/contract choices, significant repository migration, or independent implementation-agent coordination genuinely require it, create one ready migration milestone under `docs/milestones/`, with a planner-seeded material-obligation ledger before `ready` and a concise implementation handoff.

Install the complete `.guide/system/` snapshot and create or update `.guide-profile.json` in either case. Add `.guide-sync/pending/` hints only for genuinely deferred documentation cleanup.

Update target-repository docs only when needed to repair `AGENTS.md` entrypoint routing, remove guide leakage, localize concrete specialization, define semantic profile scopes, resolve profile conflicts, promote research-derived project rules, preserve genuinely useful planning evidence, or make the migration implementation-ready.

Do not copy additional, unmanaged guide documents into the target repository outside the intentional full `.guide/system/` snapshot.

Do not create `docs/research/` or `docs/RESEARCH.md` merely because the target guide version supports them.

## Migration classification

Classify migration work as:

- required;
- conditional;
- deprecated;
- manual-review;
- no-op.

When research is present, classify its treatment separately enough to distinguish retained evidence from promoted project authority and legacy copied-guide cleanup.

Record the historical research-adoption decision (`none`, `selective`, or `reconstructive`) in the adoption work when the target model defines that capability. Treat it separately from required guide-system compliance.

## Obligation completeness

For a substantive adoption milestone, planning seeds a lightweight `.execution/<milestone-id>.md` inventory of stable material obligations before `ready`, including snapshot installation and necessary project-authority changes. Preserve and reconcile it during implementation. Direct adoption without a milestone needs no ledger. Do not create evidence-case mappings, per-row validation gates, or fingerprinted receipts by default.

## Planning/implementation separation

There is no direct synchronization between the planning AI and the implementation AI.

Only for substantive adoption requiring an implementation handoff, create an implementation-ready package. The later agent must be able to apply it, open the primary milestone and seeded ledger, read only referenced project authority, implement the agreed outcomes, perform the specified proportionate checks, and finish without reconstructing planning or reading the upstream guide or non-authoritative research for operative rules. Only then generate a concise execution prompt in chat. Direct adoption requires neither a milestone package nor a separate implementation prompt.

## Deliverable

For direct adoption, deliver the authorized repository changes and a concise completion report, including the **actual resulting commit SHA** if changed through Git; no separate implementation baseline is needed. For substantive adoption, deliver the ready milestone, seeded ledger and permitted project authority using `meta/HANDOFF-TRANSPORT-MODEL.md`: authorized Git planning commit with **exact accepted baseline SHA**, milestone path and implementation branch relationship; or a repository-relative ZIP overlay with base/application instructions when relevant. Neither transport changes the project authority or mandates a PR.

Do not include implementation source files, generated code, broad unrelated documentation cleanup, TBPs, issue templates, **unmanaged guide copies outside `.guide/system/`**, planning transcripts, or research scaffolding without durable evidence.

After applying the direct adoption or preparing the substantive package, report the branch/PR or package path, installed tag and peeled commit, changed files and why, material profile/scope and specialization decisions, applicable research-adoption treatment and any deferred work, and documentation-sync hints if needed. Include the primary milestone and a filled implementation prompt **only when** the substantive path requires a handoff.
