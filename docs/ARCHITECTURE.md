# Architecture

## System shape

```text
CLI command
-> command policy / rule-set selection
-> RepositorySession
   -> repository root + target resolution
   -> Git/change context
   -> SDK project discovery
   -> one Roslyn/MSBuild workspace
   -> project/document assignment
   -> lazy syntax/semantic/compilation access
   -> lazy session facts

check/bootstrap/update
-> explicit RuleCatalog
-> independent in-process rule modules
-> host result materialization
-> ignore/filter/order/persistence

format/normalize
-> explicit RewriteCatalog
-> independent rewrite modules
-> shared rewrite transaction
```

The architecture deliberately separates **module independence** from **process independence**. A rule or rewrite is a small logical program with its own analysis/transformation logic, but all modules for one command run in-process over one shared command-scoped substrate.

## Repository session

`RepositorySession` is the conceptual command-scoped owner of expensive and reusable repository context. Exact type/file names are implementation choices, but the boundary is authoritative.

The session owns or coordinates:

```text
repository root
selected reporting targets
repository/Git change context
SDK project discovery
MSBuildWorkspace lifetime
project/document mapping
source text / syntax trees
compilations / semantic models
lazy shared facts
```

Check and rewrite commands use the same repository/session substrate rather than maintaining independent target/project/workspace implementations.

Expensive context is lazy and cached only for the lifetime of the command session. Supporting a capability must not make every invocation eagerly pay for it. There is no M0007 durable analysis cache.

## Reporting scope versus readable context

The architecture distinguishes:

```text
reporting scope
from
context analysis is allowed to read
```

A changed-file or explicitly targeted run may read unchanged project/repository context required for correct analysis while emitting source findings/review work only for the selected reporting scope, except where an existing repository/profile rule contract deliberately has repository-level subjects.

Targeting semantics remain product behavior and are not delegated independently to each rule.

## Rule model

A rule ID denotes one fixed semantic contract as specified elsewhere.

Implementation architecture is conceptually:

```text
RuleDescriptor
  identity/version/output metadata/order/participation

RuleModule
  evaluate immutable RuleContext
  perform domain-specific traversal/querying
  emit rule-domain occurrences or review work

RuleRunner
  execute the explicitly selected catalog
```

Exact interfaces are implementation freedom. The architecture requires the following properties:

- every current rule has a dedicated module/evaluator rather than rule-specific branches embedded in one central document traversal;
- production rule registration is explicit and static;
- canonical rule order is catalog metadata and remains deterministic independently of internal execution choices;
- the host selects mandatory/enabled rules before execution; an ordinary rule does not reinterpret repository configuration into rule-specific parameters;
- rule modules may traverse syntax/documents independently when that is the simplest implementation;
- there is no central `OnSyntaxNode`/`OnSymbol` callback framework, rule dependency DAG, or required-context declaration language in M0007;
- there is no reflection-based or third-party plugin discovery surface.

Repeated cheap syntax traversal is acceptable. Optimize shared work only when it is semantically important or materially expensive.

## Shared session facts

Reusable derived analysis is represented as lazy immutable **session facts**, not pipeline stages.

Conceptually:

```text
session facts
  DocumentationSubjects
  future DeclaredTypes
  future ProjectGraph
  future structural summaries
```

A fact is computed at most once per command session for the applicable key/scope and then shared read-only by consumers.

The first required fact is the existing documentation subject/carrier model. Documentation rules and summary semantic-review rules must consume one coherent subject definition rather than separately reimplementing positional-record, direct-inheritdoc, and ordinary-summary semantics.

Fact extraction remains demand-driven. M0007 does not create a universal repository index merely because future rules may need one.

## Result materialization

Product result mechanics belong to the host rather than individual rules.

The host owns the stable mechanics for:

```text
run/finding/batch handles
canonical result ordering
finding ID assignment
ignore matching/application
latest-run persistence
review-batch persistence/handoff integration
```

Rules provide the stable domain information required to materialize occurrences/review work, including their semantic anchor/evidence where applicable. Refactoring must preserve existing fingerprints/discriminators and stale-ignore behavior.

Execution order may later be optimized, but public ordering and identities remain deterministic.

## Semantic review modules

Semantic-review rules remain ordinary fixed rules whose output is review work rather than deterministic findings.

The generic deterministic mechanics for population fingerprinting, ranking, bounded selection, batch construction, expansion, and handoff should be shared infrastructure where the M0006 quality and German-language rules use the same mechanism.

Rule-specific population semantics, rubric/questions, reviewer policy, and escalation condition remain owned by each rule contract. Shared mechanics must not merge independently toggleable rules back into one configurable rule.

The CLI still performs no model/provider invocation.

## Profile rules and remediation

Mandatory profile rules follow the same modularity principle for diagnosis. Their repository-wide lifecycle remains explicit.

A rule may additionally expose fixed deterministic remediation where already authorized. Remediation is an optional capability separate from diagnosis; command policy decides whether it may execute:

```text
check
  diagnosis only

bootstrap/update
  diagnosis
  -> authorized fixed remediation
  -> re-evaluation
```

Profile ownership, effective-configuration semantics, StyleCop prohibition, and mutation safety remain governed by `docs/specs/PROFILE.md`.

## Rewrite architecture

Rules and rewrites share repository/session infrastructure but are not one abstraction.

Conceptually:

```text
RewriteModule
  inspect RepositorySession
  produce proposed edits

RewriteRunner
  select explicit rewrite module
  build complete plan
  validate complete plan
  pass plan to RewriteTransaction
```

`format` and `normalize` become independent rewrite modules rather than branches of one command switch. Adding a future formatter/normalizer should not require editing repository-loading logic or a central transformation switch.

The existing all-target transactional contract remains authoritative:

```text
resolve all targets
-> compute complete rewrite plan
-> validate complete plan
-> if check-only: report
-> else atomic/rollback-protected commit
```

No selected source file is committed before complete planning and required validation succeed.

## Sampling boundary

M0007 intentionally does not implement the future statistical sampling models described in research.

The modular rule + session-fact architecture must leave a straightforward place for future shared sampling/state tooling, but no subject-state sampler, aggregate/cohort sampler, persistent repository index, hazard model, prior, or statistical state schema is introduced merely for architectural completeness.

Existing deterministic semantic-review sampling remains supported and may be factored into shared review infrastructure.

## Profile ownership

Committed profile state remains:

```text
.hygiene/profile.json
```

Namespaced profile artifact remains:

```text
.hygiene/profile/Hygiene.props
```

The product may maintain narrowly delimited integration points in repository-standard files such as root `Directory.Build.props` and `.editorconfig`.

Ownership remains explicit and bounded; unrelated user content is preserved.

## Documentation subject model

Documentation rules operate on API documentation subjects rather than raw tag names:

```text
API subject
-> source declaration representation
-> resolved summary carrier
```

For positional records, the synthesized property is the subject while the matching record `<param>` is its source summary carrier. Direct `<inheritdoc/>` may satisfy the missing-summary contract without providing local semantic-review prose.

This subject model is the first required shared session fact.

## Model boundary

Repository/session modularization introduces no model call.

Semantic review continues to use the external caller/frontier escalation contract.
