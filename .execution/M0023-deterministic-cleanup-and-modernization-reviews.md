# M0023 Execution Ledger (planning-seeded)

Primary milestone: `docs/milestones/M0023-deterministic-cleanup-and-modernization-reviews.md`

This is operational state, not milestone authority. Planning seeded lossless obligations and validation gates; implementation owns decomposition, evidence, and status.

## Acceptance obligations

| ID | Status | Verbatim obligation | Evidence / notes |
|---|---|---|---|
| AC-01 | pending | Existing `format.csharp.roslyn` remains sole general formatter; formatting reliably aligns conventional statements/comments/XML docs, applies whitespace/newline conventions, and retains intended comment text and intentional grouping; a fallback handles only demonstrated Roslyn gaps. | — |
| AC-02 | pending | `using` organization and modifier-order formatting are deterministic, project-aware, idempotent, and do not drop required imports or reorder semantics-relevant constructs. | — |
| AC-03 | partial | Existing qualification simplifiers retain current behavior; braces and explicit accessibility style policies gain source auto-fixes while retaining current diagnostic/editorconfig owners and rule-wide disabling. | — |
| AC-04 | partial | Target-typed `new()` applies only in immediately type-visible, supported and proven-equivalent expressions; it does not rewrite return expressions or materially obscure contextual types. | — |
| AC-05 | pending | Safe cleanup candidates (unused imports, redundant casts, obvious typed default, interpolated simplification, inferred names, trivial blank-line redundancy) run under individually owned rule identities and preserve semantics, comments, original trivia and unrelated source. | — |
| AC-06 | pending | Parentheses/null-forgiving proposals apply automatically only under documented proven-safe, obvious readability conditions; nonqualifying candidates remain unmodified rather than over-eagerly normalized. | — |
| AC-07 | partial | Mechanical remediation is bounded, source-aware, transactional, idempotent, respects effective language version and repository/profile enablement, and the no-mutation check modes match the proposed actual changes without writing. | — |
| AC-08 | pending | All nine deferred modernization families have compiler-aware eligibility, fixed question/preview contract, and the appropriate file/type/member review population; technical convertibility alone never triggers an automatic change. | — |
| AC-09 | pending | Selected modernization work packages contain bounded individually addressable candidate occurrences and adequate context; implementer can accept, decline or mark uncertain for each, and package-level completion does not manufacture occurrence-level judgments. | — |
| AC-10 | pending | One shared max-five modernization work-package budget applies per ordinary gate across modernization families, with honest per-rule eligible/selected/unreviewed accounting, changed-first selection and transparent nonstarvation; existing six semantic-rule per-rule sampling behavior remains unchanged. | — |
| AC-11 | pending | Modernization verdict persistence/revalidation uses stable occurrence and enclosing-context fingerprints, supports stale/rejected/uncertain results without falsely asserting fixes, and does not suppress unrelated obligations or resurrect removed hazard/age scheduling. | — |
| AC-12 | partial | Rule catalog capabilities, enable/disable behavior, JSON/text reporting, rule explanations, review/handoff and autonomous lifecycle expose the new rules consistently; disabled rules contribute neither transformations nor review tasks. | — |
| AC-13 | pending | Explicit exhaustive read-only audit remains nonmutating and inventories modernization obligations when enabled; normal mode remains bounded, and `--review-all` cannot silently become unbounded agent work. | — |
| AC-14 | partial | Project authority documents are directly reconciled, explaining formatting vs mechanical normalization vs judgmental modernization, rule inventory, sampling units/budget, exclusion/rollback and source safety. | — |
| AC-15 | blocked | Required Windows/.NET 11 Core+CLI and installed-tool consumer checks demonstrate the real changed public workflow, with current stage-fingerprinted evidence and no substituted unsupported validation locus. | — |
| AC-16 | pending | Milestone-specific independent review records an actual authorized decision on rule safety, agent-work bound, semantic-review independence and evidence fidelity; do not self-approve. | — |

## Distinct evidence cases

