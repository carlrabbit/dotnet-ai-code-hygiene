# Engineering

## Baseline

```text
Windows 11
.NET 11 SDK line
SDK-default C#
PowerShell
System.CommandLine
TUnit
Roslyn/MSBuild project-aware loading
```

No GitHub Actions/workflows.

## M0005 profile fixtures

Use isolated SDK-style Git repositories covering:

- no profile marker/artifacts;
- clean bootstrap;
- existing root `.editorconfig`;
- existing root `Directory.Build.props`;
- project-level override of profile props;
- nested EditorConfig override of required IDE rule;
- `TreatWarningsAsErrors=true`;
- non-empty `WarningsAsErrors`;
- direct StyleCop package reference;
- central package-management StyleCop reference;
- effective StyleCop analyzer input;
- profile-owned artifact drift;
- malformed/conflicting managed-section/import state;
- bootstrap/update idempotence;
- fault/cancellation before commit.

Tests must prove unrelated existing `.editorconfig`/MSBuild content is preserved.

## Documentation fixtures

Use Roslyn/project-aware C# fixtures for:

- ordinary type/member summaries;
- direct `<inheritdoc/>`;
- inheritdoc plus explicit invalid prose;
- record and record-struct positional properties;
- normal method parameters with zero/some/all `<param>` tags;
- parameter/type-parameter mismatch and duplicates;
- `paramref`/`typeparamref` mismatch;
- optional returns/value/exception structures;
- inline `<see/>` before sentence punctuation;
- excluded elements such as `<example>`;
- semantic review population for ordinary and record-property summary carriers.

## Effective configuration

Profile validation must exercise effective MSBuild/analyzer configuration, not merely string-match generated files.

The canonical profile uses .NET 11 `AnalysisLevel=11`, explicit built-in analyzer enablement, build-time code-style enforcement, and individual IDE0011/IDE0040 error severities.

Do not introduce `latest` AnalysisLevel in the supported profile.

## Severity testing

Prove:

```text
required IDE rules -> effective error
profile emits no unrelated severity overrides
TreatWarningsAsErrors=true -> finding
WarningsAsErrors non-empty -> finding
```

User per-rule severity configuration for unrelated diagnostics is not an M0005 violation unless it weakens a required profile diagnostic.

## Mutation validation

Bootstrap/update reuse M0004 transactional planning/commit behavior.

A failed profile run must not leave a partial marker/props/import/editorconfig update.

## Validation topology

| Depth | Target | Locus |
|---|---|---|
| Tier 1 | Core rule sets/profile/documentation/remediation | Windows 11 + .NET 11 |
| Tier 1 | built CLI process | Windows 11 + .NET 11 |
| Tier 3 | isolated SDK-style Git fixture repositories | Windows 11 + .NET 11 + Git |
| Tier 4 | exact locally packed/installed 0.5.0 tool | isolated Windows consumer repository |
| Tier 2 | complete repository | `./eng/validate.ps1` |
| Human | profile artifacts + documentation policy usability | project owner/delegate |

Tier 4 must invoke the installed `hygiene` command and exercise bootstrap, update, check, rules/help, and representative documentation/profile findings.

## Regression

Retain M0002 deterministic finding/ignore/target behavior, M0003 review/handoff behavior, and M0004 format/normalize/installed-tool behavior.

## Deferred

No external specialist analyzer is approved in M0005. No StyleCop compatibility, model client, MCP, IDE integration, other language, hosted CI, or arbitrary analyzer-profile configuration.
