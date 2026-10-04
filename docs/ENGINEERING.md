# Engineering

## Development baseline

```text
Operating system: Windows 11
SDK line: .NET 11
Language version: SDK default
Shell for repository engineering launchers: PowerShell
```

Do not claim Linux or macOS support until representative process-level validation exists there.

## Required technologies

- `System.CommandLine` is the required CLI parser/command library.
- TUnit is the required test framework.
- Roslyn compiler/workspace APIs are the required C# analysis foundation.
- BCL `System.Security.Cryptography.SHA256` is the fingerprint hash primitive.
- Git is a required environment dependency only for `--changed`.

Exact compatible package patch versions remain implementation-maintained unless compatibility evidence requires stronger pinning.

## Repository shape

M0002 may extend the repository to:

```text
DotNetAiCodeHygiene.slnx
src/
  DotNetAiCodeHygiene.Cli/
  DotNetAiCodeHygiene.Core/
tests/
  DotNetAiCodeHygiene.Cli.Tests/
  DotNetAiCodeHygiene.Core.Tests/
eng/
  validate.ps1
```

Do not create additional speculative layers/projects.

`Core` is an internal implementation assembly, not a supported public library.

## Build quality

All project code keeps:

```text
TargetFramework=net11.0
Nullable=enable
ImplicitUsings=enable
TreatWarningsAsErrors=true
```

## Test strategy

The project is process-boundary/integration-first for public CLI compatibility and uses focused Core tests where more exhaustive/diagnostic.

M0002 needs both:

- Core tests for rule semantics, targeting, fingerprinting, matching, persistence, and ordering;
- CLI process tests for invocation, streams, exits, text/JSON, target modes, rules, explain, ignore, unignore, and ignores.

A Core test does not replace process-boundary evidence for public CLI behavior.

## Test fixtures

Repository discovery, Git `--changed`, project association, persistence, and path semantics use isolated temporary fixture repositories rather than the real development repository.

Fixture tests may initialize local Git repositories.

No network service is required.

## Engineering command

```powershell
./eng/validate.ps1
```

remains the complete local repository validation entry point and includes M0002 restore/build/test/pack validation.

The PowerShell launcher remains thin.

## Validation topology

| Depth | Target | Locus | Platform/capability | Command | Evidence |
|---|---|---|---|---|---|
| Tier 1 | Core rule/identity/persistence tests | local | Windows 11 + .NET 11 SDK | focused `dotnet test` | deterministic Core evidence |
| Tier 1 | built CLI process | local | Windows 11 + .NET 11 SDK | CLI TUnit process tests | invocation/stream/exit/output evidence |
| Tier 2 | complete repository | local | Windows 11 + .NET 11 SDK + PowerShell + Git | `./eng/validate.ps1` | restore/build/test/pack |
| Tier 3 | isolated temporary .NET/Git fixture repositories | local | Windows 11 + .NET 11 SDK + Git | TUnit integration scenarios | real repository/project/Git/persistence behavior |
| Tier 4 | installed `.NET tool` artifact | deferred to M0003 | Windows 11 | later milestone | later evidence |

## Process isolation

CLI integration tests invoke the built CLI process, not command handlers.

Fixture repositories isolate `.git`, `.hygiene`, sources, `.csproj`, and changed/untracked state.

Tests must not rely on global Git identity/configuration where fixture-local configuration can be used.

## Persistence validation

Tests directly prove:

- first-write creation;
- atomic replacement;
- malformed committed state rejection;
- conflicting external modification rejection;
- cancellation does not commit partial state;
- latest-run state is replaceable by a new successful check.

Lower-level fault injection may prove race/cancellation mechanics when a process-level race would be unreliable, provided public behavior is also represented where practical.

## GitHub automation

Do not add `.github/workflows/` or other repository-hosted CI/CD workflows.

## Documentation boundary

Project authority lives in `docs/` and active milestones.

No research layer is required for M0002; implementation-affecting conclusions are already promoted into project authority.
