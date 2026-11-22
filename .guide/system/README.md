# Agentic Project Guides

Version: 1.0.0

Select the entrypoint for the **assigned task**. In a product repository, this file is vendored unchanged as `.guide/system/README.md`, the router for its pinned shared planning methodology. In the upstream guide repository it lives at the root. Project-specific policy, research, and implementation contracts belong to the consuming repository. This README is a **navigation index**, not operative guidance.

## Task routing

| Assigned task | Start with |
|---|---|
| Plan a product milestone | `templates/prompts/plan-milestone.md` |
| Review an implementation or prepare focused corrections | `templates/prompts/review-milestone.md` |
| Bootstrap a new product repository | `templates/prompts/new-project.md` |
| Implement a ready milestone | **Product repository:** ready milestone, planner-seeded `.execution/<milestone-id>.md` for substantive work, referenced project authority, and relevant code/tests. Use the canonical execution prompt supplied at handoff; ordinary Implementers do **not** load the vendored guide. |
| Adopt the guide in an existing repository | `templates/prompts/adopt-guide-system.md` |
| Update a repository already using the guide | `templates/prompts/update-guide-system.md` |
| Plan an engineering migration | `templates/prompts/special/plan-engineering-migration.md` |
| Plan release readiness | `templates/prompts/special/plan-release-readiness.md` |
| Plan documentation synchronization | `templates/prompts/special/plan-documentation-sync.md` |
| Execute documentation synchronization | `templates/prompts/special/documentation-sync-pass.md` |
| Curate historical planning research | `templates/prompts/special/adopt-research.md` |
| Maintain or evolve this guide system | `meta/META-GUIDE.md` |

## Where rules and history live

- **Current methodology:** the selected canonical prompt and relevant `meta/`, `base/`, and `profiles/` documents within this pinned guide root (`.guide/system/` in a product repository). Load only what the assigned workflow needs.
- **Concrete project authority:** product-repository milestones, specifications, engineering documents, decisions, and applicable project-local guidance resolved during planning.
- **Prompt catalog:** `templates/PROMPTS.md` (additional selection context; not a second execution contract).
- **Version history and migrations:** `CHANGELOG.md` and `migrations/`. Apply the relevant migration chain during explicit snapshot upgrades; do not treat historical release descriptions as current instructions.
- **Installation and pinning:** `meta/LOCAL-GUIDE-SNAPSHOT-MODEL.md`; stable Git tags identify versions, with no package or GitHub Release publishing.
- **Design rationale:** `decisions/` when the assigned work requires it.
