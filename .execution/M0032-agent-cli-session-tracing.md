# Execution Obligation Ledger — M0032

> Planner-seeded completeness inventory for the **amended event-only** M0032 contract. The milestone is authoritative. This is not an evidence registry, validation matrix, or a record of the superseded implementation's completion.

**Milestone:** `docs/milestones/M0032-agent-cli-session-tracing.md`  
**Planning base:** amended planning commit `e1ccedc2052a1b589ddbc69e389d8d1d0e8f1d4b`
**Status:** complete; corrected implementation is on PR #45

## Planned obligations

| ID | Material outcome (same meaning as amended milestone) | Status | Brief result / blocker |
|---|---|---|---|
| CLI-01 | Independent recording-only CLI, noninteractive commands, truthful diagnostics and structured output, no subprocess execution or tool integrations | complete | Help/version and malformed invocation remain noninteractive; a complex PowerShell command is recorded but not run. |
| SES-01 | Caller-selected session key and optional actor; lifecycle as append-only start/pause/resume/stop markers without registration, state gating or generated IDs | complete | Repeated lifecycle and post-stop recording remain valid; event keys are caller supplied and reports apply pauses per actor. |
| RUN-01 | Exact command-text begin/end events; sensitive text replaced by hash, optional caller label, reported result, concurrent identical-command FIFO/ambiguity; no generated run key or command `--name` | complete | Ordinary starts preserve exact text; sensitive start/end persist hash, flag and optional label only; labeled/unlabeled pairing, rejected command `--name`, FIFO ambiguity and label disambiguation pass. |
| DECL-01 | Optional agent-declared external/MCP start/end and timestamped marks with the same caller-keyed matching and truthful provenance | complete | Operations and marks remain agent-reported; unmatched ends and overlapping FIFO ambiguity are reported. |
| CFG-01 | Optional root .agenttrace.json, default JSONL, configured SQLite/JSONL, explicit absolute external paths and relative paths, valid diagnostics and ignored defaults | complete | Default JSONL and external absolute SQLite configurations pass focused CLI checks; malformed config is rejected and default trace data is ignored. |
| STORE-01 | Equivalent JSONL and SQLite WAL append-only event model, safe concurrent CLI writes, duplicate/unmatched event preservation, and safe append/recovery after truncated JSONL tail | complete | JSONL/SQLite event fields and sensitive behavior match; shared writers retain all events. Read–append–export removes malformed partial tail bytes with a stderr warning; valid unterminated JSON is newline-separated and preserved. |
| TIME-01 | Honest cross-invocation UTC duration estimates, FIFO overlap/ambiguity, per-actor pause annotations and qualified unattributed gaps without model-thinking claims | complete | Focused report/export tests cover timestamp-derived intervals, actor-specific pauses, unmatched/ambiguous spans, and qualified unattributed gaps. |
| REPORT-01 | Human text/Markdown chronological reporting of session/actor data, paired commands, repeated executions, sensitive hashes with optional labels, anomalies and timing coverage | complete | Text and Markdown reports expose event windows, actors, repeated identities, sensitive hash/label presentation, outcomes and anomalies. |
| EXPORT-01 | Complete versioned agent-oriented JSON/JSONL raw event export with clearly distinct derived fields, clean stdout and JSON session discovery | complete | JSON/JSONL exports and JSON session discovery were exercised through independent CLI processes for the configured stores. |
| SAFE-01 | Unkeyed SHA-256 sensitive correlation with optional caller label only; no command name, command text, or parameters for sensitive events, no stream/env capture, safe configured storage writes | complete | Sensitive raw and derived output contains no command text/name/parameters; unrelated DB bytes remain unchanged after safe rejection. Other workbench tools were not modified. |
| TEST-01 | Focused unit and CLI-process tests: nonexecution, concurrency, FIFO, pauses, storage parity, hash-only sensitive commands with/without label, truncated-tail read-append-export recovery | complete | Focused AgentTrace build succeeded; test suite passed 12/12 real CLI/model tests, including both sensitive label modes on JSONL/SQLite and both unterminated-tail cases. |
| DOC-01 | Current README, architecture, engineering/help, milestone index and completion note reflect event-only design and documented timing/privacy limitations | complete | README, CLI help, architecture, engineering notes, milestone index, completion note and this ledger describe corrected behavior and limitations. |

## Amended planning decision

All **twelve obligation IDs are preserved**. Their meanings were explicitly updated because the original planning contract incorrectly required `agenttrace run` to supervise subprocesses, generated IDs, session state gating and confined SQLite paths. The previous completion ledger inside PR #45 describes **the superseded contract**, not completed obligations under this amendment. The implementer owns reconciling that branch with this updated ready milestone and may retain correct infrastructure/tests while removing obsolete behavior.

No independent ADR exists or is needed for this new tool: the amended milestone is the operative product design decision; repository architecture documentation is updated during implementation.

## Implementation progress / resume context (when useful)

Corrected existing PR #45 against exact planning baseline `e1ccedc2052a1b589ddbc69e389d8d1d0e8f1d4b`. Command-specific `--name` is removed; JSONL append repairs malformed trailing fragments under the writer lock with an stderr warning while preserving valid complete records, including valid JSON that lacks a final newline. The implementation branch remains the existing PR #45 workstream.

## Completion accounting

All twelve stable obligations are reconciled as complete against the latest amended contract. The focused AgentTrace build succeeded and the test project passed 12/12. Installed-package and consumer launch, final distribution help/packaging, broader platform qualification and consumer-repository trials remain deferred to release qualification; aggregate unrelated validators were not run.
