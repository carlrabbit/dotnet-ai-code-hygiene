# Milestone — M0005 Supported .NET Profile & Documentation Hygiene

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Baseline executor readiness | confirmed |
| Decision preservation | confirmed |
| Execution tractability | confirmed |
| Execution ledger | `.execution/M0005-supported-dotnet-profile.md` planning-seeded |
| Scope size | large coherent developer-tool milestone |
| Implementation autonomy | high within resolved contracts |
| Documentation sync | direct contradicted/public docs only; broad sync deferred |
| Focused validation | Tier 1 Core + built CLI process |
| Repository validation | Tier 2 `./eng/validate.ps1` |
| Integration validation | Tier 3 isolated SDK-style Git repositories |
| Validation locus/platform | local Windows 11 + .NET 11 SDK + Git + PowerShell |
| Consumer/release validation | Tier 4 exact locally packed/installed 0.5.0 tool |
| Human review | required |

## Execution prerequisite

M0004 must be complete before M0005 implementation begins.

If the accepted M0004 completion materially changes the public rewrite/CLI/transaction/installed-tool contracts assumed by the M0005 authority, return to planning before production edits rather than silently reinterpret this milestone.

## Goal

Make hygiene own a small, tested .NET analysis profile rather than attempt arbitrary analyzer coexistence, and add a small deterministic documentation-hygiene slice inspired by the useful 80/20 of StyleCop without depending on or cloning StyleCop's completeness model.

## Target State

A repository can establish/reconcile the supported profile with:

```text
hygiene bootstrap
hygiene update
```

and then run normal:

```text
hygiene check
```

against one coherent tested baseline.

Profile v1:

```text
dotnet-11 v1
AnalysisLevel 11
built-in .NET analyzers enabled
code-style analyzers enforced in build
IDE0011 braces = error
IDE0040 explicit accessibility = error
no repository-wide warnings-as-errors policy
StyleCop prohibited
```

Rules remain fixed contracts. Bootstrap/update apply available deterministic remediation; check never mutates.

Documentation requires only a generic summary. Optional XML documentation remains optional but must be structurally sane if present. Summary carriers account for positional records and direct `<inheritdoc/>`.

## Scope

- versioned hygiene profile marker/state;
- fixed internal bootstrap/update/normal rule sets;
- public `hygiene bootstrap` and `hygiene update`;
- namespaced generated MSBuild profile artifact plus bounded standard-file integration;
- root EditorConfig managed profile block;
- mandatory `profile.dotnet.analysis.required`;
- mandatory `profile.stylecop.prohibited`;
- non-disableable/non-ignorable mandatory profile findings;
- `.NET 11 + AnalysisLevel 11` tested analyzer baseline;
- IDE0011/IDE0040 built-in enforcement as errors;
- prohibition of repository-configured global warnings-as-errors;
- `docs.summary.required` v2 with semantic summary carriers and direct inheritdoc;
- `docs.xml.consistent` v1;
- `docs.text.sentence` v1;
- `docs.summary.quality.review` v2 using summary carriers;
- package/product version `0.5.0`;
- installed-tool consumer validation;
- M0002/M0003/M0004 compatibility.

## Non-goals

- no approved third-party async analyzer yet;
- no arbitrary analyzer-stack compatibility;
- no StyleCop dependency, compatibility mode, rule-number reproduction, or wholesale reimplementation;
- no parameter/type-parameter/return/exception documentation completeness policy;
- no recursive inheritdoc resolution;
- no deterministic semantic prose-quality heuristic;
- no nullable-profile decision;
- no target-framework enforcement;
- no configurable profile;
- no rule phase/mode parameter that changes rule meaning;
- no automatic model/provider call;
- no automatic removal of StyleCop;
- no Go/other-language profile;
- no MCP/IDE integration;
- no GitHub Actions/workflows;
- no public-feed publication requirement.

## Decisions and Constraints

