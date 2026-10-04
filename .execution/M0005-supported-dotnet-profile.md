# Execution Ledger — M0005 Supported .NET Profile & Documentation Hygiene

Primary milestone: `docs/milestones/M0005-supported-dotnet-profile.md`

This file is mutable operational execution state, not project authority.

Planning seeded the lossless obligation/evidence registry only. Implementation owns work packages, mappings, concrete evidence, statuses, and resume state.

## Milestone Obligation Registry

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | M0005 introduces committed `.hygiene/profile.json` schema v1 with exactly supported profile `dotnet-11` v1, and normal `check` rejects missing/unsupported profile state with exit `3` plus bootstrap/update guidance. | WP-01/WP-02 | HygieneEngine, ProfileManager, Program | VAL-01/02/03/05 | implemented |
| AC-02 | acceptance | Engine has fixed bootstrap/update/normal rule sets whose membership matches profile authority; a rule receives no execution-phase parameter that changes its diagnosis/remediation semantics. | WP-01/WP-02 | HygieneEngine, ProfileManager, Program | VAL-01/02/03/05 | implemented |
| AC-03 | acceptance | `hygiene bootstrap [--output text|json]` is functional, repository-wide, targetless, idempotent, applies available deterministic profile remediations, re-evaluates profile rules, reports unresolved findings, and returns success when execution succeeds even if profile findings remain. | WP-01/WP-02 | HygieneEngine, ProfileManager, Program | VAL-01/02/03/05 | implemented |
| AC-04 | acceptance | `hygiene update [--output text|json]` is functional, repository-wide, targetless, requires supported profile state, reconciles current profile-owned artifacts/drift, is idempotent, and rejects unsupported/future profile versions without inventing a migration. | WP-01/WP-02 | HygieneEngine, ProfileManager, Program | VAL-01/02/03/05 | implemented |
| AC-05 | acceptance | `profile.dotnet.analysis.required` v1 validates effective supported-project configuration for `AnalysisLevel=11`, `EnableNETAnalyzers=true`, `EnforceCodeStyleInBuild=true`, IDE0011 braces preference/error, and IDE0040 accessibility preference/error. | WP-03 | ProfileManager.Analyze and reconciliation | VAL-01/03/05 | implemented |
| AC-06 | acceptance | Profile analysis detects repository-configured `TreatWarningsAsErrors=true` and non-empty `WarningsAsErrors` as violations while the hygiene-managed profile config sets no unrelated diagnostic severities or bulk analyzer severity. | WP-03 | ProfileManager.Analyze and reconciliation | VAL-01/03/05 | implemented |
| AC-07 | acceptance | Bootstrap/update deterministically own/reconcile `.hygiene/profile/Hygiene.props`, the bounded root `Directory.Build.props` import integration, root `.editorconfig` `root=true`, and the bounded hygiene profile block while preserving unrelated user content. | WP-03 | ProfileManager.Analyze and reconciliation | VAL-01/03/05 | implemented |
| AC-08 | acceptance | Profile analysis/remediation is based on effective project/analyzer configuration: project/nested EditorConfig overrides that weaken required settings remain visible findings rather than being silently overwritten outside hygiene-owned surfaces. | WP-03 | ProfileManager.Analyze and reconciliation | VAL-01/03/05 | implemented |
| AC-09 | acceptance | `profile.stylecop.prohibited` v1 detects StyleCop activation through direct package reference, central package management, or effective analyzer input with de-duplication of the same configured source. | WP-03 | ProfileManager.Analyze and reconciliation | VAL-01/03/05 | implemented |
| AC-10 | acceptance | StyleCop prohibition has no automatic removal remediation; bootstrap/update leave the dependency/configuration untouched and surface the same deterministic finding for caller resolution/escalation. | WP-03 | ProfileManager.Analyze and reconciliation | VAL-01/03/05 | implemented |
| AC-11 | acceptance | Mandatory profile rules are exposed as non-configurable, `rules enable/disable` cannot alter them, `ignore` cannot suppress their findings, and persisted attempts to disable them are invalid product configuration. | WP-01/WP-02 | mandatory Rule.Configurable policy and ProfileManager.Commit | VAL-01/02/03 | implemented |
| AC-12 | acceptance | Bootstrap/update profile mutations reuse M0004 all-or-nothing transactional safety: validation/conflict/fault/cancellation cannot leave a partial marker/props/import/editorconfig profile installation. | WP-01/WP-02 | mandatory Rule.Configurable policy and ProfileManager.Commit | VAL-01/02/03 | implemented |
| AC-13 | acceptance | `docs.summary.required` v2 requires a non-empty documentation summary for every covered public/internal API subject while preserving the existing ordinary symbol-category exclusions except for the deliberate positional-record-property subject. | WP-04 | HygieneEngine Roslyn documentation subject/rule pipeline | VAL-01/03/04 | implemented |
| AC-14 | acceptance | Ordinary covered subjects resolve their summary through local non-empty `<summary>` or direct declaration-level `<inheritdoc/>`; inheritdoc is not recursively resolved. | WP-04 | HygieneEngine Roslyn documentation subject/rule pipeline | VAL-01/03/04 | implemented |
| AC-15 | acceptance | A synthesized public/internal property represented by a positional record or record-struct parameter resolves its required summary through the matching non-empty record `<param>` element or declaration-level `<inheritdoc/>`. | WP-04 | HygieneEngine Roslyn documentation subject/rule pipeline | VAL-01/03/04 | implemented |
| AC-16 | acceptance | Ordinary method/constructor/delegate parameters and type parameters remain optional documentation: documenting one does not require documentation for siblings, and missing `<param>/<typeparam>/<returns>/<value>/<exception>` alone is not a finding. | WP-04 | HygieneEngine Roslyn documentation subject/rule pipeline | VAL-01/03/04 | implemented |
| AC-17 | acceptance | Direct `<inheritdoc/>` excuses missing summary material only; any explicit documentation written on the same declaration remains subject to applicable consistency and sentence rules. | WP-04 | HygieneEngine Roslyn documentation subject/rule pipeline | VAL-01/03/04 | implemented |
| AC-18 | acceptance | `docs.xml.consistent` v1 validates present `<param>`, `<typeparam>`, `<paramref>`, and `<typeparamref>` names/duplicates/non-empty prose without introducing completeness requirements. | WP-04 | HygieneEngine Roslyn documentation subject/rule pipeline | VAL-01/03/04 | implemented |
| AC-19 | acceptance | `docs.xml.consistent` v1 validates present `<returns>`, `<value>`, and `<exception>` structural applicability/non-empty prose, including exception `cref` resolving to an exception type, without requiring these elements. | WP-04 | HygieneEngine Roslyn documentation subject/rule pipeline | VAL-01/03/04 | implemented |
| AC-20 | acceptance | `docs.text.sentence` v1 requires explicit non-empty prose in summary/param/typeparam/returns/value/exception elements to end with `.`, `?`, or `!` across inline XML content and does not apply this requirement to inheritdoc/remarks/example/code/c/see/seealso/list/custom elements as independent units. | WP-04 | HygieneEngine Roslyn documentation subject/rule pipeline | VAL-01/03/04 | implemented |
| AC-21 | acceptance | `docs.summary.quality.review` v2 keeps the existing max-five deterministic rubric/escalation contract but samples API subjects through the M0005 summary-carrier model, including positional-record-property `<param>` summaries and excluding inheritdoc-only/optional parameter documentation. | WP-04 | HygieneEngine Roslyn documentation subject/rule pipeline | VAL-01/03/04 | implemented |
| AC-22 | acceptance | Canonical normal rule order is exactly the M0005 order in `docs/specs/DOCUMENTATION.md`; bootstrap/update evaluate only the mandatory profile set in M0005. | WP-01/WP-06 | canonical Rules and M0002-M0004 regression | VAL-01/02/03/05 | implemented |
| AC-23 | acceptance | Existing configurable-rule enable/disable, finding ordering/IDs, explain, ignore/stale behavior, review expansion/handoff, format/normalize, `--check`, transactional rewrite, and agent-help contracts remain regression-compatible except explicit M0005 changes. | WP-01/WP-06 | canonical Rules and M0002-M0004 regression | VAL-01/02/03/05 | implemented |
| AC-24 | acceptance | Product/package version is exactly `0.5.0`, package metadata and installed `hygiene --version` agree, and Tier-4 validation uses the exact locally packed/installed tool rather than development output. | WP-05 | 0.5.0 metadata and exact installed consumer | VAL-04/05 | implemented |
| AC-25 | acceptance | Installed-tool consumer validation in an isolated repository exercises bootstrap, idempotent update, mandatory-profile check/rules behavior, representative documentation findings, and retained M0004 normalize/check workflow. | WP-05 | 0.5.0 metadata and exact installed consumer | VAL-04/05 | implemented |
| AC-26 | acceptance | M0005 adds no StyleCop dependency, external specialist analyzer requirement, arbitrary analyzer compatibility layer, model/provider call, automatic StyleCop removal, non-.NET profile, MCP/IDE integration, or GitHub workflow. | WP-05/WP-06 | scope/dependency/workflow inspection | VAL-05 | implemented |
| DOC-01 | documentation | README/public docs explain profile bootstrap/update/check lifecycle, generated/managed ownership, analyzer/severity policy, mandatory profile-rule behavior, and StyleCop prohibition. | WP-05 | README and current installed public behavior | VAL-06 | implemented |
| DOC-02 | documentation | README/public docs explain summary-carrier semantics, positional records, direct inheritdoc, optional XML documentation, structural consistency, sentence punctuation, and separation from semantic summary-quality review. | WP-05 | README and current installed public behavior | VAL-06 | implemented |
| REV-01 | review | Human completion review confirms the generated profile is appropriately opinionated/minimal, bootstrap/update ownership is safe, StyleCop handling is appropriate, documentation rules avoid accidental completeness policy, record/inheritdoc semantics are understandable, and M0002-M0004 workflows remain coherent. | WP-06 | awaiting project owner/delegate; not self-approved | VAL-07 | awaiting human review |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-01a | AC-01 | missing marker -> bootstrap guidance | VAL-02, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-01b | AC-01 | unsupported/future marker -> update/error guidance | VAL-02, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-03a | AC-03 | clean repository bootstrap creates profile | VAL-01, VAL-02, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-03b | AC-03 | second bootstrap is idempotent | VAL-01, VAL-02, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-04a | AC-04 | current-profile drift repaired | VAL-01, VAL-02, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-04b | AC-04 | unsupported/future version rejected without mutation | VAL-01, VAL-02, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-05a | AC-05 | canonical generated/effective profile passes | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-05b | AC-05 | project override weakens MSBuild property | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-05c | AC-05 | nested EditorConfig weakens required IDE rule | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-06a | AC-06 | TreatWarningsAsErrors repository setting found | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-06b | AC-06 | WarningsAsErrors non-empty found | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-07a | AC-07 | existing user `.editorconfig` preserved around managed block | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-07b | AC-07 | existing user `Directory.Build.props` preserved around managed import | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-09a | AC-09 | direct StyleCop.Analyzers reference | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-09b | AC-09 | central package management activates StyleCop | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-09c | AC-09 | effective StyleCop analyzer input detected/de-duplicated | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-11a | AC-11 | disable mandatory rule rejected | VAL-01, VAL-02, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-11b | AC-11 | ignore mandatory finding rejected | VAL-01, VAL-02, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-12a | AC-12 | fault/conflict during multi-artifact profile update leaves pre-run state | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-14a | AC-14 | ordinary direct `<summary>` | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-14b | AC-14 | ordinary direct `<inheritdoc/>` | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-15a | AC-15 | positional record class property uses `<param>` | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-15b | AC-15 | positional record struct property uses `<param>` | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-16a | AC-16 | one of several normal parameters documented; siblings absent | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-17a | AC-17 | inheritdoc-only satisfies missing summary | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-17b | AC-17 | inheritdoc plus malformed/unsentenced explicit prose still reports | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-18a | AC-18 | param/typeparam matching, duplicate and orphan cases | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-18b | AC-18 | paramref/typeparamref valid and invalid references | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-19a | AC-19 | returns/value applicability and non-empty checks | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-19b | AC-19 | exception valid/invalid cref without thrown-exception inference | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-20a | AC-20 | prose ending after inline `<see/>` with punctuation passes; without punctuation fails | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-20b | AC-20 | `<example>`/custom elements are not independently sentence-enforced | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-21a | AC-21 | ordinary `<summary>` enters semantic sample | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-21b | AC-21 | positional-property `<param>` enters semantic sample | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-21c | AC-21 | inheritdoc-only and optional normal param prose excluded | VAL-01, VAL-03 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-24a | AC-24 | packed metadata + installed `--version` are 0.5.0 | VAL-04, VAL-05 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-25a | AC-25 | installed bootstrap/update/profile check lifecycle | VAL-04 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-25b | AC-25 | installed documentation-rule behavior | VAL-04 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |
| EC-25c | AC-25 | installed M0004 normalize/check regression | VAL-04 | case implementation and mapped test/installed evidence in milestone completion map | evidence collected |

