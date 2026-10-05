# Readability Rules

## `readability.long-line.review`

Version: `1`  
Classification: review-candidate  
Configurable: yes

A physical C# source line longer than 200 UTF-16 code units produces one deterministic review candidate. Lines at or below 200 do not. The rule reports the line and asks whether it hides multiple concepts or structures that should be made visible or named. Length alone does not prove a defect, so it never wraps a line mechanically. Very long lines can raise reconstruction cost by hiding structure and concepts.

## `readability.control-flow.visual-block`

Version: `1`  
Classification: finding  
Configurable: yes

Within a block, inspect each statement after the first. The rule applies when the current statement is an `if`, `switch`, `for`, `foreach`, `foreach` with variable, `while`, `do`, `try`, `using`, or `lock`, and the immediately previous statement is not control-flow and is not a local function. It requires one completely blank line after the preceding statement and before the first leading comment for the control-flow statement, or before the statement when there is no such comment. An existing blank line in that boundary suppresses the finding. No finding is produced for a first statement, a control-flow statement following another control-flow statement, or a control-flow statement following a local function. Comments documenting the control-flow statement stay attached to it.

The blank line is structural punctuation between linear work and a control-flow group. AI-generated code can remain syntactically formatted while visually dense; explicit boundaries improve scanning and human catch-up. This is deliberate readability policy, not formatter output.
