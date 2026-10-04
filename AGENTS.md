# Agent Instructions

Start with:

```text
docs/milestones/M0003-semantic-review-sampling.md
```

Read only the project authority required by that milestone. Maintain:

```text
.execution/M0003-semantic-review-sampling.md
```

This package amends the M0003 planning authority before human completion approval. The amended milestone/ledger wording is authoritative even where it differs from the earlier M0003 package or current PR implementation.

Preserve every planner-owned obligation/evidence-case ID and wording exactly after applying this amendment. Existing completed M0003 evidence may be retained where still valid; amended/new obligations require fresh evidence.

Constraints:

- Windows 11, .NET 11 SDK line.
- `System.CommandLine`, TUnit, Roslyn.
- Deterministic semantic sampling.
- No model/provider SDK or model API call.
- No automatic escalation.
- No engine-managed review history, TTL, scheduler, audit cadence, or answer persistence.
- Durable review handoff artifacts are explicit caller-requested work products, not automatic review history.
- Product-owned review artifacts are namespaced under `.hygiene/reviews/` when stored in the repository.
- `.hygiene/reviews/` must not be Git-ignored by product defaults.
- The handoff format must also support writing to an explicit external path for fully decoupled reviewers.
- No configurable sample size, rule parameters, severity, or rule order.
- `format` and `normalize` remain deferred to M0004.
- No GitHub Actions/workflows.

Before `COMPLETE`, reconcile milestone <-> ledger <-> repository/evidence, run every required gate, obtain human review, and preserve durable completion evidence in the milestone.
