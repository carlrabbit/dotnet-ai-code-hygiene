# dotnet-ai-code-hygiene

`DotNetAiCodeHygiene.Tool` is a model-independent .NET hygiene tool for coding agents. Its supported command is `dotnet hygiene`; the executable does not call an AI model or provider. The current package version is `0.10.0`. Supported consumers use Windows 11 and the .NET 11 SDK line.

The `0.11.0` package is validated from a local feed; public-feed publication is not claimed. Install from a trusted NuGet source after the package is available there:

```powershell
dotnet tool install --global DotNetAiCodeHygiene.Tool --version 0.11.0
dotnet hygiene --version
```

## First Bootstrap

In a Git repository, ask an implementation agent to run `dotnet hygiene` and follow the emitted steps. On first use, the tool assesses existing project formatting without changing source, installs the `dotnet-11` v2 profile, and formats eligible repository C# sources. It writes a baseline report under `.hygiene/reports/` with changed paths and observed validation. The report remains **approval-required** until an authorized external approver reviews and accepts the baseline. The tool cannot determine who has that authority.

Review the baseline report and source diff before accepting it. Bootstrap preserves content outside the profile-owned sections in `.editorconfig` and `Directory.Build.props`; malformed or incompatible profile state requires a safe manual decision. The pre-hygiene formatter assessment reports current drift; it does not infer whether a formatter was previously run.

```powershell
dotnet hygiene --output json
dotnet hygiene bootstrap --output json  # manual bootstrap/diagnostics
$baseline = dotnet hygiene --output json | ConvertFrom-Json
dotnet hygiene baseline accept --approver '<authorized identity>' --report-sha256 $baseline.reportSha256
```

Baseline acceptance names the SHA-256 of the exact report reviewed. The tool refuses acceptance if the report or its relevant source/configuration/policy snapshot changes before approval. After acceptance, ordinary source edits remain allowed; a material policy change needs a new approval. A profile marker by itself never authorizes normal gate execution.

## Run on Gates

After the baseline is approved, run `dotnet hygiene` from the repository. It applies enabled deterministic normalization and formatting, infers a trusted local gate where possible, and reports deterministic findings plus selected semantic-review work grouped by source file. Follow only the selected work order. Inspect each rule's questions independently; remediation or acceptance for one rule does not satisfy another.

```powershell
dotnet hygiene
dotnet hygiene check --base origin/main --output json
dotnet hygiene review accept <batch-handle> <item-id>...
dotnet hygiene review accept <batch-handle> --all
dotnet hygiene review decide <batch-handle> <occurrence-id> --verdict desirable|retain|uncertain
```

Accept only items whose fixed questions you inspected and answered positively. Leave negative or uncertain items unaccepted and use the rule's escalation guidance. Rerun `dotnet hygiene` after source changes. `complete` means the selected gate obligations were processed; it does not certify the full repository population as defect-free. A successful command execution is not itself a complete gate.

The six established semantic-review rules keep independent, bounded gate-local selection. The nine modernization rules share one five-package budget across all families; each selected package exposes bounded, separately judged occurrences and reports its remainder. Use `review decide` for those occurrence judgments; modernization suggestions never apply automatically. `--review-all` remains bounded. Visual-block findings are review candidates, not automatic defects or formatter instructions.

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
dotnet hygiene format --check
dotnet hygiene normalize --check
```

Use `review handoff` for negative or uncertain items that need the rule's designated reviewer. Its report is `escalation-required`; handed-off items remain outstanding until a reviewer decision returns. Explicit paths, `--changed`, `--base`, and `--review-all` are mutually exclusive. If gate context is ambiguous, provide a trusted base; the tool does not fetch or invent a remote. `format --check` and `normalize --check` plan changes without applying them. Autonomous formatting snapshots C# sources and hygiene-managed profile files before mutation. A formatter failure rolls those changes back; if a file changed since the last tool checkpoint, rollback preserves it and reports the conflict. Inspect a failed or incomplete outcome rather than treating it as a successful gate.

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
dotnet tool update --global DotNetAiCodeHygiene.Tool --version 0.11.0
dotnet hygiene --version
dotnet hygiene
```

For a tool installed with `--tool-path`, run `dotnet tool update --tool-path <tool-path> DotNetAiCodeHygiene.Tool --version <approved-version>` from the same trusted package source. Offline or unavailable release metadata is not evidence that the installation is current; follow the explicit next action in the lifecycle report. Do not use floating prerelease versions or arbitrary package feeds.

## Deferred Work

`--review-all` remains a bounded inspection request, not exhaustive authorization. No hosted CI/workflow, provider integration, or public package publication is part of this release.
