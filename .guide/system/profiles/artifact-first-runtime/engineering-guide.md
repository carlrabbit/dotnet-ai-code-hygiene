# Artifact-First Runtime Engineering Profile

## Generic building blocks

| Block | Purpose |
|---|---|
| Headless product CLI | Product operations are exposed through deterministic commands. |
| Structured project data | Source data is schema-validated, stable-ID-based, diffable, and reviewable. |
| Scenario-based validation | Deterministic scenarios produce structured results and artifacts. |
| Artifact evidence and reports | Produce inspectable artifacts where behavior or review depends on them; not a mandatory proof for every criterion. |
| Generated/derived artifact pipeline | Derived outputs have provenance and rebuild rules. |
| Human review gates | A milestone may require human acceptance of evidence automation cannot judge. |
| Dual-mode runtime validation | Debug and release representations preserve semantics. |
| Resumable validation suites | Optional for suites whose measured cost warrants stages, receipts, or aggregate verification. |

## Human review

For artifact-first projects, human review commonly applies to:

- visual output;
- semantic classifications;
- UX flows;
- creative artifacts;
- generated content;
- release evidence.

A review is owned by the milestone that produced the evidence.

Only explicitly `required` human review gates that milestone's completion; `recommended` review is nonblocking and ordinary engineering review need not produce a formal record. Once the milestone completes, the record remains historical and is not revalidated against later repository changes.

A provider repository validates review-system mechanics.

A consumer repository performs domain review of its authored product or content.

A mixed/dogfood repository may do both with bounded dogfood scope.
