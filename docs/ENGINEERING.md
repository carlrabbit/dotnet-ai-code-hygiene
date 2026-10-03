# Engineering

## Development baseline

Authoritative initial development/validation environment:

```text
Operating system: Windows 11
SDK line: .NET 11
Current bootstrap SDK: 11.0.100-rc.1
Language version: SDK default
Shell for repository engineering launchers: PowerShell
```

The SDK is expected to move within the .NET 11 line, including to GA, without changing the architectural contract.

Do not claim Linux or macOS support until representative process-level validation exists there.

## Required technologies

- `System.CommandLine` is the required CLI parser/command library.
- TUnit is the required test framework.
- Roslyn compiler/workspace APIs are the intended foundation for C# analysis and formatting.
- BCL `System.Security.Cryptography.SHA256` is the required fingerprint hash primitive when M0002 implements persistence matching.

Exact compatible package patch versions are implementation-maintained dependencies, not product semantics unless a future compatibility constraint requires pinning them.

## Repository shape

M0001 should establish a conventional small .NET layout:

```text
DotNetAiCodeHygiene.slnx
global.json
Directory.Build.props
src/
  DotNetAiCodeHygiene.Cli/
tests/
  DotNetAiCodeHygiene.Cli.Tests/
eng/
  validate.ps1
```

Additional projects require a concrete architectural reason; do not split into speculative layers during M0001.

## Build quality

All project code uses:

```text
Nullable=enable
ImplicitUsings=enable
TreatWarningsAsErrors=true
```

The CLI targets `net11.0`.

## Test strategy

The project is process-boundary/integration-first for compatibility-sensitive CLI behavior.

TUnit tests may also exercise lower-level code when doing so is materially cheaper, more exhaustive, or more diagnostic.

A passing handler/unit test does not replace representative process invocation for:

- parsing;
- help/version;
- exit codes;
- stdout/stderr;
- path/process behavior.

Do not create unit-test requirements merely to satisfy a generic test pyramid.

## Engineering command

`eng/` is the stable repository engineering entry point.

For M0001:

```powershell
./eng/validate.ps1
```

must run the complete local milestone validation needed by ordinary development:

```text
restore
build
test
pack
```

The PowerShell launcher must remain thin. If orchestration becomes complex, move semantics into tested application/tooling code rather than growing shell logic.

## Validation topology

| Depth | Target | Locus | Platform/capability | Command | Evidence |
|---|---|---|---|---|---|
| Tier 1 | focused .NET tests / CLI process tests | local | Windows 11 + .NET 11 SDK | `dotnet test` with focused filter/target as appropriate | focused TUnit results |
| Tier 2 | repository build/test/package | local | Windows 11 + .NET 11 SDK + PowerShell | `./eng/validate.ps1` | successful restore/build/test/pack and produced local package |
| Tier 3 | external integration target | not applicable in M0001 | — | — | — |
| Tier 4 | installed .NET tool consumer surface | deferred to M0003 | Windows 11 | later milestone contract | later evidence |

## CLI process contract

M0001 validation must execute the built CLI process and verify representative:

- successful `--help`;
- successful `--version`;
- per-command help;
- malformed/unknown invocation;
- stable exit mapping;
- stdout/stderr ownership.

M0001 does not need to prove analysis, ignore persistence, formatting, or packaged-tool installation.

## Package production

M0001 configures the CLI project as a .NET tool package:

```text
PackageId: DotNetAiCodeHygiene.Tool
ToolCommandName: hygiene
initial development version: 0.1.0
```

`dotnet pack` must produce the current package locally.

Successful packing is not consumer-surface acceptance. M0003 must install the exact current package through `dotnet tool` into an isolated tool path or local manifest and invoke the generated command/shim.

## GitHub automation

Do not add:

```text
.github/workflows/
```

or other repository-hosted CI/CD workflow definitions.

Local Windows validation is authoritative for the initial milestones.

## Cancellation and writes

M0001 should pass `CancellationToken` through the CLI/application boundary where practical so future long-running analysis can cooperate with Ctrl+C.

M0001 has no product-state writes beyond normal build/package outputs. Atomic decision-store writes belong to M0002.

## Documentation boundary

Project authority lives in `docs/` and the active milestone.

External guide-system documents are planning/migration/synchronization inputs only and are not runtime implementation authority.

No research layer is active at initialization because the operative pre-project conclusions are promoted directly into project authority and no costly evidence corpus needs preservation.
