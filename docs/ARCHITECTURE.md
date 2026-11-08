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
-> fixed v3 SDK formatting profile
-> batched dotnet format style + whitespace passes
-> clean Git checkpoint / HEAD and index verification
-> explicit incomplete report on failure; agent-owned recovery
```

The architecture deliberately separates **module independence** from **process independence**. Check/bootstrap/update rules run in-process over one command-scoped substrate. Standard mechanical cleanup is delegated to the supported SDK CLI using the repository's fixed profile; historical rule IDs remain policy/reporting identifiers, not per-rule rewriter processes.

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

Gate-local semantic selection is a separate lazy `RuleContext` service. The shared selector receives deterministic Git gate context, ranks each rule's eligible population, and attaches private tickets to selected items. `GateLocalReviewStore` records only explicit accepted observations.

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
  optional diagnosis/remediation/projection capabilities

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
  Architecture/
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

Generic host text remains host-owned when it is genuinely generic, for example persistence errors, unavailable source context, invalid command state, or concurrent source/state changes.

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

Profile v3 owns mandatory analysis infrastructure and the fixed SDK formatting profile. It validates effective EditorConfig settings for eligible project sources; mechanical settings are not optional per-rule projections. Profile ownership and StyleCop prohibition are governed by `docs/specs/PROFILE.md`.

## Rewrite architecture

The SDK owns standard C# formatting and allowlisted style fixes. The stable rule catalog records disposition without instantiating a custom rewrite per ID.

Conceptually:

```text
RewriteEngine
  resolve eligible project sources
  batch complete solution/project coverage
  invoke supported dotnet format CLI phases
  report SDK-reported or observed changed paths
```

`format` runs SDK whitespace formatting. `normalize` runs only the fixed approved style diagnostics, then whitespace. The autonomous gate runs normalize across all eligible project sources independently of the bounded changed-file semantic review gate.

Mutating operations use a clean Git checkpoint and leave an inspectable worktree on failure:

```text
resolve eligible targets
-> snapshot HEAD/index
-> run SDK batches
-> verify HEAD/index
-> report observed edits or incomplete state
```

No automatic commit, reset, clean, or rollback occurs. Recovery is explicit and agent-owned.

## Gate-local review boundary

The six established semantic-review rules use deterministic, finite gate-local selection with a fixed ceiling of five subjects per rule. The nine modernization rules use a separate shared ceiling of five packages across all families, with individually judged occurrences inside each package. Changed subjects rank first; deterministic cross-rule rotation prevents starvation. Bare full checks schedule no semantic work. Explicit paths authorize bounded manual inspection.

Selection is potentially biased selective inspection, not a calibrated defect-rate estimate or a population-quality certificate. Tickets bind the gate, rule/algorithm versions, eligible population, subject identity, and content fingerprint. Only explicit review acceptance persists an observation.

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
