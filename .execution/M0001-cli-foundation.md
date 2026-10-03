# Execution Ledger — M0001 CLI Foundation

Primary milestone: `docs/milestones/M0001-cli-foundation.md`

This file is operational implementation state. It is not project authority and does not amend the ready milestone.

Planning seeds the lossless milestone-obligation registry and required validation gates. Implementation owns work packages, concrete evidence, statuses, resume state, and final reconciliation.

The execution ledger may compress work. It must not compress obligations.

## Milestone Obligation Registry

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | The repository contains `DotNetAiCodeHygiene.slnx`, `global.json`, `Directory.Build.props`, `src/DotNetAiCodeHygiene.Cli/`, and `tests/DotNetAiCodeHygiene.Cli.Tests/`; the projects target `net11.0`, nullable reference types and implicit usings are enabled, and compiler warnings are treated as errors. | WP-01 | `DotNetAiCodeHygiene.slnx`; `Directory.Build.props`; CLI and TUnit project files declare the required layout/settings. | VAL-03: Release build succeeded on Windows 11 with .NET SDK 11.0.100-rc.1.26425.128. | done |
| AC-02 | acceptance | `global.json` selects the .NET 11 SDK line and permits the repository to move to an appropriate compatible .NET 11 SDK without requiring a project-architecture change. | WP-01 | `global.json` pins the .NET 11.0.100 RC baseline and uses `rollForward: latestFeature`; `dotnet --version` selected 11.0.100-rc.1.26425.128. | VAL-03: SDK selection and Release build succeeded on the declared Windows locus. | done |
| AC-03 | acceptance | The CLI uses `System.CommandLine` and exposes the distributed root command name `hygiene`. | WP-01, WP-02 | CLI references `System.CommandLine` 2.0.0; package settings embed command name `hygiene`. | VAL-01 and VAL-04: root process help ran; package metadata identifies `hygiene`. | done |
| AC-04 | acceptance | Root help discovers all reserved top-level commands: `format`, `normalize`, `check`, `explain`, `ignore`, `unignore`, `ignores`, and `rules`; each reserved command has successful per-command help without claiming unimplemented domain behavior. | WP-02 | CLI creates all eight reserved commands with help-only descriptions. | VAL-01: root and eight command help process tests passed. | done |
| AC-05 | acceptance | `hygiene --version` exits `0` and reports the current product/package version (`0.1.0` for M0001) through stdout. | WP-01, WP-02 | CLI package version and informational version are `0.1.0`; source revision suffix is disabled. | VAL-01: process test asserted exit 0, stdout exactly `0.1.0`, and empty stderr. | done |
| AC-06 | acceptance | Malformed or unknown invocation is mapped to public exit code `2`, diagnostics are written to stderr, and regular result stdout is not polluted by the diagnostic. | WP-02 | CLI checks parse errors before invocation, writes each diagnostic to stderr, and returns 2. | VAL-01: unknown command and malformed option process tests asserted code 2, diagnostic on stderr, empty stdout. | done |
| AC-07 | acceptance | Representative successful help/version invocation exits `0`, and process-level tests demonstrate the documented stdout/stderr ownership rather than testing only command handlers. | WP-02 | `CliProcessTests.cs` starts the built CLI as a child process with redirected streams. | VAL-01: help and version process tests passed with exit 0, expected stdout, empty stderr. | done |
| AC-08 | acceptance | The automated test project uses TUnit and no alternative test framework is introduced. | WP-01, WP-02 | Test project references TUnit 1.72.16 and no other test framework; `global.json` selects Microsoft.Testing.Platform for `dotnet test`. | VAL-02: project inspection and `dotnet test` succeeded; 12 tests passed. | done |
| AC-09 | acceptance | `eng/validate.ps1` is a thin Windows PowerShell engineering entry point that performs restore, build, test, and pack and fails when any required step fails. | WP-03 | Script runs restore, Release build, Release test, and Release pack, checking `$LASTEXITCODE` after each command. | VAL-03: `./eng/validate.ps1` completed all four steps successfully. | done |
| AC-10 | acceptance | `dotnet pack` produces the current `DotNetAiCodeHygiene.Tool` NuGet package configured as a .NET tool whose command name is `hygiene`. | WP-01, WP-03 | Current package: `artifacts/packages/DotNetAiCodeHygiene.Tool.0.1.0.nupkg`; nuspec identity/version/type and embedded tool settings inspected. | VAL-04: package metadata reports `DotnetTool`, ID/version `DotNetAiCodeHygiene.Tool`/`0.1.0`, command `hygiene`, entry point `DotNetAiCodeHygiene.Cli.dll`. | done |
| AC-11 | acceptance | No GitHub Actions workflow or other repository-hosted CI workflow is added. | WP-03 | Repository file inspection found no `.github/workflows` or workflow YAML files. | VAL-03: negative constraint inspection completed. | done |
| AC-12 | acceptance | The implementation adds no embedded AI/model dependency and does not implement M0002 hygiene-engine, persistence, fingerprinting, formatting, or normalization behavior beyond non-functional command scaffolding. | WP-02, WP-03 | Only CLI command/help scaffolding and process tests were added; dependencies are System.CommandLine and TUnit only. | VAL-03: source/project inspection confirmed no engine, persistence, fingerprinting, formatter, normalizer, or AI/model implementation. | done |
| DOC-01 | documentation | `README.md` remains accurate after implementation: it states the product purpose, M0001 maturity, Windows-first support, .NET 11 requirement, command name, and does not imply that planned M0002/M0003 capabilities already exist. | WP-03 | `README.md` states current maturity, Windows 11, .NET 11, `hygiene`, help-only reserved commands, and deferred package consumer validation. | VAL-05: README compared with process/package evidence and milestone; claims match. | done |
| DOC-02 | documentation | Project authority remains internally consistent with the implemented M0001 surface; any implementation-detail adjustment that materially changes a documented contract is reconciled through planning rather than silently rewriting project semantics. | WP-03 | Required authority documents remain unchanged; implementation decisions (MTP runner selection and parse-error mapping) preserve stated contracts. | VAL-03: authority-to-implementation inspection found no semantic contract changes. | done |
| REV-01 | review | A human completion review confirms that the M0001 evidence demonstrates the agreed CLI skeleton and packaging foundation without pulling M0002/M0003 behavior forward. | WP-03 | Project owner approved `REV-M0001-COMPLETION` on 2026-10-03 after reviewing the milestone reconciliation. | VAL-06: explicit project-owner approval received in conversation on 2026-10-03. | done |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-04a | AC-04 | Root help lists every reserved command. | VAL-01 | `RootHelpListsAllReservedCommands` launched CLI process; exit 0, all eight command names in stdout, stderr empty. | done |
| EC-04b | AC-04 | Each reserved command returns successful help. | VAL-01 | Eight `ReservedCommandHelpSucceeds` process cases passed; each exit 0, command help on stdout, stderr empty. | done |
| EC-06a | AC-06 | Unknown command maps to the stable invalid-invocation contract. | VAL-01 | `UnknownCommandUsesInvalidInvocationContract`: `not-a-command` returned 2, diagnostic on stderr, stdout empty. | done |
| EC-06b | AC-06 | Malformed option/argument maps to the stable invalid-invocation contract. | VAL-01 | `MalformedOptionUsesInvalidInvocationContract`: `--not-a-real-option` returned 2, diagnostic on stderr, stdout empty. | done |
| EC-07a | AC-07 | Successful help process invocation proves exit/stdout/stderr behavior. | VAL-01 | `RootHelpListsAllReservedCommands`: process exit 0; help on stdout; stderr empty. | done |
| EC-07b | AC-07 | Successful version process invocation proves exit/stdout/stderr behavior. | VAL-01 | `VersionUsesProductVersionOnStandardOutput`: process exit 0; exactly `0.1.0` on stdout; stderr empty. | done |
| EC-10a | AC-10 | Pack produces the expected package identity. | VAL-04 | Current archive nuspec identity is `DotNetAiCodeHygiene.Tool`, version `0.1.0`, package type `DotnetTool`. | done |
| EC-10b | AC-10 | Package metadata declares `hygiene` as tool command. | VAL-04 | Current archive `tools/net11.0/any/DotnetToolSettings.xml` declares command `hygiene` with CLI DLL entry point. | done |

