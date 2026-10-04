# Engineering

## Baseline

Windows 11; .NET 11 SDK line; SDK-default C#; PowerShell; `System.CommandLine`; TUnit; Roslyn. No GitHub Actions/workflows.

## Rewrite tests

Focused tests must prove target de-duplication, format boundary/idempotence, normalize pre-compilation rejection, each fixed transformation, unsafe simplification no-op, post-compilation validation, normalize auto-formatting/idempotence, `--check` equivalence/non-mutation, successful transactional multi-file rewrite, external-modification conflict, and cancellation/fault rollback.

Use isolated SDK-style fixture repositories and real Roslyn/MSBuild project loading.

## Tier 4

Required consumer validation:

```text
dotnet pack
-> isolated package-source directory
-> dotnet tool install --tool-path <isolated> --add-source <source> <package-id> --version 0.4.0
-> isolated SDK-style Git repo
-> installed hygiene --version
-> installed hygiene help --agent
-> format --check + mutation
-> normalize --check + mutation
-> check
```

The installed command must be invoked, not a development DLL. No network package source is required for the package under test.

`./eng/validate.ps1` remains the canonical thin complete validation entry point and must include/invoke Tier 4.
