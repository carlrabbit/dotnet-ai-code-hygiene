# Milestones

## M0001 — CLI Foundation
**State:** done

## M0002 — Hygiene Vertical Slice
**State:** done

Established project-aware C# targets, deterministic rules/findings, JSON/text output, run/finding identities, explain, config, persistent ignores, fingerprinting, stale/unignore behavior.

## M0003 — Semantic Review Sampling & Escalation
**State:** awaiting human review
**Mode:** AI-executed, human-reviewed

Add first-class semantic `ReviewBatch` output and the first semantic sampling rule:

```text
docs.summary.quality.review
```

Every enabled run evaluates the rule. It samples up to five existing summaries; the sample may be empty. The implementation agent reviews the fixed rubric. If any answer materially fails or is uncertain, the caller explicitly runs:

```text
hygiene review expand <batch-handle>
```

Expansion emits the complete eligible population for a frontier-capability reviewer. The CLI does not invoke models and stores no semantic-review history.

Primary milestone:

```text
docs/milestones/M0003-semantic-review-sampling.md
```

## M0004 — Packaged Developer Tool
**State:** planned

Add deterministic formatting, only justified safe normalization, and installed `.NET tool` consumer validation.
