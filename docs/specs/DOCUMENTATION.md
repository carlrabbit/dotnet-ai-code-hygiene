# Documentation Hygiene

## Principle

M0005 requires only a generic documentation summary for covered API subjects.

It does **not** require complete XML documentation for parameters, type parameters, return values, exceptions, values, remarks, examples, or other optional elements.

Optional documentation that is present must satisfy the applicable deterministic structural/prose checks.

## Documentation summary and carriers

"Documentation summary" is semantic, not XML-tag-specific.

For each covered subject, resolve a summary carrier:

```text
ordinary source-declared covered symbol
  -> non-empty <summary> on its declaration

synthesized property created by a positional record/record-struct parameter
  -> non-empty matching <param name="..."> on the record declaration

direct <inheritdoc/> on the source declaration
  -> satisfies otherwise-missing summaries represented by that declaration
```

A record declaration therefore may carry:

- its own type summary in `<summary>`;
- summaries for synthesized positional properties in matching `<param>` elements.

A normal method/constructor parameter does not become required merely because another parameter is documented.

Primary-constructor parameters on ordinary classes/structs do not become summary subjects unless they correspond to an independently covered API symbol.

## `docs.summary.required`

Version: `2`  
Classification: finding  
Configurable: yes

Rationale: summaries reduce human and agent reconstruction cost at important source-level API subjects. Public and internal coverage is deliberate comprehension policy and is not a proxy for externally shipped API surface. Requiring only a generic summary avoids completeness pressure and boilerplate volume.

Covered API subject categories remain:

- source-declared public/internal named types: class, struct, interface, enum, record, record struct, delegate;
- source-declared public/internal constructors;
- source-declared public/internal ordinary methods;
- source-declared public/internal properties;
- public/internal positional-record properties whose source representation is a record positional parameter.

Continue to exclude:

- accessors;
- operators/conversions;
- explicit interface implementations;
- fields;
- events;
- local functions;
- ordinary parameters/type parameters as independent documentation subjects.

A covered subject satisfies the rule when its resolved summary carrier is non-empty, or when direct `<inheritdoc/>` satisfies the declaration-level missing-summary requirement.

For this exemption, `<inheritdoc/>` must be a direct child of the declaration's documentation comment. A nested `<inheritdoc/>` inside `<remarks>`, `<example>`, or another element does not satisfy the declaration summary.

The rule does not recursively resolve an inheritdoc target or inspect ancestor documentation.

`<inheritdoc/>` only excuses missing inherited summary material. Explicit documentation written beside it is still subject to structural and sentence rules.

Version 1 ignore occurrences do not silently migrate to version 2. Existing v1 ignore decisions naturally become stale under the existing rule-version matching model.

## `docs.xml.consistent`

Version: `1`  
Classification: finding  
Configurable: yes

Rationale: XML documentation remains optional, while prose and structure that are present should not be misleading, orphaned, duplicated, or structurally invalid. Presence-based checks preserve useful documentation without creating completeness pressure.

This is presence-based. It never requires optional documentation merely because another optional element is present.

### `<param>`

When present:

- `name` must be non-empty;
- it must identify an actual parameter of the documented declaration: method/constructor parameters, delegate parameters, indexer parameters, or positional record parameters on the record declaration;
- there is at most one `<param>` for the same parameter name on the same declaration;
- prose content must be non-empty.

The matching `<param>` for a synthesized positional-record property also serves as that property's required summary carrier.

### `<typeparam>`

When present:

- `name` must identify an actual type/method type parameter in that declaration;
- it must be declared by the documented declaration itself; a containing type parameter is not a `<typeparam>` of a method or nested declaration;
- there is at most one `<typeparam>` for the same type parameter;
- prose content must be non-empty.

### `<paramref>` / `<typeparamref>`

When present inside documentation prose, `paramref` must identify a parameter of the documented declaration. `typeparamref` may identify a type parameter declared on that declaration or an enclosing type parameter in scope. These reference scopes do not expand which declaration parameters `<param>` and `<typeparam>` may document.

