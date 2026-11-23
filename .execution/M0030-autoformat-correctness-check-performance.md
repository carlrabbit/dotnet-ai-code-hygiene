# Execution Obligation Ledger — M0030

> Planner-seeded completeness inventory. The ready milestone is authoritative; this is not an evidence register or a test matrix.

**Milestone:** `docs/milestones/M0030-autoformat-correctness-check-performance.md`  
**Planning base:** workbench `main` at `66ed726b505bac2457e0edabe5cf3b3340650853`  
**Status:** complete; [implementation PR #41](https://github.com/carlrabbit/dotnet-ai-code-hygiene/pull/41) targets `planning/m0030-autoformat-correctness-check-performance`
**External evidence:** `carlrabbit/yadg` Draft PR #22; keep unmerged

## Material obligations

| ID | Material outcome (same meaning as milestone) | Status | Brief result / blocker |
|---|---|---|---|
| TYPE-01 | Valid array declaration type synthesis for sized 1D, rectangular and jagged creations under var; no emitted array dimensions in declaration types; preserve other eligible/ineligible var-style behavior | complete | Direct deterministic regression covers sized 1D, rectangular, jagged, implicit array, object/literal and unknown local forms. YADG's affected application projects compiled; no generated `CS0270` remains. |
| TEST-01 | Fast deterministic direct source-to-source coverage of the supported and excluded syntactic categories across all nine modernization families, strengthening missing shape boundaries and weak assertions without SDK-per-case tests | complete | 27 `CanonicalizerTests` passed. Added var positive/boundary shapes, complex switch/conditional and comment/side-effect/readonly/inline exclusions; existing collection, primary-constructor and expression-body cases remain covered. |
| LAY-01 | Correct verified YADG positional-record indentation and redundant blank-line violations while preserving existing readability policy, comments and retain behavior | complete | YADG record parameters now indent 4 spaces and close at declaration column; `ConvertInlines` has one preceding blank line. Focused tests cover file-scoped record/method alignment and comments. |
| CHECK-01 | Exact, non-mutating check-only normalization parity with the real complete pipeline; preserve selected scope, SDK style/restore and no-restore, whitespace-only format behavior | complete | Single-project previews use the selected project's evaluated/transitive references; multi-project previews keep solution batching. All copied sources and destinations are containment-checked, reparse points and unsupported external/missing/dynamic references fail explicitly. Focused regressions verify outside sentinels and source remain unchanged, two SDK phases, selected project scope, and check/mutate path parity. |
| PERF-01 | Remove avoidable check-only overhead, including solution-backed batching rather than per-project SDK processes, and demonstrate material improvement using one actual YADG timing per specified check workload | complete | Full check 60.078s vs 178.621s observed; `--no-restore` 44.889s vs 125.719s. The corrected single-file observation is 26.217s (one prior observation: 39.786s; historical baseline: 36.037s). Project-scoped execution removes unrelated project evaluation; the two SDK-native phases and default restore remain. |
| EXP-01 | Fresh-baseline raw YADG rerun, append-only experimental Draft PR #22 delta, preserved original commits, one-pass convergence, imports, diagnostic build and one timing per command; no source hand repair or merge | complete | Final raw delta appended as `f6eea796487418f7679f763d52f632962e2d5794`; original policy/output commits preserved. First/second normalize 81.632s/50.533s (33/36 then 0/36). Draft PR #22 remains open and DO NOT MERGE. Build found only baseline-existing CS8600 in `M0019FocusedTests.cs:345`. |
| DOC-01 | Accurate affected specs/help, concise M0030 YADG report, milestone index, completion note, ledger and PR summaries; retain M0028/M0029 historical evidence | complete | `REWRITES.md`, M0030 research report, milestone index, completion note, ledger, YADG PR #22 body and M0030 implementation PR body reconciled. M0028/M0029 history unchanged. |

## Resume / implementation progress

Implementation began on `implementation/m0030-autoformat-correctness-check-performance` at exact planning commit `f1b4c9d1804ce11a3102451dd2ffcdd2706a9567`; the planning branch remains the PR base.

## Completion accounting

Focused verification is recorded in `docs/research/M0030-autoformat-yadg-revalidation.md` and the M0030 completion note. The correction did not change YADG formatter output; the existing raw-output commit remains authoritative. Release qualification, YADG tests, and installed-tool checks were not run.
