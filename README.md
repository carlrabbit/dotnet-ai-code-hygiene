# dotnet-ai-code-hygiene

AI-first code hygiene tooling for deterministic formatting, normalization, opinionated hygiene analysis, and agent-oriented remediation workflows. M0001 currently provides the command-line foundation; those product capabilities are planned and are not implemented yet.

## Status

`M0001 — CLI Foundation` provides a .NET CLI skeleton, local validation, and a packable .NET tool package. The hygiene engine vertical slice follows in M0002. The reserved commands currently expose help only and do not analyze or change source code.

## Product direction

The tool is designed for coding agents and humans:

```text
generated or edited code
-> deterministic formatting
-> safe normalization
-> deterministic hygiene checks
-> narrowly scoped findings/review candidates
-> remediation by the calling agent or human
```

The hygiene engine does not require an embedded AI model for ordinary detection.

## Initial platform

- Windows 11 is the first supported development and validation platform.
- The project follows the .NET 11 SDK line.
- .NET 11 SDK must be installed to build and run the project.
- No GitHub Actions or repository-hosted CI workflows are used.
- Local validation is authoritative for the initial milestones.

## Command

The distributed command name is:

```text
hygiene
```

M0001 establishes command discovery and the reserved top-level command surface. Functional hygiene analysis is introduced by later milestones.

The M0001 package can be produced locally with `dotnet pack`; installed-tool consumer validation and installation guidance are planned for M0003.

See `docs/SPECS.md`, `docs/ENGINEERING.md`, `docs/ARCHITECTURE.md`, and the active milestone for authoritative project details.
