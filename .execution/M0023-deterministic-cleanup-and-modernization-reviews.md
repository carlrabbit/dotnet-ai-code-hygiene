# M0023 Execution Ledger (planning-seeded)

Primary milestone: `docs/milestones/M0023-deterministic-cleanup-and-modernization-reviews.md`

This is operational state, not milestone authority. Planning seeded lossless obligations and validation gates; implementation owns decomposition, evidence, and status.

## Acceptance obligations

| ID | Status | Verbatim obligation | Evidence / notes |
|---|---|---|---|
| AC-01 | pending | Existing `format.csharp.roslyn` remains sole general formatter; formatting reliably aligns conventional statements/comments/XML docs, applies whitespace/newline conventions, and retains intended comment text and intentional grouping; a fallback handles only demonstrated Roslyn gaps. | — |
| AC-02 | partial | `using` organization and modifier-order formatting are deterministic, project-aware, idempotent, and do not drop required imports or reorder semantics-relevant constructs. | `style.usings.organize` sorts only contiguous plain namespace imports and honors `dotnet_sort_system_directives_first`; aliases, static/global imports, comments, and directives form boundaries. Focused case passed. Modifier ordering remains outstanding. |
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
| AC-15 | partial | Required Windows/.NET 11 Core+CLI and installed-tool consumer checks demonstrate the real changed public workflow, with current stage-fingerprinted evidence and no substituted unsupported validation locus. | Current Windows/.NET 11 plan passed all seven stages and `-Verify`; package SHA-256 `B505C17C529F4C9EF675DB6F00D36AFE496DCDBAEE093EBC11785624E88B87C0`. Modernization integration is absent, so the criterion is not complete. |
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
| EC-15a | AC-15 | partial | Exact freshly built+installed tool performs end-to-end deterministic cleanup and modern review reporting in a fixture repository | Fresh exact package and installed-consumer gate passed for deterministic cleanup; modern-review reporting is not implemented, so this case remains partial. |

## Required validation gates

| Gate | Target/depth | Execution locus | Required check | Maps to obligations/evidence | Status/evidence |
|---|---|---|---|---|---|
| VAL-01 | Targeted Core tests; Roslyn real compilations/workspaces | Windows 11/.NET 11 | Focused TUnit/MTP via repository's `eng/validate.ps1` stage plan or supported focused tests | AC-01–AC-08; EC-01a/b, 02a, 03a, 04a/b, 05a, 06a, 07a/b, 08a | `UsingOrganizationSortsOnlySimpleContiguousImportsAndPreservesOtherForms` and `CanonicalCatalogueOwnsEveryCapabilityAndProjectionKeyOnce` each passed 1/1 after the new rule; full Core receipt predates this slice |
| VAL-02 | CLI fixture gates, review persistence and shared budgeting | Windows 11/.NET 11 + Git | Focused CLI test stages, fresh isolated repositories, actual JSON and work orders | AC-09–AC-13; EC-09a, 10a/b, 11a/b, 12a, 13a | `RulesJsonDeclaresFindingAndBatchOutputKinds` and `RewriteCliJsonTextCheckModesAndPersistentStateBoundaries` each passed 1/1 with the 24-rule catalog; M0023 occurrence-review, shared-budget, and audit cases remain absent |
| VAL-03 | Failure-path/idempotence mutation checks | Windows 11/.NET 11 | Existing transaction/rollback fixtures and repeated normalize/format; `git diff --check` | AC-01–AC-07; EC-01b, 07a/b | partial / focused formatter and normalizer idempotence/check-only cases pass; current final diff check required |
| VAL-04 | Exact installed package consumer integration | Windows 11/.NET 11 + Git + isolated local NuGet feed | Current `eng/validate.ps1` plan/shard/verify stage(s) for freshly packed and installed tool; don't substitute source-level smoke test | AC-12, AC-13, AC-15; EC-12a, 13a, 15a | current pass, `.execution/validation/installed-consumers.json`, 515.763 sec, package SHA-256 `B505C17C529F4C9EF675DB6F00D36AFE496DCDBAEE093EBC11785624E88B87C0`; stale-approval negative case intentionally rejected changed source/configuration. |
| VAL-05 | Required authoritative aggregate for milestone | Windows 11/.NET 11 + PowerShell | `eng/validate.ps1` current required plan/shards and `-Verify` with current fingerprints, plus `git diff --check`; full cold only if current project authority requires it | AC-01–AC-15; evidence mapped to actual test scenarios rather than generic green | all seven current stages verified by `./eng/validate.ps1 -Verify`; implementation evidence for modernization cases remains absent because feature work is incomplete |
| VAL-06 | Independent milestone review | authorized reviewer on M0023 PR | `.review/REV-M0023-COMPLETION.md` and project review-check after real decision | AC-16 | pending / — |

## Implementation-owned work packages

