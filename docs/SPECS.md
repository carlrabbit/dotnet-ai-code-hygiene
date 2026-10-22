# Specifications

## Product role

`dotnet-ai-code-hygiene` is an opinionated installed .NET developer tool for coding agents.

It owns a supported hygiene policy rather than attempting compatibility with arbitrary analyzer stacks.

The product combines:

```text
supported .NET profile
deterministic bootstrap/update
deterministic rule-owned format/normalize remediation
deterministic hygiene findings
gate-local semantic review/handoff
```

## Rule principles

- One rule ID has one fixed semantic contract and no repository-supplied parameters.
- Optional fixed capabilities are owned by the same rule ID and gated by whole-rule enable/disable participation.
- Optional-rule configuration means enabling or disabling the whole fixed rule only; it does not configure language, thresholds, scope, severity, semantic questions, sampling, conventions, or remediation.
- Independently applicable policies use separate rule IDs, not parameters or unrelated bundled semantics.
- Rule-set membership does not alter rule meaning.
- A rule may expose one fixed deterministic remediation.
- Commands may choose whether to execute available remediation.
- Mandatory profile rules cannot be disabled or ignored.
- Configurable source/review rules retain enable/disable behavior.
- The six established semantic-review rules use independent gate-local selection; M0023 modernization reviews share a five-package budget across nine rule-owned families.
- The engine does not dynamically resolve contradictions between arbitrary third-party analyzers.

## Public CLI

Core public command surfaces include:

```text
dotnet hygiene bootstrap [--output text|json]
dotnet hygiene update [--output text|json]
dotnet hygiene format ...
dotnet hygiene normalize ...
dotnet hygiene check ...
dotnet hygiene review expand <batch-handle>
dotnet hygiene review handoff <batch-handle> [--file <path>]
dotnet hygiene review accept <batch-handle> <item-id>...
dotnet hygiene review accept <batch-handle> --all
```

`review accept` records only explicit acceptable semantic-review observations. It does not infer reviewer judgment and it does not add a fail/uncertain result-import protocol.

Existing explain/ignore/rules/help surfaces remain.

## Supported profile

The current hygiene profile remains:

```text
dotnet-11 v2
```

Detailed profile contract:

```text
docs/specs/PROFILE.md
```

The no-argument `dotnet hygiene` entrypoint bootstraps a missing marker; manual check commands require a current profile marker.

## Gate-local semantic review

Gate admission, bounded selection, accepted-review state, and legacy-state boundaries are specified by:

```text
docs/specs/SAMPLING.md
```

The three current rules share the selector but preserve independent eligibility, fixed questions, and escalation semantics. Detailed contracts are authoritative in:

```text
docs/specs/SEMANTIC-REVIEWS.md
```

## Analyzer policy

The supported profile requires:

```text
AnalysisLevel = 11
EnableNETAnalyzers = true
EnforceCodeStyleInBuild = true
```

Enabled `style.braces.required` and `style.accessibility.explicit` rules own their individual projected settings/severities. Other diagnostics remain at platform/analyzer defaults unless the user independently configures them without weakening a required enabled rule.

StyleCop is prohibited rather than treated as a supported extension.

## Documentation policy

Only a generic documentation summary is required structurally.

"Summary" is an API-documentation concept and may be carried by different XML elements depending on C# declaration form.

Detailed deterministic documentation contract:

```text
docs/specs/DOCUMENTATION.md
```

Semantic documentation review is specified separately in `docs/specs/SEMANTIC-REVIEWS.md`.

## Historical normal rule order after M0016

```text
1.  profile.dotnet.analysis.required          v2
2.  profile.stylecop.prohibited               v1
3.  style.braces.required                     v1
4.  style.accessibility.explicit              v1
5.  format.csharp.roslyn                      v1
6.  style.qualification.this.unnecessary      v1
7.  style.qualification.redundant             v1
8.  docs.summary.required                     v2
9.  docs.xml.consistent                       v1
10. docs.text.sentence                        v1
11. docs.summary.quality.review               v6
12. docs.summary.language.german.review       v4
13. readability.long-line.review              v1
14. readability.control-flow.visual-block     v2
15. architecture.boringness.review            v3
16. readability.type-responsibility.review    v1
17. tests.quality.review                      v1
18. comments.intent.review                    v1
```

This M0016 catalog snapshot is historical. M0017 appended three semantic-review rules; M0023 added cleanup capabilities and nine modernization review rules. The current canonical 38-rule order and capabilities are in `docs/specs/RULE-CAPABILITIES.md`.

The full rule capability contract remains `docs/specs/RULE-CAPABILITIES.md`.

## Package/version

M0018 product/package version is:

```text
0.11.0
```

The package exposes the canonical `dotnet hygiene` namespace through `ToolCommandName=dotnet-hygiene`; the legacy bare `hygiene` command is not supported. Automatic release updates must resolve the invoking tool installation safely, including ancestor local-tool manifests, and must not select another installation found on `PATH`. Baseline approval is an external caller assertion bound to the inspected report hash and current repository/policy snapshot; a profile marker alone is not approval.

Installed-tool Tier-4 validation remains required because M0016 changes public rule versions and adds `check --base` and `check --review-all`.

## Exit semantics

Existing exit meanings remain:

```text
0 successful command execution, even when findings/review work exists
1 unexpected internal failure
2 invalid invocation
3 invalid/unusable product input/state/profile/target or stale review acceptance
4 unavailable required environment/dependency
```

## Boundaries

M0011 does not add:

- third-party analyzer approval;
- arbitrary analyzer-stack compatibility;
- model/provider calls;
- automatic semantic remediation;
- automatic result import from frontier/planner handoffs;
- MCP/IDE integration;
- other language support;
- hosted CI;
- public plugin architecture;
- sampling/rubric configuration parameters;
- repository-wide defect-rate/confidence reporting.
