# .NET AI Workbench

This repository keeps the historical GitHub name `carlrabbit/dotnet-ai-code-hygiene` and builds the `DotNetAiWorkbench.slnx` monorepo. It includes independent `dotnet autoformat` (`DotNetAiWorkbench.Autoformat.Tool` 0.1.0), transitional `dotnet hygiene` (`DotNetAiCodeHygiene.Tool` 0.12.0), and the standalone local `agenttrace` CLI. Autoformat owns the mechanical `.editorconfig` block and canonicalizes C# syntax, readability layout and SDK whitespace. Hygiene retains its live semantic-review and lifecycle functions and delegates formatting to Autoformat. Supported consumers use Windows 11 and the .NET 11 SDK line. No public feed publication is claimed; AgentTrace packaging and installed-consumer qualification remain deferred.

## Local AgentTrace

`agenttrace` is an independent local append-only event recorder. It has no agent hooks, process runner, transcript/model integration, MCP client, background service, or Autoformat/Hygiene dependency. The caller chooses a shared work key and optional actor key; lifecycle markers do not register or gate sessions:

```powershell
agenttrace start --session PR-45 --actor agent-a
agenttrace command start --session PR-45 --actor agent-a --command "dotnet test --no-restore"
# Run the command yourself using your normal shell or tool.
agenttrace command end --session PR-45 --actor agent-a --command "dotnet test --no-restore" --exit-code 1 --status failed
agenttrace operation start --session PR-45 --actor agent-a --kind mcp --name github.create_pull_request
agenttrace operation end --session PR-45 --actor agent-a --kind mcp --name github.create_pull_request --status complete
agenttrace mark --session PR-45 --actor agent-b --note "Investigating a failure"
agenttrace pause --session PR-45 --actor agent-a
agenttrace resume --session PR-45 --actor agent-a
agenttrace stop --session PR-45 --actor agent-a
agenttrace report --session PR-45 --format markdown --out trace-report.md
agenttrace export --session PR-45 --format json
```

`command start` records the exact command text and SHA-256 fingerprint; it never runs it. `command end` supplies the same command text so the event can be correlated by session, actor, fingerprint, and optional matching `--label`. Identical overlapping commands pair FIFO and are reported as ambiguous; labels can disambiguate them. `start`, `pause`, `resume`, and `stop` are ordinary annotations, so repeated or late recording remains valid. If agents omit `--actor`, their events share an undifferentiated stream.

For a sensitive command, pass `--sensitive` on both start and end. A `--label` is optional and may identify or disambiguate the event; AgentTrace stores only the unkeyed SHA-256 fingerprint, sensitivity flag, optional label and end outcome. It does not store command text, parameters, executable names or inferred names. The hash supports equality matching but does not protect low-entropy secrets against guessing; command text may also appear in shell history or process listings. The `--name` option belongs only to declared `operation` events, not commands.

With no config, events append to JSONL under ignored `.agenttrace/sessions/`. Optional root `.agenttrace.json` can select JSONL or SQLite. Repository-relative and explicit absolute paths are supported; a shared local SQLite example is `{"schemaVersion":1,"storage":{"provider":"sqlite","path":"C:/AgentTrace/traces.db"}}`. Changing stores does not migrate or combine previous events. SQLite WAL is intended for a local database, not a shared network filesystem.

Command durations are differences between separate UTC event timestamps; they include the interval between the agent's start and end logging calls and are not precise process-runtime measurements. Actor pauses are annotations that affect only that actor's interpreted coverage. Reports show observed event windows, pairing anomalies and unattributed gaps; these gaps are not model thinking time and do not prove no work occurred.

The next local package target is `0.12.0`; public-feed publication is not claimed. Install from a trusted NuGet source after the package is available there:

```powershell
dotnet tool install --global DotNetAiCodeHygiene.Tool --version 0.12.0
dotnet hygiene --version
```


## Independent Autoformat

Install the standalone formatter from a trusted source after it is available there:

```powershell
dotnet tool install --global DotNetAiWorkbench.Autoformat.Tool --version 0.1.0
dotnet autoformat init
dotnet autoformat
dotnet autoformat format src/Example.cs
dotnet autoformat normalize src/Example.cs --output json
dotnet autoformat normalize --no-restore
```

