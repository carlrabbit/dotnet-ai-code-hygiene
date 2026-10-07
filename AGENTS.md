# Agent Instructions

Start with:

```text
docs/milestones/M0007-modular-analysis-architecture.md
```

Read only the project authority required by that milestone. Maintain:

```text
.execution/M0007-modular-analysis-architecture.md
```

M0007 is executed only after M0006 is complete and accepted. Treat the accepted M0006 rule IDs, versions, order, rationales, and repository self-hosting configuration as the behavioral baseline.

Preserve planner-owned obligation and evidence-case IDs/wording exactly. Implementation owns work-package decomposition, concrete mechanics, evidence, validation execution, and resume state.

Core constraints:

- This is an internal architecture milestone; do not add new product rules, rewrites, user configuration, or public CLI behavior.
- Rules are independent in-process modules over one shared command-scoped repository session; they are not separate OS processes or dynamically loaded plugins.
- The host owns repository/target/project loading, expensive shared context, rule selection, result materialization, persistence, and mutation policy.
- A rule owns its domain-specific traversal and decision logic. Do not replace this with a central syntax callback/event pipeline, a rule dependency DAG, or a declarative required-context DSL.
- Shared facts are lazy, immutable, session-scoped, and extracted only where semantic consistency or repeated expensive derivation justifies them.
- Rule execution and rewrite execution remain distinct contracts even though they share repository/session infrastructure.
- Existing deterministic ordering, occurrence identity, ignores, semantic-review batches/handles, bootstrap/update behavior, rewrite transactionality, and targeting semantics must remain compatible.
- Do not implement the future subject-state or aggregate statistical sampling models in this milestone.
- Do not introduce a DI framework, reflection-based discovery, uncontrolled parallelism, persistent analysis caches, model/provider calls, GitHub workflows, or non-.NET language scope.

Before completion, reconcile milestone <-> ledger <-> live repository/evidence, run every required validation gate, preserve durable completion evidence, and obtain the required human review.
