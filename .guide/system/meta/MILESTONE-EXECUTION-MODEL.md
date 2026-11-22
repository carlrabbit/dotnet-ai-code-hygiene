# Milestone Execution Model

## Status

Authoritative for milestone sizing, readiness, implementation ownership, execution outcomes, and completion. See [Validation Model](VALIDATION-MODEL.md) for assurance boundaries. This model replaces ADR-0019/ADR-0026/ADR-0027 proof machinery while preserving the lossless planner-seeded obligation inventory, as specified by ADR-0033.

## Purpose and economics

Plan **coherent capabilities at agent scale**, not human-sized increments. Invest substantial planning effort when needed to resolve material decisions and prevent implementer ambiguity, omitted constraints, and correction cycles. Optimize the **total outcome and downstream execution cost**, not planner time, token count, or document length independently. Economical implementation and validation do not mean shallow planning. The cost of discarding a reversible branch is often lower than the cost of repeated incremental implementation and exhaustive proof machinery.

The default planning question is: *What is the largest coherent capability we can implement, evaluate, and, if necessary, discard at acceptable cost?*

- A milestone may span substantial refactoring, cross-component implementation, or a complete subsystem.
- Do not split work simply because it involves many files, requires hours of agent execution, or would be large for a human developer.
- Split when the outcomes are independent, the capability becomes semantically incoherent, a material decision requires separate investigation, or failure consequences make a smaller boundary economically preferable.
- Scope remains bounded by product intent and authority, not by a prescribed number of edits, tests, or work packages.
- Evaluate implementation risk separately from consequence risk. Reversible attempts may be ambitious; irreversible data, security, public-contract, or operational consequences require stronger controls.

Experimentation, disposable prototypes, and competing candidate implementations are allowed within the agreed capability and environment authority. The executor may discard a local approach and try another without a new planning cycle when the required outcome and invariants are unchanged.

**Destructive execution authority:** Authorization to implement a destructive capability is not authorization to execute its effects on non-disposable data, shared resources, or external systems. Access to a connection string, credentials, or executable commands does not itself grant target-specific execution permission. Mutating those targets requires explicit authorization for the target and action, with isolation, backup/recovery, or rollback precautions proportionate to the consequences. Without that authority, use authorized disposable environments and report the blocked external operation; do not block otherwise safe implementation work.

## Lifecycle and readiness

Coding milestones follow:

`draft/planning -> ready -> implementing -> done`

Planning owns decisions that materially determine architecture, semantics, public or persisted compatibility, goal, non-goals, invariants, acceptance, and exceptional validation/review constraints. For substantive work it must **investigate the change impact and resolve material change boundaries before `ready`**, including important contracts already present in the repository; broad scope or authorized breaking changes do not remove that obligation. However, planning may deliberately leave a **reversible internal architectural choice** to implementation when the externally observable contract, invariants, comparison criteria, and fallback/disposal boundary are explicit. This is an implementation experiment, not permission to invent project policy. Project-local guidance and profiles apply during planning; conclusions needed by the executor must be reflected in the milestone or referenced project authority.

A milestone is **ready** when the project's normal baseline executor can pursue the intended outcome from the ready handoff and live repository without inventing a new material project decision. Readiness does **not** require predicting file edits, concrete work packages, comprehensive proof topology, or an exact execution plan. Large implementation volume is not a readiness defect. The delivery identifies the repository state containing the ready contract via an exact Git planning commit or an applied ZIP overlay; see [Planning Handoff Transport Model](HANDOFF-TRANSPORT-MODEL.md).

If a material planning decision requires evidence, perform the source inspection, research, comparison, or focused diagnostic work needed to resolve it, regardless of whether that investigation is longer than the expected implementation. Use a diagnostic milestone where an independent investigation is genuinely necessary; do not use one merely to postpone a cheap, reversible internal implementation choice.

No generic model-specific capability tier is prescribed. Project configuration may identify a baseline implementation model, but the milestone must be self-contained for the chosen executor without depending on that agent's private planning conversation. The metadata value `ai-executed` identifies **who implements**; milestone size and human acceptance are independent properties, not alternative execution modes.

## Change-impact investigation and readiness

For a **substantive code or system change**, planning must investigate the materially affected existing behavior and contracts before deciding the milestone is ready. Make the following distinctions explicit in the milestone or its referenced operative project authority wherever they affect correct implementation:

