# Terminology

**Finding** — deterministic rule occurrence.

**Review candidate** — deterministic finding requiring contextual remediation judgment.

**Semantic review rule** — a rule whose primary output is a bounded `ReviewBatch`, not deterministic findings.

**ReviewBatch** — run-scoped semantic-review workload containing population size, deterministic sample, fixed questions, reviewer class, and escalation guidance.

**Review item** — one subject in a batch, identified by path/location/symbol and the artifact to judge.

**Sample review** — bounded normal batch reviewed by the current implementation agent.

**Expanded review** — full eligible population returned after explicit escalation; M0003 requires reviewer class `frontier`.

**Escalation** — caller-controlled transition from sample batch to expanded batch. The CLI does not decide sample pass/fail or invoke a model.

**Review batch handle** — run-scoped handle such as `R-8K3M/B-1`; valid only for latest local run state.

M0003 intentionally has no semantic-review history, TTL, schedule, acceptance record, or per-item persistence.
