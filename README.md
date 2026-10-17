# dotnet-ai-code-hygiene

`DotNetAiCodeHygiene.Tool` is a model-independent .NET hygiene tool for coding agents. Its supported command is `dotnet hygiene`; the executable does not call an AI model or provider. The current package version is `0.10.0`. Supported consumers use Windows 11 and the .NET 11 SDK line.

The `0.10.0` package is validated from a local feed; public-feed publication is not claimed. Install from a trusted NuGet source after the package is available there:

```powershell
dotnet tool install --global DotNetAiCodeHygiene.Tool --version 0.10.0
dotnet hygiene --version
```

## First Bootstrap

In a Git repository, ask an implementation agent to run `dotnet hygiene` and follow the emitted steps. On first use, the tool assesses existing project formatting without changing source, installs the `dotnet-11` v2 profile, and formats eligible repository C# sources. It writes a baseline report under `.hygiene/reports/` with changed paths and observed validation. The report remains **approval-required** until an authorized external approver reviews and accepts the baseline. The tool cannot determine who has that authority.

Review the baseline report and source diff before accepting it. Bootstrap preserves content outside the profile-owned sections in `.editorconfig` and `Directory.Build.props`; malformed or incompatible profile state requires a safe manual decision. The pre-hygiene formatter assessment reports current drift; it does not infer whether a formatter was previously run.

```powershell
dotnet hygiene --output json
dotnet hygiene bootstrap --output json  # manual bootstrap/diagnostics
```

## Run on Gates

After the baseline is approved, run `dotnet hygiene` from the repository. It applies enabled deterministic normalization and formatting, infers a trusted local gate where possible, and reports deterministic findings plus selected semantic-review work grouped by source file. Follow only the selected work order. Inspect each rule's questions independently; remediation or acceptance for one rule does not satisfy another.

```powershell
dotnet hygiene
dotnet hygiene check --base origin/main --output json
dotnet hygiene review accept <batch-handle> <item-id>...
dotnet hygiene review accept <batch-handle> --all
```

Accept only items whose fixed questions you inspected and answered positively. Leave negative or uncertain items unaccepted and use the rule's escalation guidance. Rerun `dotnet hygiene` after source changes. `complete` means the selected gate obligations were processed; it does not certify the full repository population as defect-free. A successful command execution is not itself a complete gate.

The six semantic-review rules keep independent, bounded gate-local selection. A rule may select up to five items; `--review-all` remains bounded. Visual-block findings are review candidates, not automatic defects or formatter instructions.

## Manual Inspection and Debugging

The normal workflow needs no subcommand knowledge. These nonmutating diagnostics are available when investigating a result:

```powershell
dotnet hygiene check src/Example.cs
dotnet hygiene check --changed --output json
dotnet hygiene check --base origin/main --output json
dotnet hygiene check --review-all --output json
dotnet hygiene rules --output json
dotnet hygiene explain <finding-handle>
dotnet hygiene review expand <batch-handle>
dotnet hygiene review handoff <batch-handle>
dotnet hygiene format --check
dotnet hygiene normalize --check
```

Explicit paths, `--changed`, `--base`, and `--review-all` are mutually exclusive. If gate context is ambiguous, provide a trusted base; the tool does not fetch or invent a remote. `format --check` and `normalize --check` plan changes without applying them. Profile setup and source rewrites are planned and validated before their transactional writes; inspect a failed or incomplete outcome rather than treating it as a successful gate.

## Updating to a New Release

Use only a configured trusted NuGet source. When a release is approved, update out of process and rerun the installed entrypoint so it can reconcile the repository profile and request approval for a material policy change:

```powershell
dotnet tool update --global DotNetAiCodeHygiene.Tool --version 0.10.0
dotnet hygiene --version
dotnet hygiene
```

For a tool installed with `--tool-path`, run `dotnet tool update --tool-path <tool-path> DotNetAiCodeHygiene.Tool --version <approved-version>` from the same trusted package source. Offline or unavailable release metadata is not evidence that the installation is current; follow the explicit next action in the lifecycle report. Do not use floating prerelease versions or arbitrary package feeds.

## Deferred Work

`dotnet hygiene --yolo-all` is deferred to M0019 and is not available in 0.10.0. `--review-all` is a bounded inspection request, not exhaustive authorization. No hosted CI/workflow, provider integration, or public package publication is part of this release.