- Rule-set membership selects fixed rules and never changes a rule's semantics.
- A rule may expose a fixed deterministic remediation; check never executes it.
- Bootstrap/update evaluate profile rules, apply available deterministic remediation, re-evaluate, then report unresolved findings.
- Bootstrap/update are repository-wide and accept no file/directory targets or `--changed`.
- Profile rules are mandatory after profile installation and cannot be disabled or ignored.
- Normal M0005 check requires a current supported profile marker.
- Supported hygiene profile is exactly `dotnet-11` v1; there is no M0005 profile selector/configuration.
- Profile-generated artifacts are committed project state.
- Namespaced `.hygiene/profile/Hygiene.props` owns the canonical MSBuild settings; root `Directory.Build.props` contains only the bounded hygiene import integration plus user content.
- Root `.editorconfig` contains a bounded hygiene-managed block and `root=true`; unrelated user content is preserved.
- Effective project/analyzer configuration is authoritative; merely having generated text does not establish conformance.
- `AnalysisLevel` is pinned to `11`, not `latest`.
- M0005 profile sets only explicitly enforced diagnostics to `error`; it does not bulk-promote other analyzer diagnostics.
- Repository-configured `TreatWarningsAsErrors=true` and non-empty `WarningsAsErrors` are prohibited.
- External invocation-time warning handling remains the caller's choice.
- StyleCop is prohibited and has no deterministic auto-removal.
- Only generic summaries are required documentation.
- A documentation summary is semantic and may use `<summary>`, record `<param>`, or direct `<inheritdoc/>` according to the subject/carrier model.
- Normal parameters do not become required documentation subjects when another parameter is documented.
- Direct inheritdoc satisfies missing summary locally; no recursive parent lookup is performed.
- Explicit documentation beside inheritdoc must still satisfy structural/prose checks.
- `docs.summary.required` and `docs.summary.quality.review` advance to v2; existing rule IDs remain stable at the conceptual-summary level.
- Existing v1 ignore decisions do not silently migrate to v2 occurrence identity.
- M0005 package/product version is `0.5.0`.
- M0004 transactional mutation is reused for profile edits.
- Existing M0002/M0003/M0004 public behavior remains compatible unless explicitly changed here.

## Baseline Executor Readiness

Architecture, semantics, compatibility, scope, profile ownership, documentation carriers, analyzer severity policy, validation topology, and human-review policy are settled.

Implementation may choose concrete types/files/refactorings/test decomposition and safe mechanics for parsing/updating bounded managed sections/imports, but may not change the profile contract or documentation semantics.

No external research reconstruction is required.

## Required Authority

- `docs/SPECS.md`
- `docs/specs/HYGIENE.md`
- `docs/specs/PROFILE.md`
- `docs/specs/DOCUMENTATION.md`
- `docs/specs/SEMANTIC-REVIEWS.md`
- `docs/specs/REVIEW-HANDOFFS.md`
- `docs/specs/REWRITES.md`
- `docs/specs/AGENT-HELP.md`
- `docs/ARCHITECTURE.md`
- `docs/ENGINEERING.md`
- `docs/TERMINOLOGY.md`
- `docs/PUBLIC-DOCS.md`

## Acceptance Criteria

