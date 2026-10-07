# Rule Application Architecture

## Status

Planning context and rationale.

M0007 promotes the selected architecture direction into `docs/ARCHITECTURE.md`. This research document preserves the problem framing, alternatives, and reasons for choosing independent rule modules over a formal callback pipeline. It is not implementation authority.

## Starting point

The pre-M0007 `HygieneEngine.Check` is a useful vertical slice:

1. resolve selected C# targets;
2. discover/load SDK-style projects through Roslyn/MSBuild;
3. map selected files to loaded projects;
4. build project compilations;
5. visit selected documents;
6. obtain syntax/semantic context;
7. evaluate multiple deterministic and semantic-review rules inside the same central traversal;
8. collect, order, ignore-filter, and publish findings/review batches.

This already avoids the worst naive design of reparsing/reloading a project independently for every rule. The problem is not a proven repeated-parse performance failure. The problem is that rule semantics, repository loading, traversal, review sampling, output construction, ignore behavior, and persistence are concentrated in one engine method and are increasingly difficult to evolve independently.

`RewriteEngine` independently repeats repository/project/workspace concerns, so the duplicated expensive substrate is broader than rule evaluation alone.

## Architecture pressure toward 1.0

The 1.0 direction requires more:

- deterministic rules;
- semantic-review rules;
- automatic formatters/normalizers;
- eventually cross-file/repository analysis;
- eventually statistically bounded expensive rules;
- human-readable, agent-readable implementation structure.

A framework designed around today's document loop would make later rules conform to the wrong abstraction. A framework designed around every conceivable future context would create infrastructure before evidence exists.

The intended architecture therefore needs to stay BORING while allowing the product surface to grow.

## Alternative A — formal rule pipeline

A formal pipeline can require each rule to declare scope and context needs, then invoke callbacks such as:

```text
OnDocumentText
OnSyntaxNode
OnSymbol
OnProject
OnRepository
OnAggregationComplete
```

Potential strengths:

- one central traversal can dispatch to many rules;
- requirements can theoretically drive eager/lazy scheduling;
- execution can be globally optimized later.

The costs are substantial for this product:

- rules become coupled to framework lifecycle vocabulary;
- multi-file, statistical, Git/history, and domain-specific rules strain callback phases;
- capability declarations become a mini-language that must stay aligned with actual implementation needs;
- rule dependency/order graphs become tempting;
- the framework can become more complicated than the rules;
- a future rule may require a new framework phase even when its analysis is otherwise simple.

No evidence currently shows that repeated cheap syntax traversal is expensive enough to justify this complexity.

## Alternative B — separate rule processes

Treating every rule as a real executable would maximize isolation, but it would introduce:

- process startup overhead;
- an IPC/versioned protocol;
- serialization of source/Roslyn-derived data;
- duplicate project loading or a remote compiler-service design;
- distribution/versioning complexity;
- a much larger failure and compatibility surface.

That is disproportionate for an opinionated single installed tool.

## Selected direction — independent modules, shared session

The chosen direction separates **logical rule independence** from **process independence**.

A rule should behave like a small self-contained program to its author and tests, while executing in-process against one shared command-scoped repository-analysis substrate.

Conceptually:

```text
command
-> RepositorySession
-> explicit RuleCatalog
-> rule A
-> rule B
-> rule C
-> host result materialization
```

A rule owns:

```text
its fixed domain semantics
its domain-specific traversal/querying
its subject/population interpretation
its emitted occurrence/review evidence
```

The host owns:

```text
repository and target resolution
project/workspace lifetime
expensive reusable Roslyn context
rule selection from fixed configuration
shared facts/tooling
result identity/order/filter/persistence
mutation policy
```

This keeps the common infrastructure centralized without centralizing every rule's control flow.

## Why independent traversal is acceptable

The expensive unit is generally not a `foreach` over a syntax tree. The expensive/repetitive risks are:

- reopening MSBuild projects;
- rebuilding compilations;
- rereading/reparsing files unnecessarily;
- reconstructing the same semantically significant repository index;
- retaining too many whole-repository structures simultaneously.

Those belong to `RepositorySession` and lazy session facts.