## Validation Gates

| ID | Depth/target/locus | Proves | Status | Evidence |
|---|---|---|---|---|
| VAL-01 | Tier 1 Core rule-set/profile/remediation/documentation tests / local Windows 11 + .NET 11 | AC-02, AC-05..AC-22 + mapped ECs | passed | `./eng/validate.ps1` — Core 26/26, CLI 21/21, exact installed 0.5.0 consumer passed |
| VAL-02 | Tier 1 built CLI process / local Windows 11 + .NET 11 | AC-01, AC-03, AC-04, AC-11, AC-22 + mapped ECs | passed | `./eng/validate.ps1` — Core 26/26, CLI 21/21, exact installed 0.5.0 consumer passed |
| VAL-03 | Tier 3 isolated SDK-style Git fixture repositories / Windows 11 + .NET 11 + Git | AC-01, AC-03..AC-23 + mapped ECs | passed | `./eng/validate.ps1` — Core 26/26, CLI 21/21, exact installed 0.5.0 consumer passed |
| VAL-04 | Tier 4 exact locally packed+installed 0.5.0 tool / isolated Windows consumer repo | AC-24, AC-25 + EC-24a/EC-25a..c | passed | `./eng/validate.ps1` — Core 26/26, CLI 21/21, exact installed 0.5.0 consumer passed |
| VAL-05 | Tier 2 complete repo via `./eng/validate.ps1` / Windows 11 | AC-23, AC-24, AC-26 | passed | `./eng/validate.ps1` — Core 26/26, CLI 21/21, exact installed 0.5.0 consumer passed |
| VAL-06 | Tier 2 README/public docs vs installed behavior | DOC-01, DOC-02 | passed | `./eng/validate.ps1` — Core 26/26, CLI 21/21, exact installed 0.5.0 consumer passed |
| VAL-07 | Tier 5 human review `REV-M0005-COMPLETION` | REV-01 | awaiting human review | `REV-M0005-COMPLETION` remains for project owner/delegate |

