# Architecture

## System shape

```text
source targets
  -> repository/project resolution
  -> Roslyn project/source context
  -> enabled-rule selection
  -> deterministic rule evaluation
  -> occurrence matching against decisions
  -> ordered findings/review candidates
  -> run snapshot
  -> text/JSON presentation
  -> caller remediation
```

Formatting and safe normalization remain M0003 work.

The CLI is the supported public surface. Internal assemblies are implementation details unless deliberately promoted later.

## Project boundaries

M0002 establishes:

```text
src/
  DotNetAiCodeHygiene.Cli/
  DotNetAiCodeHygiene.Core/

tests/
  DotNetAiCodeHygiene.Cli.Tests/
  DotNetAiCodeHygiene.Core.Tests/
```

`DotNetAiCodeHygiene.Core` owns application/domain semantics for target resolution, Roslyn analysis, rules, identities, matching, and persistence.

It is not a supported public library API.

CLI handlers remain thin adapters.

## CLI boundary

Owns:

- `System.CommandLine` parsing;
- public options/arguments;
- help/version;
- stable exit mapping;
- stdout/stderr routing;
- text/JSON rendering;
- cancellation handoff.

It does not own rule semantics or occurrence matching.

## Repository/project context

The engine:

1. discovers the Git repository root;
2. resolves selected C# target files;
3. associates them with discoverable SDK-style `.csproj` projects;
4. loads enough Roslyn project/compilation context for syntax, semantic models, and symbols;
5. permits broader read-only project/repository context while limiting emitted findings to target files.

The public contract is project-aware behavior, not a specific Roslyn workspace type.

## Rule engine

Rules contain:

```text
stable ID
version
fixed classification
fixed semantics
fixed applicability
canonical order
evidence/fingerprint construction
explanation guidance
```

Configuration selects enabled rules only.

Ignore-status evaluation may evaluate a disabled rule to determine whether a stored occurrence still exists.

## Result construction

Rule evaluation produces internal occurrences before public finding numbers.

The engine:

1. produces occurrences for enabled rules/targets;
2. matches persistent decisions;
3. omits ignored occurrences;
4. deterministically sorts visible findings;
5. assigns `F-*`;
6. creates one run ID;
7. publishes latest-run state only after successful complete result construction;
8. renders text or JSON.

## Identity model

Rule: persistent semantic ID.  
Run: one successful completed check, e.g. `R-7K2M9P`.  
Finding: run-local, e.g. `R-7K2M9P/F-2`.  
Ignore decision: repository-persistent, e.g. `I-17`.

Run-ID encoding is implementation-local but must make accidental reuse negligible.

Ignore IDs must never be silently reused for another stored decision.

## Fingerprinting

Occurrence matching combines:

```text
rule ID
+ rule version
+ semantic anchor
+ rule-specific canonical evidence
+ local discriminator when required
```

Canonical evidence is deterministically serialized and hashed with BCL SHA-256.

Do not use `GetHashCode()`, runtime-dependent hashes, line number, or absolute path as occurrence identity.

Prefer Roslyn documentation-comment IDs for source symbols when available.

Rules own what evidence changes identity.

## Local run state

```text
.hygiene/.state/latest-run.json
```

is ephemeral engine-owned state.

Only latest run is retained in M0002. It supports `explain` and finding-to-occurrence resolution for `ignore`.

It is not committed and not a public schema.

## Committed state

```text
.hygiene/config.json
.hygiene/decisions.json
```

are Git-friendly engine-owned persistence.

They are readable/diffable but normally mutated through CLI commands.

Persistence reads validate schema before mutation.

Writes are atomic and conflict-aware. Equivalent mechanics are acceptable if they guarantee no partial committed file and no silent overwrite of concurrent external modification.

## Ignore creation

```text
latest-run finding
-> recover occurrence identity
-> revalidate current source
-> if same occurrence still exists, persist decision
```

This prevents ignoring stale findings after source changes.

## Target/context distinction

```text
target scope != context visibility
```

Target selection controls where findings may be emitted; analysis may inspect broader context.

## Cancellation

Long-running target/project loading and analysis accept cancellation.

Cancelled/failed checks do not publish partial latest-run state.

Cancelled persistence mutation does not leave partial committed JSON.

## M0002 architecture scope

M0002 implements C# target resolution, project-aware Roslyn context, three rules, deterministic result ordering, run/finding identity, latest-run state, rule configuration, persistent ignores, fingerprint matching, and text/JSON result surfaces.

It does not implement formatting, normalization, embedded AI, run history, cross-language analysis, IDE/MCP integration, or installed-package consumer validation.