If several rules independently enumerate the same already-loaded syntax tree, that is initially an acceptable simplicity cost. If measurement later shows it matters, a shared fact/visitor can be introduced without changing the rule contract.

The project should optimize measured shared work, not make every rule participate in a universal traversal preemptively.

## Lazy services rather than required-context declarations

A formal descriptor such as:

```text
requires = Syntax | Semantic | ProjectGraph | Git | RepositoryIndex
```

looks attractive but duplicates knowledge already present in the rule implementation and creates scheduler/framework coupling.

The selected direction instead makes expensive services/facts lazy. A rule asks for what it actually uses. If a service is never requested, its cost is not paid.

A declaration language can be added later only if scheduling/diagnostic evidence proves it valuable.

## Shared facts as the 80/20 optimization

Some derived concepts are both semantically important and shared by several rules. These should become lazy immutable session facts.

The first obvious example is documentation subjects/carriers. The product already has deliberate semantics for:

- ordinary public/internal API subjects;
- positional record synthesized properties;
- matching record `<param>` summary carriers;
- direct `<inheritdoc/>`;
- local prose eligibility for semantic review.

Those semantics should have one shared producer rather than being duplicated across `docs.summary.required`, XML/sentence checks, and summary review rules.

The extraction rule is intentionally conservative:

> Extract a session fact when multiple consumers need the same semantically significant or materially expensive derived data.

Do not build a universal repository index to anticipate possible future rules.

## Rewrite relationship

Formatters/normalizers need much of the same repository substrate but have a different contract from rules.

A rewrite:

```text
reads repository/session context
-> proposes edits
-> participates in complete-plan validation
-> commits only through the rewrite transaction
```

A rule:

```text
reads repository/session context
-> emits finding/review work
```

Sharing a base interface would save little and blur important semantics. They should share infrastructure, not identity.

This allows future formatter modules to be added without growing a central `if (command == ...)` transformation switch.

## Statistical sampling relationship

The previous sampling research identified two complementary future models:

1. subject-state sampling when individual coverage/reinspection guarantees matter;
2. aggregate/cohort population sampling when population-level surveillance is sufficient.

The modular architecture is compatible with both:

```text
rule
  defines population/risk semantics

future shared sampler
  implements generic statistical mechanics

future state service
  owns explicit durable statistical state
```

But M0007 does not implement those models. Implementing storage, priors, hazards, cohorts, or identities solely to prove an architecture would violate the same BORING principle motivating the refactor.

Existing deterministic semantic-review sampling is concrete product behavior and may be extracted into shared review infrastructure now.

## Determinism and host ownership

Execution modularity must not leak into public instability.

The host remains responsible for:

- canonical rule ordering;
- run/finding/batch identities;
- stable fingerprints/discriminators;
- ignore application and stale behavior;
- latest-run persistence;
- review expansion/handoff integration.

This means internal execution can later change without silently changing public ordering/identity contracts.

## Deliberately rejected M0007 complexity

M0007 should not introduce:

- separate rule executables;
- dynamic third-party rule loading;
- reflection scanning for rules;
- a dependency injection framework;
- a rule dependency graph;
- syntax/symbol event registration;
- a required-context DSL;
- parallel execution as a design premise;
- a durable compiler/repository cache;
- a universal repository index;
- the future statistical sampling subsystem.

Each may be reconsidered only when a concrete product need and evidence justify it.

## Expected outcome

The desired developer experience after M0007 is approximately:

```text
new rule
-> implement one rule module
-> reuse session/facts/tooling
-> register explicitly in the canonical catalog
-> add focused tests/spec authority
```

and:

```text
new rewrite
-> implement one rewrite module
-> reuse RepositorySession
-> register explicitly in rewrite catalog
-> reuse RewriteTransaction
-> add focused tests/spec authority
```

Neither should require editing a large central traversal or duplicating repository/project-loading machinery.

## Future statistical sampling state

Later sampling work must distinguish transient, session-scoped analysis facts from durable statistical evidence. Subject-state sampling may retain per-subject review state where the guarantee requires it; aggregate/cohort sampling may retain only scope and population summaries and materialize subjects after selecting a scope. These are future product contracts, not M0007 session caches or implementations. See `docs/research/SAMPLING-RULES.md` for the research models.
