# Specifications

## Product role

`dotnet-ai-code-hygiene` is a standalone AI-first developer tool. Its primary supported surface is the `hygiene` command-line interface. It canonicalizes code where deterministic transformation is safe and identifies opinionated code-hygiene concerns for remediation by a coding agent or human.

The repository does not initially expose a supported reusable .NET library/API surface.

## Product principles

1. Deterministic tooling performs work that does not require model intelligence.
2. Ordinary hygiene discovery does not require an embedded AI model.
3. Rules are opinionated and parameterless.
4. A rule is either enabled or disabled.
5. Alternative policy choices are separate rules. Example: a German-summary rule and an English-summary rule are distinct rules.
6. Enabled rules execute in one engine-defined deterministic order.
7. The engine does not detect or resolve contradictions between enabled rules.
8. Hygiene-set coherence is reviewed externally by a human or sufficiently capable agent.
9. `check` is read-only; source modification belongs to deterministic formatting/normalization or to the caller performing remediation.
10. Agents interact with persisted decisions through the CLI, not by parsing or editing the persistence JSON directly.

## CLI product surface

The distributed command name is:

```text
hygiene
```

The reserved top-level commands are:

```text
hygiene format
hygiene normalize
hygiene check
hygiene explain
hygiene ignore
hygiene unignore
hygiene ignores
hygiene rules
```

M0001 establishes command discovery and parsing for this surface. Domain behavior for hygiene analysis and decision persistence is introduced by M0002; deterministic formatter behavior and packaged consumer validation are completed by M0003.

All important supported operations must have non-interactive forms. No required workflow may depend on an interactive terminal.

## Standard streams

- `stdout` is the command result/output channel.
- `stderr` is the diagnostics/progress/failure channel.
- Machine-readable stdout must never be contaminated by diagnostics or progress.
- Human console styling is not a compatibility contract unless explicitly promoted later.

## Structured output

JSON is the supported machine-readable output representation for commands that return structured product data.

The activation mechanism is:

```text
--output text
--output json
```

`text` is the default.

CLI JSON is a supported consumer contract. Internal persistence JSON is engine-owned state and is not the agent API.

## Exit semantics

Stable public exit classes:

| Code | Meaning |
|---:|---|
| 0 | Command executed successfully. Product findings, when later supported, do not by themselves make execution fail. |
| 1 | Unexpected internal failure. |
| 2 | Invalid or malformed invocation. |
| 3 | Invalid or unusable product input/target. |
| 4 | Required environment/dependency unavailable or unusable. |

Framework/parser defaults must be translated to these public semantics where they differ.

A future explicit gating option may turn findings into a non-success automation result, but gating is not the default behavior.

## Help and version

- `hygiene --help` is the primary command-discovery entry point.
- Per-command help must be available for every reserved top-level command.
- `hygiene --version` reports the distributed product version.
- Help/version behavior must not depend on repository source inspection.

## Target semantics

Future target-aware commands support:

```text
repository default
--changed
explicit files
directories
```

Explicit target scope controls where results may be emitted or transformations applied. It does not prohibit broader read-only repository context.

Repository-aware operations discover the repository root by walking upward from the current working directory until `.git` is found. Repository-relative paths are anchored to the discovered repository root. Absolute paths remain absolute.

`--changed` means Git working-tree/index changes relative to `HEAD`; it does not infer a remote or base branch.

These semantics are product authority now but are implemented by M0002 unless M0001 needs small reusable primitives for its CLI skeleton.

## Rule and occurrence identity

Rule IDs are stable semantic identifiers, for example:

```text
readability.long-line.review
```

Concrete findings are run-scoped:

```text
R-8K3M/F-42
```

Persistent ignores have separate identities:

```text
I-17
```

Persistent occurrence matching is based on:

```text
rule ID
+ rule version
+ semantic anchor
+ rule-specific canonical evidence fingerprint
+ local discriminator when structurally identical occurrences require it
```

Repository-relative path is useful matching/disambiguation metadata but is not the sole identity.

The hash algorithm for evidence fingerprints is SHA-256. Rules own the canonical evidence definition for “the same finding.” Line numbers are never sufficient identity.

## Persistence direction

Project decisions will be stored in:

```text
.hygiene/decisions.json
```

Configuration for enabled/disabled rules will be stored separately, initially:

```text
.hygiene/config.json
```

The CLI owns parsing, validation, migration, and writes. Source-code suppression comments are not part of the initial design.

Persistence is implemented by M0002.

## Distribution

The CLI is distributed as a .NET tool through a NuGet package.

Project-local names:

```text
command: hygiene
CLI project/assembly: DotNetAiCodeHygiene.Cli
NuGet package ID: DotNetAiCodeHygiene.Tool
```

M0001 must produce a valid current local package. Installation/consumer-surface acceptance of that exact package is M0003.

## Supported platform

M0001 support and validation:

```text
Windows 11
```

Cross-platform support is not claimed by compilation alone and is not an M0001 acceptance requirement.

## Automation policy

The repository does not use GitHub Actions or other repository-hosted workflow automation for the initial project. Required validation must be locally invokable.
