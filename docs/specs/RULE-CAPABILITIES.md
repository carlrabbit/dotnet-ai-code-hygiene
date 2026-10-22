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
normalize-remediate
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
normalize-remediate
```

Configurable: yes; enabled by default.

Owned EditorConfig contribution:

```ini
dotnet_style_require_accessibility_modifiers = always
dotnet_diagnostic.IDE0040.severity = error
```

Diagnosis validates the effective configuration for supported projected C# compile inputs using the existing effective-EditorConfig semantics. When the rule is enabled, normalization adds the language-defined default accessibility to supported type/member declarations, including top-level `internal`, class/struct-member `private`, and interface-member `public`. It skips explicit-interface implementations and static constructors. Roslyn formats the edited document, then the normal project compilation and transactional commit path validate/apply it.

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

### `style.object-creation.target-typed` v1

Purpose: use target-typed object creation only when the same explicit type is immediately visible on a local, field, or property initializer.

Capabilities:

```text
normalize-remediate
```

The rule requires C# 9 or later in the loaded project's effective parse options and verifies that Roslyn binds the explicit creation to the declared target type. Contextual expressions such as returns, `var` declarations, comments or directives embedded in the removed type syntax, and unsupported language versions are left unchanged. Compiler validation of the full rewrite plan remains required before commit.

It emits no normal `check` finding.

### `style.whitespace.blank-lines` v1

Purpose: remove file-edge blank lines and reduce runs of more than two ordinary blank lines to two.

Capabilities:

```text
normalize-remediate
```

Runs overlapping multiline string/interpolated-string tokens, comments, documentation trivia, directives, or disabled text are retained. Interior runs of one or two blank lines remain unchanged to preserve visual grouping. Existing line endings are retained, and the rule does not wrap or reindent code.

It emits no normal `check` finding.

### `style.null-forgiving.redundant` v1

Purpose: remove a null-forgiving operator only when nullable flow analysis proves the operand was already not-null at that source position.

Capabilities:

```text
normalize-remediate
```

The rule uses the operand's Roslyn nullable flow state before the suppression operator. Maybe-null or unknown flow, error types, comments, directives, and disabled text are retained. It does not infer safety from the result type of the suppression expression.

It emits no normal `check` finding.

### `style.parentheses.redundant` v1

Purpose: remove parentheses only around an uncomplicated primary expression when Roslyn confirms that the inner and outer expressions have identical type, converted type, and symbol binding.

Capabilities:

```text
normalize-remediate
```

The rule considers identifiers, literals, `this`, and `base`. Compound expressions remain grouped, and comments, directives, or disabled text inside or around a candidate are retained. It does not guess about precedence or change a semantic review decision.

It emits no normal `check` finding.

### `style.usings.unused` v1

Purpose: remove imports only when the pinned Roslyn IDE0005 analyzer and code fix report them unnecessary.

Capabilities:

```text
normalize-remediate
```

The tool bundles the matching Microsoft.CodeAnalysis.CSharp.CodeStyle 5.0.0 analyzer and fix assemblies. The Roslyn fix owns import selection and removal. A source file with a comment- or directive-bearing using directive is left unchanged so its trivia remains intact. Required imports and alias/global/conditional semantics are delegated to Roslyn's analyzer. It emits no normal `check` finding.

### `style.usings.organize` v1

Purpose: deterministically order adjacent plain namespace imports while retaining import semantics and trivia.

Capabilities:

```text
normalize-remediate
```

Adjacent imports without aliases or static/global modifiers are sorted with ordinal-ignore-case and ordinal name order. The `dotnet_sort_system_directives_first` analyzer-config option controls whether `System` namespaces precede other imports (default: true). Alias, static, global, comment-bearing, and directive-bearing imports break a sortable group. Conditional regions and comments remain attached to their original directives. Imports are never removed by this rule.

It emits no normal `check` finding.

### `style.modifiers.organize` v1

Purpose: order declaration modifiers by the effective `csharp_preferred_modifier_order` value used by Roslyn IDE0036.

Capabilities:

```text
normalize-remediate
```

When the option is absent, the rule uses Roslyn's conventional C# modifier order. Declarations containing a modifier absent from the effective order, or comments, directives, or disabled text in modifier trivia, remain unchanged. The rewrite moves only modifier tokens; it does not change declarations or member order.

It emits no normal `check` finding.

### `style.default.literal` v1

Purpose: replace `default(T)` with `default` only for an explicitly typed local, field, or property initializer whose declared symbol type exactly matches the default expression type.

Capabilities:

```text
normalize-remediate
```

The rule requires C# 7.1 or later and leaves `var` declarations, return expressions, comment/directive-bearing type syntax, and other contextual expressions unchanged. Compiler validation of the complete rewrite plan remains required.

It emits no normal `check` finding.

### `style.cast.redundant` v1

Purpose: remove only casts for which Roslyn reports an identity conversion and the operand's exact nullable type equals the cast type.

Capabilities:

```text
normalize-remediate
```

Comment/directive-bearing candidates are left unchanged. Parentheses are retained or reduced only by Roslyn's simplifier, and the resulting project compilation is validated before commit.

It emits no normal `check` finding.

### `style.interpolation.redundant` v1

Purpose: replace a constant interpolated string with ordinary string-literal syntax when it contains only text and no interpolation holes.

Capabilities:

```text
normalize-remediate
```

The string's Roslyn constant value is retained. Multiline and raw interpolated strings, actual interpolation holes, and comment/directive-bearing expressions are left unchanged.

It emits no normal `check` finding.

### `style.names.inferred` v1

Purpose: omit a tuple or anonymous-object member name when an identifier expression unambiguously infers the exact same name.

Capabilities:

```text
normalize-remediate
```

Only simple identifier expressions with exact ordinal name matches are shortened. Mismatches and comment/directive-bearing names are retained.

It emits no normal `check` finding.

## Default compatibility target

With all new M0010 configurable rules enabled:

- generated braces/accessibility EditorConfig policy is effectively equivalent to profile v1;
- `hygiene format` retains the existing Roslyn formatter behavior;
- `hygiene normalize` retains the existing supported simplification + formatting behavior, with explicit target-typed creation, required-braces remediation, Roslyn-backed unused-import removal, and safe using organization added by M0023;
- existing documentation/readability/review rules retain their diagnostic contracts and versions.

A deliberate disable changes only the disabled rule's owned capabilities.

## Canonical rule order after M0023

```text
1.  profile.dotnet.analysis.required          v2
2.  profile.stylecop.prohibited               v1
3.  style.braces.required                     v1
4.  style.accessibility.explicit              v1
5.  format.csharp.roslyn                      v1
6.  style.qualification.this.unnecessary      v1
7.  style.qualification.redundant              v1
8.  style.object-creation.target-typed         v1
9.  style.default.literal                    v1
10. style.cast.redundant                      v1
11. style.interpolation.redundant              v1
12. style.names.inferred                     v1
13. style.usings.unused                       v1
14. style.usings.organize                     v1
15. style.modifiers.organize                  v1
16. style.null-forgiving.redundant            v1
17. style.whitespace.blank-lines              v1
18. style.parentheses.redundant               v1
19. docs.summary.required                     v2
20. docs.xml.consistent                       v1
21. docs.text.sentence                        v1
22. docs.summary.quality.review               v6
23. docs.summary.language.german.review       v4
24. readability.long-line.review              v1
25. readability.control-flow.visual-block     v2
26. architecture.boringness.review            v3
27. readability.type-responsibility.review    v1
28. tests.quality.review                      v1
29. comments.intent.review                    v1
30. modernization.var-style                  v1
31. modernization.expression-body            v1
32. modernization.collection-expression      v1
33. modernization.primary-constructor         v1
34. modernization.switch-expression           v1
35. modernization.conditional                 v1
36. modernization.object-initializer           v1
37. modernization.readonly-field               v1
38. modernization.inline-declaration           v1
```

The relative order of all pre-M0011 rules remains unchanged. M0017 appends its semantic-review rules after the B-3 Architecture rule. M0023 appends the nine analyzer-backed modernization review families after the existing catalog; their occurrence decisions and shared budget are specified in `docs/specs/SEMANTIC-REVIEWS.md` and `docs/specs/SAMPLING.md`.

## Non-goals

M0010 does not:

- convert every formatter option into a hygiene rule;
- make `readability.control-flow.visual-block` auto-fixable;
- add direct source rewrites for braces/accessibility before M0023;
- add arbitrary repository parameters for style rules;
- add a public plugin API;
- add reflection discovery, DI, a rule dependency graph, or a policy DSL;
- add model/provider invocation;
- change statistical sampling behavior;
- add unrelated style/modernization rules.