- **AC-01** — M0005 introduces committed `.hygiene/profile.json` schema v1 with exactly supported profile `dotnet-11` v1, and normal `check` rejects missing/unsupported profile state with exit `3` plus bootstrap/update guidance.
- **AC-02** — Engine has fixed bootstrap/update/normal rule sets whose membership matches profile authority; a rule receives no execution-phase parameter that changes its diagnosis/remediation semantics.
- **AC-03** — `hygiene bootstrap [--output text|json]` is functional, repository-wide, targetless, idempotent, applies available deterministic profile remediations, re-evaluates profile rules, reports unresolved findings, and returns success when execution succeeds even if profile findings remain.
- **AC-04** — `hygiene update [--output text|json]` is functional, repository-wide, targetless, requires supported profile state, reconciles current profile-owned artifacts/drift, is idempotent, and rejects unsupported/future profile versions without inventing a migration.
- **AC-05** — `profile.dotnet.analysis.required` v1 validates effective supported-project configuration for `AnalysisLevel=11`, `EnableNETAnalyzers=true`, `EnforceCodeStyleInBuild=true`, IDE0011 braces preference/error, and IDE0040 accessibility preference/error.
- **AC-06** — Profile analysis detects repository-configured `TreatWarningsAsErrors=true` and non-empty `WarningsAsErrors` as violations while the hygiene-managed profile config sets no unrelated diagnostic severities or bulk analyzer severity.
- **AC-07** — Bootstrap/update deterministically own/reconcile `.hygiene/profile/Hygiene.props`, the bounded root `Directory.Build.props` import integration, root `.editorconfig` `root=true`, and the bounded hygiene profile block while preserving unrelated user content.
- **AC-08** — Profile analysis/remediation is based on effective project/analyzer configuration: project/nested EditorConfig overrides that weaken required settings remain visible findings rather than being silently overwritten outside hygiene-owned surfaces.
- **AC-09** — `profile.stylecop.prohibited` v1 detects StyleCop activation through direct package reference, central package management, or effective analyzer input with de-duplication of the same configured source.
- **AC-10** — StyleCop prohibition has no automatic removal remediation; bootstrap/update leave the dependency/configuration untouched and surface the same deterministic finding for caller resolution/escalation.
- **AC-11** — Mandatory profile rules are exposed as non-configurable, `rules enable/disable` cannot alter them, `ignore` cannot suppress their findings, and persisted attempts to disable them are invalid product configuration.
- **AC-12** — Bootstrap/update profile mutations reuse M0004 all-or-nothing transactional safety: validation/conflict/fault/cancellation cannot leave a partial marker/props/import/editorconfig profile installation.
- **AC-13** — `docs.summary.required` v2 requires a non-empty documentation summary for every covered public/internal API subject while preserving the existing ordinary symbol-category exclusions except for the deliberate positional-record-property subject.
- **AC-14** — Ordinary covered subjects resolve their summary through local non-empty `<summary>` or direct declaration-level `<inheritdoc/>`; inheritdoc is not recursively resolved.
- **AC-15** — A synthesized public/internal property represented by a positional record or record-struct parameter resolves its required summary through the matching non-empty record `<param>` element or declaration-level `<inheritdoc/>`.
- **AC-16** — Ordinary method/constructor/delegate parameters and type parameters remain optional documentation: documenting one does not require documentation for siblings, and missing `<param>/<typeparam>/<returns>/<value>/<exception>` alone is not a finding.
- **AC-17** — Direct `<inheritdoc/>` excuses missing summary material only; any explicit documentation written on the same declaration remains subject to applicable consistency and sentence rules.
- **AC-18** — `docs.xml.consistent` v1 validates present `<param>`, `<typeparam>`, `<paramref>`, and `<typeparamref>` names/duplicates/non-empty prose without introducing completeness requirements.
- **AC-19** — `docs.xml.consistent` v1 validates present `<returns>`, `<value>`, and `<exception>` structural applicability/non-empty prose, including exception `cref` resolving to an exception type, without requiring these elements.
- **AC-20** — `docs.text.sentence` v1 requires explicit non-empty prose in summary/param/typeparam/returns/value/exception elements to end with `.`, `?`, or `!` across inline XML content and does not apply this requirement to inheritdoc/remarks/example/code/c/see/seealso/list/custom elements as independent units.
- **AC-21** — `docs.summary.quality.review` v2 keeps the existing max-five deterministic rubric/escalation contract but samples API subjects through the M0005 summary-carrier model, including positional-record-property `<param>` summaries and excluding inheritdoc-only/optional parameter documentation.
- **AC-22** — Canonical normal rule order is exactly the M0005 order in `docs/specs/DOCUMENTATION.md`; bootstrap/update evaluate only the mandatory profile set in M0005.
- **AC-23** — Existing configurable-rule enable/disable, finding ordering/IDs, explain, ignore/stale behavior, review expansion/handoff, format/normalize, `--check`, transactional rewrite, and agent-help contracts remain regression-compatible except explicit M0005 changes.
- **AC-24** — Product/package version is exactly `0.5.0`, package metadata and installed `hygiene --version` agree, and Tier-4 validation uses the exact locally packed/installed tool rather than development output.
- **AC-25** — Installed-tool consumer validation in an isolated repository exercises bootstrap, idempotent update, mandatory-profile check/rules behavior, representative documentation findings, and retained M0004 normalize/check workflow.
- **AC-26** — M0005 adds no StyleCop dependency, external specialist analyzer requirement, arbitrary analyzer compatibility layer, model/provider call, automatic StyleCop removal, non-.NET profile, MCP/IDE integration, or GitHub workflow.
- **DOC-01** — README/public docs explain profile bootstrap/update/check lifecycle, generated/managed ownership, analyzer/severity policy, mandatory profile-rule behavior, and StyleCop prohibition.
- **DOC-02** — README/public docs explain summary-carrier semantics, positional records, direct inheritdoc, optional XML documentation, structural consistency, sentence punctuation, and separation from semantic summary-quality review.
- **REV-01** — Human completion review confirms the generated profile is appropriately opinionated/minimal, bootstrap/update ownership is safe, StyleCop handling is appropriate, documentation rules avoid accidental completeness policy, record/inheritdoc semantics are understandable, and M0002-M0004 workflows remain coherent.

