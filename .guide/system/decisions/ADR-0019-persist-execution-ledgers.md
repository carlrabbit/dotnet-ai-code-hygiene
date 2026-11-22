# 0019 — Persist Execution Coverage and Progress

> **1.0.0-rc.1 status:** Historical decision; its mandatory ordinary-milestone requirements are superseded by ADR-0030 and ADR-0031. See current normative milestone and validation models.


## Status

Accepted in guide system v0.7.3.

Amended by `ADR-0026-planning-seeds-lossless-execution-ledger.md` in guide system v0.9.0. ADR-0026 changes ledger initialization ownership and lossless obligation requirements while retaining this ADR's persistent operational-state purpose.

## Context

A ready milestone can be semantically complete yet still require a long implementation trajectory. During long agent runs, repository inspection, edits, test output, validation receipts, retries, and context compaction can make the executor's conversational working state unreliable.

The v0.7.1 completion audit corrected the stopping condition, but a purely retrospective audit still requires the executor to reconstruct requirement coverage after substantial work has already occurred.

A milestone should remain one coherent semantic contract when appropriate. Splitting every large milestone into smaller milestones would fragment project semantics and reintroduce planning overhead. The missing mechanism is implementation-owned persistent execution state.

## Decision

For AI-executed coding milestones, the canonical executor creates or reconciles a repository-local execution ledger before production edits:

```text
.execution/<milestone-id>.md
```

The ledger is operational implementation state. It is not project authority and must not amend, reinterpret, or replace the ready milestone or its referenced authority.

Before implementation, the executor performs execution decomposition and maps all applicable milestone obligations into bounded work packages. The ledger records at least:

- the primary milestone path;
- bounded work packages;
- milestone acceptance criteria and completion obligations covered by each work package;
- status for each work package or obligation;
- required validation gates;
- concrete evidence as work completes;
- a concise resume point when work remains.

A small milestone may use one work package. The executor does not skip the ledger merely because the work appears simple.

Execution decomposition is not a new planning phase. It may decide local implementation order, files, types, tests, refactorings, and other mechanics already owned by implementation. It must not reopen architectural, semantic, compatibility, scope, acceptance, validation, or human-review decisions settled by planning.

The executor updates the ledger after each coherent work package and its relevant validation. On resume or after context compaction, it rereads the ready milestone and the ledger before continuing.

Before `COMPLETE`, the executor must freshly reread the primary milestone from disk and reconcile:

```text
milestone obligations
<-> execution ledger
<-> live repository and concrete evidence
```

A checked ledger row is not proof by itself. If the repository state or evidence does not establish the mapped obligation, the item is still incomplete.

The ledger is required while the milestone is active. After milestone completion, retention is repository policy; removing an operational ledger after completion does not invalidate independently established milestone evidence.

## 0.9 amendment

The original v0.7.3 decision assigned ledger creation to implementation. Guide-system 0.9 keeps execution decomposition and mutable progress/evidence state implementation-owned, but moves initialization of the lossless milestone-obligation registry to planning.

For current behavior, ADR-0026 governs where it differs from the original decision below:

- planning seeds one pending row per independently verifiable milestone obligation and the required validation gates before `ready`;
- implementation preserves those rows and maps them to work packages/evidence;
- work may be grouped, but obligations may not be compressed;
- final reconciliation verifies exact obligation-ID set equality and criterion-specific evidence.

## Consequences

- implementation progress survives context compaction, session interruption, and model attention drift;
- completion auditing becomes coverage reconciliation rather than retrospective memory reconstruction;
- large coherent milestones can remain semantically unified while implementation proceeds through bounded work packages;
- human reviewers can inspect current execution coverage directly when useful;
- `.execution/` must never become an alternate authority layer;
- no generic engineering commands are required for ledger management;
- planning must shape large milestones so their obligations can be mapped to bounded implementation work packages, but planning does not pre-author the executor's task list.
