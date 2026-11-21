# Autoformat Canonicalization

## Commands and ownership

`dotnet autoformat` is an independent source canonicalizer. `init` establishes or validates Autoformat's fixed mechanical `.editorconfig` block. It does not read Hygiene profiles, review state, approvals or audit state. Transitional `dotnet hygiene` delegates formatting to the same engine and retains its semantic-review/lifecycle host.

`format` runs SDK whitespace formatting only. Default mode and `normalize` run the source-only C# canonicalizer, the approved SDK style diagnostics, and SDK whitespace formatting. Explicit paths select supported SDK-project C# source. `--changed` selects staged, unstaged and untracked C# files and implies check-only. `--check` and `--changed` do not write source or policy state.

Mutating runs require a clean Git worktree/index and known `HEAD`; the engine records and verifies `HEAD` and the index. It never builds or tests the target project, checks semantic equivalence, commits, pushes, resets or rolls back. A failure leaves partial edits inspectable for the implementing agent to repair.

## Canonicalization portfolio

`normalize` applies these fixed, non-configurable canonicalization IDs:

| ID | Default transformation |
|---|---|
| `modernization.var-style` | Prefer an explicit type when syntax gives a direct type; leave `var` for anonymous/unknown cases. |
| `modernization.expression-body` | Convert single-expression methods and getter-only properties to expression bodies. |
| `modernization.collection-expression` | Convert eligible arrays and list initializers to C# 12 collection expressions, including interface targets even when concrete runtime type changes. |
| `modernization.primary-constructor` | Convert a single constructor that only copies parameters into fields on eligible classes/structs. |
| `modernization.switch-expression` | Convert simple return-valued switch statements with constant labels and a default arm. |
| `modernization.conditional` | Convert simple return and assignment `if/else` forms, including single-statement blocks. |
| `modernization.object-initializer` | Merge a local construction and its following simple member assignment when comments and invocation side effects are absent. |
| `modernization.readonly-field` | Mark fields readonly when every recognized assignment is constructor-owned. |
| `modernization.inline-declaration` | Inline a one-use, uninitialized local into the next `out` argument. |

The canonicalizer is syntax-directed and uses the SDK project's parse options. It does not load a semantic model or require successful compilation. It skips source-comment-bearing constructs when a rewrite would lose trivia. Unsupported patterns remain unchanged; reports include invalid retain annotations and remaining lines over the long-line target.

## Source-local retain annotations

Put an annotation in leading trivia immediately above the single construct to retain:

```csharp
// autoformat:retain modernization.collection-expression -- Interop API requires a concrete List<T>.
IReadOnlyList<int> values = new List<int> { 1, 2 };
```

The annotation must have the form `// autoformat:retain <rule-id> -- <non-empty justification>`. It exempts only that construct from that rule. Other canonicalizations still run. Unknown, malformed or unjustified annotations are reported. Annotations stay in the source. Autoformat does not treat old `hygiene:retain` annotations as exemptions.

## Layout and whitespace

The syntax phase adds visual separation around ordinary statements and control-flow statements, places method/signature parameter lists with three or more parameters vertically, and adds stable breaks to long invocation argument lists, collection expressions, initializers, boolean chains and fluent member chains where syntax provides safe boundaries. The 120-character goal is a soft target; identifiers, literals, comments, directives and XML prose are not split. Ordinary SDK whitespace formatting handles indentation, including comments, and the source newline style is carried through custom line breaks.

## SDK style phase

`normalize` applies only this SDK diagnostic set before whitespace formatting:

```text
IDE0005, IDE0011, IDE0036, IDE0040, IDE0090, IDE0034, IDE0004, IDE0037
```

Autoformat owns the corresponding fixed mechanical settings. The SDK supplies the analyzer/fixer assemblies; Autoformat does not bundle duplicate Roslyn code-style assets. No generic analyzer-fix pass runs.

## Reports and recovery

Results identify actual phases, selected canonicalization IDs, selected and changed paths, and notices for invalid annotations or unsupported long lines. Check-only combines proposed syntax changes with SDK-reported changes. Mutating results compare source snapshots. If `HEAD` or the index changes during a run, Autoformat reports incomplete state and leaves recovery to the agent.