| Case | Parent | Status | Verbatim evidence obligation | Evidence |
|---|---|---|---|---|
| EC-01a | AC-01 | pending | Ordinary and trailing `//`, `/* */` and XML docs align without textual corruption, including odd original indentation | — |
| EC-01b | AC-01 | pending | Intentional diagrams/code snippets and visual spacing are preserved; repeated format makes no edits | — |
| EC-02a | AC-02 | pending | Organize usings and modifiers in normal projects; preserve imports used through aliases/global/conditional contexts | — |
| EC-03a | partial | Required braces and explicit accessibility: enabled fixes rewrite source, disabled rules neither rewrite nor project policy | Braces enabled/disabled source behavior passed `RequiredBracesRuleAddsBlocksOnlyWhenEnabledAndIsIdempotent`; IDE0040 source auto-fix remains outstanding. |
| EC-04a | AC-04 | partial | Obvious typed local/initializer converts to `new()` and compiles with equivalent binding | `TargetTypedCreationRequiresDirectExplicitTargetAndSupportedLanguageVersion` passed 1/1 for local, field, and property initializers; complete project compile validation is part of the rewrite runner. |
| EC-04b | AC-04 | partial | Contextual method return, ambiguous overload/typing and unsupported language target do not convert | Same focused test covers method-return and C# 8 exclusions; explicit ambiguous-overload scenario remains untested. |
| EC-05a | AC-05 | pending | Each safe cleanup family has at least one positive and one ineligible/negative focused scenario | — |
| EC-06a | AC-06 | pending | Grouping parentheses and nullability assertion conveying intent remain unchanged | — |
| EC-07a | AC-07 | partial | `normalize --check` and `format --check` are read-only and produce matching actionable changed-path plans | — |
| EC-07b | AC-07 | partial | Compiler/validation failure, cancellation/conflict and second-run idempotence cause safe rollback/no further churn | — |
| EC-08a | AC-08 | pending | Each modernization family identifies correct eligible occurrences, correct scope, and excludes syntactically suggestible but semantically unsupported contexts | — |
| EC-09a | AC-09 | pending | Multiple occurrences in selected file/type/method can receive independent mixed verdicts; long package reports remainder | — |
| EC-10a | AC-10 | pending | Several simultaneously eligible families never exceed shared five-package normal budget; six legacy semantic batches unaffected | — |
| EC-10b | AC-10 | pending | Changed candidates outrank context; repeated gates permit fair selection without hazard/age and do not create unbounded reviews | — |
| EC-11a | AC-11 | pending | Repeated same-content gate does not duplicate accepted results; material surrounding change makes required decisions stale | — |
| EC-11b | AC-11 | pending | Rejected/uncertain candidate not falsely fixed/accepted; unrelated candidate remains separately eligible | — |
| EC-12a | AC-12 | partial | Rule toggles consistently affect projected settings, actual rewrite, diagnostics and work orders in CLI JSON/text | — |
| EC-13a | AC-13 | pending | Explicit full audit reports all eligible obligations without source, state or profile mutation, unlike capped normal gate | — |
| EC-15a | AC-15 | blocked | Exact freshly built+installed tool performs end-to-end deterministic cleanup and modern review reporting in a fixture repository | — |

## Required validation gates

| Gate | Target/depth | Execution locus | Required check | Maps to obligations/evidence | Status/evidence |
|---|---|---|---|---|---|
| VAL-01 | Targeted Core tests; Roslyn real compilations/workspaces | Windows 11/.NET 11 | Focused TUnit/MTP via repository's `eng/validate.ps1` stage plan or supported focused tests | AC-01–AC-08; EC-01a/b, 02a, 03a, 04a/b, 05a, 06a, 07a/b, 08a | partial / current solution build and focused Core cases passed; full 81/81 run receipt was invalidated by the final Core edit |
| VAL-02 | CLI fixture gates, review persistence and shared budgeting | Windows 11/.NET 11 + Git | Focused CLI test stages, fresh isolated repositories, actual JSON and work orders | AC-09–AC-13; EC-09a, 10a/b, 11a/b, 12a, 13a | partial / 33/33 passed before final Core rebuild; modernization workflow cases are absent |
| VAL-03 | Failure-path/idempotence mutation checks | Windows 11/.NET 11 | Existing transaction/rollback fixtures and repeated normalize/format; `git diff --check` | AC-01–AC-07; EC-01b, 07a/b | partial / focused normalizer idempotence and check-only tests passed; current final diff check required |
| VAL-04 | Exact installed package consumer integration | Windows 11/.NET 11 + Git + isolated local NuGet feed | Current `eng/validate.ps1` plan/shard/verify stage(s) for freshly packed and installed tool; don't substitute source-level smoke test | AC-12, AC-13, AC-15; EC-12a, 13a, 15a | blocked / latest run reached autonomous fixture and rejected stale pending-baseline snapshot before approval; no receipt |
| VAL-05 | Required authoritative aggregate for milestone | Windows 11/.NET 11 + PowerShell | `eng/validate.ps1` current required plan/shards and `-Verify` with current fingerprints, plus `git diff --check`; full cold only if current project authority requires it | AC-01–AC-15; evidence mapped to actual test scenarios rather than generic green | blocked / `-Verify` reports missing current installed-consumers receipt; full modernization implementation is also incomplete |
| VAL-06 | Independent milestone review | authorized reviewer on M0023 PR | `.review/REV-M0023-COMPLETION.md` and project review-check after real decision | AC-16 | pending / — |

