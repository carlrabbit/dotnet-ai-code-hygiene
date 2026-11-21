# Autoformat Readability Canonicalization

These policies run during default normalization. They are deterministic layout rules, not semantic-review findings.

## Visual blocks

Within a block, insert a blank line at a boundary between ordinary linear statements and control-flow statements. Existing blank boundaries remain. Leading comments stay attached to the statement they describe; `else`, `catch`, `finally`, directives, documentation trivia and related clauses are not separated. Nested blocks use the same rule.

## Parameter declarations

Methods and comparable declarations with three or more parameters use one parameter per line. Two-parameter declarations also wrap when the parameter list exceeds the line target. The source newline style is preserved.

## Long lines

120 characters is a soft target. The formatter adds line breaks at recognized syntax boundaries in long invocation lists, collection expressions, initializers, compound boolean expressions and fluent member chains. It does not split names, literals, comments, directives or XML prose. If no recognized syntax boundary can be used, the line remains intact and Autoformat reports the count of remaining overlong lines.

## Comments and whitespace

The SDK whitespace phase formats indentation for code and ordinary leading comments while preserving comment text, strings and source newline style. Custom syntax transformations skip comment-bearing constructs when preserving trivia is uncertain. Visual-block rules add the required boundary at statement transitions; otherwise, whitespace-only runs of excess blank lines are reduced to one blank line. Comment and directive trivia is left intact.