## Acceptance Evidence Topology

| ID | Parent | Required evidence case | Why separate evidence is required |
|---|---|---|---|
| EC-01a | AC-01 | missing marker -> bootstrap guidance | first-use state |
| EC-01b | AC-01 | unsupported/future marker -> update/error guidance | incompatible state |
| EC-03a | AC-03 | clean repository bootstrap creates profile | initial install |
| EC-03b | AC-03 | second bootstrap is idempotent | repeat execution |
| EC-04a | AC-04 | current-profile drift repaired | useful v1 update path |
| EC-04b | AC-04 | unsupported/future version rejected without mutation | version safety |
| EC-05a | AC-05 | canonical generated/effective profile passes | positive profile path |
| EC-05b | AC-05 | project override weakens MSBuild property | effective MSBuild conflict |
| EC-05c | AC-05 | nested EditorConfig weakens required IDE rule | effective analyzer-config conflict |
| EC-06a | AC-06 | TreatWarningsAsErrors repository setting found | global promotion path |
| EC-06b | AC-06 | WarningsAsErrors non-empty found | selective compiler promotion property |
| EC-07a | AC-07 | existing user `.editorconfig` preserved around managed block | standard-file ownership |
| EC-07b | AC-07 | existing user `Directory.Build.props` preserved around managed import | MSBuild ownership |
| EC-09a | AC-09 | direct StyleCop.Analyzers reference | direct dependency path |
| EC-09b | AC-09 | central package management activates StyleCop | central dependency path |
| EC-09c | AC-09 | effective StyleCop analyzer input detected/de-duplicated | resolved analyzer path |
| EC-11a | AC-11 | disable mandatory rule rejected | configuration boundary |
| EC-11b | AC-11 | ignore mandatory finding rejected | decision boundary |
| EC-12a | AC-12 | fault/conflict during multi-artifact profile update leaves pre-run state | transaction failure |
| EC-14a | AC-14 | ordinary direct `<summary>` | direct carrier |
| EC-14b | AC-14 | ordinary direct `<inheritdoc/>` | inheritdoc carrier |
| EC-15a | AC-15 | positional record class property uses `<param>` | record-class carrier |
| EC-15b | AC-15 | positional record struct property uses `<param>` | record-struct carrier |
| EC-16a | AC-16 | one of several normal parameters documented; siblings absent | no completeness inference |
| EC-17a | AC-17 | inheritdoc-only satisfies missing summary | local exemption |
| EC-17b | AC-17 | inheritdoc plus malformed/unsentenced explicit prose still reports | explicit-prose validation |
| EC-18a | AC-18 | param/typeparam matching, duplicate and orphan cases | declaration elements |
| EC-18b | AC-18 | paramref/typeparamref valid and invalid references | inline-reference path |
| EC-19a | AC-19 | returns/value applicability and non-empty checks | value-like elements |
| EC-19b | AC-19 | exception valid/invalid cref without thrown-exception inference | exception path |
| EC-20a | AC-20 | prose ending after inline `<see/>` with punctuation passes; without punctuation fails | inline XML sentence end |
| EC-20b | AC-20 | `<example>`/custom elements are not independently sentence-enforced | permissive-tag boundary |
| EC-21a | AC-21 | ordinary `<summary>` enters semantic sample | ordinary review population |
| EC-21b | AC-21 | positional-property `<param>` enters semantic sample | alternate carrier population |
| EC-21c | AC-21 | inheritdoc-only and optional normal param prose excluded | exclusion path |
| EC-24a | AC-24 | packed metadata + installed `--version` are 0.5.0 | artifact identity |
| EC-25a | AC-25 | installed bootstrap/update/profile check lifecycle | consumer profile surface |
| EC-25b | AC-25 | installed documentation-rule behavior | consumer analysis surface |
| EC-25c | AC-25 | installed M0004 normalize/check regression | retained consumer workflow |

