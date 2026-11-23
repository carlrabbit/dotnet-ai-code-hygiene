# Execution Obligation Ledger — M0031

> Planner-seeded completeness inventory only. The ready milestone is authoritative; this is not a test matrix or an evidence registry.

**Milestone:** `docs/milestones/M0031-mutation-only-autoformat.md`  
**Planning base:** workbench `main` `49f4361063bbca17690f0c1ed80a187040c97a7a`  
**Status:** complete

## Planned obligations

| ID | Material outcome (same meaning as milestone) | Status | Brief result / blocker |
|---|---|---|---|
| CLI-01 | Independent Autoformat defaults, normalize and format only mutate; remove formatter --check/--changed, reject removed flags before writes, preserve init, scopes/restore/output and truthful changed-path reporting without checkOnly | complete | Removed switches rejected by built CLI and confirmed no source writes in focused CLI regression and disposable SDK/Git fixture; JSON reports actual changed paths and no checkOnly field. |
| PIPE-01 | Remove shadow/preview/check-only SDK machinery; retain the one direct custom canonicalization → batched approved SDK style → SDK whitespace → final layout pipeline and SDK-only format | complete | Removed preview copier, shadow project loading, report-diff simulation and SDK verify-only routing. Autoformat test suite passed 31/31; actual disposable fixture used SDK mutation. |
| SAFE-01 | Preserve standalone clean known Git checkpoint, HEAD/index verification, confined target scope/source integrity, accurate partial failure, no automatic rollback, and SDK restore policy | complete | Focused regressions passed for partial SDK failure and concurrent HEAD/index changes; disposable actual CLI fixture verified explicit scope, clean repeat and dirty checkpoint refusal. No rollback path added. |
| HYG-01 | Transitional Hygiene rewrite commands/adapter become mutation-only without disrupting autonomous delegated flows or independent read-only Hygiene check --changed / review / audit | complete | Formatter options/result fields removed; manual clean-entry check retained; autonomous calls still use shared engine. Focused Hygiene CLI mutation/batching and exhaustive semantic-audit tests passed; `check --changed` route unchanged. |
| TEST-01 | Replace preview-only tests with focused direct CLI/engine/Git/SDK checks including rejected flags, scope, phase routing, checkpointing, stable second normalization, and reuse in-memory canonicalizer regressions | complete | Preview-only tests removed. Autoformat tests passed 31/31 plus concurrent Git regression; targeted Hygiene delegation/batching and audit tests passed; disposable SDK/Git fixture passed. |
| DOC-01 | Reconcile operative architecture/specs/help/README and existing affected eng/consumer scripts, explain Git/disposable-checkout verification, index and completion records; keep prior milestone/YADG evidence historical | complete | Updated operative docs, README/agent help, milestone index/completion, and consumer scripts; all three scripts parse. Historical milestones/research and YADG were left intact. |

## Implementation progress / resume context

Implementation is on the requested branch, created from exact planning commit `57e70643a493a7e1b42edd474ebdd941c1989422`.

## Completion accounting

All six obligations are reconciled as complete. Installed-package/current-package consumer qualification, the aggregate validation pipeline, broader consumer repositories, and cross-platform qualification remain deferred as specified by M0031; their scripts were updated but not executed.
