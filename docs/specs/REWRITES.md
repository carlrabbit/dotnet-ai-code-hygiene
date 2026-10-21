# Deterministic Rewrite Behavior

## Common targeting

`format` and `normalize` use the same repository and target-selection concepts as `check`:

```text
dotnet hygiene format
dotnet hygiene format --changed
dotnet hygiene format <file|directory> [...]

dotnet hygiene normalize
dotnet hygiene normalize --changed
dotnet hygiene normalize <file|directory> [...]
```

Explicit paths and `--changed` are mutually exclusive. Repository discovery, relative paths, exclusions, SDK-style project membership, changed-file semantics, outside-repository rejection, and overlap de-duplication align with `check`.

## Common output

Both commands support `--output text|json` and `--check`.

`--check` computes the same rewrite plan without committing source changes. A successful command returns exit `0` whether zero or more changes are needed/applied; nonzero exits retain existing error semantics.

JSON schema version 1 exposes: `command`, `checkOnly`, `targetCount`, `changedCount`, `unchangedCount`, deterministic repository-relative `changedPaths`, and ordered `selectedRuleIds`.

## Mutation transaction

Rewrite commands are all-or-nothing over the complete resolved target set:

```text
resolve all targets
-> load required Roslyn context
-> compute complete rewrite plan in memory
-> validate complete plan
-> if --check: report only
-> else commit plan
```

No selected source file is replaced before plan construction and required validation succeed for every selected target. Commit uses atomic per-file replacement plus rollback protection sufficient to guarantee that failure/cancellation does not leave a mixed old/new selected target set. External modification between read/plan and commit is rejected rather than silently overwritten.

## Formatting

`dotnet hygiene format` selects enabled `format-remediate` rules from the canonical rule catalogue. The autonomous lifecycle also invokes `dotnet-format` whitespace formatting and restricts style fixes to enabled hygiene-owned IDE0011/IDE0040 diagnostic IDs. It never runs generic analyzer fixes. The initial `format.csharp.roslyn` rule owns Roslyn formatting against actual loaded document/project context and applicable project/editor configuration available to Roslyn. It may change whitespace/trivia presentation but must not intentionally perform semantic simplification, structural modernization, naming changes, or normalization policy rewrites.

Formatting is idempotent and does not require a clean semantic compilation when Roslyn can load and parse the selected source sufficiently.

## Normalization

`dotnet hygiene normalize` selects enabled `normalize-remediate` rules from the canonical rule catalogue, then applies enabled format rules to changed documents. It is deterministic, project-aware, and semantics-preserving.

Relevant loaded projects must compile without compiler errors before transformation. Rewritten relevant project context must also compile without compiler errors before commit. Changed normalized documents are formatted before final comparison/commit.

Normalization is idempotent.

The autonomous bootstrap approval snapshot covers relevant source and configuration bytes. Before approval, changing those bytes invalidates the pending baseline report; after acceptance, ordinary source changes remain eligible for routine gates and only a material policy snapshot change requires another baseline approval.

## Initial normalization catalogue

M0004 intentionally limits normalization to Roslyn-backed simplification where semantic services establish equivalence:

1. `style.qualification.this.unnecessary` removes only explicit `this.` qualification proven redundant by Roslyn;
2. `style.qualification.redundant` simplifies other redundant member/type/namespace/name qualification where Roslyn proves equivalence;
3. `style.accessibility.explicit` adds the language-defined default access modifier to supported type and member declarations, while skipping static constructors and explicit-interface implementations.
4. `style.object-creation.target-typed` converts `new T(...)` only for local, field, or property initializers whose explicit declared type is exactly the Roslyn-bound creation type. It requires effective C# 9 or later and leaves return expressions, inferred declarations, and commented/directive-bearing type syntax unchanged.
5. `style.default.literal` shortens `default(T)` only in local, field, or property initializers whose explicit declared symbol type is exactly the default expression's Roslyn type. It requires effective C# 7.1 or later and leaves `var`, return expressions, and commented/directive-bearing type syntax unchanged.
6. `style.cast.redundant` removes a cast only when Roslyn reports an identity conversion and the operand's exact nullable type equals the cast type. Comment/directive-bearing candidates are retained, and Roslyn's simplifier owns any required parentheses.
7. `style.interpolation.redundant` replaces only constant interpolated strings with no interpolation holes, retaining the Roslyn constant value and skipping multiline, raw-string, and comment/directive-bearing expressions.
8. `style.names.inferred` removes tuple/anonymous-object names only when a simple identifier expression infers the exact same ordinal name; mismatches and comment/directive-bearing names remain.
9. `style.usings.organize` orders contiguous uncomplicated namespace imports by name and honors `dotnet_sort_system_directives_first` (default true). It leaves aliases, `static`/`global` imports, comments, directives, and conditional regions in place as group boundaries and never removes imports.
10. `style.null-forgiving.redundant` removes `!` only when Roslyn's nullable flow state for the operand is already not-null. Maybe-null or unknown flow, error types, comments, directives, and disabled text are retained.
11. `style.whitespace.blank-lines` trims file-edge blank lines and reduces runs of more than two ordinary blank lines to two. It skips runs touching multiline strings, comments, documentation trivia, directives, or disabled text, and preserves one- or two-line interior grouping.
12. `style.parentheses.redundant` removes parentheses only around an identifier, literal, `this`, or `base` expression when Roslyn confirms identical type, converted type, and symbol binding. Compound grouping and comment/directive-bearing candidates remain unchanged.

Required accessibility, target-typed creation, default literals, redundant casts, redundant interpolation, and inferred names are readability-oriented, rule-owned normalizers. Target-typed creation and default literals apply only with an immediately visible declared type. Other modernization families remain review decisions and are not automatic normalization.

These are independent fixed policies, each enabled by default and independently configurable as a whole rule.

Do not introduce transformations for `var` policy, expression-bodied members, collection expressions, target-typed `new`, naming, or broad modernization. Modifier ordering is a separate M0023 requirement and remains outstanding.

## Relationship to check

Rewrite commands do not automatically execute `hygiene check`.

Canonical agent workflow:

```text
implement/change code
-> run relevant tests
-> hygiene normalize
-> hygiene check
-> resolve deterministic findings and semantic review work
-> rerun/recheck as needed
```

No rewrite history database is introduced.
