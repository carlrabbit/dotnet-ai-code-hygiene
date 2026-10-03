# Milestone — M0001 CLI Foundation

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Baseline executor readiness | confirmed |
| Decision preservation | confirmed |
| Execution tractability | confirmed |
| Execution ledger | `.execution/M0001-cli-foundation.md` planning-seeded |
| Scope size | medium |
| Implementation autonomy | high |
| Documentation sync | deferred |
| Focused validation | Tier 1 TUnit + built CLI process checks |
| Repository validation | Tier 2 `./eng/validate.ps1` |
| Integration validation | not-applicable |
| Validation locus/platform | local Windows 11 with .NET 11 SDK and PowerShell |
| Consumer/release validation | Tier 4 deferred to M0003 |
| Human review | required at milestone completion |

## Goal

Create the implementation-ready repository skeleton for a public Windows-first .NET CLI product so the next milestone can implement the hygiene engine without revisiting project structure, CLI framework, process contract, test framework, local validation, or packaging fundamentals.

## Target State

The repository contains a minimal coherent .NET 11 solution with one CLI product project and one TUnit test project. The `hygiene` command exposes intentional help/version and reserved command discovery through `System.CommandLine`, process-level behavior is tested, local Windows validation is available through a stable PowerShell engineering entry point, and `dotnet pack` produces the current `.NET tool` NuGet package.

## Scope

- establish the `.NET 11` solution/project skeleton;
- use `System.CommandLine` for the supported CLI surface;
- use TUnit for automated tests;
- establish stable help/version, stdout/stderr, and exit-code behavior;
- reserve the agreed top-level command names and per-command help surface;
- establish process-boundary CLI tests;
- establish the local Windows engineering validation entry point;
- configure a locally packable .NET tool artifact with command name `hygiene`;
- keep documentation truthful about implemented versus planned capabilities.

## Non-goals

- no Roslyn hygiene-rule engine;
- no actual hygiene findings/review candidates;
- no run/finding/ignore implementation;
- no `.hygiene/decisions.json` or `.hygiene/config.json` behavior;
- no fingerprinting implementation;
- no target resolution/`--changed` implementation except parsing scaffolding if naturally required;
- no formatter implementation;
- no normalizer implementation;
- no installed-package consumer validation;
- no NuGet publication/release;
- no GitHub Actions/workflows;
- no reusable public .NET library/API surface;
- no cross-platform support claim.

## Decisions and Constraints

- Windows 11 is the authoritative M0001 execution and validation platform.
- Use the .NET 11 SDK line. The bootstrap SDK at planning time is `11.0.100-rc.1`; the repository may move within the .NET 11 line without architectural reconsideration.
- Use the SDK-default supported C# language version; do not hard-code an unsupported future language version.
- Use `System.CommandLine`; do not substitute another CLI parser framework.
- Use TUnit; do not substitute xUnit, NUnit, MSTest, or another test framework.
- Use one CLI project and one TUnit test project unless implementation discovers a concrete compile-time separation need. Do not pre-split speculative layers.
- The distributed command is `hygiene`.
- The CLI assembly/project is `DotNetAiCodeHygiene.Cli`.
- The local NuGet tool package ID is `DotNetAiCodeHygiene.Tool`.
- Initial development version is `0.1.0`.
- Reserved top-level commands are `format`, `normalize`, `check`, `explain`, `ignore`, `unignore`, `ignores`, and `rules`.
- Reserved subcommands need discoverable help in M0001; their product behavior remains unimplemented unless explicitly required by this milestone.
- Important supported operations are non-interactive.
- `stdout` is result output; `stderr` is diagnostics/progress/failure output.
- Public exit classes are fixed by `docs/SPECS.md`.
- `--help` and `--version` are intentional compatibility-sensitive CLI surfaces.
- No embedded AI/model dependency is added.
- No GitHub Actions or other repository-hosted workflow automation is added.
- M0001 package success is `dotnet pack`; installed-tool consumer validation is deliberately deferred to M0003.

