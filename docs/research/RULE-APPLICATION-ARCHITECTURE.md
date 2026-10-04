# Rule Application Architecture

## Status

Planning context, not implementation authority.

No scalability problem has yet been demonstrated by benchmark. This document records structural concerns that should be resolved before rule count and rule scope grow enough to make the current implementation expensive or difficult to evolve.

## Current shape

The current HygieneEngine.Check performs a useful vertical slice:

1. resolve selected C# targets;
2. discover/load SDK-style projects through Roslyn/MSBuild;
3. map selected files to loaded projects;
4. build one compilation per selected project group;
5. visit each selected document;
6. obtain syntax tree/root and semantic model;
7. evaluate the current deterministic and semantic-review logic while that document context is available;
8. collect, order, ignore-filter, and publish findings/review batches.

This is better than independently reparsing a file for every existing rule: syntax and semantic context are already shared inside the document pass.

The concern is architectural coupling rather than a proven repeated-parse bottleneck. Current rule implementations are embedded directly in the engine's document traversal and have different ad hoc collection needs.

## Why this may become limiting

### More rules on the same file

As rule count grows, one large engine traversal becomes harder to evolve and test independently.

Simply giving every rule its own full syntax traversal would improve modularity but could regress efficiency.

The desired direction is modular rules with shared expensive context.

### Different prerequisite levels

Rules may need different inputs:

~~~text
text only
syntax tree
semantic model
project compilation
project graph
repository index
Git/diff information
cross-file/domain aggregation
~~~

Treating all rules as if they require the same context either wastes work or constrains what future rules can express.

### Multi-file and repository rules

Some human-consumability concerns cannot be decided from one document:

- duplicated concepts across files;
- excessive forwarding/indirection;
- inconsistent patterns across sibling features;
- change fan-out/locality;
- repository-level naming or architectural conventions.

A future rule API needs a deliberate scope model rather than smuggling cross-file access into per-document callbacks.

### Diff-oriented operation

Agent workflows often run hygiene against changed files.

That can keep work bounded, but a rule evaluating a changed file may still require unchanged context to make the correct decision.

The architecture should distinguish:

~~~text
selected reporting scope
from
context that analysis is allowed to read
~~~

The current M0002 target contract already follows this principle conceptually: broader project/repository context may be read while findings are emitted only for selected files.

### Shared derived data

Future rules may repeatedly need the same derived structures: declared symbols, control-flow facts, call/reference indexes, project relationships, or repository concept indexes.

A pipeline should support lazy shared derivation rather than letting each rule recompute equivalent data.

## Candidate direction

A useful target shape is an analysis session with staged/lazy context:

~~~text
target selection
-> repository/project session
-> per-project context
-> per-document context
-> shared derived facts
-> rule evaluation
-> cross-document/project aggregation
-> finding/review-batch materialization
-> ignore/filter/order/presentation
~~~

Rules would declare the scope/context they require rather than owning project loading or repository enumeration.

Possible conceptual scopes:

- document-text
- document-syntax
- document-semantic
- project
- repository

The exact API is intentionally unresolved.

## Important design properties

### One expensive substrate, many rules

Project loading, compilations, syntax trees, semantic models, and future repository indexes should be shared within one analysis session where practical.

### Rule modularity

Adding a rule should not require editing a central monolithic traversal for ordinary cases.

Rule semantics, candidate generation, fingerprints, and review-batch behavior should be independently testable.

### Lazy cost

Do not build semantic models, compilations, symbol indexes, or repository-wide structures for rules that do not need them.

A cheap text/syntax rule should remain cheap.

### Deterministic ordering independent of execution

Execution order may eventually be optimized or parallelized, but public finding ordering and identities must remain deterministic.

Canonical rule order can remain a presentation/contract concept even if internal evaluation scheduling differs.

### Reporting scope remains explicit

A repository-level rule may read the whole repository while still emitting a finding anchored to a selected file or a defined repository/project subject.

The rule API should make this distinction explicit.

### Cancellation and bounded resource use

Large repository scans need cancellation boundaries and should avoid accidentally retaining multiple complete compilations/source copies longer than necessary.

Any caching should initially be run/session scoped unless a later milestone proves durable caches worthwhile.

### Parallelism is optional, not foundational

An explicit pipeline should not be justified primarily by parallel execution.

Roslyn/MSBuild workloads, deterministic output, memory pressure, and shared workspace behavior make uncontrolled parallelism risky. First obtain clean scope/context boundaries; measure before adding concurrency.

## Potential rule contract

One possible conceptual split is:

~~~text
RuleDescriptor
- id/version/output kind/order
- required scope/context

RuleEvaluator
- receives immutable analysis context
- emits deterministic occurrences and/or review subjects

AnalysisSession
- owns repository/project/document loading
- lazily supplies shared facts
- controls selected reporting scope

ResultMaterializer
- fingerprints/orders/IDs
- applies ignore decisions
- builds semantic review batches
~~~

This is a planning sketch, not a committed class model.

## What to measure before redesign

Before making pipeline work its own milestone, obtain evidence on representative repositories:

- repository target count;
- project count and project-load time;
- compilation time;
- per-document rule evaluation time;
- repeated semantic queries as rule count increases;
- memory retained by workspace/compilations;
- changed-file scan latency versus full-repository scan latency.

The first architectural milestone should solve observed or strongly imminent constraints, not speculative scale.

## Near-term planning recommendation

Do not redesign the engine merely because it currently contains several rules in one loop.

Revisit the architecture when planning one of these:

- several additional rules that need different context levels;
- the first genuinely multi-file/repository rule;
- a demonstrated unacceptable large-repository scan cost;
- reuse of the same derived semantic/index data by multiple rules.

At that point, plan the analysis-session/pipeline contract before adding more ad hoc branches to HygieneEngine.Check.

## Open questions

- Should rules declare required context explicitly, or should context be a lazy service surface?
- What is the smallest useful repository-level subject/anchor model?
- How should repository findings interact with existing file-oriented ignore persistence?
- Can one Roslyn workspace/session safely serve all rule scopes without excessive memory retention?
- Should changed-file runs construct repository indexes incrementally or scan broader context on demand?
- Which derived facts are common enough to cache within a run?
- At what repository/rule scale does pipeline restructuring become materially valuable?
