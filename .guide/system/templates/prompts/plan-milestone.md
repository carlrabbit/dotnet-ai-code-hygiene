# Prompt — Plan Milestone

Convert the requested capability into a ready milestone for a disconnected implementation agent:

`{milestone}`

Use the product repository's pinned `.guide/system/` as **planning** methodology (or this repository's root when planning guide maintenance). Consult `.guide/system/README.md` for task routing and selectively load applicable models and shared profiles. Do not use drifting upstream `main`, copy guides into project docs, or require ordinary implementers to load the snapshot.

## Objective: agent-scaled, coherent delivery

Plan the **largest coherent capability economical to implement and evaluate**, not the smallest human-sized vertical slice. A milestone may implement an entire subsystem, cross-cutting feature, or structural migration. Do not split merely because many edits or tests are involved.

Explicitly distinguish **reversible implementation uncertainty** (cheap to experiment, discard, and retry) from **consequential risks** (irreversible data, security, public/persisted compatibility, operational impact). Prefer enabling experimentation to preemptively shrinking reversible work. Split or stage only where independent outcomes, meaningful boundaries, unresolved material decisions, or expensive failure consequences justify it.

Invest as much planning investigation and decision work as material consequences require. **Do not optimize for minimal planner effort, tokens, or document length.** The objective is to prevent implementer ambiguity, lost requirements, and costly correction cycles, while keeping implementation and verification efficient. Avoid process artifacts that do not improve the resulting contract.

## Planning authority and repository inspection

Inspect enough of the target repository to understand applicable project truth:
- `AGENTS.md`, `README.md`, relevant specs, engineering rules, architecture, decisions, milestones, and representative source/tests;
- `.guide-profile.json` for applicable shared profiles, semantic scopes, execution metadata, and repository role;
- `docs/PROJECT-GUIDANCE.md` and registered `.guide/profiles/<id>/PROFILE.md` for project-local planning extensions;
- focused `docs/research/` only when relevant to material open questions;
- review or sync metadata only if relevant to this assignment.

Resolve applicable base/shared/local profiles and conflicts through concrete project authority. Do not invent a new central profile to express one SDK, runtime choice, or test strategy. Research is planning evidence, never active implementation authority. Promote durable, implementation-affecting conclusions to normal project truth.

Planning owns target capability, meaningful architectural/semantic/compatibility decisions, invariants, scope, acceptance, exceptional validation/review, and authorization for any non-routine environment changes. For changes that may mutate non-disposable data or shared/external systems, explicitly distinguish permission to implement/test on disposable targets from permission to execute against the actual named target; declare authorized actions and required recovery/isolation precautions where relevant. Planning may intentionally delegate selection among **reversible internal architectures** when fixed product invariants, evaluation criteria, and rollback bounds make implementation trials cheaper than speculative upfront analysis. Do not make detailed file lists or patch steps a prerequisite for `ready`.

## Change-impact analysis — planning responsibility

For substantive milestones, investigate the affected implementation and product context rigorously enough to identify and resolve:

- **Intended changes:** the requested behavior and explicit breaking changes.
- **Permitted secondary consequences:** what may regress, become temporarily invalid, or require later consumer repair as an intentional trade-off; never treat that permission as general authorization to change unrelated behavior.
- **Protected invariants:** consequential behavior and contracts that must survive, including relevant accessibility/security, data, source/artifact, persisted/public interfaces, and existing exclusions.
- **Affected interactions:** neighboring and ineligible cases, subsystem integrations, and related execution modes that must remain consistent.
- **Plausible high-impact mistakes:** defects that a positive example, aggregate green suite, or successful compilation might not detect.

Inspect relevant implementation, tests, contracts, and research; investigate competing interpretations rather than silently choosing the simplest. Resolve consequential ambiguity before `ready`, including through deep planning research where useful. Deliberately delegate only **implementation mechanics or bounded reversible choices**, not unsettled product semantics. Do not impose a blanket semantic-equivalence, compilability, or compatibility requirement where the product explicitly permits otherwise.

Record the material conclusions in the existing architecture/invariants, scope, acceptance, validation, or referenced project-authority sections. Detailed analysis is welcome; it need not become a separate document, checklist, or exhaustive matrix. A complete handoff and complete ledger obligations matter more than brevity.

## Ready milestone contract

Produce a single primary milestone following `templates/milestones/milestone-template.md` with:

