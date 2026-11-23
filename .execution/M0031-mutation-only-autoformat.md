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
| SAFE-01 | Preserve standalone clean known Git checkpoint, HEAD/index verification, confined target scope/source integrity, accurate partial failure, no automatic rollback, and SDK restore policy | complete | Prior standalone checkpoint and concurrent HEAD/index evidence remains valid. Manual Hygiene `format`/`normalize` now resolve `HEAD^{commit}` before status or mutation and reject an unborn/unavailable checkpoint with recovery guidance; focused ignored-source unborn-HEAD CLI regression confirmed source bytes stay unchanged. Autonomous calls remain outside this manual guard. |
| HYG-01 | Transitional Hygiene rewrite commands/adapter become mutation-only without disrupting autonomous delegated flows or independent read-only Hygiene check --changed / review / audit | complete | Formatter options/result fields remain removed; manual entry now requires valid HEAD and a clean tree/index, while autonomous calls retain their outer checkpoint/authorization path. M0031 explicitly narrows exhaustive audit to deterministic findings and semantic obligations without formatter/normalizer drift proposals; authorization, full-population semantic obligations, judgment transport, resume, read-only behavior and coverage remain. Focused audit preservation test passed; `check --changed` route unchanged. |
| TEST-01 | Replace preview-only tests with focused direct CLI/engine/Git/SDK checks including rejected flags, scope, phase routing, checkpointing, stable second normalization, and reuse in-memory canonicalizer regressions | complete | Added focused tests for unrelated EditorConfig content, nested effective policy conflict, concurrent EditorConfig change, and clean-but-unborn manual Hygiene refusal. Existing batched SDK fixture now asserts a required collections import survives and passed; focused exhaustive audit test confirms supported findings/obligations and no `rewriteProposals`. Existing direct mutation and autonomous lifecycle evidence is reused. |
| DOC-01 | Reconcile operative architecture/specs/help/README and existing affected eng/consumer scripts, explain Git/disposable-checkout verification, index and completion records; keep prior milestone/YADG evidence historical | complete | M0031 and this ledger authorize/document the audit contract change; HYGIENE and AUTONOMOUS-WORKFLOW specify no formatter drift proposals; manual recovery prerequisites appear in agent guidance. Historical milestones/research and YADG remain untouched. PR #43 description updated with correction scope and focused evidence. |

## Implementation progress / resume context

Implementation is on the requested branch, created from exact planning commit `57e70643a493a7e1b42edd474ebdd941c1989422`.

## Completion accounting

All six obligations are reconciled as complete, including the review corrections on the existing implementation branch. Focused Autoformat and Hygiene test-project builds succeeded; the three Autoformat policy initialization tests, unborn-HEAD manual Hygiene test, SDK batching/import fixture, and exhaustive audit preservation test passed. Installed-package/current-package consumer qualification, the aggregate validation pipeline, broader consumer repositories, release qualification, and cross-platform qualification remain deferred as specified by M0031; consumer scripts were not executed.
