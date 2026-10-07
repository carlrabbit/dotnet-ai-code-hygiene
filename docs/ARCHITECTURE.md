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

`RepositorySession` is the command-scoped owner of expensive and reusable repository context.

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

Expensive context is lazy and cached only for the lifetime of the command session. Supporting a capability must not make every invocation eagerly pay for it. There is currently no durable analysis cache.

Opt-in statistical sampling is a separate lazy `RuleContext` service. It stages durable hazard/evidence state outside `RepositorySession` facts and commits at the host-owned boundary after rule evaluation and result materialization. Current production rules do not request it. The two supported shared mechanics are subject-state hazard sampling and aggregate population/cohort hazard sampling; rule modules retain all domain and risk policy.

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

The architecture requires:

- every product rule has a dedicated module/evaluator rather than rule-specific branches embedded in central orchestration;
- production rule registration is explicit and static;
- canonical rule order is deterministic catalog order;
- the host selects mandatory/enabled rules before execution;
- rule modules may traverse syntax/documents independently when that is the simplest implementation;
- there is no central syntax/symbol callback framework, rule dependency DAG, declarative required-context DSL, or reflection-based plugin discovery.

Repeated cheap syntax traversal is acceptable. Optimize shared work only when it is semantically important or materially expensive.

## Rule locality

Logical modularity is reflected in physical source layout.

Production rule code lives under:

```text
Rules/
  <shared rule execution/catalog types>
  Documentation/
  SemanticReview/
  Readability/
  Profile/
```

Each production rule module has one obvious source file in its family. Do not collect several unrelated production rules into category bucket files such as `DocumentRuleModules.cs`.

A rule file owns the fixed presentation contract that is specific to that rule, including as applicable:

```text
descriptor identity/version/output/classification/purpose/configurability
finding message/suggestion/observation/reason/constraint templates
semantic-review questions/rubric text
rule-specific reviewer/escalation text
```

Dynamic values may be interpolated at evaluation time. Shared evaluators/helpers may consume rule-owned text, but rule-specific fixed text must not be scattered through central engine code or unrelated shared helpers.

The catalog is primarily ordered explicit registration. It must not become a second manually synchronized copy of rule descriptors when descriptors are already owned by the registered modules.

Generic host text remains host-owned when it is genuinely generic, for example persistence errors, unavailable source context, invalid command state, or transaction failures.

This locality rule does **not** justify a resource system, localization framework, generated rule metadata, generic message registry, rule-definition DSL, attribute discovery, or new dependency. Plain C# constants/static data colocated with the owning rule are preferred.

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

Documentation rules and summary semantic-review rules consume one coherent documentation subject/carrier model rather than separately reimplementing positional-record, direct-inheritdoc, and ordinary-summary semantics.

Fact extraction remains demand-driven. There is no universal repository index merely because future rules may need one.

## Result materialization

Product result mechanics belong to the host rather than individual rules.

The host owns stable mechanics for:

```text
run/finding/batch handles
canonical result ordering
finding ID assignment
ignore matching/application
latest-run persistence
review-batch persistence/handoff integration
```

Rules provide the stable domain information required to materialize occurrences/review work. Refactoring must preserve existing fingerprints/discriminators and stale-ignore behavior.

## Semantic review modules

Semantic-review rules remain ordinary fixed rules whose output is review work rather than deterministic findings.

Generic deterministic mechanics for population fingerprinting, ranking, bounded selection, batch construction, expansion, and handoff are shared infrastructure where rules use the same mechanism.

Rule-specific population semantics, rubric/questions, reviewer policy, and escalation condition remain owned by each rule contract. Shared mechanics must not merge independently toggleable rules into one configurable rule.

The CLI performs no model/provider invocation.

## Profile rules and remediation

Mandatory profile rules follow the same modularity and locality principles for diagnosis.

A rule may additionally expose fixed deterministic remediation where already authorized. Remediation is an optional capability separate from diagnosis; command policy decides whether it may execute:

```text
check
  diagnosis only

bootstrap/update
  diagnosis
  -> authorized fixed remediation
  -> re-evaluation
```

Profile lifecycle/remediation infrastructure may remain shared. Rule-specific diagnostic text remains owned by the corresponding profile rule even when shared inspection code emits the underlying condition.

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

`format` and `normalize` are independent rewrite modules rather than branches of one command switch.

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

Future statistical sampling models remain research/planning scope. The modular rule + session-fact architecture must leave a straightforward place for future shared sampling/state tooling, but no subject-state sampler, aggregate/cohort sampler, persistent repository index, hazard model, prior, or statistical state schema is introduced merely for architectural completeness.

Existing deterministic semantic-review sampling remains supported.

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

## Model boundary

Repository/session/rule modularization introduces no model call.

Semantic review continues to use the external caller/frontier escalation contract.