## Validation

| ID | Depth | Target | Locus/platform | Command/check | Proves | Expected evidence |
|---|---|---|---|---|---|---|
| VAL-01 | Tier 1 | Core rule-set/profile/remediation/documentation logic | local Windows 11 + .NET 11 | focused TUnit suite | AC-02, AC-05..AC-22 and mapped ECs | focused pass + scenario evidence |
| VAL-02 | Tier 1 | built CLI process | local Windows 11 + .NET 11 | CLI process tests | AC-01, AC-03, AC-04, AC-11, AC-22 and mapped ECs | stdout/stderr/exit/files evidence |
| VAL-03 | Tier 3 | isolated SDK-style Git fixture repositories | local Windows 11 + .NET 11 + Git | integration fixture suite | AC-01, AC-03..AC-12, AC-13..AC-23 and mapped ECs | real effective MSBuild/EditorConfig/source behavior |
| VAL-04 | Tier 4 | exact locally packed/installed `DotNetAiCodeHygiene.Tool` 0.5.0 | isolated Windows tool path + consumer repo | local package install + installed CLI scenario | AC-24, AC-25 and EC-24a/EC-25a..c | package/install/process/source evidence |
| VAL-05 | Tier 2 | complete repository | local Windows 11 | `./eng/validate.ps1` | AC-23, AC-24, AC-26 | complete green repository validation + scope/dependency inspection |
| VAL-06 | Tier 2 | README/public docs vs installed behavior | local repository | documentation inspection + command comparison | DOC-01, DOC-02 | docs match live public contract |
| VAL-07 | Tier 5 | profile/documentation usability | project owner/delegate | human review `REV-M0005-COMPLETION` | REV-01 | explicit acceptance/rejection |

## Human Review

Applicability: required and blocking for milestone completion.

Review class: milestone completion review.

Canonical review ID:

```text
REV-M0005-COMPLETION
```

Review subject:

- generated profile artifacts and ownership boundaries;
- severity policy;
- mandatory profile rule UX;
- StyleCop prohibition workflow;
- bootstrap/update behavior;
- documentation summary/carrier behavior;
- no accidental parameter-documentation completeness requirement;
- installed-tool workflow.

Required evidence: completed automated gates plus representative generated artifacts/findings/record examples.

Reviewer role: project owner/delegate.

Waiver policy: none for M0005.

Completion behavior: implementation terminates `AWAITING HUMAN REVIEW` until accepted.

## Documentation Policy

Implementation directly updates README/public/project authority contradicted by M0005 behavior.

Broad unrelated documentation normalization remains deferred.

## Completion Expectations

Implementation owns execution decomposition, code/tests, evidence collection, validation execution, direct required documentation updates, final reconciliation, and durable completion evidence.

Before `AWAITING HUMAN REVIEW`, implementation must freshly reread this milestone and verify exact milestone/ledger obligation-ID and evidence-case-ID equality plus concrete evidence for every non-review obligation/case.

## Completion Evidence

Automated evidence collected on 2026-10-04, Windows 11, .NET SDK `11.0.100-rc.1.26425.128`:

