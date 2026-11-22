# ADR-0027 — Preserve acceptance evidence topology and durable completion trace

> **1.0.0-rc.1 status:** Historical decision; its mandatory ordinary-milestone requirements are superseded by ADR-0030 and ADR-0031. See current normative milestone and validation models.


## Status

Accepted for guide-system 0.9.0.

## Context

ADR-0026 prevents implementation from compressing many milestone obligations into a smaller number of execution summaries.

That is necessary but not sufficient.

A single acceptance criterion may itself span materially different behavioral paths. For example, one contract may support scalar and enum dispatch, Root and Batch state scopes, inherited and direct behavior, or source-project and packaged-consumer surfaces. A ledger can preserve the parent criterion perfectly while still accepting evidence from only one of those paths.

A second problem appears after completion. The mutable execution ledger may be removed according to repository policy. If the only obligation-to-evidence mapping lived in that ledger, later reviewers and release-readiness work must reconstruct why the milestone was considered complete from code, tests, and PR prose.

## Decision

Planning may define explicit acceptance evidence cases with stable IDs such as `EC-01a` when one milestone obligation requires independent proof across materially distinct behavioral paths.

An evidence case is required only when the distinction could plausibly hide a different implementation path, state mechanism, compatibility surface, target surface, or contract failure.

Planning must not mechanically generate a Cartesian product of all possible dimensions.

Evidence cases describe what must be proven, not how implementation or tests are written.

For AI-executed milestones:

1. planning seeds required evidence cases into the execution ledger before `ready`;
2. validation gates identify the exact obligation/evidence-case IDs they are intended to prove;
3. implementation chooses concrete test/validation mechanics;
4. implementation must not replace sibling evidence cases with one aggregate validation claim;
5. final reconciliation verifies exact evidence-case set equality where evidence cases exist and concrete evidence for each case.

Before a completed milestone transitions to `done`, implementation preserves a compact durable completion-evidence reconciliation in the milestone itself or another immutable completion record explicitly referenced by the milestone.

The durable completion record maps:

```text
obligation/evidence case
-> concrete evidence
-> validation gate/target
-> result
```

It is historical evidence, not an amendment to the ready contract.

The mutable execution ledger may still be removed according to repository policy after that durable trace exists.

## Consequences

Simple milestones do not need evidence-case IDs when each obligation has one obvious proof surface.

Semantically dense milestones gain explicit acceptance-evidence topology without forcing planning to prescribe concrete tests.

Features declared across heterogeneous existing surfaces must not use one surface as implicit evidence for all materially different surfaces.

Release-readiness work can consume durable milestone completion evidence without requiring retained execution ledgers.

Prior milestone evidence does not replace release-specific validation of the final candidate when project authority requires fresh release evidence.