`init` establishes or validates Autoformat's fixed .NET 11 mechanical EditorConfig policy and does not normalize source. Default mode and `normalize` apply the nine opinionated modernization families and readability layout, run SDK style and whitespace passes, then reapply final syntax layout. Source-local retain exceptions are respected throughout. `format` remains SDK whitespace-only. SDK formatting performs its normal implicit dependency restore by default; pass `--no-restore` to skip it explicitly. Every formatter invocation mutates source in place; `--check` and `--changed` are rejected. Autoformat has no check-only command because a simulated formatter duplicates SDK/project execution and can drift from the real write path. To verify without changing a working repository, run Autoformat in a disposable clean checkout and inspect its Git diff. Autoformat does not read Hygiene profiles, approvals or review state. Mutating runs require a clean Git checkpoint; inspect partial edits after formatter failures. If initialization reports conflicting or malformed policy, resolve the `.editorconfig` ownership conflict without deleting unrelated settings.
## First Bootstrap

In a Git repository with a known, clean `HEAD`, ask an implementation agent to run `dotnet hygiene` and follow the emitted steps. On first use, the tool initializes Autoformat's mechanical policy, installs the Hygiene `dotnet-11` v3 analysis profile, and applies Autoformat normalization over eligible repository C# projects. It writes a baseline report under `.hygiene/reports/` with changed paths and observed validation. The report remains **approval-required** until an authorized external approver reviews and accepts the baseline. The tool cannot determine who has that authority.

Review the baseline report and source diff before accepting it. Bootstrap preserves content outside the profile-owned sections in `.editorconfig` and `Directory.Build.props`; malformed or incompatible profile state requires a safe manual decision. The pre-hygiene formatter assessment reports current drift; it does not infer whether a formatter was previously run.

```powershell
dotnet hygiene --output json
dotnet hygiene bootstrap --output json  # manual bootstrap/diagnostics
$baseline = dotnet hygiene --output json | ConvertFrom-Json
dotnet hygiene baseline accept --approver '<authorized identity>' --report-sha256 $baseline.reportSha256
```

Baseline acceptance names the SHA-256 of the exact report reviewed. The tool refuses acceptance if the report or its relevant source/configuration/policy snapshot changes before approval. After acceptance, ordinary source edits remain allowed; a material policy change needs a new approval. A profile marker by itself never authorizes normal gate execution.

## Run on Gates

After the baseline is approved, start autonomous mutation from a clean Git worktree and index with an identifiable `HEAD`, then run `dotnet hygiene` from the repository. It runs Autoformat canonicalization and SDK formatting, infers a trusted local gate where possible, and reports deterministic findings plus selected semantic-review work grouped by source file. Follow only the selected work order. Inspect each rule's questions independently; remediation or acceptance for one rule does not satisfy another. On failure, inspect and recover the partial worktree explicitly; the tool does not roll back or create commits.

```powershell
dotnet hygiene
dotnet hygiene check --base origin/main --output json
dotnet hygiene review accept <batch-handle> <item-id>...
dotnet hygiene review accept <batch-handle> --all
```

Accept only items whose fixed questions you inspected and answered positively. Leave negative or uncertain items unaccepted and use the rule's escalation guidance. Rerun `dotnet hygiene` after source changes. `complete` means the selected gate obligations were processed; it does not certify the full repository population as defect-free. A successful command execution is not itself a complete gate.

The 17 focused general semantic-review rules share a gate budget of at most ten distinct rule-subject obligations, including at most two context-affected obligations. Each obligation is independently judged, and a candidate signal is not proof of a defect. The nine Hygiene modernization review rules retain their separate five-package budget across all families; Autoformat's transformations are applied independently during normalization. Hygiene-only retained candidates may use `// hygiene:retain modernization.<rule-id> -- <reason>`. Autoformat uses `// autoformat:retain <rule-id> -- <reason>` for a construct-specific canonicalization exception. `--review-all` remains bounded.

## Manual Inspection and Debugging

The normal workflow needs no subcommand knowledge. Use these commands for nonmutating diagnostics and review handoffs:

```powershell
dotnet hygiene check src/Example.cs
dotnet hygiene check --changed --output json
dotnet hygiene check --base origin/main --output json
dotnet hygiene check --review-all --output json
dotnet hygiene rules --output json
dotnet hygiene explain <finding-handle>
dotnet hygiene review expand <batch-handle>
dotnet hygiene review handoff <batch-handle> --output json
dotnet hygiene format
dotnet hygiene normalize
```

