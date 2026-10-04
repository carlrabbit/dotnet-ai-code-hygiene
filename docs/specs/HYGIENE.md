# Hygiene Behavior

This document defines the first functional C# hygiene surface introduced by M0002.

## Check result ordering

Enabled rules execute in canonical rule order.

Reported findings are ordered by:

```text
rule order
-> repository-relative path using ordinal comparison
-> source span start
-> deterministic local discriminator
```

Finding IDs `F-1`, `F-2`, ... are assigned only after the final ordered result set is established.

Ignored occurrences are omitted from the normal finding list. The result summary reports the ignored count.

## Check classifications

M0002 exposes:

```text
finding
review-candidate
```

These are fixed rule semantics, not configurable severity.

## Text output

Each visible finding includes at least:

- short finding ID;
- rule ID;
- classification when it is `review-candidate`;
- repository-relative path and 1-based location;
- concise observation;
- concise suggested direction when the rule defines one.

The run ID and summary counts are emitted once per completed check.

## JSON output

`hygiene check --output json` emits one JSON object to stdout. In the example the semantic review rule is disabled, so the additive batch list is empty:

```json
{
  "schemaVersion": 1,
  "runId": "R-7K2M9P",
  "findings": [
    {
      "id": "F-2",
      "handle": "R-7K2M9P/F-2",
      "ruleId": "readability.long-line.review",
      "ruleVersion": 1,
      "classification": "review-candidate",
      "path": "src/Foo.cs",
      "line": 38,
      "column": 1,
      "symbol": "Foo.ProcessAsync(string)",
      "message": "Physical line exceeds 200 characters.",
      "suggestion": "Review whether the line hides multiple concepts or structures that should be made visible or named."
    }
  ],
  "ignoredCount": 1,
  "reviewBatches": []
}
```

Contract:

- `schemaVersion`, `runId`, `findings`, `ignoredCount`, and `reviewBatches` are always present.
- shown finding fields are present; `symbol` may be `null`.
- line/column are 1-based.
- internal fingerprints/persistence evidence are not exposed.
- diagnostics/progress go to stderr, never JSON stdout.

`rules`, `explain`, and `ignores` also support `--output json`; their JSON uses top-level `schemaVersion: 1` and exposes consumer-relevant data only.

`reviewBatches` contains semantic review work selected for the current run. Batches are not findings and do not affect finding or ignored counts. Public batch/item fields, the fixed summary-quality rubric, and expansion behavior are defined in `docs/specs/SEMANTIC-REVIEWS.md`. Empty eligible populations are rendered as an explicit `sample 0/0` batch.

## Explain

```text
hygiene explain <finding-handle>
```

accepts a full latest-run handle or bare latest-run `F-*`.

Explanation includes:

- rule ID/version;
- classification;
- location;
- observation;
- why the rule triggered;
- suggested cleanup direction;
- rule-specific constraint/anti-fix guidance when applicable.

`explain` reads latest-run state; it does not rerun the complete check.

## Initial rules

### `docs.summary.required`

Version: `1`  
Classification: `finding`

A non-empty XML `<summary>` is required for source-declared C# symbols with declared accessibility `public` or `internal` in these categories:

- named types: class, struct, interface, enum, record, record struct, delegate;
- constructors;
- ordinary methods;
- properties.

Exclude:

- implicitly declared symbols;
- accessors;
- operators/conversions;
- explicit interface implementations;
- fields;
- events;
- local functions;
- record positional parameters.

A summary is present when the declaration has XML documentation containing a `<summary>` element with non-whitespace textual/documentation content.

This rule is deliberately language-neutral in M0002. A future German-language rule is a separate rule rather than a parameter or fuzzy mode.

Semantic anchor: prefer Roslyn documentation-comment ID for the declared symbol.

Canonical evidence is the rule/version plus missing-summary state anchored to that symbol. Moving the declaration without changing symbol identity must not create a new occurrence.

### `readability.long-line.review`

Version: `1`  
Classification: `review-candidate`

Trigger once for each physical C# source line whose content length exceeds 200 UTF-16 code units, excluding the line break.

This is not a maximum-line-length violation.

Guidance:

> The physical line exceeds 200 characters. Review whether it hides multiple concepts or structures that should be made visible or named. Do not split mechanically merely to satisfy a line-length limit.

The occurrence is anchored to the smallest containing declared member/type when available.

Canonical evidence uses significant Roslyn tokens intersecting the physical line and excludes ordinary whitespace trivia. Unrelated whitespace changes that leave the same structure long may preserve an ignore; wrapping it so the line no longer exceeds 200 resolves the occurrence.

One physical line produces at most one finding.

### `readability.control-flow.visual-block`

Version: `1`  
Classification: `finding`

Within a C# block, when a control-flow statement immediately follows a linear statement sibling, the control-flow statement must be separated by at least one completely blank line.

M0002 control-flow statements:

```text
if
switch
for
foreach
while
do
try
using statement/block
lock
```

A previous sibling is linear when it is a statement that is not one of the listed control-flow kinds and is not a local-function declaration.

No finding when:

- control flow is first in the block;
- previous sibling is another listed control-flow statement;
- at least one completely blank line separates the previous statement from control flow.

Leading comments directly documenting the control-flow statement belong to its visual group. The required blank line is between the preceding linear statement and the first leading comment line.

The rule does not require blank lines between individual linear statements and does not require a blank line after control flow.

Canonical evidence includes:

- containing anchor;
- previous-statement kind and significant tokens;
- control-flow kind and significant tokens;
- absence of the required blank-line boundary.

Adding the required blank line resolves the occurrence.

## Target behavior

Repository/directory discovery excludes `.git`, `.hygiene`, `bin`, and `obj`.

Explicit/discovered C# files must belong to a discoverable SDK-style `.csproj`.

Project membership and Roslyn context follow the MSBuild-evaluated project model, including evaluated `Compile` items, project references, compiler references/options, conditional symbols, linked source, and generated/project-provided files represented as compile inputs. Project discovery is bounded by the Git repository root; an ancestor project outside the repository cannot claim a repository target.

The engine may read broader project/repository context than the selected targets, but findings are emitted only for selected files.

A file included through multiple target expressions is analyzed once.

## Changed behavior

`--changed` includes:

- staged additions/modifications/renames at current `.cs` path;
- unstaged additions/modifications/renames at current `.cs` path;
- untracked `.cs` files.

Deleted paths are ignored.

Git invocation/environment failure maps to exit code `4`.

An empty changed target set succeeds with zero findings.

## Rules command

`hygiene rules` lists all built-in rules in canonical order.

`enable` removes a rule from `disabledRules`.

`disable` adds it.

Repeated enable/disable is idempotent.

Unknown rule ID returns exit code `3`.

## Ignores filtering

```text
hygiene ignores
hygiene ignores <file-or-directory> [...]
```

Without paths, list all persisted decisions.

With paths, filter decisions to occurrences under selected repository-relative targets.

`ignores` does not accept `--changed` in M0002.

## Invalid persistence

Malformed/unsupported `.hygiene/config.json`, `.hygiene/decisions.json`, or latest-run state must not be silently rewritten.

Committed config/decisions errors return exit code `3`.

Malformed latest-run state causes `explain`/`ignore` to return `3` and instruct the caller to run `check` again. A new successful `check` may replace local latest-run state.
