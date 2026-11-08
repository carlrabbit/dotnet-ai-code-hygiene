# REV-M0024-COMPLETION

## Review request

Please independently review the M0024 implementation in the linked pull request and record an actual decision below. This request is intentionally pending until a reviewer examines the current source, execution ledger, validation receipts, and PR diff.

- Milestone: `M0024 SDK-Backed Hygiene Profile and Source-Owned Modernization Exceptions`
- Baseline: `6f9fa952def8da4a36c0de715bd206f7223a7f7c` (PR #25 head)
- Implementation source revision: `c2926723f91013b7b071b848a85976e74dc58d2a` (includes all corrections for the independent implementation review)
- Implementation branch: `codex/m0024-sdk-format-profile-and-source-exceptions`
- Pull request: https://github.com/carlrabbit/dotnet-ai-code-hygiene/pull/26 (draft; target `codex/m0023-deterministic-cleanup`)
- Candidate package SHA-256: `a2d8a3746655818e5b1ebd3e4e36d4258ec4b079d357d4d3f41d271f1dd578c7`
- Execution ledger: `.execution/M0024-sdk-format-profile-and-source-exceptions.md`
- Validation on the corrected implementation: solution build; Core suite 106/106; CLI suite 36/36; README policy; exact package; installed consumers; and self-host all passed. The self-host receipt fingerprints the complete tracked diff, so it is rerun after this metadata commit and followed by `./eng/validate.ps1 -Verify`.

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