| Obligation/evidence cases | Concrete evidence | Gate/target | Result |
|---|---|---|---|
| AC-01..AC-04; EC-01a/EC-01b, EC-03a/EC-03b, EC-04a/EC-04b | `ProfileManager.RequireCurrent`, `Bootstrap`, `Update`; `M0005EvidenceTests.EC01MissingAndFutureProfileStatesGiveSafeGuidanceWithoutMutation`, `LifecycleTests.ProfileBootstrapIsIdempotentPreservesUserContentAndReportsMandatoryViolations`, `M0005EvidenceTests.EC04UpdateRepairsProfileOwnedDriftWithoutOverwritingUserContent` | VAL-01..VAL-03; isolated SDK-style Git profile fixtures | Focused missing/future marker, clean/idempotent bootstrap, and drift repair assertions passed. |
| AC-05..AC-10; EC-05a..EC-09c | `ProfileManager.Analyze`; `M0005EvidenceTests.EC05CanonicalEffectiveProfilePassesAfterBootstrap`, `EC05EffectiveImportedMsbuildPropertiesAndWarningPromotionAreReported`, `EC05NestedEditorConfigOverridesAndRootCutoffsAreEffective`, `EC05MissingRootCutoffInheritsParentEditorConfig`, `EC05EffectiveEditorConfigAppliesOnlyToEvaluatedCompileSources`, `EC07BootstrapPreservesPreexistingEditorConfigAndBuildProperties`, `EC09PackageCentralAndPathAnalyzerInputsAreDetectedWithoutDuplicateSourceFindings`, `EC09ResolvedNugetAssetsAndPathBasedAnalyzerInputsAreRecognized` | VAL-01, VAL-03 | Focused effective MSBuild/EditorConfig, projected-versus-loose source, warnings-as-errors, ownership, and StyleCop cases passed in Debug/Release profile evaluation; resolved analyzer evidence uses an object-shaped .NET 11 `project.assets.json` entry. |
| AC-11..AC-12; EC-11a..EC-12a | `Rule.Configurable`, mandatory disable/ignore rejection, `ProfileManager.Commit`; `LifecycleTests.ProfileBootstrapFaultRollsBackTheWholeProfileInstallation` | VAL-01..VAL-03 | Disable/ignore rejection passed; injected cancellation restored the pre-run four-artifact state. |
| AC-13..AC-21; EC-14a..EC-21c | `DocumentationFindings`; `LifecycleTests.DocumentationSummaryCarriersAcceptRecordParameterAndDirectInheritdoc`; `M0005EvidenceTests.EC16AndEC18OptionalDocumentationAndDeclarationElementsArePresenceBased`, `EC18DelegateIndexerAndTypeParameterScopeAreCorrect`, `EC18bInvalidInlineReferencesAreRejectedWhileEnclosingTypeParametersAreInScope`, `EC19PresentReturnsValuesAndExceptionsAreValidatedWithoutCompletenessRules`, `EC20InlineSentenceBoundariesAndPermissiveElementsAreEnforcedPrecisely`, `EC21SemanticSummaryPopulationUsesOnlyLocalSummaryCarriers`, `EC17NestedInheritdocDoesNotExemptMissingDeclarationSummary` | VAL-01, VAL-03 | Focused carrier, consistency, sentence, and semantic-population tests passed; per-case mapping is recorded in the execution ledger. |
| AC-22..AC-23 | Canonical `Rules` order and existing M0002/M0003/M0004 Core/CLI regression suites | VAL-01..VAL-03, VAL-05 | Core 43/43 and CLI 21/21 passed. |
| AC-24..AC-25; EC-24a..EC-25c | `eng/validate.ps1` packs `DotNetAiCodeHygiene.Tool.0.5.0`, hash-checks the local feed copy, installs to an isolated tool path, and invokes installed `hygiene.exe` for version, rewrite, bootstrap, update, rules, documentation, and check scenarios | VAL-04, VAL-05 | Exact installed package reported `0.5.0`; Tier-4 consumer lifecycle passed. |
| AC-26, DOC-01..DOC-02 | Scope/dependency/workflow inspection and README comparison against profile/documentation CLI contracts | VAL-05, VAL-06 | No prohibited integration scope added; public documentation reflects M0005 behavior. |
| REV-01 | Project-owner/delegate completion review `REV-M0005-COMPLETION` | VAL-07 | Awaiting human review; implementation has not self-approved. |

Focused evidence by planner-owned evidence-case ID is recorded individually in `.execution/M0005-supported-dotnet-profile.md`; each `evidence collected` row names the exercised test or installed-tool assertions for that exact case.
