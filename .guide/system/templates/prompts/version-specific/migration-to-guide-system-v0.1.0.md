# Prompt — Migrate Repository to Guide System v0.1.0

Use this version-specific prompt only when an exact v0.1.0 target is required.

Prefer `templates/prompts/adopt-guide-system.md` for normal work.

Our planning task is to create an implementation-ready migration milestone package for migrating this repository from the old repository-local setup/engineering guide model to guide system v0.1.0.

The active guide authority is external:

- Guide repository: `carlrabbit/agentic-project-guides`

Do not copy guide documents into the target repository.
Do not make target repository documentation reference guide documents as operational authority.
Use the guide system only for planning, migration, documentation synchronization, and release readiness.

Target repository documentation must contain project truth only.

If the target repository contains old copied setup or engineering guides, treat them as legacy/non-authoritative unless the repository explicitly marks them as active project documentation.

If the target repository contains `.guide-profile.json`, use it as guide-selection metadata. Ordinary implementation agents must not be required to read `.guide-profile.json`.

If the target repository contains `.guide-sync/`, treat it as deferred documentation synchronization metadata. Ordinary implementation agents must not be required to read `.guide-sync/`.

Target the v0.1.0 model:

- external guide repository authority;
- `.guide-profile.json` for traceability;
- no copied guide documents in product repositories;
- project docs contain project truth only;
- provider/consumer distinction;
- maturity and repository-role aware profile selection;
- migrations instead of generic upgrades;
- validation tiers;
- no default TBPs or issue templates.

## Planning/implementation separation

There is no direct synchronization between the planning AI and the implementation AI.

Create an implementation-ready package. The later implementation agent must be able to unpack the ZIP, open the primary milestone document, read only the authority documents explicitly listed in that milestone, implement the focus areas, run the specified validation tiers, and finish without reconstructing planning context or reading the external guide repository.

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
6. documentation-sync hints and the `.guide-sync/pending/` files created.
