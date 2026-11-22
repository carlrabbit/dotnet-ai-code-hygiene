# ADR-0030 — Plan Coherent Agent-Scaled Capabilities

**Status:** Accepted for 1.0.0-rc.1; the optional-only ledger decision is superseded for stable 1.0 by ADR-0033. Coherent agent-scale scope and reversible experiments remain operative.
**Supersedes in ordinary milestone planning:** human-sized incremental slicing and mandatory lossless execution-ledger scaffolding in ADR-0019 and ADR-0026; amends the applicability of ADR-0020/0021.

## Context

The guide permits broad AI execution in principle but its default workflow required small bounded planning packages, exhaustive tracking, and proof mechanics that made coherent feature development unnecessarily expensive. Implementation agents can iterate, replace, and discard local approaches at substantially lower cost than traditional human development.

## Decision

Plan the **largest coherent capability economical to implement, evaluate, and possibly discard**. Do not require multiple milestones because the change is large for a human, touches many files, or has multiple internal steps.

Planning resolves product-level architecture, semantics, compatibility, invariants, acceptance, and execution authority. It may intentionally delegate a **bounded, reversible internal architectural choice** to the executor when invariant requirements, evaluation criteria, and the fallback/disposal boundary are settled. Trials between such candidates do not themselves require a diagnostic milestone. It does not prescribe disposable implementation mechanics. The executor owns decomposition, experiments, retries, and implementation replacement within the accepted product contract.

Explicitly distinguish:
- **reversible implementation risk** — cheap experimentation and branch disposal are legitimate;
- **high-consequence risk** — destructive operations, sensitive data, security, external/public contracts, and hard-to-undo changes require stronger precautions.

A ready milestone must specify verifiable outcomes, not a preconstructed task graph or proof map. An optional lightweight progress ledger may help with interrupted execution; it is not part of readiness or standard acceptance.

## Consequences

Milestones may be larger and more ambitious. Rework may occasionally increase per attempt, but planning/validation/review overhead should decrease across completed capabilities. High-consequence work remains appropriately constrained. A new material project decision returns to planning; local technical uncertainty does not.