## Baseline Executor Readiness

The project-wide decisions required for M0001 are preserved in the required authority documents below. The baseline executor may choose ordinary local implementation details such as exact compatible package patch versions, internal class names, test organization, and thin command-handler decomposition as long as they preserve the stated contract.

No unresolved architecture, semantics, compatibility, scope, validation-policy, or platform-support decision remains for M0001.

## Decision Preservation

If the planning conversation disappeared, the executor can recover:

- the product/CLI role from `docs/SPECS.md`;
- the intended architecture boundaries from `docs/ARCHITECTURE.md`;
- the Windows/.NET/TUnit/System.CommandLine/packaging engineering contract from `docs/ENGINEERING.md`;
- precise vocabulary from `docs/TERMINOLOGY.md`;
- public documentation truthfulness requirements from `docs/PUBLIC-DOCS.md`;
- milestone-specific scope and validation from this document.

## Execution Tractability

Planning seeds one lossless ledger row for every independently verifiable obligation below. Implementation owns work packages, implementation evidence, validation evidence, statuses, and resume state.

The execution ledger may compress work. It must not compress obligations.

## Required Authority

Implementation must read:

- `docs/SPECS.md`
- `docs/ARCHITECTURE.md`
- `docs/ENGINEERING.md`
- `docs/TERMINOLOGY.md`
- `docs/PUBLIC-DOCS.md`

Implementation does not need the external guide repository, planning conversation, `.guide-profile.json`, `.guide-sync/`, or non-authoritative research.

## Acceptance Criteria

- **AC-01** — The repository contains `DotNetAiCodeHygiene.slnx`, `global.json`, `Directory.Build.props`, `src/DotNetAiCodeHygiene.Cli/`, and `tests/DotNetAiCodeHygiene.Cli.Tests/`; the projects target `net11.0`, nullable reference types and implicit usings are enabled, and compiler warnings are treated as errors.
- **AC-02** — `global.json` selects the .NET 11 SDK line and permits the repository to move to an appropriate compatible .NET 11 SDK without requiring a project-architecture change.
- **AC-03** — The CLI uses `System.CommandLine` and exposes the distributed root command name `hygiene`.
- **AC-04** — Root help discovers all reserved top-level commands: `format`, `normalize`, `check`, `explain`, `ignore`, `unignore`, `ignores`, and `rules`; each reserved command has successful per-command help without claiming unimplemented domain behavior.
- **AC-05** — `hygiene --version` exits `0` and reports the current product/package version (`0.1.0` for M0001) through stdout.
- **AC-06** — Malformed or unknown invocation is mapped to public exit code `2`, diagnostics are written to stderr, and regular result stdout is not polluted by the diagnostic.
- **AC-07** — Representative successful help/version invocation exits `0`, and process-level tests demonstrate the documented stdout/stderr ownership rather than testing only command handlers.
- **AC-08** — The automated test project uses TUnit and no alternative test framework is introduced.
- **AC-09** — `eng/validate.ps1` is a thin Windows PowerShell engineering entry point that performs restore, build, test, and pack and fails when any required step fails.
- **AC-10** — `dotnet pack` produces the current `DotNetAiCodeHygiene.Tool` NuGet package configured as a .NET tool whose command name is `hygiene`.
- **AC-11** — No GitHub Actions workflow or other repository-hosted CI workflow is added.
- **AC-12** — The implementation adds no embedded AI/model dependency and does not implement M0002 hygiene-engine, persistence, fingerprinting, formatting, or normalization behavior beyond non-functional command scaffolding.
- **DOC-01** — `README.md` remains accurate after implementation: it states the product purpose, M0001 maturity, Windows-first support, .NET 11 requirement, command name, and does not imply that planned M0002/M0003 capabilities already exist.
- **DOC-02** — Project authority remains internally consistent with the implemented M0001 surface; any implementation-detail adjustment that materially changes a documented contract is reconciled through planning rather than silently rewriting project semantics.
- **REV-01** — A human completion review confirms that the M0001 evidence demonstrates the agreed CLI skeleton and packaging foundation without pulling M0002/M0003 behavior forward.