### `<returns>`

When present:

- at most one per documentation comment;
- non-empty prose;
- valid only for a callable declaration that returns a value/non-void type.

It is never required by this rule.

### `<value>`

When present:

- at most one per documentation comment;
- non-empty prose;
- valid only for a property/indexer documentation subject.

It is never required.

### `<exception>`

When present:

- `cref` must resolve to an exception type;
- prose must be non-empty.

The rule does not try to prove that the documented exception is actually thrown and does not require exception documentation.

### Other XML documentation

Other valid XML documentation elements are not forbidden merely because M0005 does not understand or require them.

In particular, no completeness/presence requirement is introduced for:

```text
remarks
example
code
c
see
seealso
list
custom XML elements
```

Malformed XML documentation continues to follow compiler/Roslyn parsing behavior as applicable; M0005 does not create a parallel XML parser policy.

## `docs.text.sentence`

Version: `1`  
Classification: finding  
Configurable: yes

Rationale: a cheap deterministic baseline catches visibly unfinished documentation without pretending to judge semantic quality. Its scope is intentionally narrow and mechanical; semantic prose quality remains review work.

Explicit non-empty prose in these elements must end, after trimming trailing whitespace, with:

```text
.
?
!
```

Elements:

```text
summary
param
typeparam
returns
value
exception
```

The check examines the final textual/prose content across inline XML. An inline element at the end without following sentence punctuation does not satisfy the rule.

Examples:

```xml
<summary>Returns the configured <see cref="Widget"/>.</summary>
```

passes.

```xml
<summary>Returns the configured <see cref="Widget"/></summary>
```

fails.

This rule does not evaluate whether the prose is semantically good, complete, German, or useful.

It does not apply to `<inheritdoc/>`, `<remarks>`, `<example>`, `<code>`, `<c>`, `<see>`, `<seealso>`, `<list>`, or arbitrary/custom tags as independent sentence units.

## Summary quality review

`docs.summary.quality.review` is version `4`; it uses subject-state sampling with explicit accepted observations. `docs.summary.language.german.review` is version `2` and maintains independent subject-state. Both contracts are defined in `docs/specs/SEMANTIC-REVIEWS.md`.

Version 4 uses subject-state statistical sampling with a maximum normal budget of five due subjects and explicit accepted observations. Its language-neutral rubric remains separate from the independent German review policy.

Population changes from literal `<summary>` elements to explicit non-empty documentation summaries resolved through the summary-carrier model:

- ordinary direct `<summary>`;
- matching `<param>` carrying a synthesized positional-record property's summary.

An inheritdoc-only subject has no local prose and is excluded from the semantic review population.

Optional method/constructor parameter documentation is not a summary subject and is not sampled merely because a `<param>` exists.

Subject identity remains the API subject identity; content fingerprint uses the normalized local summary-carrier content.

## Canonical normal-rule order

Canonical normal order:

```text
1. profile.dotnet.analysis.required         v1  mandatory finding
2. profile.stylecop.prohibited              v1  mandatory finding
3. docs.summary.required                    v2  configurable finding
4. docs.xml.consistent                      v1  configurable finding
5. docs.text.sentence                       v1  configurable finding
6. docs.summary.quality.review              v4  configurable review-batch
7. docs.summary.language.german.review      v2  configurable review-batch
8. readability.long-line.review             v1  configurable review-candidate
9. readability.control-flow.visual-block    v1  configurable finding
10. architecture.boringness.review          v1  configurable review-batch
```

Structural documentation findings are evaluated before semantic summary review.

## Non-goals

M0005 does not enforce:

- parameter completeness;
- type-parameter completeness;
- return-value documentation presence;
- exception-documentation presence;
- remarks/examples presence;
- minimum documentation length;
- capitalization;
- semantic quality through deterministic heuristics;
- recursive inheritdoc resolution;
- StyleCop rule-number compatibility.
