# Prompt — Update Repository to Latest Agentic Project Guide System

Our task is to assess and update a repository from an existing `carlrabbit/agentic-project-guides` version to the latest available guide-system version. Apply straightforward metadata/documentation corrections directly when authorized; prepare a ready migration package only when coordinated implementation or material design decisions require it.

Use the repository's existing `.guide/system/` snapshot as its **current-state** shared guide authority. For an explicitly requested upgrade, resolve the selected upstream **target tag** and peeled commit SHA, then follow **that target tag's** canonical update prompt and migration contract for the transition. Older guide instructions cannot veto target-version transformations. If the repository has no local snapshot, temporarily read the selected target tagged guide upstream to bootstrap installation. Install or replace the full pinned snapshot and reconcile project authority; once committed, the target local snapshot governs routine planning. Do not customize the snapshot, copy guides into project docs, or silently follow upstream `main`. See `meta/LOCAL-GUIDE-SNAPSHOT-MODEL.md`.

Target repository documentation must contain project truth only, plus non-authoritative planning knowledge when the applicable guide version explicitly defines such a layer and durable evidence warrants retention.

If the target repository contains old copied setup or engineering guides, treat them as legacy/non-authoritative unless the repository explicitly marks them as active project documentation.

If the target repository contains `.guide-profile.json`, use it as guide-selection metadata. Ordinary implementation agents must not be required to read `.guide-profile.json`.

If the target repository contains `.guide-sync/`, treat it as deferred documentation synchronization metadata. Ordinary implementation agents must not be required to read `.guide-sync/`.

If the target repository contains `.review/`, treat it as repository-local human-review state. Read it only when the milestone requires human review, engineering migration, documentation synchronization, or release readiness.

## Resolve current and latest guide versions

Read the target repository's `.guide-profile.json` and existing `.guide/system/` snapshot, if present, to establish the current effective guide version. Resolve the requested stable **target tag** upstream and its peeled commit SHA. Do not treat a drifting upstream `main` branch as a version. If migrating from an external-only repository, install the first local snapshot as part of this update.

Read at minimum:

- `README.md`;
- `CHANGELOG.md`;
- `meta/VERSIONING.md`;
- `meta/MIGRATION-MODEL.md`;
- all migration documents newer than the current version;
- relevant decisions;
- relevant meta models introduced or changed by those migrations, including `meta/RESEARCH-MODEL.md` when applicable;
- `templates/PROMPTS.md`.

State the current and target versions, target Git tag, and source commit in the final report and in any migration milestone.

Do not assume a hard-coded latest version.

Read the applicable migration chain for version-to-version semantics, then implement the **effective target-version requirements**. Do not reenact superseded intermediate process machinery merely to remove it. The 1.0 target deliberately restores **lightweight planner-seeded material-obligation ledgers** while discarding obsolete evidence-case/proof registries. Preserve intermediate transformations needed to protect project data, historical evidence, compatibility, or active authority.

## Repository inspection

Inspect the target repository before writing files.

Usually read:

- `.guide-profile.json`;
- `docs/PROJECT-GUIDANCE.md` and registered `.guide/profiles/<id>/PROFILE.md`, if present;
- `.guide-sync/pending/` if present;
- `.review/` if present and relevant;
- `README.md`;
- `AGENTS.md`;
- `docs/TERMINOLOGY.md`;
- `docs/SPECS.md`;
- relevant `docs/specs/*`;
- `docs/ENGINEERING.md`;
- relevant `docs/engineering/*`;
- `docs/MILESTONES.md`;
- relevant milestones, decisions, architecture docs, public docs, workflows, and command scripts.

If `docs/RESEARCH.md` exists and a migration introduces or changes research/planning-knowledge semantics, use it as a discovery surface and inspect only research subjects relevant to the migration decisions.

Treat copied guides under `docs/research/` as non-authoritative legacy material unless explicitly marked otherwise. Do not assume every file under `docs/research/` has the same role: the applicable migration may distinguish genuine project research/evidence from copied guide leakage or obsolete notes.

## Replace the local guide snapshot

Install or upgrade the complete, unmodified tagged upstream tree at `.guide/system/` (prefer `git subtree add/pull --squash`, or an exact tracked copy). Update `.guide-profile.json` source tag, peeled revision, version, and path **in the same change** as snapshot replacement and applicable project-authority migration. Do not automatically synchronize or change unrelated project files. If a nonstandard local change exists in the snapshot, report it as an authority conflict rather than overwriting it silently.

## Apply version-specific model changes

Derive required repository changes from the complete migration chain between the recorded current version and the resolved target version.

For every changed guide concept:

