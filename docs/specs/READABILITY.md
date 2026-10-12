# Readability Rules

Readability rules may identify deterministic patterns for review. A match is not automatically a defect. `format` and `normalize` do not apply semantic readability changes.

## `readability.long-line.review`

Version: `1`  
Classification: review-candidate  
Configurable: yes

A physical C# source line longer than 200 UTF-16 code units produces one deterministic review candidate. Lines at or below 200 do not. The rule asks whether the line hides multiple concepts or structures that should be made visible or named. Length alone does not prove a defect, so it never wraps a line mechanically.

## `readability.control-flow.visual-block`

Version: `2`
Classification: review-candidate
Configurable: yes

Within a block, inspect each statement after the first. The rule applies when the current statement is an `if`, `switch`, `for`, `foreach`, `foreach` with variable, `while`, `do`, `try`, `using`, or `lock`, and the immediately previous statement is not control-flow and is not a local function. It reports when the current control-flow statement has no blank line after the previous statement and before the first leading comment for the control-flow statement, or before the statement when there is no such comment. An existing blank line in that boundary suppresses the candidate. No candidate is produced for a first statement, a control-flow statement following another control-flow statement, or a control-flow statement following a local function. Comments documenting the control-flow statement stay attached to it.

The match is an inspection signal, not proof of poor readability. Review conceptual grouping and choose whether to leave code unchanged, add whitespace, or restructure it. It has no automatic formatter or fixer; `format` and `normalize` do not alter it.

## `readability.type-responsibility.review`

Version: `1`, semantic batch B-4, configurable and enabled by default. Reviews source-backed class, struct, interface, and record declarations, including nested and partial parts. Its fixed questions cover coherent ownership, reconstruction of unrelated state/workflows, and concrete responsibility separation without needless fragmentation. The normal reviewer is `implementer`; escalate material concerns or insufficient context to `planner`. Type length and member count are not verdicts.
