# Specifications

## Product role

`dotnet-ai-code-hygiene` is an opinionated installed .NET developer tool for coding agents.

It owns a supported hygiene policy rather than attempting compatibility with arbitrary analyzer stacks.

The product combines:

```text
supported .NET profile
deterministic bootstrap/update
deterministic format/normalize
deterministic hygiene findings
bounded semantic review/handoff
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
- The engine does not dynamically resolve contradictions between arbitrary third-party analyzers.

## M0005 public CLI additions

```text
hygiene bootstrap [--output text|json]
hygiene update [--output text|json]
```

Existing format/normalize/check/review/explain/ignore/rules/help surfaces remain.

Bootstrap/update are repository-wide and do not accept source targets or `--changed`.

## Supported profile

The current hygiene profile is:

```text
dotnet-11 v2
```

Shared statistical sampling mechanics and their rule-policy boundary are specified in:

```text
docs/specs/SAMPLING.md
```

Detailed contract:

```text
docs/specs/PROFILE.md
```

Normal checking requires a current profile marker.

## Analyzer policy

M0005 intentionally uses built-in .NET analyzers/code-style analyzers as first-class supported-profile infrastructure where they already implement the desired rule well.

Profile v2 requires mandatory analyzer infrastructure and projects braces/accessibility policy from enabled configurable rules:

```text
AnalysisLevel = 11
EnableNETAnalyzers = true
EnforceCodeStyleInBuild = true
```

No global warnings-as-errors repository policy is allowed.

The enabled `style.braces.required` and `style.accessibility.explicit` rules own their individual `error` severities. Other diagnostics remain at platform/analyzer defaults unless the user independently configures them without weakening a required enabled rule.

StyleCop is prohibited rather than treated as a supported extension.

## Documentation policy

Only a generic documentation summary is required.

"Summary" is an API-documentation concept and may be carried by different XML elements depending on C# declaration form.

Detailed contract:

```text
docs/specs/DOCUMENTATION.md
```

M0005 does not impose StyleCop-style parameter/type-parameter/return documentation completeness.

## Canonical normal rule order

```text
1. profile.dotnet.analysis.required         v2
2. profile.stylecop.prohibited              v1
3. style.braces.required                    v1
4. style.accessibility.explicit             v1
5. format.csharp.roslyn                     v1
6. style.qualification.this.unnecessary     v1
7. style.qualification.redundant            v1
8. docs.summary.required                    v2
9. docs.xml.consistent                      v1
10. docs.text.sentence                      v1
11. docs.summary.quality.review             v3
12. docs.summary.language.german.review     v1
13. readability.long-line.review            v1
14. readability.control-flow.visual-block   v1
```

The full capability contract is `docs/specs/RULE-CAPABILITIES.md`. Commands select enabled rules from this single catalogue by capability.

## Package/version

M0010 product/package version is:

```text
0.6.0
```

Installed-tool Tier-4 validation remains required for affected public CLI/profile behavior.

## Exit semantics

Existing exit meanings remain:

```text
0 successful command execution, even when findings/pending profile findings exist
1 unexpected internal failure
2 invalid invocation
3 invalid/unusable product input/state/profile/target
4 unavailable required environment/dependency
```

## Boundaries

M0005 does not add:

- third-party async-analyzer approval;
- StyleCop dependency or compatibility mode;
- arbitrary analyzer-stack compatibility;
- model/provider calls;
- automatic semantic remediation;
- MCP/IDE integration;
- Go/other-language support;
- GitHub Actions/workflows.