- identify existing repository state shaped by the old model;
- apply the effective target-state classification and transformation, retaining any indispensable intermediate data/authority preservation;
- preserve concrete project truth rather than replacing it with copied guide methodology;
- update coordination metadata only where the target guide version requires it;
- keep the implementation handoff independent of external guide documents and planning conversation.

When a target migration introduces or changes a non-authoritative research/planning-knowledge layer:

- follow that migration's retention/promotion/freshness rules rather than inventing a generic archival policy;
- do not create research directories, indexes, or placeholder notes mechanically;
- distinguish retained evidence from project authority;
- promote any implementation-affecting conclusion into ordinary project authority when required by the target model;
- ensure ordinary implementation does not need research to reconstruct the migrated project contract.

## Optional transition capabilities

Some migration steps may introduce a reusable capability whose historical adoption is useful but not required for guide-system compliance.

Follow the migration document that introduces the capability. Keep its adoption decision separate from required migration changes.

For a transition that introduces historical research adoption, such as crossing from a pre-0.8 guide model into 0.8, evaluate the migration-defined mode:

```text
none
selective
reconstructive
```

Do not infer `reconstructive` merely because historical chats/files exist or because the project has a particular maturity stage.

A bounded optional adoption may be included when it is cheap and useful. If substantial reconstruction would dominate or delay the guide update, route it to `templates/prompts/special/adopt-research.md` and allow the guide migration to complete independently unless the migration document explicitly makes a recovered conclusion necessary for correctness.

Unavailable historical evidence must not be fabricated and does not by itself block guide migration.

Do not persist a historical adoption mode in `.guide-profile.json` unless a future migration explicitly changes the coordination schema to require such state.

This prompt intentionally delegates exact optional-transition behavior to the applicable version migration documents so future guide versions can evolve without being constrained by hard-coded 0.8 semantics.

## Local planning guidance and profiles

When the migration chain includes 0.9.2 or later, evaluate existing project-local planning objectives and registered local profiles as part of the effective planning contract. `docs/PROJECT-GUIDANCE.md` is optional, and `.guide/profiles/<id>/PROFILE.md` only applies when explicitly registered as `local:<id>` in schema-version-2 `.guide-profile.json`.

Do not mechanically create either layer or reclassify technology-specific engineering settings as local profiles. Preserve project-owned versions, resolve missing definitions and scope conflicts during planning, and promote implementation-affecting obligations into normal authority or the ready migration milestone.

## AGENTS.md entrypoint reconciliation

When a migration changes implementation handoff, required reading, validation routing, or authority boundaries, compare existing root `AGENTS.md` with the target model. It is an agent navigation entrypoint, not a second generic execution prompt. Preserve valid project-specific instructions; remove stale or conflicting requirements and unnecessary copies of methodology. If absent, create an entrypoint only when useful for the project's agent workflow.

For a substantive ready milestone, route first to the milestone, its planner-seeded obligation ledger, explicitly referenced operative authority, then relevant code/tests. For other tasks, route to the assigned workflow. Do not require ordinary Implementers to consult the external guide, planning-only project guidance/profiles, or research to reconstruct an operative rule.

Do not mechanically replace `AGENTS.md` on every guide update. Classify it as a required direct documentation change when current instructions contradict the target handoff; conditional when navigation actually needs improvement; no-op when sound. Record the decision in the migration report or milestone when one is needed. Keep validation cadence, stage receipts/fingerprints, simulation isolation, and concrete commands in milestone/project engineering authority rather than accumulating them in `AGENTS.md`.

## Deliverable selection

**Direct update (default for straightforward changes):** If the target state can be reached by a straightforward tagged snapshot replacement plus small, well-understood metadata, configuration, or documentation edits without material design decisions or coordinated implementation, apply those changes directly in the authorized repository/branch. Examples include updating `.guide-profile.json`, fixing `AGENTS.md` routing, and removing obsolete guide-derived process statements. Do **not** create `docs/milestones/` entries, implementation prompts, or execution scaffolding merely to administer those edits. Verify the affected text/configuration and summarize the achieved target state and any real residual work.

**Ready migration milestone (when warranted):** If the transition needs substantive coordinated changes, unresolved material architecture/contract decisions, or an independent implementation-agent handoff, produce **one** ready migration milestone under `docs/milestones/` and seed a compact obligation ledger before `ready`. Include only the materially applicable current/target versions, profile/scope and execution metadata, required/conditional/deprecated/manual-review changes, relevant authority, acceptance, concrete validation, direct documentation impact, `AGENTS.md` reconciliation, optional transition choices, and recovery considerations. Omit sections with no purpose; do not fabricate validation matrices or an evidence registry.

