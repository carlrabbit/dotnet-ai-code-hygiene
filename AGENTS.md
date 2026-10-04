# Agent Instructions

Start with:

```text
docs/milestones/M0003-semantic-review-sampling.md
```

Read only the project authority required by that milestone. Maintain:

```text
.execution/M0003-semantic-review-sampling.md
```

Preserve every planner-owned obligation/evidence-case ID and wording exactly.

Constraints:

- Windows 11, .NET 11 SDK line.
- `System.CommandLine`, TUnit, Roslyn.
- Deterministic semantic sampling.
- No model/provider SDK or model API call.
- No automatic escalation.
- No review history, TTL, scheduler, audit cadence, or answer persistence.
- No configurable sample size, rule parameters, severity, or rule order.
- `format` and `normalize` remain deferred to M0004.
- No GitHub Actions/workflows.

Before `COMPLETE`, reconcile milestone <-> ledger <-> repository/evidence, run every required gate, obtain human review, and preserve durable completion evidence in the milestone.
