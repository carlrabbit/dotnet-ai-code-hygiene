# Autoformat Canonicalization

## Commands and ownership

`dotnet autoformat` is an independent source canonicalizer. `init` establishes or validates Autoformat's fixed mechanical `.editorconfig` block. It does not read Hygiene profiles, review state, approvals or audit state. Transitional `dotnet hygiene` delegates formatting to the same engine and retains its semantic-review/lifecycle host.

`format` runs SDK whitespace formatting only. Default mode and `normalize` apply source modernizations and readability layout, run approved SDK style diagnostics and SDK whitespace formatting, then reapply the syntax layout rules so the SDK formatter cannot undo opinionated layouts. SDK formatting uses the SDK's normal implicit dependency restore by default; explicit `--no-restore` skips that restore for the style and whitespace phases in `format`, `normalize`, default normalization, and check-only execution. Explicit paths select supported SDK-project C# source. `--changed` selects staged, unstaged and untracked C# files and implies check-only. Check-only normalization runs that same complete pipeline against a temporary copy of the selected project context and reports final source differences without writing to the repository; the opt-in `--no-restore` choice is carried through to that isolated run.

Mutating runs require a clean Git worktree/index and known `HEAD`; the engine records and verifies `HEAD` and the index. It never builds or tests the target project, checks semantic equivalence, commits, pushes, resets or rolls back. A failure leaves partial edits inspectable for the implementing agent to repair.

## Canonicalization portfolio

`normalize` applies these fixed, non-configurable canonicalization IDs:

| ID | Default transformation |
|---|---|
| `modernization.var-style` | Prefer an explicit type when syntax gives a direct type; leave `var` for anonymous/unknown cases. |
| `modernization.expression-body` | Convert single-expression methods and getter-only properties to expression bodies. |
| `modernization.collection-expression` | Convert array and `List<T>` initializers to C# 12 collection expressions only when a recognized explicit variable, property or return type supplies a supported array, list or collection-interface target. Preserve creation syntax in untargeted contexts such as `foreach` sources and invocation arguments. Eligible interface targets may change the concrete runtime collection implementation. |
| `modernization.primary-constructor` | Convert a single explicitly public constructor that only copies parameters into fields on eligible classes/structs. |
| `modernization.switch-expression` | Convert simple return-valued or same-target assignment switches with constant labels and a default arm. |
| `modernization.conditional` | Convert simple return and assignment `if/else` forms, including single-statement blocks. |
| `modernization.object-initializer` | Merge a local construction and its following simple member assignment when comments and invocation side effects are absent. |
| `modernization.readonly-field` | Mark fields readonly when every recognized assignment is constructor-owned. |
| `modernization.inline-declaration` | Inline an uninitialized local into the next eligible `out` argument, retaining subsequent variable uses in scope. |

The canonicalizer is syntax-directed and uses the SDK project's parse options. It does not load a semantic model or require successful compilation. It skips source-comment/directive-bearing constructs when a rewrite would lose trivia. Collection-expression conversions require a recognized direct target from an explicit variable/property declaration or method/local-function return type. Supported targets are rank-one arrays, `List<T>`, and the standard generic collection interfaces; no target is inferred from `foreach`, invocation arguments, or general assignment expressions. This conservative syntax boundary prevents invalid untargeted collection expressions while retaining known typed conversions. Target-typed `new()` with an initializer converts only when its direct declared target is syntactically `List<T>`; arbitrary DTO/object initializers remain object initializers. Array conversions require rank-one array levels and flat array-initializer entries. List conversions require a flat collection initializer; object/member assignments and nested brace initializers are left unchanged. Unsupported patterns remain unchanged; reports include invalid retain annotations and remaining lines over the long-line target.

Primary-constructor conversion skips constructors whose accessibility is not explicitly public, or which have attributes, comments, directives, constructor initializers, or other syntax outside the direct field-assignment shape. This avoids changing restricted construction to public construction. Type-level `autoformat:retain modernization.primary-constructor` annotations suppress only that type's conversion.

## Source-local retain annotations

Put an annotation in leading trivia immediately above the single construct to retain:

```csharp
// autoformat:retain modernization.collection-expression -- Interop API requires a concrete List<T>.
IReadOnlyList<int> values = new List<int> { 1, 2 };
```

The annotation must have the form `// autoformat:retain <rule-id> -- <non-empty justification>`. It exempts the directly annotated statement/declaration from that rule; expression rewrites resolve the annotation through their owning statement/declaration. `readability.comments` applies to the annotated declaration's comment and whitespace normalization. Other canonicalizations still run. Unknown, malformed or unjustified annotations are reported. Annotations stay in the source. Autoformat does not treat old `hygiene:retain` annotations as exemptions.

## Layout and whitespace

The syntax phase adds visual separation around ordinary statements and control-flow statements, places method/signature parameter lists with three or more parameters vertically with the closing delimiter on its own line, and adds stable breaks to long invocation argument lists, collection expressions, initializers and boolean chains where syntax provides safe boundaries. It keeps receiver/member-access pairs together and preserves existing multiline fluent chains without inserting an empty line before `.`. The 120-character goal is a soft target; identifiers, literals, comments, directives and XML prose are not split. Ordinary SDK whitespace formatting handles indentation, including comments, and the source newline style is carried through custom line breaks.

## SDK style phase

`normalize` applies only this SDK diagnostic set before whitespace formatting:

```text
IDE0005, IDE0011, IDE0036, IDE0040, IDE0090, IDE0034, IDE0004, IDE0037
```

Autoformat owns the corresponding fixed mechanical settings. The SDK supplies the analyzer/fixer assemblies; Autoformat does not bundle duplicate Roslyn code-style assets. No generic analyzer-fix pass runs.

## Reports and recovery

Results identify actual phases, selected canonicalization IDs, selected and changed paths, and notices for invalid annotations or unsupported long lines. Check-only combines proposed syntax changes with SDK-reported changes. Mutating results compare source snapshots. If `HEAD` or the index changes during a run, Autoformat reports incomplete state and leaves recovery to the agent.