Update `.guide-profile.json` with matching snapshot source/commit metadata; create `.guide-sync/pending/` hints only where the effective target state requires them. Do not copy unmanaged guide documents outside `.guide/system/` or perform unrelated product development.

## Human review

Apply relevant historical migration rules only to the versions traversed. Under stable 1.0.0, formal human acceptance is optional unless explicitly required by the milestone or project. A normal PR review or concise milestone-local decision may satisfy required acceptance; do not create new review registries, aliases, command hosts or review-check launchers for one approval.

Existing repositories with a deliberately adopted structured review subsystem may retain their project-owned contract. Preserve completed decisions as historical rather than demanding reapproval after unrelated commits. Classify an ambiguous active legacy review for manual resolution.

When the migration milestone itself requires human judgment, specify the subject and evidence and record the decision in an already supported location. Do not create an ongoing repository-wide review gate.

## Constrained execution

Inspect engineering commands for long aggregate wrappers or agent-harness sensitivity.

When migration validation is demonstrably expensive or constrained, consider independently runnable stages. Add resumability, receipts, or a verifier only where these mechanisms reduce total cost and project authority specifies reliable stage success criteria. Do not infer success from partial output.

## Obligation completeness

For a substantive migration milestone, planning must seed `.execution/<milestone-id>.md` with every independently deliverable material obligation before `ready`. The implementer preserves IDs and reconciles outcomes. Direct lightweight guide updates need no milestone or ledger. Do not introduce evidence-case registries, validation-gate maps, or proof receipts by default.

## Planning/implementation separation

There is no direct synchronization between the planning AI and the implementation AI.

For a substantial migration, create an implementation-ready package. The later implementation agent must be able to:

1. apply the package into the target repository;
2. open the primary milestone document;
3. read only the authority documents explicitly listed in that milestone;
4. implement the focus areas;
5. run the specified validation;
6. handle constrained execution using resumable validation when required;
7. complete the current migration milestone's required human review or report its blocking status;
8. finish without reconstructing planning context, reading the external guide repository, or depending on non-authoritative research for operative project rules.

Only when an implementation handoff is needed, supply a concise execution prompt for the later implementation agent. Direct updates require no such prompt.

## Deliverable

For straightforward updates, deliver authorized direct changes and a concise report with the resulting commit SHA when Git was used; do not manufacture an implementation baseline. For substantive migrations, follow `meta/HANDOFF-TRANSPORT-MODEL.md` for planning-only Git or ZIP delivery. Git handoffs identify the **exact accepted planning commit SHA**, primary milestone, and whether implementation continues on the planning branch or branches from that commit; ZIP overlays identify their target base when known and any move/deletion/application instructions.

Do not include implementation source files, generated code, broad unrelated documentation cleanup, TBPs, issue templates, **unmanaged guide copies outside `.guide/system/`**, planning transcripts, or research scaffolding that the migration chain does not justify.

For direct updates, report the versions, changed files, target-state verification, and remaining issues. When a migration package is needed, additionally report:

1. branch\/PR link or download link, as applicable;
2. included file list;
3. reason each file is included;
4. confirmed or inferred profiles/scopes, role, maturity, and execution mode as defined by the target guide version;
5. a filled execution prompt for the later implementation agent;
6. documentation-sync hints and the `.guide-sync/pending/` files created;
7. required human-review items and evidence expectations;
8. constrained-execution or resumable-validation instructions, if applicable;
9. research/planning-knowledge migration actions, if applicable;
10. optional transition-capability decisions and any deferred dedicated workflow, if applicable.

## Quality bar

A direct update is complete when the effective target-state changes are correctly applied and proportionately verified, without mandatory milestone creation. For a substantial migration package, it is acceptable only if:

- the implementation goal is unambiguous;
- required authority documents are explicitly listed;
- the later implementation agent does not need to read the external guide repository;
- the later implementation agent does not need to reconstruct project rules from planning research;
- applicable migrations are inspected, necessary intermediate preservation is performed, and superseded transient obligations are not reenacted;
- optional historical reconstruction is not confused with required guide compliance;
- validation expectations are concrete for the target model;
- direct documentation updates are separated from deferred synchronization;
- `AGENTS.md` is reconciled when its routing contract changes, without duplicating implementation methodology;
- provider and consumer responsibilities are not blurred;
- human-review requirements are explicit and milestone-scoped when the applicable guide version requires them;
- long validation is made resumable only when its cost or execution limits justify additional machinery;
- aggregate success is never inferred from partial output;
- copied guide material is not mistaken for current project authority or durable project research;
- genuine research/evidence is not deleted or promoted mechanically without applying the relevant migration semantics;
- unavailable historical evidence is reported rather than fabricated;
- no TBP or issue-template dependency is introduced;
- the later implementation agent does not need to reconstruct planning context.
