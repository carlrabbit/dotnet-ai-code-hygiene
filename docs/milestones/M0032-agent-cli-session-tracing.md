# Milestone — M0032: Local Agent Session Tracing CLI

**ID:** M0032  
**State:** complete (implementation PR #45 updated; awaiting review)
**Type:** feature  
**Planning base:** amended planning commit `6439d4411b96dd05491a058c1e32c085ea74b16a` (PR #44; M0031 already merged)
**Methodology:** repository-pinned Agentic Project Guides 1.0.0 in `.guide/system/`, including the `cli-tool` profile; project `docs/PROJECT-GUIDANCE.md`

## Goal and target capability

Deliver **one complete local, agent-operated, append-only event recorder** named `agenttrace` in the .NET AI Workbench. It runs as an ordinary CLI command within an existing agent chat, independent of the agent runtime. Agents explicitly log the beginning and ending of commands they execute **using their own existing tools**; they may also record MCP/external activity, markers and lifecycle annotations. The CLI must **never execute, wrap, supervise or intercept the traced command**.

One work identifier (for example `PR-45`) can be shared by parallel agents on the same machine. The caller chooses the session key and, when useful, the actor key. No generated session ID, run ID, event ID, registration, session manager, agent integration or daemon is needed. Durable JSONL (default with no configuration) and SQLite storage, human reports and complete machine-readable data exports are all part of **this one milestone**.

This amends the original M0032 planning decision before its PR is merged. **Superseded decisions:** process execution by `agenttrace run`, supervised subprocess timings, generated session/operation handles, pausing/stopping as recording gates, repository-confined SQLite paths and opaque full-command redaction without a correlation hash. The original implementation in PR #45 followed that earlier plan; implementation must now adapt rather than assert prior completion still meets this revised contract. Historical planning commits and reviews remain historical.

## Architecture and invariants

### 1. Event recorder, not execution engine

- The tool is an independent .NET 11 CLI integrated into `DotNetAiWorkbench.slnx` with focused tests. It has no product dependency on Autoformat, Hygiene semantic review, Roslyn, provider-specific APIs or their state.
- Every **recording** command does exactly one durable append of an event with a UTC timestamp and caller-supplied metadata. Recording must not execute the supplied command, alter the Git worktree, start background work, or depend on prior session registration. The read commands `sessions`, `report` and `export` are nonmutating with respect to logical trace data.
- No Codex/Claude hooks, MCP transport/client/server, OpenTelemetry, transcript ingestion, OS command interception, model calls, long-running connection/collector, user-facing dashboard or hosted analytics. The external agent chooses which events to report. Missing events are not proof of inactivity.
- Command discovery and automation surfaces must be noninteractive with usable `--help`/`--version`, clear input errors, consistent exit codes and uncontaminated structured stdout.

### 2. Caller-selected session and actor; lifecycle as ordinary events

- Every recorded event has required, nonempty `--session <key>`; the key is an **opaque caller-chosen string**, not a generated or registered identifier. For example, `PR-45` can be shared across repositories if the agents are configured to use the same store. Do not infer IDs from process ID, terminal, branch or filename. An optional nonempty `--actor <key>` identifies one agent within a shared session. No actor identifier is generated; when actors omit it, they knowingly share an undifferentiated actor stream.
- `start`, `pause`, `resume`, `stop` and `mark` append ordinary timestamped lifecycle/note events carrying session and optional actor. They **do not create, pause, resume, close, own or lock** a session. Multiple `start`/`stop` events, late events, actions while paused, and events without a preceding `start` are valid and retained. Session discovery is derived from observed event keys; no session registry, status gate, transition validation or generated key.
- A pause is a declaration about the **specified actor**, not an authoritative session-wide pause over other agents. A marker without an actor remains a session-level annotation. Reports may interpret paired actor pause/resume markers for that actor's estimated active-time coverage, but must not silently apply one actor's pause to another or suppress events.

### 3. Command and other timed-operation events

The following invocation shape is an illustrative **required CLI capability** (the implementer may refine syntactic aliases, not the semantics):

```powershell
agenttrace start --session PR-45 --actor agent-a

agenttrace command start --session PR-45 --actor agent-a --command "dotnet test --no-restore"
# Agent independently runs this exact command through its normal shell tool.
agenttrace command end --session PR-45 --actor agent-a --command "dotnet test --no-restore" --exit-code 0

agenttrace operation start --session PR-45 --actor agent-a --kind mcp --name github.create_pull_request
# Agent independently performs the MCP invocation.
agenttrace operation end --session PR-45 --actor agent-a --kind mcp --name github.create_pull_request --status complete

agenttrace pause --session PR-45 --actor agent-a
agenttrace mark --session PR-45 --actor agent-b --note "Investigating a failure"
agenttrace stop --session PR-45 --actor agent-a
```

- `command start` and `command end` both carry the **full original command text** so a deterministic SHA-256 fingerprint can be calculated from the exact UTF-8 text, without reparsing, tokenizing, rewriting, trimming, normalizing PowerShell quoting, or reconstructing an executable/argument array. Preserve original text on ordinary command-start events. An end may supply an exit code and/or explicit completion status; neither is inferred by running a process. Command-end records need the fingerprint and reported outcome, not a second plaintext copy.
- **No generated command/run/span/event identifier.** Match starts/ends from **session key + actor key + command fingerprint + optional caller-supplied label**. The label, when supplied on a start, must be the same on its end. It is metadata and a disambiguator, not a generated ID. Both command events are recorded as submitted, including repeated starts, unmatched ends, late ends, failures and missing results; appending must not be blocked by correlation/lifecycle assumptions.
- Allow overlapping commands from one actor. For a matching identity, pair ends to the **oldest unmatched start (FIFO)** in append order. If identical identities overlap, mark the affected pairs **ambiguous** in derived reporting/export interpretation: durations may be misassigned even though counts and events are preserved. Users/agents can distinguish identical concurrent operations by changing their actual command text where appropriate or supplying distinct safe `--label` values on both events. Do not require operation IDs or forbid concurrency merely for exactness.
- `operation start/end` support optional agent-declared noncommand work (such as `mcp` or `external`), matched by session, actor, kind, operation name and optional label with the same FIFO/ambiguity rule. All command and operation events are agent-reported: avoid `supervised`/`verified subprocess` provenance claims. A recorded `mcp` label is **not** independent evidence that an MCP server was invoked. `mark` is a single instantaneous event.
- Internally the store may use an insertion-order sequence/rowid for durable ordering. It is **not an operation handle** and is never required as input to `end`. Pairing is a read-time projection; avoid a separate persisted “running command”, “active session” or operation-registry table.

### 4. Sensitive commands: hash-only persistence and optional labels

- Ordinary command-start events store the **exact complete command text** and deterministic SHA-256 fingerprint. For **sensitive** command-start events, `--command` remains required as transient hashing input, but the stored event contains **only** the unkeyed fingerprint, `sensitive=true`, and an **optional caller-provided `--label`**; it must contain no command text, executable name, argument array, or derived command-name field. `--sensitive` must work **without `--label`**, and the CLI must **not offer or require a command-specific `--name` option**. The same `--label` that optionally makes a sensitive command recognizable in human reports also disambiguates concurrently active identical command fingerprints: when supplied on start it must match on end. The label is caller-controlled metadata, not a parsed or automatically inferred command name. On `command end`, calculate the fingerprint from the supplied exact command text; store fingerprint, optional label, outcome and sensitivity marker but never store the command text.
- No HMAC, password, encryption key, repository-local secret, reversible token registry or hash salt. A **sensitive hash is flagged as sensitive** in both outputs so users can locate and clean up recorded values later. With an optional label, human reports may display the label; without it, display a **sensitive command hash/placeholder**, not an invented name. A plain hash supports stable equality correlation, **not confidentiality** against dictionary/guessing attacks; document this limitation. The caller is responsible for keeping optional labels and notes nonsecret, and `--command` arguments may still appear in shell history or process listings. Do not perform automatic command-name extraction, secret detection or retrospective rewriting of other agents' data.
- Never implicitly read, store, or forward subprocess stdout/stderr, stdin, environment variables, prompts or tool results. The tracer **does not own** those processes. Errors must not print sensitive argument values.


Example of an **unlabeled sensitive command** (the `--command` input is used only for hashing, not persisted):

```powershell
agenttrace command start --session PR-45 --actor agent-a --command "dotnet nuget push package.nupkg --api-key SECRET" --sensitive
# The agent executes the command independently.
agenttrace command end --session PR-45 --actor agent-a --command "dotnet nuget push package.nupkg --api-key SECRET" --sensitive --exit-code 0
```

If the caller wants human-readable identification or to disambiguate identical overlapping commands, it may additionally pass the **same** safe `--label "Publish package"` on both start and end. No command-specific `--name` argument exists. The separate `operation --name` parameter still identifies declared noncommand operations.

### 5. Repository configuration, JSONL and SQLite

- Discover the repository root from the invoking directory, or use explicit `--repo <path>`; descendant directories load the same optional repository-root `.agenttrace.json`. **No config file means JSONL by default** in ignored `.agenttrace/sessions/` with one append-only event file per caller session key. Invalid/malformed explicit config fails visibly, not by silent fallback.
- Support `{"schemaVersion":1,"storage":{"provider":"sqlite","path":"C:/AgentTrace/traces.db"}}` and repository-relative paths (e.g. `.agenttrace/traces.db`). The same path semantics apply to configured JSONL directories. Explicit absolute paths **outside the repository are allowed** (local filesystem), enabling several repositories to share one SQLite store. Do not impose the former repository-containment restriction on configured external destinations. Paths must not accidentally point at unrelated files or silently overwrite an incompatible database. Generated default trace data is ignored by Git; a committed config is not.
- JSONL and SQLite expose the same **versioned logical event schema** (session/actor, type, timestamp, command fingerprint, optional label, sensitivity, reported outcome and other payload). Neither backend stores an extracted command name. Events are append-only, including duplicates and unmatched ends. Retain both raw event order and an interpreted chronological view. A record that was never durably appended cannot be claimed as captured.
- Support concurrent **independent CLI processes**, including writers to the **same session**. JSONL uses cross-process-safe complete-record appends/coordination. An unterminated final JSONL record may be ignored on reads **if malformed**, but **the next append must never concatenate a fresh record to the unterminated tail**. Under the writer lock, distinguish a valid final JSON record that merely lacks a newline (preserve it and insert a delimiter) from genuinely incomplete/malformed trailing bytes (recover or quarantine those bytes with an observable diagnostic), preserve all previously complete records, then append. Subsequent read/export must succeed and include the new event. SQLite uses WAL, short write transactions and a bounded busy timeout; simultaneous writers serialize, readers can coexist, and **no long-lived connection or daemon is required**. SQLite WAL is intended for a local database, not a shared network filesystem.
- Separate configuration selects **one active store**, with no implicit data migration, synchronizing different stores, global catalog or backup manager. Reports clearly distinguish no data in the chosen store from an empty valid session.

### 6. Derived timing and reporting truth

- Each CLI record is timestamped at **record time in UTC**. A paired command duration is the difference between start/end event timestamps, **not instrumented child execution time**: it includes the agent's tracing gap, tool launch/response latency and other intervening work. Since calls are separate processes, do not claim a continuous cross-invocation monotonic clock or precise model thinking time. Detect and disclose impossible/backwards-clock intervals rather than silently manufacturing elapsed precision.
- Combined reports group by session; show actors separately where present. Provide observed event window, per-command elapsed estimates, counts, failures, missing pairs, repeated command identities, cross-actor overlap and FIFO ambiguity, and the union of paired command intervals (avoid double-counting overlapping intervals).
- Actor-specific paired pause/resume markers may be reflected as declared inactivity; **never** treat another actor's pause as global session downtime. Multiple/unpaired lifecycle markers are annotations, not state failures; flag ambiguous or unpaired lifecycle interpretation. Unobserved gaps within a report's **observed event window** may be presented as unattributed, not “thinking”, “throttling”, or evidence of inactivity. If start/stop are absent, do not imply a complete elapsed session lifetime or infer it from a generated session entity.
- `report --session <key> [--format text|markdown] [--out <path>]` provides a readable timeline and concise accurate statistics, displaying the **optional safe label** for a sensitive command when supplied, otherwise only its hash/placeholder, with sensitivity visible. `export --session <key> [--format json|jsonl] [--out <path>]` provides a complete, deterministic versioned machine-readable event dump plus any **explicitly derived** matching/ambiguity/coverage fields clearly distinguished from recorded facts. `sessions` discovers keys, including JSON output. No Markdown/progress chatter contaminates JSON stdout, and reports/exports never mutate event data.
- A missing `--actor` is legal for simple single-agent usage but cannot distinguish independent agents that choose the same default stream. State this limitation in documentation; do not secretly invent actor identities.

### 7. Protected neighboring contracts

- Autoformat, transitional Hygiene, the existing guide snapshot, rule/review lifecycle, active Git checkpoint requirements of **other** products, and their existing projects/tests remain intact. AgentTrace writes local trace data (not source) without requiring a clean Git checkpoint; no cross-component services or shared DB-index architecture.
- No new release publication, CI workflows, persistent host provisioning, external targets, permissions changes, or execution of potentially destructive commands. The absence of a subprocess runner **reduces** these risks; tests need only make harmless recorder calls.

## Scope and exclusions

**In scope:** independent CLI and help, caller-keyed event stream and lifecycle markers, concurrent command and declared-operation matching, optional caller labels and unkeyed sensitive hashes, repository/default and external-location config, append-only JSONL and SQLite, human report, machine-readable export, and focused tests/documentation.

**Out of scope:** any command or process execution by AgentTrace; generated session/run/event IDs; hook/native/MCP/OTel integrations; automatic tool observation; long-running service, UI or analytics; credential-key management or guaranteed secret irreversibility; full subprocess output capture, live-model performance inference, general history migration or release/package qualification.

## Acceptance and material obligations

The material obligation **IDs remain stable** from the initial planning handoff; their meanings are amended explicitly below. The earlier completed ledger in implementation PR #45 is not acceptance of this corrected plan.

- **CLI-01 — Independent recording-only CLI.** All supported commands are noninteractive and discoverable; writing operations only append events, never run the traced command, and have reliable diagnostics/structured output without depending on other workbench tools.
- **SES-01 — Caller-keyed, annotation-only lifecycle.** Every event uses a caller-chosen session and optional actor; session keys are never generated/registered. Repeated start/pause/resume/stop and commands after pause/stop remain valid; sessions are discovered from events, without lifecycle gating or status ownership.
- **RUN-01 — Command begin/end recording and correlation.** Full exact command text is logged at ordinary starts; sensitive starts retain **only the deterministic hash and sensitivity marker plus optional label**, never a command name or plaintext. Both sides correlate by caller-selected session/actor, exact-text hash and optional identical label; ends record the reported outcome. No command-specific `--name`, process execution or generated run handle. Concurrent identical commands use FIFO matching with disclosed ambiguity.
- **DECL-01 — Declared external/MCP events.** Generic start/end spans and point marks can be logged without generated IDs or tool integration; the same session/actor/name/label matching, ambiguity and honest agent-reported provenance apply.
- **CFG-01 — Repository-configured local storage.** Optional root config, no-config JSONL default, explicit JSONL/SQLite choice, usable repository-relative **and absolute external storage paths**, schema/invalid-path diagnostics and ignored generated defaults.
- **STORE-01 — Durable append-only storage parity.** JSONL and SQLite WAL retain equivalent canonical event records under short independent CLI invocations, concurrent same-session writers and duplicates, with no session/operation registry. A truncated JSONL tail is recovered under append locking so an additional event does not corrupt previously readable data.
- **TIME-01 — Honest timestamp-derived interpretation.** UTC pairs, union overlap calculations, actor-specific pause annotations, unmatched/clock-invalid/ambiguous intervals and explicitly unattributed observed gaps are reported without claiming precise subprocess duration or model thinking time.
- **REPORT-01 — Human reporting.** Text/Markdown reports summarize complete ordered events, sessions and actors, command durations, failures, repeats, sensitive hashes (with optional labels), unmatched and ambiguous pairs, and qualified coverage/pauses.
- **EXPORT-01 — Raw agent-readable data.** Versioned JSON/JSONL exports expose all recorded events (and clearly labelled deterministic derived interpretations if supplied), with no non-data stdout; machine-readable session discovery works for both stores.
- **SAFE-01 — Privacy and adjacent behavior.** Sensitive starts and ends persist only hash, sensitivity marker, optional caller label and relevant outcome (never command text, arguments, executable or inferred name); no label is mandatory. Hash uses no secret; subprocess streams/env are not captured, explicit external storage paths do not clobber unrelated files, and existing workbench functionality is unaffected.
- **TEST-01 — Focused evidence.** Unit/model and actual independent CLI-process fixtures distinguish the event-only contract, shared-session concurrency, duplicate-command FIFO ambiguity, sensitive matching **with and without labels** across both stores, and a truncated-JSONL-tail **read then append then export** recovery sequence; no subprocess supervisor, installed tool or model simulation test matrix.
- **DOC-01 — Operative product documentation.** README, architecture, engineering commands, CLI help/config examples, milestone index and completion note describe the corrected design, useful reports, timing/privacy limitations, and release deferrals; obsolete process-runner descriptions are removed.

## Execution obligation ledger

**Planner-seeded:** `.execution/M0032-agent-cli-session-tracing.md` carries these same twelve stable IDs as **pending under this amended contract**, not as a proof registry. PR #45's prior `complete` claims were for the superseded design and must be reconciled during correction.

## Validation

**Implementation feasibility / locus:** The repository documents a Windows 11 and .NET 11 SDK development environment with PowerShell, System.CommandLine and TUnit in `docs/ENGINEERING.md`, `Directory.Build.props` and the existing CLI/test projects. Earlier PR #45's focused Windows 11/.NET 11 tests and SQLite package references are useful implementation evidence, **not** proof of this changed product contract. The implementer verifies its actual authorized SDK/toolchain with a small `dotnet --info` preflight, then builds/tests only affected AgentTrace projects. No additional provisioning, remote server, CI runner, agent hook or permissions are required.

**During implementation:** exercise narrow CLI parsing/event creation, deterministic fingerprint and sensitive redaction, pair/FIFO/overlap and pause calculations, configuration discovery/invalid paths, and backend persistence. Reuse surviving PR #45 tests/services where correct and delete process-supervisor tests instead of preserving unused machinery.

**At milestone completion:** representative real CLI invocations (not a tool-installed package) demonstrate:
- A command with complex PowerShell syntax is **recorded but not executed**; its exact text appears at ordinary start, a matching end with a nonzero reported exit code closes it, and no generated key is needed.
- Two actors write to the same session concurrently; commands with distinct text match correctly despite out-of-order completion, while two overlapping identical commands match FIFO and are **marked ambiguous**, unless distinct labels are supplied.
- `pause`/`stop` do not block subsequent recording, actor-specific pause interpretation does not erase concurrent work, and missing/extra begin/end events are retained and reported honestly.
- The same logical event set flows through default JSONL and explicitly configured SQLite (including an external local absolute path), with consistent raw exported fields and human-report interpretations. Concurrent multi-process append remains uncorrupted; **after a torn final JSONL record, reading, appending another event and exporting succeeds** without losing prior complete records; torn-byte recovery is observable.
- A sensitive command retains **only** its stable SHA-256 fingerprint, sensitivity marker, optional caller-provided label and reported end outcome—not its secret-bearing text or any separate command name—in **either** store or export. Test an unlabeled sensitive command, then a labeled one, including matching both start/end records and ensuring no `--name` is required or accepted by `command start/end`.

**Important risks to discriminate:** using `ProcessStartInfo` despite the event-only design; implementing hidden session/operation state; confusing concurrent actors; matching ends solely to “latest command”; treating FIFO pairing as certain for overlapping duplicates; persisting sensitive command names or arguments, or requiring a label; incorrectly subtracting one actor's pause from another; strict path containment preventing legitimate external SQLite storage; WAL write contention/JSONL torn-tail append corruption; misleading inference from timestamp gaps. A few focused fixtures and code review suffice; no per-obligation test/gate infrastructure.

**Deferred to release:** installed package/manifest and real consumer launch, final-distribution help and packaging, broader platform qualification (repository currently supports Windows 11), and consumer-repository trials. Do not invoke unrelated full Autoformat/Hygiene release validators.

## Documentation, handoff and authorization

Project authority remains `AGENTS.md`, `docs/PROJECT-GUIDANCE.md`, `docs/ARCHITECTURE.md`, `docs/ENGINEERING.md`, `.guide-profile.json`, the checked-in `.guide/system/` 1.0.0 snapshot, and `DotNetAiWorkbench.slnx`. No new ADR file is required: this milestone is the authoritative product-level decision for a new independent tool, and this repository has no separate project-ADR convention. Implementer should update operative architecture documentation to reflect the delivered design; historical documents and the guide snapshot remain unchanged.

**Transport:** amend planning PR #44 in place, then use the **exact new amended planning commit SHA** as the accepted implementation baseline. Existing implementation PR #45 is based on the original planning commit `d567fb8e2b9a42f3ad331c2a64ca174b8336e3fc`; reconcile/rebase its implementation branch with the amended plan, keep its PR targeting the planning branch, replace the obsolete ledger completion state, and revise the implementation PR description before claiming completion. Never treat the earlier source/test results as proof of new semantics. No new milestone or separate planning PR is needed.

Authorized implementation/test writes are restricted to the existing implementation worktree/branch and owned disposable trace test destinations. This does not authorize modifying unrelated tool behavior, CI workflows, machine provisioning, shared stores, secrets or consumer repositories. No live external MCP/model calls are necessary for validation.

## Completion note (implementer fills after delivery)

Implemented the event-only `agenttrace` CLI with caller-selected session/actor keys, append-only lifecycle annotations, exact command-content SHA-256 correlation, FIFO matching and ambiguity disclosure, optional labels, declared external/MCP spans, marks, JSONL and SQLite WAL stores, text/Markdown reports, and versioned JSON/JSONL exports. Sensitive command events retain only the safe start name, unkeyed fingerprint, sensitivity marker and end outcome; neither command side persists secret-bearing parameters. Configured absolute local stores can be shared across repositories. The recorder never executes command text.

Focused validation on Windows 11 with .NET SDK `11.0.100-rc.3.26522.148`: the AgentTrace test project build succeeded, and `dotnet test tests/DotNetAiWorkbench.AgentTrace.Tests/DotNetAiWorkbench.AgentTrace.Tests.csproj -c Release --no-restore` passed **11/11** focused tests. Real CLI processes covered nonexecution of a complex PowerShell command, ordinary and sensitive pairing, distinct command correlation across actors with out-of-order completion, identical command/operation FIFO ambiguity, label disambiguation, annotation-only lifecycle and actor pauses, JSONL/SQLite logical parity, external absolute SQLite storage, simultaneous JSONL and SQLite writers, partial trailing JSONL recovery, invalid configuration and refusal to overwrite an unrelated destination. The build emitted the repository analyzer advisory about `EnableGenerateDocumentationFile`; there were no build errors.

Limitations remain explicit: timestamps are cross-process UTC estimates, hashes are guessable equality fingerprints rather than confidentiality, absent actors share an undifferentiated stream, and event gaps do not establish inactivity. Installed package/manifest behavior, final-consumer launch, other-platform qualification and consumer-repository trials remain deferred to release qualification. No aggregate Autoformat/Hygiene validation or package publication was performed.
