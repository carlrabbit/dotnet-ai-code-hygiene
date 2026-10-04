# Specifications

## Product role

`dotnet-ai-code-hygiene` is a standalone AI-first developer tool. Its primary supported surface is the `hygiene` command-line interface.

The repository does not currently expose a supported reusable .NET library/API surface.

## Product principles

1. Deterministic tooling performs work that does not require model intelligence.
2. Ordinary hygiene discovery does not require an embedded AI model.
3. Rules are opinionated and parameterless.
4. A rule can only be enabled or disabled.
5. Alternative policy choices are separate rules rather than parameters.
6. Enabled rules execute in one engine-defined deterministic order.
7. The engine does not detect or resolve contradictions between enabled rules.
8. Hygiene-set coherence is reviewed externally by a human or sufficiently capable agent.
9. `check` is read-only.
10. Agents interact with persisted state through the CLI, not by editing persistence JSON.

## CLI product surface

The distributed command name is:

```text
hygiene
```

Top-level commands:

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

M0002 makes `check`, `explain`, `ignore`, `unignore`, `ignores`, and `rules` functional.

`format` and `normalize` remain intentionally non-functional scaffolding until M0003.

All supported operations have non-interactive forms.

## Standard streams

- `stdout` is the command result/output channel.
- `stderr` is the diagnostics/progress/failure channel.
- Machine-readable stdout must never contain diagnostics or progress.
- Human console styling is not a compatibility contract unless explicitly promoted later.

## Structured output

Commands that return structured product data support:

```text
--output text
--output json
```

`text` is the default.

CLI JSON is a supported consumer contract. Internal `.hygiene/*.json` is engine-owned persistence and is not the agent API.

Detailed M0002 hygiene behavior and JSON fields are defined by:

```text
docs/specs/HYGIENE.md
```

## Exit semantics

| Code | Meaning |
|---:|---|
| 0 | Command executed successfully. Hygiene findings do not by themselves make execution fail. |
| 1 | Unexpected internal failure. |
| 2 | Invalid or malformed invocation. |
| 3 | Invalid/unusable product input, repository state, target, rule ID, finding handle, ignore ID, or persistence state. |
| 4 | Required environment/dependency unavailable or unusable. |

Parser/framework defaults must not leak as accidental public exit semantics.

## Repository and target contract

M0002 commands are repository-scoped. The repository root is discovered by walking upward from the current working directory until `.git` is found.

Targets must resolve within that repository.

Relative target paths are repository-root-relative. Absolute paths are permitted only when they resolve inside the discovered repository.

Supported `check` target forms:

```text
hygiene check
hygiene check --changed
hygiene check <file> [<file>...]
hygiene check <directory> [<directory>...]
```

Explicit paths and `--changed` are mutually exclusive.

`hygiene check` without targets checks repository C# source.

`--changed` means staged, unstaged, and untracked C# files relative to `HEAD`; it does not infer a remote/base branch. Deleted paths do not produce source targets.

Broad repository/directory scans exclude:

```text
.git
.hygiene
bin
obj
```

M0002 supports C# files that belong to a discoverable SDK-style `.csproj`. An explicitly requested C# file that cannot be associated with a project is invalid input for this milestone.

## Rule identity and ordering

M0002 built-in canonical order:

```text
1. docs.summary.required
2. readability.long-line.review
3. readability.control-flow.visual-block
```

Rule version starts at `1` for all M0002 rules. Rule semantics and order are engine-defined and not configurable.

## Rule configuration

All built-in rules are enabled when `.hygiene/config.json` does not exist.

The persistence model records only disabled rule IDs:

```json
{
  "schemaVersion": 1,
  "disabledRules": [
    "readability.long-line.review"
  ]
}
```

Mutation commands:

```text
hygiene rules enable <rule-id>
hygiene rules disable <rule-id>
```

`hygiene rules` lists canonical order, ID, version, classification, short purpose, and enabled state.

No parameters, severity, ordering, path scopes, or other rule configuration exist.

## Run, finding, and ignore identity

Rule:

```text
readability.long-line.review
```

Run:

```text
R-7K2M9P
```

Finding handle:

```text
R-7K2M9P/F-2
```

Ignore decision:

```text
I-17
```

Finding numbers are deterministic ordinals within one completed ordered result set but have no persistent meaning across runs.

A bare `F-2` refers only to the latest locally persisted run.

M0002 persists only the latest run. A qualified handle whose run ID is not the latest available run must fail rather than resolve to a finding from another run.

## Local run state

The CLI owns:

```text
.hygiene/.state/latest-run.json
```

`.hygiene/.state/` is Git-ignored.

The latest-run snapshot contains enough engine-owned state for `explain` and `ignore`. It is not a supported consumer schema.

Historical run retention is not part of M0002.

## Persistent ignore decisions

Committed reviewed exceptions are stored in:

```text
.hygiene/decisions.json
```

Each decision has:

- stable `I-*` identity;
- rule ID and rule version;
- repository-relative path metadata;
- semantic anchor;
- rule-specific SHA-256 evidence fingerprint;
- local discriminator when required;
- optional reason;
- UTC creation timestamp.

`createdBy` is not stored in M0002.

`hygiene ignore <finding-handle>` revalidates that the referenced occurrence still exists in current source before committing the decision. If source relevant to the finding changed after the check, the command fails and instructs the caller to run `check` again.

`hygiene unignore <ignore-id>` removes exactly that persistent decision.

No source-code suppression comments are supported.

## Occurrence fingerprinting

Persistent occurrence matching uses:

```text
rule ID
+ rule version
+ semantic anchor
+ rule-specific canonical evidence
+ local discriminator when needed
```

Evidence is hashed with SHA-256.

Line numbers and absolute paths are never sufficient identity.

Repository-relative path is matching/disambiguation metadata but not the complete identity.

Each rule owns its canonical evidence definition. Formatting/trivia is ignored unless it is directly relevant to that rule.

## Ignore status

`hygiene ignores` reports persisted decisions as:

```text
active
stale
```

A disabled rule does not automatically make its decision stale. Ignore-status evaluation may evaluate the relevant rule independently of normal enablement.

Stale decisions are not automatically deleted.

## Persistence writes

`.hygiene/config.json` and `.hygiene/decisions.json` writes are atomic from the repository consumer's perspective.

A write must not silently overwrite a file that changed since the command read it. Conflicting modification fails instead of losing state.

Cancellation before commit leaves the previous committed file intact.

## Supported platform

M0002 authoritative validation remains Windows 11 with the .NET 11 SDK line.

Cross-platform support is not claimed.

## Automation policy

No GitHub Actions or other repository-hosted workflow automation is used. Required validation is locally invokable.
