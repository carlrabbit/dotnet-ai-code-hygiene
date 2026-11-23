# Architecture

## System shape

```text
DotNetAiWorkbench.slnx
├── dotnet autoformat -> Autoformat engine -> SDK project/Git services
└── dotnet hygiene (transitional) -> review/profile host -> Autoformat engine

legacy check/bootstrap/update
-> explicit RuleCatalog
-> independent in-process rule modules
-> host result materialization
-> ignore/filter/order/persistence

Autoformat format/normalize
-> C# syntax/readability canonicalizer (normalize only)
-> approved SDK style pass, then whitespace (normalize only)
-> syntax layout stabilization (normalize only)
-> clean Git checkpoint / HEAD and index verification
-> explicit incomplete report on failure; agent-owned recovery
```

The GitHub repository name is historical; the root solution is the workbench monorepo. Autoformat is independently executable and has no reference to Hygiene profile, review, audit or index code. Transitional Hygiene delegates formatting to Autoformat and retains its semantic-review capabilities.

The workbench solution is `DotNetAiWorkbench.slnx`; the historical GitHub repository name remains unchanged. `dotnet autoformat` owns its fixed mechanical policy and source transforms. `dotnet hygiene` delegates formatting to Autoformat and keeps its independent profile/lifecycle and semantic-review host. The Autoformat command session contains only target discovery, SDK project context, source syntax and Git checkpoint operations; it does not construct compilations or semantic-review facts.

## Repository session

`RepositorySession` is the command-scoped owner of expensive and reusable repository context.

The Hygiene session owns or coordinates:

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

Hygiene review commands use their own session substrate. Autoformat maintains a narrower repository session with project/document mapping, source syntax and Git checkpoint operations only.

Hygiene review context is lazy and cached only for the lifetime of its command session. Autoformat does not request compilations, semantic models, review facts or relationship analysis. Neither product uses a durable analysis cache.

Gate-local semantic selection is a separate lazy `RuleContext` service. Rule modules discover mechanically eligible subjects and minimum stable selection data first. The shared selector receives deterministic Git gate context, ranks each rule's eligible population, and attaches private tickets to selected items. Selected subjects then resolve cross-file relationships and assemble detailed source evidence; ordinary closed or zero-work gates do not run unrelated relationship analysis. Expansion and authorized exhaustive audit use the same rule-owned enrichment path within their declared scopes. `GateLocalReviewStore` records only explicit accepted observations.

Relationship-based subjects keep a project-qualified identity and the changed reporting declaration as their primary anchor. Compiler-resolved related declarations remain separate anchors with their relationship basis stated explicitly; unresolved dynamic or external paths remain limitations. Acceptance fingerprints cover the relevant primary and selected related source declarations, together with the rule question/version, rather than a small line window or an arbitrary repository-wide digest.

Evidence is assembled per selected judgment obligation. The complete serialized work item is limited to eight source excerpts and 24,000 UTF-8 bytes; anchor summaries and truncation metadata are included in that same bound. If necessary source is absent, unresolved, omitted by the bound, or truncated, the item reports partial or unavailable context with the reason.

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

The Hygiene profile owns mandatory analysis infrastructure and StyleCop prohibition. Autoformat independently owns the fixed mechanical `.editorconfig` block and validates its effective settings; Hygiene does not project or adopt mechanical rule settings.

## Rewrite architecture

The SDK supplies whitespace formatting and allowlisted style fixes. Autoformat's syntax canonicalizer directly handles the selected modernization and readability policies.

```text
RewriteEngine
  resolve eligible SDK project source
  source-only syntax/readability canonicalization (normalize)
  invoke approved dotnet format style/whitespace phases
  reapply syntax layout rules after SDK whitespace (normalize)
  report syntax proposals, SDK changes, paths and notices
```

`dotnet autoformat format` runs SDK whitespace formatting only. The default command and `normalize` apply all nine opinionated modernization families and readability rules, run approved SDK style and whitespace phases in solution/project batches, then reapply final syntax layout and retained-comment stabilization. It does not request target compilations, run builds/tests or require equivalence; the implementing agent repairs build consequences. Autoformat has one direct mutation path and no formatter check-only or changed-files mode. For read-only semantic inspection use `dotnet hygiene check`; for CI verification use a disposable clean checkout and inspect the mutator's Git diff.

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

The 17 focused general semantic-review rules use deterministic, finite gate-local selection under one shared ceiling of ten distinct rule-subject obligations. Changed primary subjects rank first; at most two context-affected obligations may be admitted when a concrete related anchor changed. Deterministic rotating rule order prevents permanent first-rule priority. The nine modernization rules use a separate shared ceiling of five packages across all families, with individually judged occurrences inside each package. Bare full checks schedule no semantic work. Explicit paths authorize bounded manual inspection.

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
