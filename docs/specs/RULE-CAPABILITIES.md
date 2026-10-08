# Rule Capabilities and Policy Projection

## Product model

A hygiene rule ID is the semantic owner of one fixed product policy.

A rule may expose one or more capabilities:

```text
diagnose
format-remediate
normalize-remediate
editorconfig-project
profile-remediate
```

Capabilities are optional. A rule does not need to emit findings merely because it owns a deterministic rewrite or toolchain projection.

One rule ID owns all of its capabilities. Do not create separate semantic registries in which a rewrite or generated setting has no owning rule.

Rule enable/disable is whole-policy participation. For a configurable rule, disabling the rule disables every optional capability owned by that rule:

```text
diagnosis
+ format remediation
+ normalize remediation
+ hygiene-owned EditorConfig projection
```

Mandatory rules remain non-disableable.

Repository configuration still contains only whole-rule enable/disable state. It does not parameterize rule semantics, rewrite behavior, severity, formatting choices, or generated values.

## Static catalogue

Production rule registration remains explicit, static, and ordered.

There is one canonical rule catalogue. Command-specific execution selects rules from that catalogue by capability rather than maintaining an independent semantic rewrite catalogue.

A small implementation may use capability interfaces or equivalent explicit code. Exact type names are implementation freedom, but the design must remain direct and in-process. Do not introduce reflection discovery, DI, a rule DSL, generated metadata, a dependency graph, or a generic policy framework.

The catalogue owns deterministic canonical order. A rule descriptor remains local to the rule implementation.

## Command surfaces

### `check`

`check` evaluates enabled rules with diagnostic capability.

Rules that have no diagnostic capability do not execute merely to produce an empty check result.

### `format`

`format` applies enabled rules with format-remediation capability in canonical order over the selected source scope, using the existing complete-plan/all-target transaction.

### `normalize`

`normalize` applies enabled rules with normalize-remediation capability in canonical order. After normalization, enabled format-remediation rules are applied to changed documents before final comparison/validation. This preserves the existing normalized-and-formatted default outcome while making the participating rules explicit and disableable.

### `bootstrap` / `update`

Bootstrap/update materialize mandatory profile infrastructure plus hygiene-owned projections contributed by currently enabled rules.

The profile host owns composition, delimiters, transactionality, and file preservation. Individual rules own only their fixed contribution values and the semantic requirement represented by those values.

### `enable` / `disable`

Changing participation for a configurable rule is one transaction over:

```text
.hygiene/config.json
+ affected hygiene-owned derived projections
```

A successful toggle leaves generated policy artifacts consistent with the new enabled-rule set. A failure/conflict rolls back the complete change.

Disabling a rule never removes equivalent user-authored configuration outside hygiene-owned sections. It only removes hygiene ownership/enforcement of that policy.

## Rewrite reporting

`hygiene rules` must make rule capabilities visible so an agent can determine which rules participate in `check`, `format`, `normalize`, and EditorConfig projection.

`format` and `normalize` result output must identify the ordered rule IDs selected for the command. The command does not need to attribute every character edit to one rule in M0010.

Existing target counts, changed paths, check-only behavior, and transaction semantics remain.

## EditorConfig projection

The hygiene-owned EditorConfig content is derived from enabled rule contributions rather than from a hard-coded style block in `ProfileManager`.

Composition is deterministic:

```text
root/profile infrastructure
-> one [*.cs] hygiene-managed projection block
-> enabled contributing rules in canonical rule order
-> each rule's fixed entries in rule-owned order
```

The generated block includes a short rule-ID comment before each rule contribution so ownership is visible in the file.

Example shape:

```ini
# hygiene rule: style.braces.required
csharp_prefer_braces = true
dotnet_diagnostic.IDE0011.severity = error

# hygiene rule: style.accessibility.explicit
dotnet_style_require_accessibility_modifiers = always
dotnet_diagnostic.IDE0040.severity = error
```

`root = true` remains mandatory profile infrastructure rather than an optional style rule.

No two production rules may own conflicting values for the same hygiene-managed EditorConfig key. Production catalogue tests must reject duplicate conflicting ownership. There is no runtime conflict-resolution system.

User-authored EditorConfig content outside the hygiene-managed block is preserved.

## Profile v2

M0010 advances the supported profile marker:

```text
dotnet-11 v2
```

`profile.dotnet.analysis.required` advances to rule version `2` because its semantic contract changes.

Version 2 owns mandatory analysis infrastructure only:

```text
AnalysisLevel = 11
EnableNETAnalyzers = true
EnforceCodeStyleInBuild = true
root .editorconfig cutoff = true
managed projection plumbing is structurally valid
repository-wide TreatWarningsAsErrors/WarningsAsErrors prohibition
```

It no longer semantically owns:

```text
braces-required preference/severity
explicit-accessibility preference/severity
```