1. **Goal / target:** the observable capability and why it belongs together.
2. **Architecture and invariants:** settled material decisions, permitted consequences, protected contracts, and affected interactions.
3. **Scope / exclusions:** inclusion and non-goals without a speculative edit allowlist.
4. **Acceptance:** independently verifiable outcomes covering the entire intended capability; do not compress material obligations for brevity.
5. **Validation:** high-information focused checks derived from plausible failure modes, including contrasting boundaries and cross-mode invariants where material; identify consequence risks and checks deferred to release.
6. **Required authority and exceptional obligations:** only where they matter, including direct docs, local specializations, permissions, or required human judgment.

**Seed a lightweight obligation ledger by default for substantive implementation milestones**, even if the agent expects to finish in one session. Give each independently deliverable material acceptance obligation a stable ID, including direct documentation/migration work, and seed one pending row with that same ID and meaning in `.execution/<milestone-id>.md` **before** `ready`. Do not compress distinct requirements into one vague row. A genuinely trivial indivisible task may omit the ledger with a brief rationale. Do not require test/evidence cases per criterion, receipts, gate matrices, or proof registries: obligation completeness is not exhaustive validation.

Make the contract complete for the project's baseline executor without relying on planning chat history or reading the guide. Do not reduce a coherent milestone merely because implementing it may be lengthy; the implementer can decompose and resume work internally.

If a **material project decision** really cannot be made without evidence, research it or plan a focused diagnostic investigation. A bounded, reversible internal architecture experiment with agreed comparison criteria is an implementer choice, not automatically a planning blocker. Do not treat cheap alternative implementation trials as an unresolved planning decision.

## Validation policy

Use `meta/VALIDATION-MODEL.md`. **Design** checks during planning using the plausible wrong behaviors from change-impact analysis; a passing happy path alone may not distinguish correct from incorrect behavior. Select a small number of high-information checks, such as adjacent ineligible inputs or cross-mode consistency where material, without creating a default case matrix.
- During implementation: affected build, static feedback, targeted tests.
- At milestone completion: focused behavioral checks and a few representative integration fixtures; review architecture and protected invariants.
- At release: final package/installed tool/real consumer, compatibility, broader operational and platform qualification where required.

A real integration boundary may matter at milestone stage even when installed-product qualification does not. State exceptional earlier consumer tests only where packaging/installation is itself the milestone outcome, project authority mandates it, or the risk of deferring is material.

Do not make a general test matrix from every acceptance clause. Reuse relevant current evidence; do not add fingerprinting/sharding infrastructure unless repeated validation cost justifies it. Use the target/locus/platform detail necessary for meaningful real-boundary checks; integration need not run in CI. Authorized execution is not permission for new machine software or CI infrastructure.

For reviews/corrections, prefer concrete defects and minimal focused rechecks. Never pre-author a policy that every correction must grow the validation contract.

## Project documentation and transport

Update engineering/specification/decision authority only when required to fix project truth. Add `.guide-sync/pending/` hints only for genuinely deferred documentation changes. Do not duplicate the implementation prompt in the package. Project-local planning research is optional and non-authoritative.

Follow `meta/HANDOFF-TRANSPORT-MODEL.md`. Prefer an authorized dedicated Git planning branch when practical; otherwise deliver a repository-relative ZIP overlay. A Git handoff identifies the **exact accepted planning-baseline commit SHA**, not just the branch/PR, and whether implementation continues there or branches from that commit. A ZIP identifies its target repository/base revision when known and any move/deletion instructions. Planning commits, PRs, merges, and implementation are separate actions; transport is not a second contract.

Provide the primary milestone path, affected authority documents, actual Git branch/commit or ZIP instructions, key validation expectations, deferred release checks, required human-review obligations if any, and a concise handoff using `templates/prompts/execute-milestone.md`. Do not promise a file artifact or PR that has not been created.

## Readiness check

Before marking `ready`, verify:
- material architectural, semantic, compatibility, security, and scope choices are resolved;
- affected behavior/contracts have been investigated, with intended changes, tolerated secondary consequences, protected invariants, and interactions made unambiguous where material;
- plausible consequential failures that a happy path or build might miss have been considered when selecting discriminating checks;
- the outcome is coherent and appropriately ambitious for agent execution;
- acceptance is observable and covers the actual requested capability;
- validation is proportionate and release qualification is properly separated;
- any required external target/authority is real and authorized;
- an implementer can act independently using the ready milestone and project truth;
- every material deliverable obligation has survived into the seeded ledger when applicable, without creating validation-host, receipt, test-mapping, or human-review ceremony.

Do not confuse implementation uncertainty with missing project policy. Return genuinely material unresolved decisions to planning.
