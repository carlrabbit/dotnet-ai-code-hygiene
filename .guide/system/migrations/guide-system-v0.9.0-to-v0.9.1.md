# Migration — Guide System v0.9.0 to v0.9.1

## Purpose

Add the reusable `cli-tool` profile for repositories or repository scopes that expose a supported command-line product surface.

## Migration required

No repository migration is required merely to move from guide-system 0.9.0 to 0.9.1.

Apply the new profile only when the repository actually exposes a command-line executable as a first-class user or automation interface.

## Applicability

Consider `cli-tool` when the product has supported CLI/process behavior such as:

- commands, arguments, or options;
- stable automation entry points;
- stdout/stderr behavior;
- exit semantics;
- configuration/input precedence;
- path/filesystem behavior;
- structured output;
- installed/distributed command consumption;
- compatibility-sensitive help, version, diagnostics, or generated artifacts.

Do not apply `cli-tool` merely because the repository contains build scripts, developer utilities, or internal engineering commands.

## .NET tool projects

A .NET global/local tool should normally be modeled as:

```text
base
+ cli-tool
+ project-local specialization:
    implementation = .NET
    distribution = dotnet-tool
```

Do not add `dotnet-library` solely because `dotnet tool` packages use NuGet as their transport.

Add `dotnet-library` only when the repository also exposes a supported reusable library/API surface with library-specific package, API, compatibility, and validation obligations.

## Project authority to review

When adopting `cli-tool`, review whether project authority explicitly resolves the material parts of:

- command/subcommand and argument/option behavior;
- interactive versus non-interactive operation;
- stdout/stderr ownership;
- structured-output behavior;
- exit codes;
- input/configuration source precedence;
- relative-path anchoring and filesystem side effects;
- environment/process/cancellation behavior;
- diagnostics and secret handling;
- help/version behavior;
- compatibility-sensitive CLI surfaces;
- distribution/installation specialization;
- consumer-surface validation of the actual distributed CLI artifact.

Do not invent policies that are not relevant to the product.

## Validation

The profile does not require all detailed behavior to be tested through the packaged surface.

Use focused or direct integration tests for detailed behavior where appropriate, but preserve representative process-boundary validation.

When the CLI is distributed, validate the exact artifact real consumers receive through the intended install/extract/launch mechanism.

For a .NET tool, representative Tier 4 validation should install the current packed package through `dotnet tool` using an isolated tool path or local manifest and invoke the resulting command/shim.

## Guide metadata

Repositories adopting guide-system 0.9.1 should update recorded guide-system/profile versions to `0.9.1` according to their normal guide-update workflow.

If `cli-tool` applies, add it to `.guide-profile.json` with the appropriate repository/component/surface scope and record the concrete CLI/distribution decisions in normal project authority.

No guide-profile schema change is required.

## Completion

The update is complete when:

- guide metadata records 0.9.1 where applicable;
- `cli-tool` is selected only for genuine supported CLI product surfaces;
- concrete runtime/distribution choices remain project-local specialization;
- project authority contains the material CLI contract required by implementation and validation.
