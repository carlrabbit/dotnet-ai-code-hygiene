# Prompt — Repair Repository After Shortened v0.5.0 Prompt Application

Our planning task is to assess and repair a repository that may have been migrated or planned using an unintentionally shortened v0.5.0 prompt template.

The active guide authority is external:

- Guide repository: `carlrabbit/agentic-project-guides`

Do not copy guide documents into the target repository.
Do not make target repository documentation reference guide documents as operational authority.
Use the guide system only for planning, migration, documentation synchronization, review, and release readiness.

Target repository documentation must contain project truth only.

If the target repository contains old copied setup or engineering guides, treat them as legacy/non-authoritative unless the repository explicitly marks them as active project documentation.

If the target repository contains `.guide-profile.json`, use it as guide-selection metadata. Ordinary implementation agents must not be required to read `.guide-profile.json`.

If the target repository contains `.guide-sync/`, treat it as deferred documentation synchronization metadata. Ordinary implementation agents must not be required to read `.guide-sync/`.

If the target repository contains `.review/`, treat it as repository-local human-review state. Read it only when the milestone requires human review, engineering migration, documentation synchronization, or release readiness.

## Context

The v0.5.0 update package abbreviated several prompt templates and omitted important operational requirements. The repository may still be correct; do not assume it is broken.

Perform a focused assessment and create a corrective migration package only for missing or incomplete obligations.

## Guide version resolution

Inspect the guide repository and confirm that the latest version is at least v0.5.1.

Read:

- `CHANGELOG.md`;
- `migrations/guide-system-v0.5.0-to-v0.5.1.md`;
- `migrations/repair-shortened-prompt-application.md`;
- the corrected prompt templates;
- relevant meta models and ADRs.

## Repository inspection

Inspect:

- `.guide-profile.json`;
- `.guide-sync/pending/`;
- `.review/`;
- `README.md`;
- `AGENTS.md`;
- `docs/TERMINOLOGY.md`;
- `docs/SPECS.md`;
- `docs/ENGINEERING.md`;
- `docs/MILESTONES.md`;
- the milestone or migration package generated from the shortened prompt;
- relevant specs, decisions, command scripts, and validation suites.

## Assessment areas

Check whether the shortened prompt caused omissions in:

- repository role and maturity classification;
- guide profile selection;
- authority-document routing;
- milestone goal, scope, non-goals, and focus areas;
- provider/consumer distinction;
- direct versus deferred documentation impact;
- `.guide-sync/pending/` hints;
- human-review applicability, evidence, and blocking behavior;
- validation tiers and concrete commands;
- constrained-runtime handling;
- resumable-validation plan, shards, receipts, and verifier;
- execution prompt generation;
- migration classification;
- chat handoff requirements.

## Output

Create one focused corrective milestone under `docs/milestones/`.

Classify every assessed area as:

- required repair;
- conditional repair;
- already satisfied;
- manual review;
- no-op.

Do not recreate or broadly rewrite correct repository files.

Create `.guide-sync/pending/` hints only for deferred documentation work.

Create `.review/pending/` requests only when actual human judgment is required.

Update `.guide-profile.json` to v0.5.1 only when the corrective migration includes that step.

## Planning/implementation separation

There is no direct synchronization between the planning AI and the implementation AI.

Create an implementation-ready package. The later implementation agent must be able to:

1. unpack the ZIP into the target repository;
2. open the primary milestone document;
3. read only the authority documents explicitly listed in that milestone;
4. implement the focus areas;
5. run the specified validation tiers;
6. handle constrained execution using resumable validation when required;
7. complete required human review or report its blocking status;
8. finish without reconstructing planning context or reading the external guide repository.

Also generate a concise execution prompt in chat for the later implementation agent.

## Deliverable

Create a downloadable ZIP archive containing only new or replacement files that should be added to the target repository. Preserve repository-relative paths inside the ZIP.

Do not include implementation source files, generated code, broad unrelated documentation cleanup, TBPs, issue templates, or copied guide documents.

After creating the ZIP, respond with:

1. download link;
2. included file list;
3. reason each file is included;
4. confirmed or inferred profile, role, maturity, and execution mode;
5. a filled execution prompt for the later implementation agent;
6. documentation-sync hints and the `.guide-sync/pending/` files created;
7. required human-review items and evidence expectations;
8. constrained-execution or resumable-validation instructions, if applicable.

## Quality bar

The package is acceptable only if:

- the implementation goal is unambiguous;
- focus areas are sized appropriately for the selected execution mode;
- AI-executed milestones are not artificially split into tiny human-sized tasks;
- required authority documents are explicitly listed;
- the later implementation agent does not need to read the external guide repository;
- validation expectations are tiered and concrete;
- direct documentation updates are separated from deferred synchronization;
- provider and consumer responsibilities are not blurred;
- human-review requirements are explicit when automation cannot decide acceptance;
- long validation is made resumable when the execution environment may terminate long-running commands;
- aggregate success is never inferred from partial output;
- old copied guides are not treated as active authority;
- no TBP or issue-template dependency is introduced;
- the later implementation agent does not need to reconstruct planning context.
