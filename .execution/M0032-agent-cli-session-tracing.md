# Execution Obligation Ledger — M0032

> Planner-seeded completeness inventory for the **amended event-only** M0032 contract. The milestone is authoritative. This is not an evidence registry, validation matrix, or a record of the superseded implementation's completion.

**Milestone:** `docs/milestones/M0032-agent-cli-session-tracing.md`  
**Planning base:** `main` `2cea53e3184026c4ff96f4ce5dd3351bbae3325b`  
**Status:** pending under amended milestone; implementation PR #45 requires reconciliation with the new planning commit

## Planned obligations

| ID | Material outcome (same meaning as amended milestone) | Status | Brief result / blocker |
|---|---|---|---|
| CLI-01 | Independent recording-only CLI, noninteractive commands, truthful diagnostics and structured output, no subprocess execution or tool integrations | pending | |
| SES-01 | Caller-selected session key and optional actor; lifecycle as append-only start/pause/resume/stop markers without registration, state gating or generated IDs | pending | |
| RUN-01 | Exact command-text begin/end events; content-hash-plus-label matching, reported result, concurrent identical-command FIFO/ambiguity, no generated run key | pending | |
| DECL-01 | Optional agent-declared external/MCP start/end and timestamped marks with the same caller-keyed matching and truthful provenance | pending | |
| CFG-01 | Optional root .agenttrace.json, default JSONL, configured SQLite/JSONL, explicit absolute external paths and relative paths, valid diagnostics and ignored defaults | pending | |
| STORE-01 | Equivalent JSONL and SQLite WAL append-only event model, safe concurrent CLI writes to a shared session, duplicate/unmatched event preservation | pending | |
| TIME-01 | Honest cross-invocation UTC duration estimates, FIFO overlap/ambiguity, per-actor pause annotations and qualified unattributed gaps without model-thinking claims | pending | |
| REPORT-01 | Human text/Markdown chronological reporting of session/actor data, paired commands, repeated executions, sensitive labels, anomalies and timing coverage | pending | |
| EXPORT-01 | Complete versioned agent-oriented JSON/JSONL raw event export with clearly distinct derived fields, clean stdout and JSON session discovery | pending | |
| SAFE-01 | Unkeyed SHA-256 sensitive correlation retaining safe command name but no secret-bearing parameters, no stdout/stderr/env capture, safe explicit storage writes, unchanged other tools | pending | |
| TEST-01 | Focused unit and real CLI-process tests verifying no command execution, shared-session concurrency, duplicate FIFO ambiguity, pause semantics, storage parity and privacy | pending | |
| DOC-01 | Current README, architecture, engineering/help, milestone index and completion note reflect event-only design and documented timing/privacy limitations | pending | |

## Amended planning decision

All **twelve obligation IDs are preserved**. Their meanings were explicitly updated because the original planning contract incorrectly required `agenttrace run` to supervise subprocesses, generated IDs, session state gating and confined SQLite paths. The previous completion ledger inside PR #45 describes **the superseded contract**, not completed obligations under this amendment. The implementer owns reconciling that branch with this updated ready milestone and may retain correct infrastructure/tests while removing obsolete behavior.

No independent ADR exists or is needed for this new tool: the amended milestone is the operative product design decision; repository architecture documentation is updated during implementation.

## Implementation progress / resume context (when useful)

No work against this **amended** contract is accepted yet. Reuse existing implementation PR #45 as the correction workstream, refresh from the exact new planning commit, then update this ledger from actual outcomes. Do not add evidence registries or validation gates.

## Completion accounting

Before `COMPLETE`, reconcile exactly these twelve IDs against the revised milestone and delivered source, not against the original implementation's old completion report. Record a short milestone completion note, focused tests actually run, and release-only deferrals.