## Work Packages

| WP | Live-repository basis | Obligation mapping | Status |
|---|---|---|---|
| WP-01 Rule model and CLI boundaries | `HygieneEngine.Rules`, persistence, and `Program.cs` | AC-01..AC-04, AC-11, AC-22..AC-23 | complete |
| WP-02 Profile ownership and transactional lifecycle | `ProfileManager` plus the marker, generated props, root props, and EditorConfig | AC-03..AC-12 | complete |
| WP-03 Effective profile and analyzer checks | `ProfileManager.Analyze` over projects, props/targets, EditorConfig, and StyleCop references | AC-05..AC-10 | complete |
| WP-04 Documentation carriers and rules | Roslyn documentation analysis in `HygieneEngine` | AC-13..AC-21 | complete |
| WP-05 Package, installed consumer, and public docs | CLI `0.5.0`, `eng/validate.ps1`, and README | AC-22..AC-26, DOC-01..DOC-02 | complete |
| WP-06 Regression, evidence, and reconciliation | Core/CLI fixture suites and final validation gate | AC-23..AC-26, ECs, VAL-01..VAL-07 | automated work complete; human review pending |

## Resume Point

Automated implementation and validation are complete. Awaiting required project-owner/delegate review `REV-M0005-COMPLETION`; implementation has not self-approved.

## Final Reconciliation

- [x] freshly reread the M0005 milestone and required authority;
- [x] verify exact milestone/ledger obligation-ID set equality;
- [x] verify exact milestone/ledger evidence-case-ID set equality;
- [x] verify every obligation/case has concrete implementation and required validation evidence;
- [x] verify M0004 prerequisite remained compatible with this ready contract;
- [x] verify profile rule semantics did not become command/phase-parameterized;
- [x] verify mandatory profile rules cannot be disabled/ignored;
- [x] verify bootstrap/update mutations are transactional and preserve unrelated user content;
- [x] verify no documentation completeness rule was introduced beyond generic summaries;
- [x] verify positional record + inheritdoc cases explicitly;
- [x] run exact installed-tool Tier-4 scenario using current 0.5.0 package;
- [x] run complete `./eng/validate.ps1`;
- [x] confirm no StyleCop/external-analyzer/model/MCP/workflow scope was added;
- [x] update direct required public docs;
- [x] preserve durable completion evidence in the milestone;
- [ ] request `REV-M0005-COMPLETION` and terminate `AWAITING HUMAN REVIEW`.
