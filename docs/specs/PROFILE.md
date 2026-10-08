# Supported .NET Hygiene Profile

M0010 profile v2 supersedes the M0005 v1 contract below where noted. Update migrates valid `dotnet-11` v1 repositories to v2.

Rationale: the mandatory analyzer rule gives agents one predictable, tested .NET analyzer/code-style baseline independent of ambient defaults; it hardens only explicitly selected diagnostics instead of imposing warnings-as-errors broadly. The StyleCop prohibition keeps the project-owned supported policy coherent instead of composing overlapping style stacks. Automatic removal is unsafe because dependency and policy impact require caller judgment.

## Profile identity

The current supported profile is:

```text
id: dotnet-11
version: 2
```

Committed marker:

```json
{
  "schemaVersion": 1,
  "profile": "dotnet-11",
  "version": 2
}
```

Path:

```text
.hygiene/profile.json
```

This hygiene profile is product policy. It is unrelated to `.guide-profile.json`.

The tool does not support arbitrary profile selection or custom profile parameters.

## Prerequisite

Normal `hygiene check` requires a current supported profile marker. A missing marker returns product exit `3` with guidance to run:

```text
hygiene bootstrap
```

An unsupported/future profile ID/version returns exit `3`. An older supported profile version is handled by `hygiene update` when such a migration exists.

## Fixed rule sets

The engine owns three rule sets:

```text
bootstrap
update
normal
```

Rule-set membership never changes rule semantics.

Current membership:

```text
bootstrap
  profile.dotnet.analysis.required
  profile.stylecop.prohibited

update
  profile.dotnet.analysis.required
  profile.stylecop.prohibited

normal
  profile.dotnet.analysis.required
  profile.stylecop.prohibited
  configurable source/review rules in canonical order
```

Future profile migrations may add distinct fixed migration rules to the `update` set. M0005 does not emulate migration by passing a phase parameter into ordinary rules.

## Rule/remediation boundary

A rule has one fixed diagnosis and may expose one fixed deterministic remediation.

Given the same repository state, rule evaluation means the same thing in bootstrap, update, and normal evaluation.

Commands differ only in execution policy:

```text
check
  evaluate rules
  never execute remediation

bootstrap/update
  evaluate profile rules
  apply available deterministic remediation
  re-evaluate
  report remaining findings
```

A rule without deterministic remediation remains a finding for the caller to resolve/escalate.

## Bootstrap

```text
hygiene bootstrap [--output text|json]
```

Repository-wide; no file/directory target and no `--changed`.

Bootstrap:

1. establishes/reconciles profile `dotnet-11` v2;
2. applies deterministic remediation for `profile.dotnet.analysis.required`;
3. never removes StyleCop automatically;
4. re-evaluates the bootstrap rule set;
5. persists the current profile marker when repository/profile state is usable;
6. reports remaining profile findings.

Bootstrap is idempotent.

Profile findings do not become process errors merely because they exist; successful execution remains exit `0`. Invalid/malformed/conflicting state that prevents safe profile establishment uses existing product/error exits.

## Update

```text
hygiene update [--output text|json]
```

Repository-wide; no targets and no `--changed`.

Update requires a supported profile marker. It reconciles profile-owned artifacts to the current canonical profile and applies any fixed migration rules defined for the recorded version.

For profile `dotnet-11` v1, update migrates the marker and replaces the former profile-owned style settings with projections from enabled rules. For v2, update repairs drift in hygiene-owned profile artifacts. Both paths are idempotent.

M0005 defines no fictional v0->v1 migration rule.

## Profile-managed artifacts

The profile owns only explicit namespaced artifacts/sections.

### Marker

```text
.hygiene/profile.json
```

### MSBuild profile fragment

```text
.hygiene/profile/Hygiene.props
```

Canonical effective properties:

```xml
<AnalysisLevel>11</AnalysisLevel>
<EnableNETAnalyzers>true</EnableNETAnalyzers>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
```

The repository root `Directory.Build.props` imports the namespaced profile fragment. If the root file already exists, bootstrap/update add or reconcile only the hygiene-managed import and preserve unrelated project content. If it does not exist, a minimal import file may be created.

Project-local settings may still override imported props. The profile rule validates effective project values and reports such conflicts; bootstrap/update do not rewrite arbitrary project files merely to win precedence.

### Root EditorConfig profile block

The repository-root `.editorconfig` is the supported editor/analyzer configuration surface. Bootstrap/update maintain a clearly delimited hygiene-owned block and preserve unrelated user content.

The root config must stop parent-directory inheritance:

```text
root = true
```

The managed C# projection is composed in canonical rule order. The current default contributions are:

