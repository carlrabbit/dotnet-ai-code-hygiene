# Source Generator Engineering Profile

Apply this profile to repository scopes that ship Roslyn source generators or analyzer-style compile-time extensions as a first-class consumer surface.

## Engineering expectations

Project-local engineering authority should resolve, as applicable:

- supported compiler/Roslyn/MSBuild host expectations;
- generator/analyzer package layout and consumption mechanism;
- generated-source determinism and stability requirements;
- public diagnostic IDs/descriptors and compatibility policy;
- configuration inputs and precedence;
- failure behavior for invalid source/configuration;
- generated API/source compatibility requirements;
- interaction with runtime companion packages;
- package-version alignment when generator and runtime packages are released as one suite.

Do not assume every project in a mixed repository is a source generator. Scope these expectations to the applicable components/surfaces.

## Validation

Representative validation should exercise the real compile-time consumption boundary rather than only invoking generator implementation types directly.

Useful validation targets include:

- a compiler/Roslyn driver using representative source input;
- an MSBuild project consuming the generator through the supported reference mechanism;
- at release readiness, the current packed NuGet artifact consumed by an isolated project when distributed through NuGet;
- generated output compiled together with representative consumer code;
- diagnostic behavior observed through the supported compilation surface.

Detailed generator logic may also use focused/internal tests where they are cheaper or more diagnostic. Those tests do not replace representative compile-time consumer validation.

A NuGet-delivered compile-time generator/analyzer is not automatically a reusable .NET library/API surface. Compose this profile with `dotnet-library` only when a separately supported .NET library/API also exists in the applicable scope; keep generator packaging and compile-time consumption rules here.

## Packaging

At release qualification, validate the actual package mechanism used by consumers. Ordinary milestones may exercise representative compile-time behavior directly through Roslyn/MSBuild without packing the final artifact unless packaging is in scope or materially risky to defer. Source-generator/analyzer packages often have packaging semantics different from ordinary runtime libraries; those concrete layout rules belong in project-local engineering authority.

When consumer-surface qualification is performed, use the actual candidate artifact and avoid accidental success through project references, stale package caches, globally installed tools, or repository-internal substitutes.

## Public surface

Treat generated source shape, documented configuration, diagnostics, and package consumption behavior as public compatibility surfaces when the project exposes them as supported contracts.
