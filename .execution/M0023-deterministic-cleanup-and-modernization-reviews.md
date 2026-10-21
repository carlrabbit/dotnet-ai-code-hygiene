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
| VAL-01 | Targeted Core tests; Roslyn real compilations/workspaces | Windows 11/.NET 11 | Focused TUnit/MTP via repository's `eng/validate.ps1` stage plan or supported focused tests | AC-01–AC-08; EC-01a/b, 02a, 03a, 04a/b, 05a, 06a, 07a/b, 08a | latest solution stage passed; focused formatter, accessibility, default-literal, redundant-cast, and redundant-interpolation cases each pass 1/1; full 81/81 receipt is stale against current Core source edits |
| VAL-02 | CLI fixture gates, review persistence and shared budgeting | Windows 11/.NET 11 + Git | Focused CLI test stages, fresh isolated repositories, actual JSON and work orders | AC-09–AC-13; EC-09a, 10a/b, 11a/b, 12a, 13a | prior full suite passed 33/33 but is stale against the latest Core source edit; new M0023 occurrence-review, shared-budget, and audit cases remain absent |
| VAL-03 | Failure-path/idempotence mutation checks | Windows 11/.NET 11 | Existing transaction/rollback fixtures and repeated normalize/format; `git diff --check` | AC-01–AC-07; EC-01b, 07a/b | partial / focused formatter and normalizer idempotence/check-only cases pass; current final diff check required |
| VAL-04 | Exact installed package consumer integration | Windows 11/.NET 11 + Git + isolated local NuGet feed | Current `eng/validate.ps1` plan/shard/verify stage(s) for freshly packed and installed tool; don't substitute source-level smoke test | AC-12, AC-13, AC-15; EC-12a, 13a, 15a | passed for the preceding implementation state on 2026-10-21 / exact package SHA-256 `EE8B914DC38E7BDD6F2490F124710445C0C504842031C6E9DA1ED822980C1F4A`; now stale after additional source edits and must be rerun. The validator's stale-approval case intentionally rejected changed source/configuration. |
| VAL-05 | Required authoritative aggregate for milestone | Windows 11/.NET 11 + PowerShell | `eng/validate.ps1` current required plan/shards and `-Verify` with current fingerprints, plus `git diff --check`; full cold only if current project authority requires it | AC-01–AC-15; evidence mapped to actual test scenarios rather than generic green | pending implementation / the installed-consumers stage now has a fresh receipt; full modernization implementation and current aggregate evidence remain outstanding |
| VAL-06 | Independent milestone review | authorized reviewer on M0023 PR | `.review/REV-M0023-COMPLETION.md` and project review-check after real decision | AC-16 | pending / — |

## Implementation-owned work packages

1. **WP-1 — Baseline and rewrite contracts: complete.** Latest fetched baseline was `origin/main` commit `3cf38b8`; the M0020 validation stage plan and source transaction/rule mechanisms were inspected.
2. **WP-2 — Deterministic cleanup: partial.** Required-braces, explicitly typed object creation, IDE0040 accessibility, directly typed `default(T)`, identity casts, constant interpolation simplification, and exact inferred tuple/anonymous names are implemented as separate rules. Focused tests also establish Roslyn comment/diagram preservation, CRLF retention, trailing-whitespace cleanup, and format idempotence. Using organization, modifier ordering, unused imports, blank-line cleanup, and conservative parentheses/null-forgiving cases remain outstanding.
3. **WP-3 — Modernization candidate model: not started.** All nine compiler-aware family populations, scoped identities, and previews remain outstanding.
4. **WP-4 — Shared modernization review transport: not started.** The cross-rule five-package selector, occurrence verdicts, fairness/accounting, persistence, and audit inventory remain outstanding. The existing six semantic-review rules were not changed.
5. **WP-5 — Authority and installed consumer: partial.** `REWRITES.md` and `RULE-CAPABILITIES.md` were reconciled for implemented rules. The remaining authority documents and successful exact consumer gate are outstanding.
6. **WP-6 — Independent review and PR: pending.** Review packet prepared; no reviewer decision or PR exists yet.

