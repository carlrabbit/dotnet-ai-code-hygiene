# ADR-0026 — Planning seeds lossless execution-ledger coverage

> **1.0.0-rc.1 status:** Historical decision; its mandatory ordinary-milestone requirements are superseded by ADR-0030 and ADR-0031. See current normative milestone and validation models.


## Status

Accepted for guide-system 0.9.0.

## Context

The 0.7–0.8 execution model made the implementation agent responsible for creating the persistent execution ledger and mapping milestone obligations into work packages.

In practice, an executor can create a structurally valid ledger while accidentally compressing many independently verifiable acceptance criteria into a smaller number of broad work-package summaries. Once that happens, criterion-specific validation gaps can disappear behind aggregate claims such as a passing integration scenario or a completed work package.

Planning already owns the milestone's acceptance criteria, material completion obligations, validation policy, and ready boundary. Requiring implementation to reconstruct that obligation set adds a lossy translation step without adding useful implementation autonomy.

At the same time, planning should not return to pre-authoring concrete implementation decomposition, file edits, tasks, or evidence.

## Decision

For AI-executed milestones:

1. planning assigns a stable ID to every independently verifiable acceptance criterion and every other material completion obligation;
2. planning initializes `.execution/<milestone-id>.md` before the milestone becomes `ready`;
3. the initial ledger contains one separate pending row per milestone obligation plus the required validation gates;
4. planning owns the seeded obligation identity and wording;
5. implementation owns work-package decomposition, concrete implementation mapping, evidence, status, and resume state;
6. several obligations may map to one work package, but implementation must not merge, delete, renumber, paraphrase, or replace planner-seeded obligation rows merely to simplify tracking;
7. each validation gate identifies the specific obligation IDs it is intended to prove;
8. final reconciliation verifies exact set equality between applicable milestone obligation IDs and ledger obligation IDs, then verifies criterion-specific implementation and validation evidence.

The governing rule is:

```text
the execution ledger may compress work
the execution ledger must not compress obligations
```

The ledger remains non-authoritative operational state. The milestone and referenced project authority remain the implementation contract.

No generic ledger-initialization command is required. Planning already owns the obligation set and can project it directly into the ledger without a parser or generator.

## 0.9 evidence-topology refinement

`ADR-0027-preserve-acceptance-evidence-topology.md` extends this decision where one obligation contains materially distinct proof paths.

ADR-0026 remains authoritative for lossless obligation identity and planning-seeded ledger ownership. ADR-0027 adds selective `EC-*` evidence cases and durable post-completion evidence traceability.

## Consequences

Planning packages for AI-executed milestones contain both the ready milestone and its seeded execution ledger.

Implementation begins from the seeded registry rather than recreating obligation coverage from scratch.

Work-package design remains implementation-owned and may still group many obligations when that is the most coherent execution shape.

Aggregate validation success is insufficient evidence for an obligation unless the executed scenario actually exercises the behavior required by that obligation.

Legacy or externally prepared milestones without seeded ledgers may still be executed by constructing a lossless ledger from the milestone before production work, but 0.9 planning must seed it before `ready`.

Completed historical milestones do not require retroactive ledger rewriting unless they are reopened.