## Acceptance Evidence Topology

Only materially distinct process paths receive evidence cases.

| ID | Parent obligation | Required evidence case | Why separate evidence is required |
|---|---|---|---|
| EC-04a | AC-04 | root help lists every reserved command | Root discovery can work while individual command help is broken. |
| EC-04b | AC-04 | each reserved command returns successful help | Per-command parser/help construction is a distinct path. |
| EC-06a | AC-06 | unknown command | Unknown-command parsing may differ from malformed option/argument parsing. |
| EC-06b | AC-06 | malformed option/argument | Parser-framework defaults can leak different exit/stream behavior. |
| EC-07a | AC-07 | successful help process invocation | Help uses parser/help rendering path. |
| EC-07b | AC-07 | successful version process invocation | Version uses product-version reporting path. |
| EC-10a | AC-10 | pack produces the expected package identity | Packaging can succeed with the wrong package/tool metadata. |
| EC-10b | AC-10 | package metadata declares `hygiene` as tool command | Tool command exposure is a distinct consumer-facing package property. |

## Validation

| ID | Depth | Target | Locus/platform | Command/check | Proves | Expected evidence |
|---|---|---|---|---|---|---|
| VAL-01 | Tier 1 | CLI process contract | local Windows 11 + .NET 11 SDK | TUnit process-level CLI tests | EC-04a, EC-04b, AC-05, EC-06a, EC-06b, EC-07a, EC-07b | passing tests with asserted exit codes and stream contents |
| VAL-02 | Tier 1 | test framework/project contract | local Windows 11 + .NET 11 SDK | inspect project + execute `dotnet test` | AC-08 | TUnit project restores/discovers/runs successfully; no alternative framework packages |
| VAL-03 | Tier 2 | complete repository | local Windows 11 + .NET 11 SDK + PowerShell | `./eng/validate.ps1` | AC-01, AC-02, AC-03, AC-09, AC-11, AC-12, DOC-02 | restore/build/test/pack succeeds; repository inspection establishes negative constraints |
| VAL-04 | Tier 2 | local NuGet package artifact | local Windows 11 + .NET 11 SDK | inspect output from M0001 pack | EC-10a, EC-10b | current `.nupkg` has expected package ID/tool metadata and was produced by current validation |
| VAL-05 | Tier 2 | public repository documentation | local repository | compare implemented CLI/package surface to `README.md` | DOC-01 | README claims match live M0001 behavior and status |
| VAL-06 | human review | milestone completion evidence | human reviewer | inspect completion reconciliation + representative CLI/package evidence | REV-01 | explicit accepted review decision |

Tier 4 installed-tool consumer validation is not applicable to M0001 and is reserved for M0003.

## Completion Evidence

Implementation fills this section before `COMPLETE`.

