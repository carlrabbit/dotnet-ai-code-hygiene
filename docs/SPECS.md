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
statistically scheduled semantic review/handoff
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
- Semantic-review rules may use the shared statistical sampling substrate through ordinary rule-owned policy code.
- The engine does not dynamically resolve contradictions between arbitrary third-party analyzers.

## Public CLI

Core public command surfaces include:

```text
hygiene bootstrap [--output text|json]
hygiene update [--output text|json]
hygiene format ...
hygiene normalize ...
hygiene check ...
hygiene review expand <batch-handle>
hygiene review handoff <batch-handle> [--file <path>]
hygiene review accept <batch-handle> <item-id>...
hygiene review accept <batch-handle> --all
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

Normal checking requires a current profile marker.

## Statistical sampling

Shared statistical mechanics are specified by:

```text
docs/specs/SAMPLING.md
```

M0011 makes the first production use of both models:

```text
subject-state:
  docs.summary.quality.review
  docs.summary.language.german.review

aggregate population:
  architecture.boringness.review
```

Rule-specific populations, hazard policy, review acceptance, and escalation semantics are authoritative in:

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

## Canonical normal rule order after M0011

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
11. docs.summary.quality.review               v4
12. docs.summary.language.german.review       v2
13. readability.long-line.review              v1
14. readability.control-flow.visual-block     v1
15. architecture.boringness.review            v1
```

The new rule is appended so all pre-M0011 relative ordering remains unchanged.

The full rule capability contract remains `docs/specs/RULE-CAPABILITIES.md`; M0011 must update its catalogue/version sections consistently.

## Package/version

M0011 product/package version is:

```text
0.7.0
```

Installed-tool Tier-4 validation remains required because M0011 changes public rule contracts and adds a public CLI review command.

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
