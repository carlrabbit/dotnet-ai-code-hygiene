# Engineering

## Repository self-hosting policy

The repository is English. `.hygiene/config.json` disables only `docs.summary.language.german.review`; generic summary-quality review, required summaries, and visual control-flow separation remain enabled. The German policy remains fixed and available to consuming repositories that choose to enable it.

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

## M0007 architecture test seams

Architecture tests should prove behavior through small internal seams rather than through implementation-shape assertions alone.

Useful focused seams include:

- a test rule supplied to the rule runner without editing production orchestration;
- a test rewrite supplied to the rewrite runner without editing a command switch;
- observable session factories/counters proving expensive project/compilation/fact creation is lazy and reused within one command session;
- a test fact provider proving one computation is shared by multiple consumers;
- targeted fixture runs proving reporting scope remains narrower than readable project context.

Do not introduce production plugin discovery or a DI framework solely to obtain these seams. Internal constructors/factories/test doubles are sufficient.

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
- semantic review population for ordinary and record-property summary carriers;
- independent quality and German-language review batches when both are enabled.

The documentation-subject/carrier fixture set is also the primary M0007 shared-fact regression surface.

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

User per-rule severity configuration for unrelated diagnostics is not a profile violation unless it weakens a required profile diagnostic.

## Mutation validation

Bootstrap/update and source rewrites retain all-or-nothing mutation behavior.

A failed profile run must not leave a partial marker/props/import/editorconfig update. A failed source rewrite must not leave a mixed selected-target state.

M0007 may share planning/transaction infrastructure where sensible, but profile mutation and source rewrite semantics must remain independently regression-covered.

## Validation topology

| Depth | Target | Locus |
|---|---|---|
| Tier 1 | rule runner/catalog/session/facts/result materialization + existing Core behavior | Windows 11 + .NET 11 |
| Tier 1 | built CLI process | Windows 11 + .NET 11 |
| Tier 3 | isolated SDK-style Git fixture repositories, including full/explicit/changed scope and rewrite commands | Windows 11 + .NET 11 + Git |
| Tier 4 | exact current locally packed/installed tool | isolated Windows consumer repository |
| Tier 2 | complete repository | `./eng/validate.ps1` |
| Documentation | architecture/spec consistency | repository review |
| Human | modularity/BORING architecture and behavior preservation | project owner/delegate |

Tier 4 must invoke the installed `hygiene` command and exercise representative bootstrap/update/check/rules/review and format/normalize behavior from the current build artifact.

## Architecture regression expectations

M0007 must preserve:

- M0002 target/finding/ignore/explain behavior;
- M0003/M0006 semantic-review population, independent batches, expansion, and handoff behavior;
- M0004 format/normalize/check-only and transactional mutation behavior;
- M0005 profile/bootstrap/update/documentation behavior;
- M0006 fixed-rule/toggle-only policy and repository German-rule disablement.

Architecture tests may inspect internal collaboration boundaries, but the primary correctness signal remains externally observable product behavior plus focused tests for session reuse/laziness and module independence.

## Deferred

No external specialist analyzer is currently approved. No StyleCop compatibility, model client, MCP, IDE integration, other language, hosted CI, public plugin architecture, statistical sampling subsystem, persistent analysis cache, or speculative repository index is introduced by M0007.
