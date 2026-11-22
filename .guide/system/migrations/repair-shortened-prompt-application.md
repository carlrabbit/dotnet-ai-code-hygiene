# Migration — Repair a Repository Planned with Shortened v0.5.0 Prompts

## Historical scope

This repair exists for repositories affected by the abbreviated v0.5.0 prompts. The trigger names below refer to the historical v0.5.0 prompt names.

For current execution of this repair, use:

```text
templates/prompts/version-specific/repair-v0.5.0-shortened-prompt-application.md
```

Current normal prompt entry points are listed in `templates/PROMPTS.md`.

## Purpose

Assess and repair a product repository that may have been migrated, initialized, or planned using abbreviated v0.5.0 prompt templates.

## Trigger

Use this migration when one or more of these occurred:

- the shortened v0.5.0 `update-to-latest-guide-system.md` prompt was used;
- a milestone package was generated from one of the shortened v0.5.0 milestone prompts;
- an implementation agent received the shortened v0.5.0 execution prompt;
- the resulting repository may have omitted guide-system obligations.

## Required assessment

Inspect the repository for omissions in:

- repository role and maturity classification;
- profile selection;
- authority-document routing;
- milestone scope and non-goals;
- provider/consumer distinction;
- validation tiers and concrete commands;
- human-review applicability and evidence;
- constrained-execution strategy;
- resumable validation suite requirements;
- direct documentation impact;
- deferred `.guide-sync/pending/` hints;
- disconnected implementation handoff;
- migration classification.

## Change classification

Classify findings as:

- required;
- conditional;
- already satisfied;
- manual review;
- no-op.

## Output

Create a focused corrective milestone package. Do not re-migrate or rewrite the repository wholesale when only a subset of requirements was omitted.

For current guide-system methodology, the corrective milestone should converge on the v0.7.0 ready-milestone boundary and use `templates/prompts/execute-milestone.md` for implementation rather than reproducing a bespoke execution methodology.
