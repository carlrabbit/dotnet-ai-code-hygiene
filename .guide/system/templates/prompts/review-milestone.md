# Prompt — Review a Milestone Implementation and Prepare Focused Corrections

Review the implementation or pull request for:

`{milestone-or-pr}`

Use the ready milestone, its referenced project authority, changed code/tests, relevant existing checks, and—for substantive work—the planner-seeded `.execution/<milestone-id>.md` obligation ledger. When an exact accepted planning-baseline commit is supplied, distinguish that handoff from later branch/PR changes and check material contract changes were reconciled rather than silently substituted. This is **engineering review**, not automatically a formal human-acceptance gate. Do not require the implementation agent to read the pinned guide snapshot or external upstream repository.

## Objective

Determine whether the intended **whole coherent capability** is delivered within its authorized change boundaries. Examine intended behavior, expressly permitted consequences, protected invariants, related modes/integrations, architecture, documentation, and plausible defects. A build or green happy-path suite does not establish that protected boundaries survived; look for high-information boundary or cross-mode evidence where material. Do not treat intentionally permitted breakage as a defect, nor require an exhaustive test matrix.

For a substantive milestone, compare the ledger's stable obligation IDs with the ready milestone, and inspect whether **each material deliverable is actually present**. A `done` status without the deliverable is not completion; a missing or compressed obligation is a real handoff defect. This is completeness accounting, not a requirement for one test or proof artifact per row.

Review at agent scale. Do not penalize a broad but coherent change merely because it contains many edits or would be large for a human team.

## Findings

For each actionable finding, give:
- the concrete defect and relevant file/location;
- its observable consequence or contract violation and severity;
- the **smallest credible focused verification** of its correction.

Distinguish actual defects from stylistic suggestions, speculative risks, and release-only qualification. Prioritize by consequence and likelihood, not number of missing evidence cases. Do not turn every acceptance clause into a new test, validation gate, or proof row.

## Corrections without process inflation

If asked for a correction prompt, direct the implementer to fix **all substantiated findings** within the existing milestone contract, using focused tests and small representative integration fixtures. Do not create new acceptance IDs, evidence-case registries, fingerprinted receipts, sharding systems, broad test matrices, or installed-consumer checks merely because a review found a defect.

Add a lasting regression test when a reproducible bug can plausibly recur and a small deterministic check is economical. Otherwise a disposable focused check and review may be adequate. If inherited validation obligations are excessive, recommend deleting or reducing them; sunk effort is not a reason to preserve them.

Expand the milestone contract or require stronger validation **only** for a newly demonstrated material product risk, an explicit product requirement, or a truly changed public/integration boundary. Explain that justification. Package, installed-consumer, real-repository dogfood, and broad compatibility checks normally remain at release readiness.

## Outcome

Report **Accept**, **Corrections required**, or **Blocked on material decision**. Cite exact evidence from code and checks for findings; acknowledge unverified areas without manufacturing defects. When corrections are required, provide a concise actionable correction prompt for the implementer and the specific focused rechecks. Do not require an independent approval subsystem unless the milestone explicitly needs human judgment.
