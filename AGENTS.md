# Agent Instructions

Start with:

```text
docs/milestones/M0005-supported-dotnet-profile.md
```

Read only the project authority required by that milestone. Maintain:

```text
.execution/M0005-supported-dotnet-profile.md
```

M0005 is executed only after M0004 is complete.

Preserve planner-owned obligation and evidence-case IDs/wording exactly. Implementation owns work-package decomposition, concrete mechanics, evidence, validation execution, and resume state.

Core constraints:

- Windows 11 and the .NET 11 SDK line remain authoritative.
- The supported hygiene profile is versioned and repository-local.
- Rule semantics are fixed; rule-set membership never changes a rule's meaning.
- Bootstrap/update may execute a rule's fixed deterministic remediation; `check` never does.
- Mandatory profile rules cannot be disabled or ignored.
- Do not add model/provider calls, arbitrary analyzer compatibility, StyleCop, GitHub Actions, or non-.NET language scope.
- M0004 rewrite/install behavior remains supported and regression-covered.

Before completion, reconcile milestone <-> ledger <-> live repository/evidence, run every required validation gate, preserve durable completion evidence, and obtain the required human review.
