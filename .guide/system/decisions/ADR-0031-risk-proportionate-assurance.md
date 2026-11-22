# ADR-0031 — Risk-Proportionate Milestone Assurance and Release Qualification

**Status:** Accepted for 1.0.0 and retained, with the distinction that the small planner-seeded **obligation ledger** is restored by ADR-0033 while evidence-case/test-proof machinery remains optional.
**Supersedes as defaults:** mandatory evidence topology (ADR-0027), compulsory release/consumer checks during ordinary feature milestones, and unconditional application of receipt/sharding requirements from ADR-0014–0018/0029.

## Context

M0026 in dotnet-ai-code-hygiene exposed a validation-cost inversion: a straightforward architectural split accumulated 20 obligations, 25 evidence cases, eight gates, fingerprinted receipts, installed-consumer scenarios, self-host checks, and completion reconciliation. Review corrections amplified validation infrastructure rather than product value.

## Decision

Evaluate confidence at three delivery boundaries:

1. **Development:** economical focused feedback while editing.
2. **Milestone:** affected tests, a few representative integration scenarios, and review of important behavior and invariants.
3. **Release:** final distribution/package/consumer, compatibility, platform and operational qualification required by the product's release policy.

The ordinary milestone requires levels 1–2. Move release qualification forward only when its boundary is the milestone's goal, project authority explicitly requires it, or deferral creates material risk. Real integration (database, runtime, process) can still be the cheapest milestone evidence; it is distinct from installed-distribution qualification.

Acceptance outcomes remain mandatory but a one-to-one map to evidence cases, individual tests, separate gates, or immutable proof records is not. Existing relevant evidence can be reused when valid. A compact planner-seeded **obligation inventory** is required for substantive milestones under ADR-0033, but detailed evidence/proof ledgers, fingerprinted receipts and expensive resumability machinery remain optional cost-saving mechanisms justified by demonstrated need.

**Corrections must not ratchet up infrastructure.** Fix concrete defects and run focused checks. Add obligations or validation stages only for demonstrated material risk. Remove excessive existing machinery when possible.

## Consequences

Milestones complete with credible product confidence rather than release-level exhaustive qualification. Release preparation explicitly owns final-candidate risk. Validation strategy minimizes implementation + validation + review + expected rework, rather than maximizing proof inventory.