- **Intended changes:** capabilities, semantics, interfaces, or behavior that must become different.
- **Permitted consequences:** tolerated secondary changes, accepted incompatibilities, or intentionally temporary limitations (for example, a change that may require subsequent consumer repair). Permission for one consequence is not permission for unrelated breakage.
- **Protected invariants:** existing behavior and properties that must remain intact, such as relevant data integrity, security/access boundaries, public or persisted contracts, and source/artifact integrity.
- **Affected interactions:** neighboring behavior, similar-but-ineligible inputs, related operating modes (such as preview/check versus mutation), and integration surfaces that could disagree or regress.
- **Plausible material failures:** incorrect interpretations or defects that ordinary successful examples or a passing build could miss; identify which deserve discriminating checks.

This is an **investigation and decision obligation**, not an instruction to minimize planning work or to write a universal inventory of unrelated system properties. Inspect code, tests, specifications, documentation, and other evidence as deeply as needed. Resolve consequential ambiguities in the ready contract rather than delegating product semantics to the implementer. Do not turn these dimensions into mandatory extra milestone sections, per-invariant ledger rows, or exhaustive test matrices. The implementation agent remains free to choose mechanisms and reversible approaches within the resolved boundaries. Not all changes require semantic equivalence or compilable intermediate artifacts: the explicitly authorized contract determines what is protected.

## The ready contract

Use [the milestone template](../templates/milestones/milestone-template.md). A ready milestone ordinarily needs:

1. **Goal and target capability:** observable end state.
2. **Architecture and invariants:** project-level decisions, intended behavioral changes, permitted consequences, affected interactions, and protected contracts established by change-impact analysis.
3. **Scope and exclusions:** meaningful limits; not an exhaustive file allowlist.
4. **Acceptance:** independently deliverable, verifiable outcomes; not a test-case inventory or artificially compressed summary.
5. **Validation:** discriminating focused checks for plausible consequential defects and boundary violations; explicitly identify any exceptional milestone-stage qualification.

Reference only project authority needed to implement the change. Include documentation impact, migration, or required human judgment where material. The acceptance contract must preserve all independently deliverable, material obligations; do not collapse distinct outcomes merely to shorten the document. For a substantive milestone, assign stable IDs to those obligations (including required documentation or migration deliverables) and seed the same IDs in a lightweight execution ledger before `ready`. A genuinely trivial, indivisible task may omit a ledger with a short rationale. No evidence-case registry, test matrix, or one-to-one acceptance-to-test mapping is implied.

**Completeness and correctness are separate concerns.** The obligation inventory prevents the executor from losing planned work; focused validation establishes credible behavior. One test or architectural inspection may support multiple outcomes. A passing test suite alone never excuses ignoring an obligation it does not cover.

## Planning and implementation separation

Planning establishes the *what* and any material product constraints; implementation determines the *how*. Do not force the planner to predict mechanics available from inspecting live code.

The implementer may:
- choose classes, files, APIs internal to the subsystem, algorithms, tests, and refactoring sequence;
- decompose a large coherent milestone internally and change that decomposition as evidence emerges;
- run disposable experiments and compare alternative approaches;
- repair defects, including those found in review, using focused verification;
- add supporting edits needed to fulfill the agreed outcome.

The implementer must not:
- silently change public/persisted compatibility, required semantics, security constraints, or **settled** major architecture decisions;
- expand the product into unrelated capabilities;
- treat a missing toolchain as permission to install persistent software or modify CI/shared infrastructure;
- execute destructive or consequential mutations against non-disposable/shared/external state without explicit target and execution authorization;
- treat a failing check as permission to weaken an agreed product invariant.

If live repository discoveries invalidate a material decision, resolve that decision with planning. An unexpected local implementation detail does not automatically require replanning.

Ordinary implementers read the milestone, referenced project truth, and relevant code/tests. They do not need the guide repository, planning research corpus, or planning conversation to reconstruct the contract.

## Planner-seeded obligation ledger and execution progress

For every **substantive implementation milestone**, planning seeds a compact `.execution/<milestone-id>.md` ledger **before** declaring `ready`. Its purpose is lossless transfer of material work obligations, not proof collection. This default does not depend on expected elapsed time or whether the implementer is likely to be interrupted. Omission is appropriate only for a genuinely trivial, indivisible task where a second inventory provides no benefit; record that decision briefly in the milestone.

