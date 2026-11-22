# Migration — Guide System v0.7.3 to v0.7.4

## Summary

v0.7.4 adds consumer-surface validation for distributable artifacts.

The guide now requires representative acceptance coverage that exercises the artifact produced by the current build through the mechanism real consumers use.

This migration does not add a project type, lifecycle phase, engineering command, or new execution artifact.

## Applicability

Migration is recommended when the repository produces or validates distributable artifacts such as:

- NuGet packages;
- `dotnet tool` packages;
- standalone published executables;
- other packaged/published artifacts whose consumer boundary differs from direct source-project execution.

Repositories with no distributable artifact surface require only guide-version metadata updates.

## Required review

Inspect current validation and milestone conventions for distributable surfaces.

A repository is already compliant when affected distributable artifacts have at least one required validation path that:

1. uses the artifact produced by the current build;
2. consumes it through the intended installation/package/publication mechanism;
3. exercises representative public behavior;
4. cannot accidentally pass by using a globally installed, stale, cached, or repository-internal substitute.

## NuGet libraries

Where a NuGet package is a supported consumer surface, keep detailed tests against source/internal projects as appropriate, but add or retain a bounded acceptance path that consumes the current packed package from an isolated consumer and exercises representative public API behavior.

Do not treat successful `dotnet pack` alone as consumer acceptance.

## dotnet tools

For a `dotnet tool`, required packaged-tool acceptance should normally:

```text
pack the current tool
-> install that exact package through dotnet tool
-> use an isolated tool path or local tool manifest
-> invoke the installed command/shim
-> verify representative behavior and exit semantics
```

At minimum, cover the applicable subset of:

- installation succeeds from the locally produced package;
- installed command/shim resolves;
- `--help`, `--version`, or equivalent basic invocation works;
- one representative successful command works through the installed tool;
- invalid/failure invocation has the expected exit semantics when public contract requires it;
- representative file/artifact I/O works when material to the tool;
- no repository-relative build path or unrelated global installation is required.

Do not move the full internal test suite to the installed-tool boundary. Keep packaged-tool acceptance small and representative.

## Standalone executables

Where a published executable is the distributable surface, required acceptance should invoke the current published/package artifact rather than only the project entry point or ordinary build output.

## Milestones

When planning a milestone that affects package metadata, installation behavior, tool command discovery/routing, startup composition, executable packaging, public command behavior, or dependencies that materially affect a distributable surface:

- mark consumer-surface validation as required;
- include an observable acceptance criterion for the distributable boundary;
- include the concrete package/install/invoke validation path;
- ensure the execution ledger maps that obligation to current artifact-level evidence.

Internal tests remain necessary where appropriate but are not sufficient evidence for this boundary.

## Existing milestones

Do not rewrite completed historical milestones solely for v0.7.4.

For an active or newly planned milestone that changes a distributable surface, add the missing consumer-surface validation if it is not already represented.

If an active implementation is otherwise complete but has only tested internal assemblies/project execution, treat the applicable consumer-surface check as remaining agent-resolvable work rather than claiming `COMPLETE`.

## Version metadata

Update guide-system/profile references from `0.7.3` to `0.7.4` where the repository records the applied guide version.