Use `review handoff` for negative or uncertain items that need the rule's designated reviewer. Its report is `escalation-required`; handed-off items remain outstanding until a reviewer decision returns. Semantic `check` targets `paths`, `--changed`, `--base`, and `--review-all` are mutually exclusive. If gate context is ambiguous, provide a trusted base; the tool does not fetch or invent a remote. Hygiene `format` and `normalize` mutate through Autoformat and require a clean checkpoint. They reject formatter `--check` and `--changed`; `hygiene check --changed` remains read-only semantic review. Normal autonomous cleanup runs SDK format across all eligible project sources. Failures leave edits inspectable and recovery is explicit from Git. The tool does not roll back, commit, reset, or clean.

## Exhaustive read-only audit

`dotnet hygiene --yolo-all` is a separate full audit. The first call asks whether the calling agent was explicitly tasked by the user. If yes, rerun with `--attest-explicit-task`; if no, stop or pass `--deny-explicit-task`. Without affirmative attestation, the scan does not start. The assertion is recorded but not authenticated.

An authorized audit requires a valid initialized profile and accepted baseline; it never creates or refreshes either. It evaluates every enabled deterministic rule without applying changes and enumerates every eligible semantic rule/subject pair, including previously accepted or normally ignored items. It does not bootstrap or update the profile/tool, format or normalize code, change normal review state, ask for fixes, or call an AI provider. The external agent supplies `acceptable`, `problem`, or `uncertain` judgments; the last two require rationale.

The tool creates a local audit directory under `.hygiene/audits/` by default. `dotnet hygiene audit next --run <audit-dir>` returns bounded work with source context. Record agent judgments using `dotnet hygiene audit record --run <audit-dir> --file <judgments.json>`, then create `report.json` and `report.md` with `dotnet hygiene audit finish --run <audit-dir>`. Work and valid judgments remain in that audit directory so collection can resume. Reports disclose that source changes during collection are not tracked and that the inventory is not an atomic repository snapshot. Source text is embedded in audit work; choose a local destination appropriate for that content.

```powershell
dotnet hygiene --yolo-all --attest-explicit-task
dotnet hygiene audit next --run <audit-dir>
dotnet hygiene audit record --run <audit-dir> --file <judgments.json>
dotnet hygiene audit finish --run <audit-dir>
```

## Updating to a New Release

On each autonomous run, the entrypoint checks configured NuGet sources for a newer stable package using an isolated temporary tool installation. If it finds one and can identify the invoking local-manifest or apphost installation scope, it starts a separate updater, exits to release its loaded files, then the updater updates only that installation, restarts the installed command, and returns its revalidation report. Local manifest lookup searches ancestor directories and matches package IDs without case sensitivity. The updater never picks a different same-named PATH tool. The running executable is never replaced in process. If no stable package can be resolved, the compatible installed version continues and the lifecycle report says release discovery was unavailable. Unknown installation scopes receive an update instruction rather than an inferred global update.

You can also update explicitly from the same trusted package source:

```powershell
dotnet tool update --global DotNetAiCodeHygiene.Tool --version 0.12.0
dotnet hygiene --version
dotnet hygiene
```

For a tool installed with `--tool-path`, run `dotnet tool update --tool-path <tool-path> DotNetAiCodeHygiene.Tool --version <approved-version>` from the same trusted package source. Offline or unavailable release metadata is not evidence that the installation is current; follow the explicit next action in the lifecycle report. Do not use floating prerelease versions or arbitrary package feeds.

## Planning and Semantic Rule Development

The pre-1.0 planning policy is in [Project Guidance](docs/PROJECT-GUIDANCE.md), with the rationale in [Semantic Rule Portfolio Strategy](docs/research/SEMANTIC-RULE-PORTFOLIO-STRATEGY.md). Rule development favors broad agent-implemented candidate portfolios followed by consolidation; it does not preserve experimental rule contracts for compatibility. Deterministic candidate and handoff contracts are tested, but invented fixed-label LLM judgments are not treated as repeatable semantic regression tests.

## Deferred Work

`--review-all` remains a bounded inspection request, not exhaustive authorization. No hosted CI/workflow, provider integration, or public package publication is part of this release.
