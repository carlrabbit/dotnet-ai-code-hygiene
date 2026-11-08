# REV-M0024-COMPLETION

## Review request

Please independently review the M0024 implementation in the linked pull request and record an actual decision below. This request is intentionally pending until a reviewer examines the current source, execution ledger, validation receipts, and PR diff.

- Milestone: `M0024 SDK-Backed Hygiene Profile and Source-Owned Modernization Exceptions`
- Baseline: `6f9fa952def8da4a36c0de715bd206f7223a7f7c` (PR #25 head)
- Implementation source revision: `a52ba2302557cf6ee9ff82139e4bb334e9a66cff` (includes the third source-exception attachment-boundary correction)
- Implementation branch: `codex/m0024-sdk-format-profile-and-source-exceptions`
- Pull request: https://github.com/carlrabbit/dotnet-ai-code-hygiene/pull/26 (draft; target `codex/m0023-deterministic-cleanup`)
- Candidate package SHA-256: `1a07855de09be081d6be61e858c0a90437324a90698088425a5b859f31b1f8fb`
- Execution ledger: `.execution/M0024-sdk-format-profile-and-source-exceptions.md`
- Validation on the corrected implementation: solution build; Core suite 109/109; CLI suite 36/36; README policy; exact package; and installed consumers all have fresh current receipts. The exact package was installed locally and consumer provenance records closing-brace nonattachment as well as same-line source-subject and exact IDE0007/IDE0008 behavior. Self-host fingerprints the complete tracked diff and is run after this review-packet commit, followed by `./eng/validate.ps1 -Verify`.

## Implementation review corrections

The independent implementation review findings against revision `57aaebc49193c3b89c675f414e29c785fe26ccfd` were addressed in `c2926723f91013b7b071b848a85976e74dc58d2a`:

1. IDE0090 now uses `csharp_style_implicit_object_creation_when_type_is_apparent=true`. Nested false/true settings are exercised against real SDK formatting; profile generation, migration and installed bootstrap verify the supported key. IDE0036 order includes `file` and `required`; SDK formatting and compilation verify modern declarations.
2. Native IDE0090 pragma validation is separate from the nine hygiene modernization populations. Valid, missing-reason, overbroad, unbalanced, ineffective and stale pragmas are checked by hygiene; SDK normalization demonstrates only the intended IDE0090 remains suppressed while IDE0034 and unsuppressed IDE0090 fixes still run. The pinned analyzer package does not provide IDE0079 for this check; effectiveness is verified with the actual IDE0090 analyzer against a source view with that native pragma removed.
3. Each style/whitespace phase and project batch writes to its own report location; check-only aggregation unions reports. A two-project SDK fixture compares exact changed paths for style-only, whitespace-only, both phases, separate phase files, no-change and repeat runs, and confirms check-only bytes do not change.
4. IDE0071 is removed from automatic formatting and `style.interpolation.redundant` is Review-only. Direct .NET 11 SDK behavior shows IDE0071 simplifies explicit `ToString()` in an interpolation but does not turn `$"hello"` into a string literal; no unrelated automatic transform was introduced.
5. Source exception discovery first checks for relevant annotations. No-annotation lifecycle instrumentation proves no project/candidate analysis occurs; source parsing is cached per path and existing exceptions build only the applicable rule/subject candidates. The complete Core/CLI gates retain ordinary sampling and exhaustive audit coverage.
6. `format --changed` and `normalize --changed` are explicitly check-only selection modes. Help, docs and tests agree; an installed dirty consumer with a real formatting defect reports the selected path without writes, and ordinary dirty mutation still rejects.
7. The entire approved diagnostic inventory and EditorConfig mappings were reviewed. No diagnostic was added. Documentation specifies each approved diagnostic purpose, option/value, fixer disposition and IDE0071 mismatch. IDE0037 tuple and anonymous-type options and all other listed mappings are exercised by the SDK fixture.

Correction-specific focused tests passed: Core source-exception no-work/cache (2/2), profile generation/migration/effective IDE0090 nested behavior (2/2), fixed SDK inventory (1/1), CLI native suppression behavior (1/1), and formatter multi-phase/multi-project aggregation (1/1). Full Core 106/106 and CLI 36/36 passed. Final stage receipts and installed package provenance are in `.execution/M0024-sdk-format-profile-and-source-exceptions.md`.

## Second implementation review corrections

The second review findings against PR #26 revision `2dfba9377202494f3649c257c80a31bef8aa4e3c` are addressed in commit `b45b249904554ccf32472f3c0a5f846f45157b16` on the same PR branch:

1. Native suppression filtering now requires both the modernization rule family and exact Roslyn diagnostic identity. Candidate spans and diagnostic IDs are carried through all nine population adapters. Fresh Core tests prove IDE0007 does not filter IDE0008; the CLI test generates both IDs under nested conflicting preferences, confirms only IDE0007 is filtered in normal review, and confirms exhaustive audit still includes both with a reason only on the suppressed candidate.
2. Hygiene exceptions preserve source syntax spans from parse through candidate filtering and stale validation. Same-line statements no longer share a suppression range. Core fixtures prove next-subject containment and exhaustive reason annotations; the CLI case confirms the sibling statement remains normally sampled, audit keeps both, and making only the attached candidate ineligible reports stale even while its sibling remains.
3. Native pragma handling uses active Roslyn `PragmaWarningDirectiveTriviaSyntax` rather than line regexes. Justification must come from an actual same-line or adjacent comment. Parser fixtures cover missing/boilerplate text, noncomment preceding source, matched/unmatched and multiple-ID directives, scope length, literal lookalikes and inactive preprocessor content. Fresh focused Core suite passed 5/5; fresh focused CLI lifecycle suite passed 1/1 and includes actual SDK normalize plus authorized exhaustive audit.

The exact newly packed installed-consumer stage passed against package SHA-256 `fa0cb290539533a31d848e6a1226309145b76284541280029aa45837c7e70098`, including same-line source-subject and IDE0007/IDE0008 exact-identity checks. The execution ledger records current receipt fingerprints and provenance hash. No reviewer identity or decision has been entered; this packet remains pending independent review.

## Third implementation review correction

The third review identified a `hygiene:retain` comment immediately before a closing brace that could bind to an enclosing block/declaration because the selected syntax node began before the annotation. Commit `a52ba2302557cf6ee9ff82139e4bb334e9a66cff` corrects attachment selection to require the selected Roslyn statement/member span to begin after the annotation. The added Core regression covers method-body, type and nested-block closing braces with earlier candidates; valid following statements/members; and same-line sibling precision. Invalid comments are findings and do not suppress earlier candidates in either normal filtering or exhaustive filtering. The actual CLI `check`/`--review-all` regression and exact installed-consumer fixture confirm the behavior across both consumer boundaries.

Focused evidence: Core `ModernizationSourceExceptionTests/*` 7/7 and CLI `NativeSuppressionsAreValidatedSeparatelyAndDoNotDisableOtherSdkFixes` 1/1. Full current gates: solution fingerprint `27365569244f2d673a31189c9c58e9c6d791b5ddc208b89d03f55555ac52582c`; Core 109/109 fingerprint `1cc85fe7280cb28786382a8d3c74ff4f319938c48114d36a7e38fd719e89c533`; CLI 36/36 fingerprint `f6a125a5be76d3c5acc156a145750a44104f22312a552d55d61a5ca0a7f572f9`; README policy `5d3dba1cbc8d99e2ca79e58f9102905a31f09b65f5f24777c60b5eea60ae4513`; package `2ce081ff2d6b62c7b274a3f595dd51c4acf5ed8b26b4fc6b3dd4bf5e9881b727`; installed consumers `47b630629e2150801ba0f4340480923b0f1298c26852263e232f294909228aef`. Exact package SHA-256 `1a07855de09be081d6be61e858c0a90437324a90698088425a5b859f31b1f8fb`; consumer provenance SHA-256 `4A8633D711A3088ED8172948CF557959AE5D48EE4D885B5C018E64B1426FE308`. The final self-host and `-Verify` receipts are recorded in the execution ledger after this packet is committed.

## Independent review questions

1. Is delegation to the supported .NET 11 SDK formatter complete and bounded to the approved diagnostic set, and is any retained specialized fallback necessary?
2. Does the per-rule disposition inventory report unsupported or narrower behavior honestly, including concrete negative cases, without claiming parity the SDK does not provide?
3. Are native pragma and `hygiene:retain` exceptions precise, justified, adjacent to one intended subject, and reported when malformed, stale, unknown, or ineffective?
4. Do clean Git checkpoint, stable HEAD/index checks, partial-failure behavior, and manual recovery avoid clobbering user data or claiming false rollback/completion?
5. Are the six semantic review budgets and nine modernization populations still independent and bounded, with exhaustive audit accounting for retained exceptions?
6. Does each acceptance/evidence obligation have current, case-specific evidence in the ledger and validator receipts, with no aggregate-green substitution?

## Reviewer decision

- Decision: pending
- Reviewer identity: unassigned
- Decision date: pending

## Reviewer rationale

Pending independent reviewer input. Do not replace this text with an implementation-agent conclusion.

## Decision evidence

Pending independent reviewer input. Record links or exact evidence inspected, including any requested changes and the revision reviewed.