- Seed one pending entry per independently deliverable material milestone obligation, retaining the same stable ID and meaning as the milestone. Include direct documentation, migration, and setup deliverables where they are part of acceptance; do not generate rows for speculative implementation steps or each possible test.
- Planning owns the inventory. The implementer may add its own work tasks, progress details, and new discovered implementation steps, but may not silently merge, renumber, delete, reword away, or mark an unfulfilled planner-seeded obligation not applicable. Material scope changes return to planning.
- During implementation, keep seeded entries current (pending / in progress / done / blocked) and use the ledger for optional resume information. It is operational state subordinate to the ready milestone, not an independent source of policy.
- Before `COMPLETE`, reconcile the set of applicable milestone obligation IDs with the seeded ledger IDs and account for each entry against the delivered result. An entry is `done` only when the actual outcome is present; a green aggregate test or assertion of completion alone is insufficient. Unfinished entries require correction or a genuine blocker, never silent omission.

Use [the compact ledger template](../templates/milestones/execution-ledger-template.md). Do **not** mandate evidence-case IDs, per-obligation proof links, validation gate IDs, work-package mappings, receipt fingerprints, or immutable acceptance-to-proof registries. This is a lightweight completeness check, not a release or testing gate. Keep the ledger through implementation and review; afterward retain or archive it according to project policy, with a short durable milestone completion note.

## Completion and review

Completion establishes that the delivered capability meets the milestone's acceptance contract with credible evidence, not that its distributable product has completed release qualification.

At completion:
- reconcile planned obligation IDs to the ledger, ensure every applicable deliverable is actually complete, and inspect the resulting behavior against every substantive outcome and invariant;
- run focused automated checks and a small number of representative integrations appropriate to actual risks;
- identify any known gaps and release-only checks that have been deferred;
- update direct documentation or migration material explicitly in scope;
- write a short **durable completion note** in the milestone or a project-defined completion record, then report the implementation, validation performed and results, and residual risks. This note is not a criterion-to-proof registry.

Do not create a separate validation case for each acceptance clause merely to produce a complete table. Existing current test results may be reused when still relevant. A brief review of architecture and behavior is usually sufficient; mandatory independent human sign-off requires an explicit reason.

Release preparation owns final package/installation/consumer, compatibility, cross-platform, and operational qualification of the integrated release candidate unless the milestone itself is about those boundaries or a material risk justifies earlier qualification.

### Correction discipline

Review findings must describe concrete defects, why they matter, and a **smallest credible focused verification** of the repair. A correction prompt must not automatically grow acceptance criteria, evidence cases, validation stages, test infrastructure, or broad regression requirements. If a prior validation contract is disproportionate, simplify it instead of carrying accumulated gates forward. Expand the contract only for a newly demonstrated material risk or an explicitly requested product change. Add a small lasting regression check for a recurring, deterministic defect when worthwhile; otherwise a focused disposable verification may suffice. Use [the canonical review/correction prompt](../templates/prompts/review-milestone.md) when a separate reviewer prepares correction instructions.

### Terminal execution outcomes

- **COMPLETE** — acceptance is credibly met, proportionate milestone validation succeeded, and required documentation/review is satisfied.
- **AWAITING HUMAN REVIEW** — remaining work is only a specifically required human decision.
- **BLOCKED** — an external dependency or genuinely material unresolved planning decision prevents completion. Fixable implementation failures are not blockers.

These describe the execution run; they do not add lifecycle states. An agent must not declare completion solely because it built successfully, nor continue adding infrastructure after the milestone is adequately established.

## Workflow types and review

Normal feature, migration, diagnostic, documentation synchronization, and release-readiness work can use dedicated planning prompts where their delivery boundary differs. Special prompts use the lightweight planner-seeded obligation ledger for substantive milestones but must not reinstate evidence-case registries, per-criterion testing gates, or other proof machinery.

Milestone-scoped human review is optional unless required by the project or milestone. Approved reviews are historical decisions, not rolling approval of future changes. See [Human Review Model](HUMAN-REVIEW-MODEL.md).

Validation **depth** (focused, integration, release), **target**, and **locus** (local, CI, remote) remain independent. See [Validation Model](VALIDATION-MODEL.md).