## Work Packages

| ID | Work package | Status | Evidence |
|---|---|---|---|
| WP-01 | Establish .NET 11 solution, CLI/TUnit projects, SDK configuration, and .NET tool package metadata. | done | Root build files and project files; VAL-02/03/04. |
| WP-02 | Implement System.CommandLine root/help/version and reserved help-only commands; establish process-boundary exit/stream tests. | done | `Program.cs`, `CliProcessTests.cs`; VAL-01. |
| WP-03 | Add truthful README, local PowerShell validation launcher, ignore build outputs, inspect negative constraints, and reconcile evidence. | done | `README.md`, `eng/validate.ps1`, `.gitignore`, this ledger and milestone; VAL-03/05. |

## Validation Gates

| ID | Required validation | Target/locus | Proves evidence units | Status | Evidence |
|---|---|---|---|---|---|
| VAL-01 | TUnit process-level CLI tests | built CLI process / local Windows 11 + .NET 11 SDK | EC-04a, EC-04b, AC-05, EC-06a, EC-06b, EC-07a, EC-07b | pass | `dotnet test` via MTP: 12 passed, 0 failed; Release process invocations asserted exit/stdout/stderr. |
| VAL-02 | inspect test project and execute `dotnet test` | repository / local Windows 11 + .NET 11 SDK | AC-08 | pass | `TUnit` 1.72.16 only test framework; full `dotnet test` succeeded. |
| VAL-03 | `./eng/validate.ps1` plus repository inspection for negative constraints | complete repository / local Windows 11 + .NET 11 SDK + PowerShell | AC-01, AC-02, AC-03, AC-09, AC-11, AC-12, DOC-02 | pass | Windows 11 build `10.0.26200`; SDK `11.0.100-rc.1.26425.128`; restore/build/test/pack succeeded; repository/source/dependency/workflow inspection recorded above. |
| VAL-04 | inspect current M0001 `.nupkg` output | package artifact / local Windows 11 + .NET 11 SDK | EC-10a, EC-10b | pass | `artifacts/packages/DotNetAiCodeHygiene.Tool.0.1.0.nupkg`; nuspec and `DotnetToolSettings.xml` inspected; ID/version/type/command match. |
| VAL-05 | compare implemented surface with README | public documentation / local repository | DOC-01 | pass | README claims compared against CLI tests, package metadata, and M0001 scope. |
| VAL-06 | human completion review | completion evidence / human reviewer | REV-01 | pass | Project owner explicitly approved `REV-M0001-COMPLETION` on 2026-10-03 after review of the evidence summary. |

## Resume Point

Last completed work package: WP-03

Current work package: none

Next concrete action: none; M0001 implementation and required review are complete.

Known agent-resolvable gaps: none.

External blockers or planning escalations: none.

## Final Reconciliation

Before `COMPLETE`:

- [x] reread `docs/milestones/M0001-cli-foundation.md`;
- [x] enumerate all applicable milestone obligation IDs;
- [x] verify exact set equality with this obligation registry (15 IDs on each side);
- [x] verify no obligation was merged, deleted, renumbered, or replaced; seeded ID/type/obligation text was compared with the initialization archive;
- [x] enumerate all evidence-case IDs and verify exact set equality with the milestone (8 IDs on each side);
- [x] reconcile every obligation with concrete implementation evidence;
- [x] reconcile every evidence case with concrete validation evidence;
- [x] verify each gate actually exercises the behavior it claims to prove;
- [x] confirm every required gate has current evidence from the declared Windows 11 locus;
- [x] confirm no agent-resolvable gap remains;
- [x] obtain the required human review; project owner approved `REV-M0001-COMPLETION` on 2026-10-03;
- [x] write the compact durable completion reconciliation into the milestone before this ledger is eligible for cleanup.
