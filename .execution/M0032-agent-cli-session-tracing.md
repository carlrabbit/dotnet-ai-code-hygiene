# Execution Obligation Ledger — M0032

> Planner-seeded completeness inventory. The ready milestone is authoritative; this is not an evidence registry or per-criterion test matrix.

**Milestone:** `docs/milestones/M0032-agent-cli-session-tracing.md`  
**Planning base:** `main` `2cea53e3184026c4ff96f4ce5dd3351bbae3325b`  
**Status:** pending

## Planned obligations

| ID | Material outcome (same meaning as milestone) | Status | Brief result / blocker |
|---|---|---|---|
| CLI-01 | Independent agenttrace CLI, noninteractive recording/read commands and stable process/exit surface, without integrations or Autoformat/Hygiene coupling | pending | |
| SES-01 | Durable, explicit-ID session lifecycle, list, pause/resume/stop, invalid transitions, concurrent session isolation, unfinished states | pending | |
| RUN-01 | Actual child-process wrapper with faithful argv/cwd/streams/exit status, pre-launch start persistence, launch failure and interruption behavior | pending | |
| DECL-01 | Optional declared external/MCP begin/end spans and point-in-time marks, with honest provenance and IDs | pending | |
| CFG-01 | Optional root .agenttrace.json, root discovery, JSONL default, SQLite option, path/invalid-config behavior and Git ignore | pending | |
| STORE-01 | Equivalent, durable JSONL/SQLite session/event semantics under concurrent CLI processes, including partial records | pending | |
| TIME-01 | Monotonic command timings, UTC chronology, interval-union coverage, paused time, declared time, and honestly unattributed intervals | pending | |
| REPORT-01 | Console/file human report in text/Markdown with timelines, summaries, failures, repeats and provenance | pending | |
| EXPORT-01 | Pure, versioned machine-readable JSON/JSONL session export and machine-usable session discovery, consistent across providers | pending | |
| SAFE-01 | No default stream/environment capture, sensitive-argument suppression, scoped writes, truthful incomplete state, no neighboring tool regression | pending | |
| TEST-01 | Focused deterministic tests and representative real-process CLI fixtures across failures, concurrency, storage modes and reporting | pending | |
| DOC-01 | Current README, architecture, engineering, help/examples, milestone index and completion notes accurately describe independent AgentTrace | pending | |

## Implementation progress / resume context (when useful)

No implementation started. Read the ready milestone, this ledger, relevant project authority and live source; the implementer owns decomposition and internal mechanics.

## Completion accounting

Before `COMPLETE`, reconcile the twelve milestone/ledger IDs and inspect real delivered outcomes. Mark each row truthfully; record a concise milestone completion note and focused results. No evidence-case IDs, test-by-obligation registry, receipts or installed-consumer release gate are required.