```ini
[*.cs]
# hygiene rule: style.braces.required
csharp_prefer_braces = true
dotnet_diagnostic.IDE0011.severity = error

# hygiene rule: style.accessibility.explicit
dotnet_style_require_accessibility_modifiers = always
dotnet_diagnostic.IDE0040.severity = error
```

`root = true` is mandatory profile infrastructure. Braces/accessibility settings are configurable rule-owned policy, not mandatory profile infrastructure.

Deeper/user EditorConfig entries are allowed, but they must not weaken an enabled projecting rule's effective settings. Each style rule resolves the EditorConfig hierarchy for C# files that are evaluated `Compile` inputs of supported SDK-style C# projects, applying matching sections from parent to child until a `root = true` cutoff. A missing cutoff allows parent configuration to affect the repository. Files shared by multiple supported projects are validated once. Loose or otherwise unprojected C# files are not style-rule subjects. Diagnosis validates the effective configuration seen by supported project source files, not every raw setting regardless of whether it matches a source path.

## Severity policy

The supported profile is intentionally binary and minimal:

```text
diagnostic explicitly enforced by an enabled hygiene rule
    -> error

everything else
    -> profile leaves platform/analyzer default unchanged
```

Repository-wide warning promotion is prohibited:

```text
TreatWarningsAsErrors = true
WarningsAsErrors = non-empty
```

The profile does not prohibit callers from choosing different build-time handling externally/at invocation time.

M0005 does not police arbitrary user per-rule severity choices except where they weaken a mandatory profile diagnostic.

No category-wide/all-analyzer severity override is generated by hygiene.

## Mandatory rule: `profile.dotnet.analysis.required`

Rationale: one predictable, tested .NET analyzer/code-style baseline keeps agents independent of ambient defaults. Only explicitly selected diagnostics are hardened, avoiding indiscriminate warnings-as-errors policy.

Version: `2`
Output: deterministic finding  
Configurable: no  
Ignorable: no  
Deterministic remediation: yes

The repository must effectively use, for every supported SDK-style C# project:

```text
AnalysisLevel = 11
EnableNETAnalyzers = true
EnforceCodeStyleInBuild = true
```

Repository-configured `TreatWarningsAsErrors=true` or non-empty `WarningsAsErrors` violates the rule.

The rule does not require a specific `TargetFramework`; the supported analyzer/runtime baseline is the .NET 11 SDK line.

The rule validates effective MSBuild property values for Debug and Release evaluations and mandatory profile plumbing, not only whether canonical text happens to exist in generated artifacts. It scans repository project/import configuration for warning-promotion declarations, including project files, `.props`, and `.targets` files. Enabled style rules independently validate their effective EditorConfig contributions.

Its deterministic remediation installs/reconciles only the profile-owned marker/artifacts/managed sections described above. It does not rewrite arbitrary nested project/EditorConfig overrides.

## Mandatory rule: `profile.stylecop.prohibited`

Rationale: the product owns its supported policy rather than composing overlapping style-policy stacks. Automatic removal is unsafe because dependency and policy impact require caller judgment.

Version: `1`  
Output: deterministic finding  
Configurable: no  
Ignorable: no  
Deterministic remediation: none

StyleCop analyzers are outside and conflict with the supported hygiene policy.

The rule detects at least:

- direct `StyleCop.Analyzers` package references;
- central package-management declarations/references that activate `StyleCop.Analyzers`;
- evaluated analyzer inputs identifiable as StyleCop analyzer assemblies, including repository path-based `<Analyzer>` inputs;
- resolved NuGet analyzer assets identifiable as StyleCop analyzer assemblies.

One configured source should not be double-reported merely because it is visible through more than one inspection path.

Bootstrap/update do not remove StyleCop. The caller resolves or escalates the dependency/policy change.

## Mandatory rule behavior

`hygiene rules` exposes whether a rule is configurable.

Attempts to disable a mandatory profile rule return exit `3`.

Attempts to create an ignore decision for a mandatory profile finding return exit `3`.

Manual insertion of mandatory profile rule IDs into `disabledRules` is invalid product configuration and returns exit `3`.

## Mutation safety

Bootstrap/update reuse the M0004 all-or-nothing mutation contract.

All proposed profile-owned changes are planned and validated before commit. Failure/cancellation/conflict must not leave a partial profile installation.

## Non-goals

M0005 does not:

- approve or require a third-party async analyzer;
- attempt compatibility with arbitrary analyzer stacks;
- remove arbitrary analyzers automatically;
- set all warnings/errors globally;
- configure nullable policy;
- enforce a target framework;
- support another language/profile;
- invoke a model or network package service as part of profile evaluation.