Those policies move to independent configurable rules.

`hygiene update` supports the real `dotnet-11 v1 -> v2` migration. It removes the old profile-owned style entries and regenerates the managed EditorConfig projection from enabled M0010 rules.

The new style rules are enabled by default, so a repository that has not disabled them preserves the effective v1 policy after migration.

## Initial M0010 rule catalogue

M0010 adds five configurable rules and updates one existing mandatory rule.

### `profile.dotnet.analysis.required` v2

Capabilities:

```text
diagnose
profile-remediate
```

Configurable: no.

The v2 contract is the mandatory profile infrastructure described above.

### `style.braces.required` v1

Purpose: require the supported built-in braces policy without making it mandatory product profile infrastructure.

Capabilities:

```text
diagnose
editorconfig-project
```

Configurable: yes; enabled by default.

Owned EditorConfig contribution:

```ini
csharp_prefer_braces = true
dotnet_diagnostic.IDE0011.severity = error
```

Diagnosis validates the effective configuration for supported projected C# compile inputs using the existing effective-EditorConfig semantics. It does not introduce a second custom source analyzer for IDE0011 in M0010.

No source rewrite is added for this rule in M0010.

### `style.accessibility.explicit` v1

Purpose: require explicit accessibility through the supported built-in analyzer policy without making it mandatory profile infrastructure.

Capabilities:

```text
diagnose
editorconfig-project
```

Configurable: yes; enabled by default.

Owned EditorConfig contribution:

```ini
dotnet_style_require_accessibility_modifiers = always
dotnet_diagnostic.IDE0040.severity = error
```

Diagnosis validates the effective configuration for supported projected C# compile inputs using the existing effective-EditorConfig semantics.

No source rewrite is added for this rule in M0010.

### `format.csharp.roslyn` v1

Purpose: apply the supported Roslyn C# formatter as an explicit, discoverable, disableable format policy.

Capabilities:

```text
format-remediate
```

Configurable: yes; enabled by default.

The remediation is the current project-aware Roslyn `Formatter` behavior against the loaded document/workspace and effective repository formatting configuration. M0010 does not explode Roslyn whitespace options into individual hygiene rules.

The rule emits no normal `check` finding in M0010.

### `style.qualification.this.unnecessary` v1

Purpose: remove explicit `this.` qualification only where Roslyn semantic simplification proves it redundant.

Capabilities:

```text
normalize-remediate
```

Configurable: yes; enabled by default.

This rule owns only explicit `this.` removal. It does not add `this.`, rename symbols, alter member selection, or change qualification when semantic equivalence is not established.

It emits no normal `check` finding in M0010.

### `style.qualification.redundant` v1

Purpose: remove redundant member/type/namespace/name qualification where Roslyn semantic simplification proves equivalence.

Capabilities:

```text
normalize-remediate
```

Configurable: yes; enabled by default.

This rule owns the remainder of the existing M0004 simplification policy after explicit `this.` removal is separated. It may simplify redundant member/type/namespace qualification and equivalent explicit type/name syntax where Roslyn proves equivalence.

It must not introduce unrelated modernization such as:

```text
var policy
expression-bodied members
namespace-style conversion
collection expressions
target-typed new
modifier/member ordering
naming changes
```

It emits no normal `check` finding in M0010.

## Default compatibility target

With all new M0010 configurable rules enabled:

- generated braces/accessibility EditorConfig policy is effectively equivalent to profile v1;
- `hygiene format` retains the existing Roslyn formatter behavior;
- `hygiene normalize` retains the existing supported simplification + formatting behavior, now attributed to explicit rules;
- existing documentation/readability/review rules retain their diagnostic contracts and versions.

A deliberate disable changes only the disabled rule's owned capabilities.

## Canonical rule order after M0010

```text
1.  profile.dotnet.analysis.required          v2
2.  profile.stylecop.prohibited               v1
3.  style.braces.required                     v1
4.  style.accessibility.explicit              v1
5.  format.csharp.roslyn                      v1
6.  style.qualification.this.unnecessary      v1
7.  style.qualification.redundant              v1
8.  docs.summary.required                     v2
9.  docs.xml.consistent                       v1
10. docs.text.sentence                        v1
11. docs.summary.quality.review               v3
12. docs.summary.language.german.review       v1
13. readability.long-line.review              v1
14. readability.control-flow.visual-block     v1
```

The relative order of all pre-M0010 rules remains unchanged.

## Non-goals

M0010 does not:

- convert every formatter option into a hygiene rule;
- make `readability.control-flow.visual-block` auto-fixable;
- add direct source rewrites for braces/accessibility;
- add arbitrary repository parameters for style rules;
- add a public plugin API;
- add reflection discovery, DI, a rule dependency graph, or a policy DSL;
- add model/provider invocation;
- change statistical sampling behavior;
- add unrelated style/modernization rules.