1. **WP-1 — Baseline and rewrite contracts: complete.** Latest fetched baseline was `origin/main` commit `3cf38b8`; the M0020 validation stage plan and source transaction/rule mechanisms were inspected.
2. **WP-2 — Deterministic cleanup: partial.** Required-braces, explicitly typed object creation, IDE0040 accessibility, directly typed `default(T)`, identity casts, constant interpolation simplification, exact inferred tuple/anonymous names, and safe contiguous `using` organization are implemented as separate rules. `style.usings.organize` honors `dotnet_sort_system_directives_first` and does not remove imports. Focused tests also establish Roslyn comment/diagram preservation, CRLF retention, trailing-whitespace cleanup, and format idempotence. Modifier ordering, unused imports, blank-line cleanup, and conservative parentheses/null-forgiving cases remain outstanding. An IDE0005 analyzer experiment using the loaded project's analyzer references and Roslyn Features did not surface an actionable diagnostic, so no import is removed by a fallback heuristic.
3. **WP-3 — Modernization candidate model: not started.** All nine compiler-aware family populations, scoped identities, and previews remain outstanding.
4. **WP-4 — Shared modernization review transport: not started.** The cross-rule five-package selector, occurrence verdicts, fairness/accounting, persistence, and audit inventory remain outstanding. The existing six semantic-review rules were not changed.
5. **WP-5 — Authority and installed consumer: partial.** `REWRITES.md` and `RULE-CAPABILITIES.md` were reconciled for implemented rules. The remaining authority documents and successful exact consumer gate are outstanding.
6. **WP-6 — Independent review and PR: partial.** Draft PR #25 is open and the review packet is prepared; no reviewer decision exists yet.

## Resume point

WP-2 remains active. Next, investigate a supported Roslyn path for conventional modifier ordering, then add only demonstrably trivial blank-line cleanup; continue preserving comments, conditional regions, profile toggles, and transactional validation. Unused-import cleanup remains tied to a supported actionable IDE0005 result or a planning decision, not a heuristic replacement. Proceed to WP-3 analyzer-backed modernization populations only after the remaining deterministic rules are either implemented or explicitly resolved.

## Execution evidence (2026-10-21)

- Baseline: branch `codex/m0023-deterministic-cleanup` from latest fetched `origin/main` (`3cf38b8`, including M0019 and M0021). Host: Windows 11 build 26200, PowerShell 7.6.5, .NET SDK 11.0.100-rc.1.
- Latest `./eng/validate.ps1 -Shard solution`: reused the current pass receipt `.execution/validation/solution.json`.
- Focused Core cases: target-typed creation 1/1, required braces 1/1, accessibility 1/1, and default literal 1/1 passed. Latest solution build passed; full Core/CLI receipts are stale after the newest normalization changes.
- Current full Core suite passed 87/87; current full CLI suite passed 33/33. Current solution, readme-policy, package, installed-consumer and self-host stages passed, and `./eng/validate.ps1 -Verify` reused all seven current receipts.
- The installed-consumer validator's exact version/catalogue pins and PowerShell line continuations were corrected. The current exact-package consumer run passed and produced `.execution/validation/installed-consumers.json`; its stale-approval negative case intentionally rejected changed source/configuration. This does not establish EC-15a because sampled modernization reporting is not implemented.
- The first current Core run exposed an inconsistent legacy fixture that asserted preservation of XML documentation absent from its input. The fixture now includes the documented source; the complete rerun passed 87/87.
- WP-2 using-organization increment: solution build passed; focused Core organizer and catalog cases passed 1/1; focused CLI catalog and rewrite-selection cases passed 1/1. This focused evidence does not refresh full Core/CLI/package/consumer receipts for the new rule.
- The previous `./eng/validate.ps1 -Verify` succeeded against the prior implementation state. Current source edits invalidate dependent receipts; rerun required shards and verification after the implementation stabilizes. The most recent focused failure was fixed; `git diff --check` must be rerun at closure.

## AC/EC reconciliation

- AC-03 and EC-03a are partial: required braces and explicit accessibility now normalize under their existing rule toggles; focused enabled/disabled, check-only, idempotence, static-constructor and explicit-interface cases pass. Remaining interaction coverage is outstanding.
- AC-05 is partial: typed `default(T)` has positive local/field/property cases and negative `var`, return, and C# 7.0 cases; identity casts, constant interpolation, and exact inferred tuple/anonymous names have positive, negative, comment-preservation, and toggle cases. IDE0005 did not produce an actionable diagnostic in the project-backed experiment, so unused-import remediation remains outstanding; blank-line cleanup is also outstanding.
- AC-01/EC-01a/b have focused formatter evidence for XML and ordinary comments, intentional visual comment grouping, preserved comment text, CRLF, trailing whitespace, non-mutating check mode, and idempotence. Additional alignment/layout cases remain outstanding.
- AC-04 is partial: direct explicit local, field, and property initializers are converted with C# 9+ and matching Roslyn types; tests cover those positives, return exclusion, C# 8 exclusion, check-only, and idempotence. Ambiguous-overload coverage is outstanding.
- AC-07 is partial: existing transactional compile validation remains; new normalizers have focused check-only and idempotence coverage, but the complete milestone failure/interaction matrix is absent.
- AC-12 and AC-14 are partial: catalogue output was updated and two directly affected specs were reconciled. CLI/review/audit and remaining authority updates depend on unimplemented modernization behavior.
- AC-02 is partial: contiguous plain imports sort deterministically, with `dotnet_sort_system_directives_first` honored and alias/static/global/comment/directive boundaries retained. Focused positive, boundary, toggle, check-only and idempotence coverage passes; modifier ordering remains outstanding.
- VAL-04 passed with a fresh installed-consumer receipt. AC-15 and EC-15a remain partial because the installed package lacks the M0023 modernization workflow. VAL-05 passed for the repository's current seven-stage plan; implementation evidence for required modernization cases is still missing.
- AC-16 and VAL-06 remain pending an authorized independent human decision. `.review/REV-M0023-COMPLETION.md` is a pending request, not approval.

**Outcome: incomplete implementation; human review pending.** PR: https://github.com/carlrabbit/dotnet-ai-code-hygiene/pull/25 (open draft). No milestone completion is claimed. The nine modernization families, shared max-five budget, occurrence-level decision workflow, and related audit behavior remain unimplemented. All current repository validation stages pass; the outstanding blocker is incomplete implementation scope, not an environment failure.
