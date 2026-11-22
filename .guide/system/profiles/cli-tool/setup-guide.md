# CLI Tool Setup Profile

Use this profile when a repository or bounded repository scope ships a command-line executable as a first-class user or automation surface.

The profile is technology-neutral. A .NET global/local tool, standalone executable, Node CLI, native CLI, or similar product may use the same reusable CLI shape while keeping concrete packaging/runtime rules in project-local specialization.

Do not apply this profile merely because a repository has internal developer scripts or build commands. Apply it when the command-line surface is itself a supported product or consumer interface.

## Common active layers

Common project authority includes:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ENGINEERING.md
docs/MILESTONES.md
```

When the CLI is public or externally consumed, also activate the applicable public documentation surface, commonly:

```text
docs/PUBLIC-DOCS.md
public-docs/
```

Use a focused `docs/specs/<cli-surface>.md` or `docs/engineering/<cli-topic>.md` only when the command/process contract is too large for the normal repository authority. Do not create extra documentation layers merely because the profile exists.

## New-project decision surface

Before the first implementation milestone becomes `ready`, resolve the material parts of the CLI contract.

### Command surface

Decide, as applicable:

- command/subcommand structure;
- required and optional arguments/options;
- aliases and abbreviations if supported;
- unknown-command/unknown-option behavior;
- repeated option semantics;
- mutually exclusive or dependent options;
- whether commands may prompt interactively;
- whether every important operation has a non-interactive form.

Do not prescribe a command-line parser library at profile level.

### Process contract

Decide:

- success and failure exit-code semantics;
- stdout ownership;
- stderr ownership;
- whether machine-readable output is separated from human diagnostics/progress;
- behavior when stdout/stderr is redirected;
- encoding/newline requirements when they are compatibility-sensitive;
- cancellation/interrupt behavior when material.

### Input and configuration

Identify supported input/configuration sources such as:

- command arguments/options;
- stdin;
- files/directories;
- environment variables;
- configuration files;
- credentials/secrets;
- repository/project discovery.

When multiple sources can set the same value, define precedence explicitly.

### Paths and filesystem

Resolve where relative paths are anchored, for example:

- current working directory;
- input file directory;
- discovered project root;
- configuration file directory.

Where material, also define overwrite policy, directory creation, atomic replacement, temporary-file cleanup, partial-failure behavior, and whether generated paths are stable.

### Diagnostics and errors

Define the externally meaningful distinction between:

- invalid invocation/user input;
- expected product/domain failure;
- unavailable dependency/environment;
- unexpected internal failure.

Diagnostics should be actionable, should not leak secrets, and should not require consumers to understand internal implementation types.

### Compatibility surface

Identify which CLI surfaces are compatibility-sensitive, such as:

- command/subcommand names;
- options/arguments;
- exit codes;
- structured output schemas;
- configuration formats;
- environment-variable names;
- generated files/artifacts;
- documented automation behavior.

### Distribution

Record the actual distribution/installation mechanism as project-local specialization.

Examples include:

- .NET global/local tool;
- standalone executable/archive;
- OS package;
- npm package;
- application-bundled executable.

The distribution mechanism does not change the profile identity.

## Composition

A CLI repository may compose this profile with other broad reusable shapes when they genuinely apply.

Examples:

```text
base + cli-tool
base + cli-tool + artifact-first-runtime
```

Do not add `dotnet-library` merely because a .NET CLI is transported in a NuGet package. Add `dotnet-library` only when the repository also exposes a meaningful reusable library surface with library-specific engineering/public API obligations.

Use `artifact-first-runtime` only when artifact production, provenance, rebuild rules, and artifact evidence are genuinely central to the product shape rather than merely because the CLI writes a file.
