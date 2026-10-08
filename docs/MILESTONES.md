# Milestones

## M0001 — CLI Foundation
**State:** done

## M0002 — Hygiene Vertical Slice
**State:** done

Established project-aware C# targets, deterministic findings/rules, text/JSON output, run/finding identities, explain, configuration, persistent ignores, and stable occurrence matching.

## M0003 — Semantic Review Sampling & Escalation
**State:** done

Established bounded semantic review batches and explicit frontier expansion/handoff without embedding model invocation.

## M0004 — Deterministic Rewrites & Installed Agent Tool
**State:** done

Established deterministic `format`/`normalize`, agent-facing CLI help, transactional rewrite behavior, and installed-tool consumer validation.

## M0005 — Supported .NET Profile & Documentation Hygiene
**State:** done

Established the supported .NET hygiene profile, bootstrap/update lifecycle, mandatory profile rules, documentation subject/carrier semantics, and documentation hygiene rules.

## M0006 — Rule Policy Rationale & Semantic Review Separation
**State:** done
**Mode:** AI-executed, human-reviewed

Clarified fixed rule semantics versus enable/disable-only participation, separated language-neutral summary quality from the fixed German-language review policy, and promoted rule-by-rule rationale into current authority.

Primary milestone:

```text
docs/milestones/M0006-rule-policy-rationale.md
```

## M0007 — Modular Analysis Architecture
**State:** done
**Mode:** AI-executed, human-reviewed

Refactored monolithic rule/rewrite execution into independent in-process rule and rewrite modules over a shared lazy repository-analysis session while preserving public behavior.

`REV-M0007-COMPLETION` is durably recorded as approved in the completed M0007 milestone and execution ledger. M0008 does not reopen that completed milestone.

Primary milestone:

```text
docs/milestones/M0007-modular-analysis-architecture.md
```

## M0008 — Rule Locality & Agent Routing Hygiene
**State:** done
**Mode:** AI-executed, human-reviewed

Makes the M0007 logical module boundary physically obvious: stable repository-level `AGENTS.md`, family-based rule folders, one product rule per file, and rule-owned fixed interface text without introducing a metadata/message framework or changing product behavior.

`REV-M0008-COMPLETION` is durably recorded as approved in the completed M0008 milestone and execution ledger.

Primary milestone:

```text
docs/milestones/M0008-rule-locality-and-agent-routing.md
```

## M0009 — Statistical Sampling Core
**State:** done
**Mode:** AI-executed, human-reviewed

`REV-M0009-COMPLETION` is durably recorded as approved in the completed M0009 milestone and execution ledger.

Implements two shared statistically defensible sampling mechanics for future expensive rules: subject-state hazard sampling for individual reinspection guarantees and aggregate population/cohort hazard sampling for dense populations, with deterministic hash-derived randomness, transparent lightweight persistence, and explicit due-versus-observed semantics.

M0009 does not migrate existing rules. Current summary-review SHA-256 ranking and exact five-item maximum sample remain unchanged.

Primary milestone:

```text
docs/milestones/M0009-statistical-sampling-core.md
```

## M0010 — Rule-Owned Remediation & Policy Projection
**State:** ready
**Mode:** AI-executed, human-reviewed

Makes rule IDs the semantic owners of diagnosis, deterministic format/normalize remediation, and hygiene-generated EditorConfig policy. Splits the current opaque rewrite policies into explicit configurable rules and advances the supported profile to v2 so braces/accessibility become independent disableable policies while preserving default behavior.

Primary milestone:

```text
docs/milestones/M0010-rule-owned-remediation-and-policy-projection.md
```
