# Prompt — Plan a Documentation Synchronization Milestone

Our planning task is to create a documentation synchronization milestone package: `{milestone}`.

Use the product repository's pinned `.guide/system/` snapshot as shared planning methodology (or the upstream guide's root when maintaining that repository). Project-specific authority and research stay local. Do not edit the pinned guide, duplicate it elsewhere, or make implementers read it. See `meta/LOCAL-GUIDE-SNAPSHOT-MODEL.md` within the guide root.

Target repository documentation must contain project truth only.

If the target repository contains old copied setup or engineering guides, treat them as legacy/non-authoritative unless the repository explicitly marks them as active project documentation.

If the target repository contains `.guide-profile.json`, use it as guide-selection metadata. Ordinary implementation agents must not be required to read `.guide-profile.json`.

If the target repository contains `.guide-sync/`, treat it as deferred documentation synchronization metadata. Ordinary implementation agents must not be required to read `.guide-sync/`.

## Planner-seeded obligation completeness

For a substantive documentation synchronization milestone, seed `.execution/<milestone-id>.md` before `ready` with stable IDs for every material documentation deliverable. The implementer preserves and reconciles the entries. Do not require evidence cases, test-to-criterion mappings, validation-gate tables, or proof receipts.

## Planning/implementation separation

There is no direct synchronization between the planning AI and the implementation AI.

Create an implementation-ready package. The later implementation agent must be able to apply the package, open the primary milestone document, read only the authority documents explicitly listed in that milestone, implement the focus areas, run the specified validation tiers, and finish without reconstructing planning context or reading the external guide repository.

Also generate a concise execution prompt in chat for the later implementation agent.

## Repository inspection

Inspect the target repository before writing files. Read the minimum required target-repository documentation, usually:

- `README.md`;
- `AGENTS.md`;
- `.guide-profile.json` if present;
- `docs/TERMINOLOGY.md`;
- `docs/SPECS.md`;
- relevant `docs/specs/*`;
- `docs/ENGINEERING.md`;
- relevant `docs/engineering/*`;
- relevant existing milestones and decisions.

Treat genuine `docs/research/` artifacts as non-authoritative planning evidence, not as legacy material simply because they are research. Treat copied guide-system documents separately as legacy guide leakage unless the project deliberately assigned them another role. Do not remove current research during documentation cleanup merely because it is non-authoritative.

## Required milestone document

Create exactly one primary milestone document under `docs/milestones/`.

Define the documentation target, project authority to reconcile, scope/exclusions, observable acceptance, and lightweight checks for correctness and links. Add project-specific constraints, deferred sync hints, or human judgment only when material. Do not require evidence-case tracking or release-style validation for ordinary documentation synchronization.

## Deferred documentation synchronization hints

If deferred documentation work must survive handoff, create one or more files under:

```text
.guide-sync/pending/
```

Do not require the implementation agent to read `.guide-sync/`.

## Provider versus consumer distinction

Do not confuse implementing a capability with using that capability.

Capability-provider repositories validate capability implementation. Capability-consumer repositories use capabilities for product validation. Mixed/dogfood repositories may do both, but dogfood scope must be bounded.

## Mode requirements

Workflow type: `documentation-sync` (independent of the project's `defaultImplementationMode`). This milestone may read `.guide-sync/pending/`. Do not include implementation source changes unless documentation metadata cannot be updated safely without them.

## Deliverable

Deliver the permitted planning-owned repository-relative files under `meta/HANDOFF-TRANSPORT-MODEL.md`: an authorized Git planning commit or ZIP overlay. For Git, report the **exact accepted planning commit SHA**, primary milestone path and whether implementation continues on that branch or branches from the commit. For ZIP, preserve paths and state the intended base when known plus application instructions for moves/deletions.

Do not include implementation source files, generated code, broad unrelated documentation cleanup, TBPs, issue templates, or unmanaged copied guide documents outside `.guide/system/`.

After preparing the package, respond with:

1. branch\/PR link or download link, as applicable;
2. included file list;
3. reason each file is included;
4. confirmed or inferred profile, role, maturity, and execution mode;
5. a filled execution prompt for the later implementation agent;
6. documentation-sync hints and the `.guide-sync/pending/` files created.