## Implementation-owned work packages

1. **WP-1 — Baseline and rewrite contracts: complete.** Latest fetched baseline was `origin/main` commit `3cf38b8`; the M0020 validation stage plan and source transaction/rule mechanisms were inspected.
2. **WP-2 — Deterministic cleanup: partial.** Required-braces normalization and directly typed object creation are implemented and focused-tested. Formatting behavior gaps, explicit accessibility, and remaining cleanup transformations are outstanding.
3. **WP-3 — Modernization candidate model: not started.** All nine compiler-aware family populations, scoped identities, and previews remain outstanding.
4. **WP-4 — Shared modernization review transport: not started.** The cross-rule five-package selector, occurrence verdicts, fairness/accounting, persistence, and audit inventory remain outstanding. The existing six semantic-review rules were not changed.
5. **WP-5 — Authority and installed consumer: partial.** `REWRITES.md` and `RULE-CAPABILITIES.md` were reconciled for implemented rules. The remaining authority documents and successful exact consumer gate are outstanding.
6. **WP-6 — Independent review and PR: pending.** Review packet prepared; no reviewer decision or PR exists yet.

## Resume point

WP-3: implement the analyzer-backed modernization populations and review transport. First diagnose the installed-consumer fixture's stale-baseline failure without weakening freshness or approval behavior.

## Execution evidence (2026-10-21)

- Baseline: branch `codex/m0023-deterministic-cleanup` from latest fetched `origin/main` (`3cf38b8`, including M0019 and M0021). Host: Windows 11 build 26200, PowerShell 7.6.5, .NET SDK 11.0.100-rc.1.
- Latest `./eng/validate.ps1 -Shard solution`: passed with receipt `.execution/validation/solution.json`.
- Focused Core tests after that build: `TargetTypedCreationRequiresDirectExplicitTargetAndSupportedLanguageVersion` passed 1/1; `RequiredBracesRuleAddsBlocksOnlyWhenEnabledAndIsIdempotent` passed 1/1.
- Full Core suite passed 81/81 and full CLI suite passed 33/33 during execution. Those receipts are stale against the latest solution receipt after the final Core edge-case edit.
- `./eng/validate.ps1 -Shard readme-policy`: current receipt reused successfully. `-Shard pack-current`: passed for `DotNetAiCodeHygiene.Tool.0.11.0`, SHA-256 `D73EAC6654168068F781EAA9A8EC6EE5CFB9CE388EAEFAD09223861F94C0D724`.
- `-Shard self-host` passed before the final Core edit; its receipt is not current against the latest solution receipt.
- The installed-consumer validator's exact version and catalogue-count pins were stale and were corrected. The latest consumer run reached the autonomous fixture, then failed because the pending baseline snapshot was stale before fixture approval. No VAL-04 or EC-15a receipt exists.
- `./eng/validate.ps1 -Verify` reports stale/missing `core-tests`, `cli-tests`, `pack-current`, `installed-consumers`, and `self-host` receipts after the final source/documentation edits. `git diff --check` passed before final documentation/ledger edits and must be rerun.

## AC/EC reconciliation

- AC-03 and EC-03a are partial: required braces are normalized under the existing rule and enable/disable behavior is tested; IDE0040 source remediation is outstanding.
- AC-04 is partial: direct explicit local, field, and property initializers are converted with C# 9+ and matching Roslyn types; tests cover those positives, return exclusion, C# 8 exclusion, check-only, and idempotence. Ambiguous-overload coverage is outstanding.
- AC-07 is partial: existing transactional compile validation remains; new normalizers have focused check-only and idempotence coverage, but the complete milestone failure/interaction matrix is absent.
- AC-12 and AC-14 are partial: catalogue output was updated and two directly affected specs were reconciled. CLI/review/audit and remaining authority updates depend on unimplemented modernization behavior.
- AC-15, VAL-04, and EC-15a are blocked by the consumer fixture failure and missing receipt. VAL-05 is blocked because `-Verify` rejects that missing receipt.
- AC-16 and VAL-06 remain pending an authorized independent human decision. `.review/REV-M0023-COMPLETION.md` is a pending request, not approval.

**Outcome: blocked / incomplete implementation; human review pending.** PR: https://github.com/carlrabbit/dotnet-ai-code-hygiene/pull/25 (open draft). No milestone completion is claimed. The nine modernization families, shared max-five budget, occurrence-level decision workflow, and related audit behavior remain unimplemented.