| Obligation/evidence case | Concrete evidence | Validation gate/target | Result |
|---|---|---|---|
| AC-01 | `DotNetAiCodeHygiene.slnx`, `global.json`, `Directory.Build.props`, and both required project directories exist; both projects inherit `net11.0`, nullable and implicit usings; warnings are errors. | VAL-03; Windows 11, .NET 11 Release build | Pass |
| AC-02 | `global.json` selects the 11.0.100 RC baseline with `rollForward: latestFeature`; installed SDK selected was 11.0.100-rc.1.26425.128. | VAL-03; `dotnet --version`, Release build | Pass |
| AC-03 | CLI uses System.CommandLine 2.0.0; package tool settings expose the distributed command `hygiene`. | VAL-01, VAL-04 | Pass |
| AC-04 | Root help lists all eight reserved commands; each command help process exits 0 and makes no domain-behavior claim. | VAL-01; root and per-command process tests | Pass |
| AC-05 | Version process exits 0 and writes exactly `0.1.0` to stdout, with empty stderr. | VAL-01; version process test | Pass |
| AC-06 | Unknown command and malformed option each exit 2, write parse diagnostics to stderr, and leave stdout empty. | VAL-01; invalid-invocation process tests | Pass |
| AC-07 | Help and version process tests assert successful exit and stream ownership from built CLI child processes. | VAL-01; TUnit process suite | Pass |
| AC-08 | Test project references TUnit 1.72.16; no alternative test framework is referenced; MTP runner is selected for .NET 11 `dotnet test`. | VAL-02; project inspection and test run | Pass |
| AC-09 | `eng/validate.ps1` runs restore, Release build, Release test, and Release pack, checking every native exit code. | VAL-03; complete launcher run | Pass |
| AC-10 | Package `artifacts/packages/DotNetAiCodeHygiene.Tool.0.1.0.nupkg` has expected ID/version and DotnetTool package type; tool settings declare command `hygiene`. | VAL-04; current package nuspec and settings inspection | Pass |
| AC-11 | No `.github/workflows` directory or repository workflow YAML file exists. | VAL-03; repository file inspection | Pass |
| AC-12 | Added product code is limited to CLI scaffolding; no AI dependency, analysis engine, persistence, fingerprinting, formatter, or normalizer behavior is present. | VAL-03; source and dependency inspection | Pass |
| DOC-01 | README states product purpose, M0001 maturity, Windows 11/.NET 11, `hygiene`, help-only reserved commands, and capabilities deferred to later milestones. | VAL-05; README compared with live behavior and package | Pass |
| DOC-02 | Required authority documents remain consistent with command scaffolding, .NET tool packaging, process contract, TUnit/MTP validation, and deferred engine scope. | VAL-03; authority-to-implementation inspection | Pass |
| EC-04a | Root-help process test passed; all eight reserved command names appeared on stdout, exit 0, stderr empty. | VAL-01 | Pass |
| EC-04b | All eight per-command help process cases passed; each exited 0 and wrote help to stdout. | VAL-01 | Pass |
| EC-06a | Unknown-command process case returned 2, with diagnostic on stderr and stdout empty. | VAL-01 | Pass |
| EC-06b | Malformed-option process case returned 2, with diagnostic on stderr and stdout empty. | VAL-01 | Pass |
| EC-07a | Help process case returned 0, wrote help to stdout, and left stderr empty. | VAL-01 | Pass |
| EC-07b | Version process case returned 0, wrote `0.1.0` to stdout, and left stderr empty. | VAL-01 | Pass |
| EC-10a | Current package nuspec reports ID `DotNetAiCodeHygiene.Tool`, version `0.1.0`, type `DotnetTool`. | VAL-04 | Pass |
| EC-10b | Current package `tools/net11.0/any/DotnetToolSettings.xml` declares command `hygiene` with the CLI DLL entry point. | VAL-04 | Pass |
| REV-01 | Human reviewer decision for `REV-M0001-COMPLETION` has not yet been recorded. | VAL-06; project owner/delegate | Awaiting human review |

## Human Review

Review ID: `REV-M0001-COMPLETION`

Class: milestone completion review  
Applicability: required  
Reviewer: project owner or delegated human reviewer

Review subject:

- M0001 CLI/process contract evidence;
- repository skeleton;
- TUnit/System.CommandLine choices;
- Windows-local validation;
- generated .NET tool package metadata;
- confirmation that M0002/M0003 work was not prematurely implemented as accidental public contract.

Acceptance:

The reviewer confirms M0001 is a coherent minimal foundation and the durable completion evidence supports every obligation.

Waiver: none unless project authority is explicitly changed through planning.

Decision: pending. M0001 remains awaiting human review until the project owner or delegated reviewer accepts the completion evidence.
