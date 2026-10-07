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

## Agent entrypoint

`AGENTS.md` is stable repository-level routing. It must not point at one current/completed milestone or duplicate a milestone execution contract.

Milestone-specific scope, constraints, evidence requirements, and completion instructions belong in the active milestone, its `.execution/` ledger, and the execution prompt.

## Rule source layout

Production rules live below `src/DotNetAiCodeHygiene.Core/Rules/`.

Use family folders:

```text
Rules/
  Documentation/
  SemanticReview/
  Readability/
  Profile/
```

Shared rule execution/catalog types may live directly under `Rules/`.

Use one production rule module per source file. Shared family facts/helpers may use separate files in that family. Do not create category bucket files containing several unrelated production rule implementations.

When adding or modifying a rule, the normal locality path is:

```text
open the rule file
-> see its descriptor and fixed interface text
-> see or follow its evaluator
-> follow only clearly named shared facts/helpers when needed
```

Rule-specific fixed presentation text belongs to the rule file as plain C# constants/static data where practical. This includes descriptor purpose, finding templates, review questions/rubric text, and rule-specific reviewer/escalation wording.

Do not introduce `.resx`, localization infrastructure, generated metadata, a generic message registry, attributes/reflection, or a rule-definition DSL merely to centralize strings.

Truly generic product/host text remains with the shared mechanism that owns it.

## Architecture test seams

Architecture tests should prove behavior through small internal seams rather than implementation-shape assertions alone.

Useful focused seams include:

- a test rule supplied to the rule runner without editing production orchestration;
- a test rewrite supplied to the rewrite runner without editing a command switch;
- observable session factories/counters proving expensive project/compilation/fact creation is lazy and reused within one command session;
- a test fact provider proving one computation is shared by multiple consumers;
- targeted fixture runs proving reporting scope remains narrower than readable project context.

Do not introduce production plugin discovery or a DI framework solely to obtain these seams.

Physical source-layout requirements are validated by repository/source review plus normal compilation/tests; do not build a runtime architecture framework merely to enforce folders.

## Profile fixtures

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

The documentation-subject/carrier fixture set is the primary shared-fact regression surface.

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

Profile mutation and source rewrite semantics remain independently regression-covered.

## Validation topology

| Depth | Target | Locus |
|---|---|---|
| Tier 1 | rule runner/catalog/session/facts/result materialization + Core behavior | Windows 11 + .NET 11 |
| Tier 1 | built CLI process | Windows 11 + .NET 11 |
| Tier 3 | isolated SDK-style Git fixture repositories, including full/explicit/changed scope and rewrite commands | Windows 11 + .NET 11 + Git |
| Tier 4 | exact current locally packed/installed tool | isolated Windows consumer repository |
| Tier 2 | complete repository | `./eng/validate.ps1` |
| Documentation | architecture/spec consistency | repository review |
| Human | modularity/BORING architecture, locality, and behavior preservation | project owner/delegate |

Tier 4 must invoke the installed `hygiene` command and exercise representative bootstrap/update/check/rules/review and format/normalize behavior from the current build artifact.

## Regression expectations

Architecture/refactoring work must preserve:

- target/finding/ignore/explain behavior;
- semantic-review population, independent batches, expansion, and handoff behavior;
- format/normalize/check-only and transactional mutation behavior;
- profile/bootstrap/update/documentation behavior;
- fixed-rule/toggle-only policy and repository German-rule disablement;
- exact public rule IDs, versions, order, descriptor metadata, and established interface text unless a milestone explicitly changes them.

## Deferred

No external specialist analyzer is currently approved. No StyleCop compatibility, model client, MCP, IDE integration, other language, hosted CI, public plugin architecture, statistical sampling subsystem, persistent analysis cache, speculative repository index, localization framework, or rule metadata DSL is introduced merely for rule locality.
