# Execution Ledger — M0006 Rule Policy Rationale & Semantic Review Separation

Primary milestone: `docs/milestones/M0006-rule-policy-rationale.md`

This file is mutable operational execution state, not project authority.

Planning seeded the obligation/evidence registry only. Implementation owns work packages, concrete evidence, statuses, and resume state.

## Milestone Obligation Registry

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | Fixed rule semantics and enable/disable-only optional participation are explicit. | WP-01; WP-02 | `docs/SPECS.md`, `docs/specs/HYGIENE.md` define fixed parameter-free rules and whole-rule optional toggle semantics. | VAL-05 passed | complete |
| AC-02 | acceptance | Independently applicable policies use separate rule IDs rather than parameters/bundles. | WP-01 | `docs/SPECS.md`, `docs/specs/HYGIENE.md` require separate IDs for independently applicable policy. | VAL-05 passed | complete |
| AC-03 | acceptance | docs.summary.quality.review v3 is language-neutral with correctness/value/clarity only. | WP-02; WP-03 | `HygieneEngine.Rules`/`QualityQuestions`, `docs/specs/SEMANTIC-REVIEWS.md`; Core split test asserts three non-language questions. | VAL-01/02/04 passed | complete |
| AC-04 | acceptance | docs.summary.language.german.review v1 is a separate fixed German-language semantic rule. | WP-02; WP-03 | `HygieneEngine.Rules`/`GermanQuestions`, separate fixed German contract in semantic spec and installed rule output. | VAL-01/02/04 passed | complete |
| AC-05 | acceptance | Both semantic rules preserve independent deterministic bounded sampling/expansion/handoff. | WP-03 | Rule-specific ranking/fingerprint, canonical independent `B-1`/`B-2`, expand/handoff verified by Core split test and CLI installed validation. | VAL-01/02/04 passed | complete |
| AC-06 | acceptance | Canonical order/rules/help expose both rules correctly. | WP-02; WP-04 | Catalog versions/purposes and canonical spec order updated; order regression and CLI rule-list checks pass. | VAL-01/02/04 passed | complete |
| AC-07 | acceptance | This repository disables only the German-language rule while retaining generic quality review. | WP-05 | `.hygiene/config.json` disables only German; `docs/ENGINEERING.md` records reason. Repository check produced quality batch only (2/2). | VAL-02/04 passed | complete |
| AC-08 | acceptance | Summary-required rationale makes deliberate public/internal coverage and generic-only scope explicit. | WP-04 | Rationale beside summary rule in `docs/specs/DOCUMENTATION.md`. | VAL-05 passed | complete |
| AC-09 | acceptance | XML-consistency and sentence-rule rationales are explicit. | WP-04 | Both rationales beside respective contracts in `docs/specs/DOCUMENTATION.md`. | VAL-05 passed | complete |
| AC-10 | acceptance | Canonical READABILITY.md preserves semantics and rationale for both readability rules. | WP-04 | New `docs/specs/READABILITY.md` specifies preserved thresholds/grouping and rationale; Hygiene authority references it. | VAL-05 passed | complete |
| AC-11 | acceptance | Mandatory profile rules retain semantics and gain explicit rationale. | WP-04 | Both rules receive explicit rationale in `docs/specs/PROFILE.md`; implementation unchanged. | VAL-04/05 passed | complete |
| AC-12 | acceptance | Product-direction research records fixed opinion / optional applicability rationale. | WP-04 | Directional principle expanded in `docs/research/PRODUCT-DIRECTION.md`. | VAL-05 passed | complete |
| AC-13 | acceptance | Disabling German suppresses only the German review batch. | WP-03 | Core split test disables German and confirms quality fingerprint and selected items remain identical; installed CLI test confirms German disable leaves quality active. | VAL-01/02/04 passed | complete |
| AC-14 | acceptance | Both enabled rules produce independent batches in a German-policy fixture. | WP-03 | Isolated SDK/Git fixture emits both versions/batches and includes English and natural German summary subjects. | VAL-01/03/04 passed | complete |
| AC-15 | acceptance | Multiple semantic batches remain independently expandable/handoff-capable. | WP-03 | Core test expands B-2 and writes German handoff; installed CLI validates both rule batches and German batch rubric. | VAL-01/02/04 passed | complete |
| AC-16 | acceptance | Prior deterministic/profile/rewrite/review behavior remains compatible except explicit changes. | WP-03; WP-05 | Existing regression coverage retained; Core 45/45 and CLI 21/21 pass, including rewrite/profile/ignore/target regressions. | VAL-01/02/04 passed | complete |
| DOC-01 | documentation | Public/agent guidance describes generic quality and German-language policy separately. | WP-04 | README explains fixed separate German rule and repository disable policy; semantic/spec guidance separated. | VAL-05 passed | complete |
| DOC-02 | documentation | Every current rule has authoritative rationale or direct rationale reference. | WP-04 | Rationales added for documentation, profile, semantic, and readability rules in canonical specifications. | VAL-05 passed | complete |
| REV-01 | review | Human completion review confirms intended rule model and rationale fidelity. | WP-01..WP-05 evidence complete | Project owner approved the completion review in this task conversation on 2026-10-06, confirming the fixed-rule split, M0006 rationale fidelity, and repository German-disable policy. | VAL-06 passed | complete |

