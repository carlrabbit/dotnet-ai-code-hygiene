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
**State:** prerequisite for M0007
**Mode:** AI-executed, human-reviewed

Clarifies fixed rule semantics versus enable/disable-only participation, separates language-neutral summary quality from the fixed German-language review policy, and promotes rule-by-rule rationale into current authority.

M0007 must not execute until M0006 is complete and accepted.

Primary milestone:

```text
docs/milestones/M0006-rule-policy-rationale.md
```

## M0007 — Modular Analysis Architecture
**State:** ready after M0006 completion
**Mode:** AI-executed, human-reviewed

Refactors the current monolithic rule/rewrite execution into independent in-process rule and rewrite modules over a shared lazy repository-analysis session, while preserving public behavior. Establishes the BORING architecture intended to support a larger 1.0 rule and formatter catalogue without introducing a general plugin framework or formal callback pipeline.

Primary milestone:

```text
docs/milestones/M0007-modular-analysis-architecture.md
```
