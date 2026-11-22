# CLI Tool Engineering Profile

Apply this profile when a command-line executable is a supported product or consumer surface.

The reusable contract is the CLI/process shape, not a particular language, parser framework, operating system, or package mechanism.

## Engineering expectations

Project-local authority should resolve the material command/process behavior rather than allowing framework defaults to become accidental public contracts.

### Command and invocation contract

Define, as applicable:

- command and subcommand hierarchy;
- argument/option semantics;
- required/optional/repeated values;
- unknown or malformed invocation behavior;
- interactive versus non-interactive behavior;
- confirmation semantics for destructive operations;
- stable automation entry points.

Important operations intended for automation should not require an interactive terminal unless that limitation is an explicit product constraint.

### Standard streams and structured output

Define what belongs on:

```text
stdout
stderr
```

When machine-readable output is supported:

- define its activation mechanism;
- keep diagnostics/progress from corrupting the machine-readable stream;
- define compatibility expectations for the structured representation;
- make redirection/piping behavior representative validation targets where material.

Human-readable console formatting does not automatically become a compatibility contract unless project authority says it does.

### Exit semantics

Define stable exit behavior where callers or scripts depend on it.

At minimum, distinguish successful completion from failure. When the project exposes multiple stable failure classes, document them rather than leaking arbitrary internal/process exit values.

Do not allow exception type, parser-library default, or incidental implementation path to decide compatibility-sensitive exit semantics accidentally.

### Configuration and source precedence

If the CLI accepts the same setting from multiple sources, project authority should define precedence explicitly.

Example decision surface:

```text
explicit command option
> environment/configuration override
> project/user configuration
> default
```

The exact order is project-specific.

Treat secrets separately from ordinary configuration. Diagnostics, structured output, logs, and failure dumps must not expose secret values.

### Path and filesystem semantics

Path resolution is part of the CLI contract when consumers can observe it.

Define the anchor for relative paths and apply it consistently.

Where the product writes files, resolve as applicable:

- overwrite behavior;
- atomic versus incremental writes;
- directory creation;
- temporary files;
- cleanup after failure/cancellation;
- partial output semantics;
- deterministic/stable generated names;
- behavior across supported platforms.

### Environment, terminal, process, and cancellation behavior

Resolve behavior that materially depends on process context, including:

- current working directory;
- environment variables;
- terminal attached versus redirected execution;
- child-process invocation;
- cancellation/interrupt signals;
- timeouts;
- platform-specific process behavior.

Do not claim cross-platform support from compilation alone. For a milestone affecting platform-sensitive behavior, test the relevant process path on a representative real target when the difference matters; qualifying the **full declared platform support** belongs to release readiness unless deferral carries material risk.

### Diagnostics

Diagnostics should distinguish, where useful:

- invalid invocation;
- invalid input/configuration;
- expected domain/product failure;
- dependency/environment failure;
- unexpected internal failure.

The public diagnostic surface should be stable only to the degree declared by project authority.

When diagnostics have machine-consumed identifiers or structured forms, treat those identifiers/forms as compatibility-sensitive public surface.

### Help and version surface

Treat `--help`, `--version`, or equivalent command-discovery surfaces as intentional product behavior when exposed.

Help should make the supported command surface discoverable without requiring source-code inspection.

Version reporting should identify the distributed product/version according to project release authority rather than an incidental development assembly or stale globally installed command.

## Validation

When a milestone changes observable CLI/process behavior, representative validation should exercise the real process boundary, not only command-handler/library methods. Purely internal CLI refactoring may rely on existing process coverage plus focused tests when that gives credible confidence.

Use lower-level tests where they are materially cheaper, more exhaustive, or more diagnostic, but they do not replace representative command-level behavior.

Representative validation should cover the applicable contract, such as:

- successful command invocation;
- malformed/unknown invocation;
- exit codes;
- stdout/stderr routing;
- structured-output isolation;
- stdin or redirected execution when supported;
- configuration/source precedence;
- relative-path behavior;
- filesystem side effects;
- cancellation or timeout behavior when material;
- representative failure diagnostics.

Use a few representative process-boundary scenarios chosen for material behavior risks. Separate evidence-case registries are not a default; add case distinctions only when they improve confidence enough to justify their cost.

## Consumer/distribution validation

At **release readiness**, when the CLI is distributed, validation **must** invoke the actual candidate artifact through each actively supported installation/launch mechanism, unless project release authority explicitly excludes a mechanism with justification. An ordinary implementation milestone does not automatically qualify the installed tool. Earlier qualification requires explicit scope or a material risk-based justification.

The generic shape is:

```text
build/package current CLI artifact
-> install/extract/resolve through intended consumer mechanism
-> invoke the resulting command/executable
-> exercise representative public behavior and exit semantics
```

The validation must avoid accidental success through repository build output, stale packages, globally installed commands, unrelated PATH entries, or developer-machine state outside the declared contract.

### .NET tool specialization

For a project distributed as a .NET global/local tool, project-local authority should resolve the concrete tool packaging details, including the supported command name and installation model.

At release readiness, representative consumer validation must:

```text
pack the current tool package
-> install that exact package through dotnet tool
-> use an isolated tool path or local tool manifest
-> invoke the generated command/shim
-> exercise representative CLI behavior
```

A NuGet transport mechanism alone does not make the product a reusable .NET library. Compose with `dotnet-library` only when a separate supported library/API surface actually exists.

## Public documentation

For externally consumed CLIs, documentation should normally include:

- installation/distribution instructions;
- command discovery/help entry point;
- representative copy-pasteable invocations;
- input/output examples where useful;
- exit/error behavior relevant to automation;
- configuration/environment behavior that users must know;
- compatibility-sensitive structured output or generated artifacts.

Where practical, validate public examples against the current CLI artifact rather than allowing examples to drift independently.

## Release considerations

Release readiness should consider the surfaces callers actually consume:

- distributed executable/package;
- command discovery;
- command names/options;
- stable exit semantics;
- structured output;
- configuration compatibility;
- generated files/artifacts;
- documented examples.

Internal unit/integration success does not establish release readiness if the installed/distributed CLI surface has not been exercised.
