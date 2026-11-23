# Execution Obligation Ledger — M0031

> Planner-seeded completeness inventory only. The ready milestone is authoritative; this is not a test matrix or an evidence registry.

**Milestone:** `docs/milestones/M0031-mutation-only-autoformat.md`  
**Planning base:** workbench `main` `49f4361063bbca17690f0c1ed80a187040c97a7a`  
**Status:** pending implementation

## Planned obligations

| ID | Material outcome (same meaning as milestone) | Status | Brief result / blocker |
|---|---|---|---|
| CLI-01 | Independent Autoformat defaults, normalize and format only mutate; remove formatter --check/--changed, reject removed flags before writes, preserve init, scopes/restore/output and truthful changed-path reporting without checkOnly | pending | |
| PIPE-01 | Remove shadow/preview/check-only SDK machinery; retain the one direct custom canonicalization → batched approved SDK style → SDK whitespace → final layout pipeline and SDK-only format | pending | |
| SAFE-01 | Preserve standalone clean known Git checkpoint, HEAD/index verification, confined target scope/source integrity, accurate partial failure, no automatic rollback, and SDK restore policy | pending | |
| HYG-01 | Transitional Hygiene rewrite commands/adapter become mutation-only without disrupting autonomous delegated flows or independent read-only Hygiene check --changed / review / audit | pending | |
| TEST-01 | Replace preview-only tests with focused direct CLI/engine/Git/SDK checks including rejected flags, scope, phase routing, checkpointing, stable second normalization, and reuse in-memory canonicalizer regressions | pending | |
| DOC-01 | Reconcile operative architecture/specs/help/README and existing affected eng/consumer scripts, explain Git/disposable-checkout verification, index and completion records; keep prior milestone/YADG evidence historical | pending | |

## Implementation progress / resume context

No implementation changes on the planning branch. Branch from the **exact accepted planning commit** provided in the handoff. Preserve the obligation IDs and maintain statuses truthfully during implementation.

## Completion accounting

Before declaring `COMPLETE`, reconcile these six IDs against the milestone and the delivered code/docs. Summarize meaningful focused test evidence and deferred release qualification in the milestone completion note. Do not add one-test-per-row mapping, aggregate receipts, or a human review gate without demonstrated need.