## Implementation Work Packages

| ID | Work package | Scope/evidence locus | Status |
|---|---|---|---|
| WP-01 | Fixed rule policy authority | `docs/SPECS.md`, `docs/specs/HYGIENE.md`, `docs/TERMINOLOGY.md`, product-direction research | complete |
| WP-02 | Rule catalog and semantic split | Core rule catalog/questions, canonical order, semantic and documentation specifications | complete |
| WP-03 | Independent batches and regression evidence | Rule-specific sampling, expansion/handoff, Core/CLI tests, isolated SDK/Git fixture | complete |
| WP-04 | Rule rationale and documentation sync | Profile/documentation/readability authorities, README, engineering/self-hosting guidance | complete |
| WP-05 | Repository configuration and validation | `.hygiene/config.json`, installed-tool gate, complete repository validation, live self-check | complete |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-03a | AC-03 | English technically correct/value-adding/clear summary passes generic quality without language judgment. | VAL-01, VAL-03 | Core fixture asserts English sample carries only Q1-Q3 quality rubric; sample 2/2. | complete |
| EC-04a | AC-04 | English summary fails enabled German-language review. | VAL-01, VAL-03 | German fixed rubric explicitly says English is eligible and fails; English subject is present in enabled German fixture batch. | complete |
| EC-04b | AC-04 | Natural German summary passes German-language review. | VAL-01, VAL-03 | Natural German fixture subject included in batch; passing criterion is the fixed published question, retained for required human review. | complete |
| EC-05a | AC-05 | Same eligible population independently feeds both enabled semantic rules deterministically. | VAL-01, VAL-03 | Core fixture reports both batches over the same summary subjects; implementation uses rule/version-specific fingerprint/ranking and the repeatability protocol passes. | complete |
| EC-07a | AC-07 | Repository configuration emits no German-language batch. | VAL-02, VAL-04 | Live repository CLI check emits only quality batch with German disabled in committed config. | complete |
| EC-07b | AC-07 | Repository configuration retains generic quality review. | VAL-02, VAL-04 | Live repository CLI check quality batch sample 2/2. | complete |
| EC-13a | AC-13 | Disabling German leaves quality population/ranking/rubric unchanged. | VAL-01, VAL-03 | Core split test compares quality fingerprint/items before and after German disable. | complete |
| EC-14a | AC-14 | Both rules enabled produce separately identifiable batches. | VAL-01, VAL-02, VAL-03 | Core and installed consumer runs report canonical rule-specific B-1 and B-2 IDs and versions. | complete |
| EC-15a | AC-15 | Review expand selects intended batch when multiple batches exist. | VAL-02, VAL-03 | Core split test expands B-2 and confirms German rule ID; installed validation covers both batches. | complete |
| EC-15b | AC-15 | Handoff persists intended rule ID/version/rubric for each rule. | VAL-02, VAL-03 | Core split test handoff JSON checked for B-2 rule; legacy quality handoff test asserts ID/v3/Q1-Q3; complete suites pass. | complete |

## Validation Registry

| ID | Depth | Target | Evidence | Status |
|---|---|---|---|---|
| VAL-01 | Tier 1 | Core rule catalog, sampling, questions, configuration | `dotnet test tests/DotNetAiCodeHygiene.Core.Tests/DotNetAiCodeHygiene.Core.Tests.csproj --no-build`: 45/45 passed. | passed |
| VAL-02 | Tier 1 | Built CLI rules/check/review expand/handoff | `dotnet test tests/DotNetAiCodeHygiene.Cli.Tests/DotNetAiCodeHygiene.Cli.Tests.csproj --no-build`: 21/21 passed; installed checks included in VAL-04. | passed |
| VAL-03 | Tier 3 | Isolated English/German SDK-style Git fixtures | Isolated net11.0 SDK/Git split fixture plus full Core fixture suite: 45/45 passed. | passed |
| VAL-04 | Tier 2 | Complete repository via `./eng/validate.ps1` | Restore/build passed; Release tests 66/66 passed; exact 0.5.0 package packed/installed and script's installed consumer checks completed. Live repository CLI check also passed: one quality batch, sample 2/2. | passed |
| VAL-05 | documentation | Authority/spec/research/public consistency | Reviewed `SPECS`, HYGIENE, DOCUMENTATION, SEMANTIC-REVIEWS, READABILITY, PROFILE, PRODUCT-DIRECTION, README, ENGINEERING, TERMINOLOGY, and self-host config; `git diff --check` clean. | passed |
| VAL-06 | human | Completion review | Project owner approved M0006 completion review in this task conversation on 2026-10-06; REV-01 criteria reviewed and accepted. | passed |

## Resume State

Implementation, automated validation, and the required project-owner completion review are complete. The approved M0006 state is the behavioral baseline for M0007.
