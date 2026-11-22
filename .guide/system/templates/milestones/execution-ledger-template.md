# Execution Obligation Ledger — <Milestone ID>

> Planner-seeded operational completeness inventory for a substantive milestone. The ready milestone is authoritative. Not an evidence registry or validation matrix. For a genuinely trivial indivisible task, the planner may explicitly omit this file.

**Milestone:** <repository-relative milestone path>
**Updated:** <date/commit or session>

## Planned obligations

| ID | Material outcome (same meaning as milestone) | Status | Brief result / blocker |
|---|---|---|---|
| AC-01 | <First independently deliverable requirement> | pending | |
| AC-02 | <Second independently deliverable requirement> | pending | |
| DOC-01 | <Required documentation or migration deliverable, if any> | pending | |

Planning seeds **all** material obligations before `ready`; IDs correspond to the milestone and cannot be compressed, deleted, or renumbered by implementation. Status: `pending`, `in progress`, `done`, `blocked`. Omit example rows that do not apply; do not invent rows for individual tests.

## Implementation progress / resume context (when useful)

- <Next implementation step, significant discovery, or pointer to live code>

## Completion accounting

Before `COMPLETE`, check that the set of applicable milestone obligation IDs equals the seeded ledger IDs and every entry has a truthful disposition against the delivered result. Resolve scope changes with planning. This is **not** a requirement to attach a test, evidence case, validation gate, or fingerprint per obligation. Run proportionate focused checks separately.

Keep this ledger through implementation/review. Afterward retain or archive per project policy; place a short durable completion summary in the milestone.