## Resume point

WP-2: extend safe normalizers and deterministic-format evidence, then proceed to WP-3 analyzer-backed modernization populations and review transport. Preserve the explicit conservative exclusion conditions already recorded above.

## Execution evidence (2026-10-21)

- Baseline: branch `codex/m0023-deterministic-cleanup` from latest fetched `origin/main` (`3cf38b8`, including M0019 and M0021). Host: Windows 11 build 26200, PowerShell 7.6.5, .NET SDK 11.0.100-rc.1.
- Latest `./eng/validate.ps1 -Shard solution`: reused the current pass receipt `.execution/validation/solution.json`.
- Focused Core cases: target-typed creation 1/1, required braces 1/1, accessibility 1/1, and default literal 1/1 passed. Latest solution build passed; full Core/CLI receipts are stale after the newest normalization changes.
- Full Core (81/81) and CLI (33/33), current package, consumer, self-host, and aggregate verification all passed before the newest accessibility normalizer edit; those dependent receipts are stale now. Latest solution build passed and focused `ExplicitAccessibilityRuleAddsSafeDefaultsOnlyWhenEnabled` passed 1/1. A full current gate rerun is still required after implementation stabilizes.
- The installed-consumer validator's exact version and catalogue-count pins were stale and were corrected. The latest exact-package consumer run passed and produced `.execution/validation/installed-consumers.json`; its stale-approval negative case intentionally rejected changed source/configuration. This confirms the consumer gate, but does not yet establish AC-15/EC-15a for M0023 because sampled modernization reporting is not implemented.
- The previous `./eng/validate.ps1 -Verify` succeeded against the prior implementation state. Current source edits invalidate dependent receipts; rerun required shards and verification after the implementation stabilizes. The most recent focused failure was fixed; `git diff --check` must be rerun at closure.

## AC/EC reconciliation

- AC-03 and EC-03a are partial: required braces and explicit accessibility now normalize under their existing rule toggles; focused enabled/disabled, check-only, idempotence, static-constructor and explicit-interface cases pass. Remaining interaction coverage is outstanding.
- AC-05 is partial: typed `default(T)` has positive local/field/property cases and negative `var`, return, and C# 7.0 cases; identity casts, constant interpolation, and exact inferred tuple/anonymous names have positive, negative, comment-preservation, and toggle cases. Unused imports and blank-line cleanup remain outstanding.
- AC-01/EC-01a/b have focused formatter evidence for XML and ordinary comments, intentional visual comment grouping, preserved comment text, CRLF, trailing whitespace, non-mutating check mode, and idempotence. Additional alignment/layout cases remain outstanding.
- AC-04 is partial: direct explicit local, field, and property initializers are converted with C# 9+ and matching Roslyn types; tests cover those positives, return exclusion, C# 8 exclusion, check-only, and idempotence. Ambiguous-overload coverage is outstanding.
- AC-07 is partial: existing transactional compile validation remains; new normalizers have focused check-only and idempotence coverage, but the complete milestone failure/interaction matrix is absent.
- AC-12 and AC-14 are partial: catalogue output was updated and two directly affected specs were reconciled. CLI/review/audit and remaining authority updates depend on unimplemented modernization behavior.
- VAL-04 passed with a fresh installed-consumer receipt. AC-15 and EC-15a remain incomplete because the installed package still lacks the M0023 modernization workflow. VAL-05 remains incomplete because implementation/evidence for the milestone's required modernization cases is missing; a current aggregate verify must be run after those changes.
- AC-16 and VAL-06 remain pending an authorized independent human decision. `.review/REV-M0023-COMPLETION.md` is a pending request, not approval.

**Outcome: incomplete implementation; human review pending.** PR: https://github.com/carlrabbit/dotnet-ai-code-hygiene/pull/25 (open draft). No milestone completion is claimed. The nine modernization families, shared max-five budget, occurrence-level decision workflow, and related audit behavior remain unimplemented.
