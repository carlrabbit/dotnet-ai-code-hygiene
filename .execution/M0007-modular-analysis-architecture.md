# Execution Ledger — M0007 Modular Analysis Architecture

Primary milestone: `docs/milestones/M0007-modular-analysis-architecture.md`

This file is operational implementation state. It is not project authority and does not amend the ready milestone.

Planning seeds the lossless obligation registry, evidence cases, and validation gates below. Implementation owns work-package decomposition, concrete mechanics, evidence, status progression, and resume state.

The execution ledger may compress work. It must not compress obligations.

## Milestone Obligation Registry

Planner-owned columns are `ID`, `Type`, and `Obligation`. Implementation must not delete, merge, renumber, paraphrase, or replace planner-seeded obligation rows.

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | Check and source-rewrite execution use one shared command-scoped repository/session abstraction for repository root, target/change resolution, SDK project discovery, Roslyn workspace lifetime, and project/document assignment rather than maintaining separate implementations of those concerns. | WP-01; WP-04 | RepositorySession owns root/target/change resolution, SDK project loading, workspace, document assignment, and compilation access; Check and Rewrite use it. | VAL-01/02/03 | satisfied |
| AC-02 | acceptance | Expensive session context is lazy and reused within one command invocation: supporting compilation/semantic/fact access does not eagerly construct all such context, and repeated consumers do not independently rebuild the same session-owned project/fact context. | WP-01; WP-02 | Production LongLineReviewRuleModule runs through RuleCatalog.Runner and requests only RuleContext.Text; ProductionLightweightRuleUsesLazyProductionContextWithoutLoadingRoslyn proves workspace/project/compilation are not created and only text is materialized. RegisteredProductionRulesShareLazyDocumentationFactAndCompilation proves distinct production consumers reuse one compilation and one DocumentationSubjectFact computation. | VAL-01 | satisfied |
| AC-03 | acceptance | The architecture represents reporting scope separately from readable analysis context, preserving full-repository, explicit-target, and changed-file semantics while allowing rules to read broader project/repository context required for correct evaluation. | WP-01; WP-02 | Reporting targets are assigned to project documents separately; existing changed and explicit target fixture tests retained. | VAL-03 | satisfied |
| AC-04 | acceptance | Production rules are registered in one explicit static catalog that preserves canonical M0006 order, rule identity/version/output/configurability metadata, and mandatory/configurable participation without reflection or runtime plugin discovery. | WP-03 | RuleCatalog.Modules explicitly registers each production IRuleModule in canonical order; descriptor metadata remains in RuleCatalog.All. HygieneEngine.Check invokes selected modules through RuleCatalog.Runner. | VAL-01/02 | satisfied |
| AC-05 | acceptance | Every current accepted rule has a dedicated evaluator/module, and the central rule runner/engine contains no rule-specific Roslyn traversal or semantic decision branch for those rules. | WP-03 | XML and sentence modules call separate evaluators; summary required, long-line and visual-block have dedicated modules; each profile diagnosis and each quality/German review has independent module logic. HygieneEngine.Check only selects/runs modules and performs generic host result handling. | VAL-01/02 | satisfied |
| AC-06 | acceptance | Rule modules own their domain-specific traversal/querying and request session services/facts directly; M0007 introduces no central syntax/symbol callback pipeline, rule dependency DAG, or declarative required-context DSL. | WP-03 | Production modules receive RuleContext backed by RepositorySession and request text/tree/root/compilation/semantic model or shared facts as needed. The light-rule test proves an actual production module leaves Roslyn resources unloaded. | VAL-01 | satisfied |
| AC-07 | acceptance | A lazy immutable session-fact mechanism exists, and the documentation subject/carrier model is represented once and consumed coherently by applicable documentation finding/review rules, preserving ordinary subjects, positional-record property carriers, direct inheritdoc behavior, and local review-prose eligibility. | WP-02 | DocumentationSubjectFact.Create is the shared immutable summary-carrier producer; deterministic summary and semantic population consume it. | VAL-01/03 | satisfied |
| AC-08 | acceptance | Session facts are computed at most once per applicable session key/scope, shared read-only by consumers, and discarded with the command session; M0007 adds no durable analysis cache/index. | WP-02 | Lazy command-local workspace, project, compilation, and fact caches; fact and compilation compute-once assertions in Core tests. | VAL-01 | satisfied |
| AC-09 | acceptance | Run/finding/batch identity assignment, canonical ordering, ignore application/stale matching, latest-run persistence, and review persistence/handoff integration remain host-owned and behavior-compatible; rule modules cannot bypass those product mechanics. | WP-03 | HygieneEngine retains canonical finding order, handle assignment, ignores/stale matching, persistence, and review handoff mechanics. | VAL-02/03 | satisfied |
| AC-10 | acceptance | Shared deterministic semantic-review infrastructure supports the independently enabled M0006 summary-quality and German-language rules without merging their IDs, versions, rubrics, deterministic rankings/population fingerprints, batch handles, or escalation/handoff semantics. | WP-03 | Quality/German modules retain separate descriptor, questions, ranking identity and batch number; shared deterministic batch builder. | VAL-02/03 | satisfied |
| AC-11 | acceptance | Mandatory profile rule diagnosis is modularized consistently with the rule architecture while fixed deterministic remediation remains a separate optional capability invoked only by bootstrap/update policy; normal check remains non-mutating. | WP-03 | Profile diagnosis modules are separate from ProfileManager bootstrap/update remediation; Check only analyzes. | VAL-02/03 | satisfied |
| AC-12 | acceptance | `format` and `normalize` are represented as distinct explicit rewrite modules behind a rewrite runner/catalog and are not implemented as rule subtypes or as branches of a central transformation switch. | WP-04 | RewriteCatalog registers separate FormatRewriteModule and NormalizeRewriteModule; normalization validation is implemented by that module’s validation hooks. RewriteEngine contains no format/normalize recognition. | VAL-01/02 | satisfied |
| AC-13 | acceptance | Rewrite execution reuses the shared repository/session substrate and preserves the complete-plan, validation, check-only, conflict-detection, atomic replacement, and rollback semantics required by existing rewrite authority. | WP-04 | RewriteEngine reuses RepositorySession; all-plan validation, check-only, conflict detection, atomic replacement, rollback retained. | VAL-02/03 | satisfied |
| AC-14 | acceptance | Focused tests prove a test-only rule can execute through the rule runner and a test-only rewrite can execute through the rewrite runner without modifying central production orchestration, dynamic discovery, or repository-loading code. | WP-02; WP-04 | TestOnlyRuleAndRewriteModulesRunThroughTheirRunners injects test-only modules into the same RuleModuleRunner and RewriteRunner used by production; no central orchestration or repository loader edits are involved. | VAL-01 | satisfied |
| AC-15 | acceptance | All accepted M0006 externally observable behavior remains regression-compatible, including rule IDs/versions/order, fixed-rule enable/disable policy, repository German-rule disablement, findings/JSON, explain/ignore/stale behavior, review batching/expand/handoff, profile bootstrap/update, target resolution, and format/normalize semantics. | WP-01..WP-05 | Current canonical validation passes Core 51/51 and CLI 22/22. Core asserts exact legacy positional-record ReviewItem values; CLI asserts those fields across check/expand/handoff. M0006 self-host keeps generic quality active and German review disabled. | VAL-02/03/05 | satisfied |
| AC-16 | acceptance | Tier-4 validation installs and invokes the exact current locally packed tool from the M0007 build and proves representative check/rules/review plus bootstrap/update and format/normalize consumer workflows, without relying on stale/global installation state. | WP-05 | Tier-4 canonical script hash-checks and installs exact packed package, then invokes installed version/rules/check/review/profile/rewrite commands. | VAL-04 | satisfied |
| AC-17 | acceptance | M0007 adds no public plugin API, new runtime framework/dependency solely for orchestration, DI framework, reflection discovery, separate rule process/protocol, uncontrolled parallelism, persistent analysis cache/index, future statistical sampling subsystem, new rule, or new rewrite transformation. | WP-01..WP-05 | Project/dependency/source inspection shows no new runtime framework, reflection/plugin discovery, process boundary, scheduler, cache/index, sampler, rule, or rewrite. | VAL-05 | satisfied |
| AC-18 | acceptance | The post-refactor coordinator/engine is structurally limited to orchestration and shared product mechanics: current rule-specific analysis logic and duplicated rewrite repository-loading logic are absent from the central coordinator surfaces. | WP-03; WP-04 | Source inspection confirms HygieneEngine.Check contains no summary-specific branch/subject construction and no rule semantic decisions; it runs selected modules, materializes results and owns host persistence/review handling. RewriteEngine has no command-specific format/normalize branch and uses shared RepositorySession. | VAL-01/05 | satisfied |
| DOC-01 | documentation | Current architecture, engineering, terminology, milestone, and rule-application research documents describe the modular-rule/shared-session architecture consistently and distinguish authoritative architecture from research rationale. | WP-05 | Overlay reconciles ARCHITECTURE, ENGINEERING, TERMINOLOGY, MILESTONES, AGENTS and rule-application research. | VAL-06 | satisfied |
| REV-01 | review | Human completion review confirms the result is BORING and locally comprehensible: a new ordinary rule/rewrite has a small locality boundary, shared infrastructure is not over-generalized, central orchestration no longer owns rule semantics, public behavior is preserved, and no speculative plugin/pipeline/sampling framework was introduced. | WP-05 | Project owner approved M0007 in this task conversation on 2026-10-07 after PR 10 merged to main at `aa690728d555e5781f455d831a94a1c1ab03857a`; approval recorded in the milestone Completion Evidence. | VAL-07 | satisfied |

## Evidence Case Registry

Planner-owned columns are `ID`, `Parent obligation`, and `Required evidence case`. Implementation must not delete, merge, renumber, paraphrase, or replace planner-seeded evidence cases.

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-02a | AC-02 | a lightweight/test-only rule does not trigger compilation/semantic/fact work it never requests | VAL-01 | ProductionLightweightRuleUsesLazyProductionContextWithoutLoadingRoslyn runs the production long-line module via RuleCatalog.Runner; short input requests only text, leaves workspace/projects/compilation absent, and records one text fact. | satisfied |
| EC-02b | AC-02 | two consumers of the same session-owned expensive/fact context reuse one construction | VAL-01 | RegisteredProductionRulesShareLazyDocumentationFactAndCompilation runs summary, quality and German production modules; compilation is constructed once and DocumentationSubjectFact is computed once for the document despite three consumers. | satisfied |
| EC-03a | AC-03 | `--changed` reports only changed targets while a rule can read required unchanged project context | VAL-03 | Lifecycle changed-scope fixture proves only changed reporting targets emit results while projects are loaded for broader context. | satisfied |
| EC-03b | AC-03 | explicit file/directory targeting preserves existing reporting containment/de-duplication behavior | VAL-03 | Lifecycle explicit file/directory fixture tests containment, path rejection, and target de-duplication. | satisfied |
| EC-07a | AC-07 | shared documentation subjects preserve ordinary summary and direct-inheritdoc semantics | VAL-01, VAL-03 | DocumentationSummaryCarriersAcceptRecordParameterAndDirectInheritdoc verifies ordinary summary and direct inheritdoc. | satisfied |
| EC-07b | AC-07 | shared documentation subjects preserve positional record/record-struct property `<param>` carrier semantics | VAL-01, VAL-03 | Same carrier fixture covers record and record-struct positional property <param> carriers. | satisfied |
| EC-10a | AC-10 | quality and German semantic-review rules enabled together produce independent deterministic batches/rubrics and remain independently expandable/hand-offable | VAL-03 | SummaryQualityAndGermanReviewsAreIndependentFixedBatches plus CLI expand/handoff coverage. | satisfied |
| EC-11a | AC-11 | check diagnoses profile state without mutation while bootstrap/update retain authorized remediation + re-evaluation | VAL-02, VAL-03 | Profile lifecycle coverage proves Check diagnosis and bootstrap/update remediation/re-evaluation; profile transaction rollback remains covered. | satisfied |
| EC-13a | AC-13 | format/normalize check-only and successful commit behavior remain compatible | VAL-02, VAL-03 | RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent covers check-only and commits. | satisfied |
| EC-13b | AC-13 | injected conflict/fault during multi-file rewrite leaves pre-run selected-target state | VAL-02, VAL-03 | NormalizeRejectsCompilerErrorsAndRewriteConflictNeverOverwritesSource and multi-file rollback fixture. | satisfied |
| EC-14a | AC-14 | test-only rule executes through runner without production catalog/engine traversal change | VAL-01 | TestOnlyRuleAndRewriteModulesRunThroughTheirRunners executes a test-only IRuleModule through RuleModuleRunner, the same runner/context contract used by production modules. | satisfied |
| EC-14b | AC-14 | test-only rewrite executes through runner without production command switch/repository-loader change | VAL-01 | TestOnlyRuleAndRewriteModulesRunThroughTheirRunners executes a test-only IRewriteModule through RewriteRunner; the engine does not branch on its command identity. | satisfied |
| EC-15a | AC-15 | deterministic finding/order/ignore/explain behavior remains compatible | VAL-02, VAL-03 | Core/CLI regression cases for order, JSON, explain, ignore/stale and targeting pass. | satisfied |
| EC-15b | AC-15 | semantic review sample/expand/handoff behavior remains compatible | VAL-02, VAL-03 | M0006 review tests preserve batch fingerprints, independent expansion, and handoff rubric. | satisfied |
| EC-15c | AC-15 | repository self-host check retains M0006 German-disable and generic-quality behavior | VAL-05 | Installed/repository self-host check reports generic quality only, with German disabled in committed hygiene config. | satisfied |
| EC-16a | AC-16 | exact packed artifact is installed in isolated consumer and exercises representative lifecycle + rewrite commands | VAL-04 | eng/validate.ps1 uses exact packed artifact SHA-256 parity and isolated tool install for lifecycle/review/rewrite commands. | passed |

## Work Packages

Implementation decomposition (derived after live M0006 inspection):

| ID | Work package | Scope/evidence locus | Status |
|---|---|---|---|
| WP-01 | RepositorySession substrate | Extract shared root/target/change/project/workspace/document loading and distinguish reporting scope from analysis context; check/rewrite consumers | complete |
| WP-02 | Lazy session facts and documentation subjects | Add session-scoped lazy fact store; unify documentation subject/carrier representation and tests for ordinary, positional-record, and inheritdoc paths | complete |
| WP-03 | Independent rule modules and host result mechanics | Explicit static ordered catalog, per-rule evaluators, rule runner/context, host materialization/persistence, and shared semantic-review mechanics | complete |
| WP-04 | Rewrite modules and transaction integration | Explicit format/normalize modules and runner over RepositorySession; retain complete-plan and transactional guarantees | complete |
| WP-05 | Architecture tests, authority sync, validation, audit | Test module locality/laziness/reuse; reconcile docs; execute VAL-01..VAL-07 and durable completion audit | complete |
## Validation Gates

Planning seeds required validation gates before `ready`. Implementation records execution evidence and status.

| ID | Required validation | Target/locus | Proves evidence units | Status | Evidence |
|---|---|---|---|---|---|
| VAL-01 | Focused TUnit tests for RepositorySession, rule runner/catalog, fact store, result materializer, and rewrite runner/catalog | Tier 1 / local Windows 11 + .NET 11 | AC-01..AC-14, AC-18; EC-02a, EC-02b, EC-07a, EC-07b, EC-14a, EC-14b | passed | `eng/validate.ps1` Release build passed; Core 51/51 and CLI 22/22 passed. Focused tests exercise production lazy module path, shared fact/compilation construction, test-only module seams, profile ownership and rewrite validation hooks. |
| VAL-02 | Existing/focused Core and built CLI process tests | Tier 1 / local Windows 11 + .NET 11 | AC-09..AC-15; EC-11a, EC-13a, EC-13b, EC-15a, EC-15b | passed | `eng/validate.ps1`: Core 51/51 and CLI 22/22 passed, including exact positional-record review fields in Core and check/expand/handoff CLI tests. |
| VAL-03 | Full/explicit/changed check plus profile, semantic-review, and rewrite scenarios in isolated SDK-style Git repositories | Tier 3 / local Windows 11 + .NET 11 + Git | AC-03, AC-07, AC-10..AC-15; EC-03a, EC-03b, EC-07a, EC-07b, EC-10a, EC-11a, EC-13a, EC-13b, EC-15a, EC-15b | passed | `eng/validate.ps1` passed Core/CLI isolated Git and SDK/MSBuild fixtures for changed/explicit scope, independent reviews, profile lifecycle and transactional rewrite behavior. |
| VAL-04 | Pack current tool, install exact produced artifact into isolated consumer, and invoke representative check/rules/review/bootstrap/update/format/normalize workflows | Tier 4 / isolated Windows consumer repository | AC-15, AC-16; EC-16a | passed | Canonical `eng/validate.ps1` exact-package isolated-consumer tier passed; script emitted `Tier-4 exact installed consumer validation passed`. |
| VAL-05 | `./eng/validate.ps1` including repository self-hosting checks | Tier 2 / local Windows 11 | AC-15..AC-18; EC-15c | passed | Canonical `eng/validate.ps1` exited 0 and emitted `repository M0006 self-host behavior passed`; generic-quality batch active and German rule disabled. |
| VAL-06 | Authority consistency review across M0007 architecture/engineering/terminology/milestone/research documents | Documentation / repository-local | DOC-01 | passed | Reviewed ARCHITECTURE, ENGINEERING, TERMINOLOGY, MILESTONES, AGENTS, and research rationale; architecture is authoritative and research is explicitly rationale; `git diff --check` passed. |
| VAL-07 | `REV-M0007-COMPLETION` architecture review | Human / project owner or delegate | REV-01 | passed | Project owner explicitly approved the M0007 milestone in this task conversation on 2026-10-07, against the merged main commit `aa690728d555e5781f455d831a94a1c1ab03857a`. |

## Resume Point

Last completed work package:

WP-01 through WP-05 implementation, required automated validation, authority consistency review, completion audit, and project-owner review are complete.

Current work package:

No remaining work. M0007 is complete.

Next concrete action:

Project owner approved `REV-M0007-COMPLETION` on 2026-10-07; approval is recorded in the primary milestone against merged main commit `aa690728d555e5781f455d831a94a1c1ab03857a`.

Prerequisite record: project owner approved M0006 REV-01 in this task conversation on 2026-10-06; M0006 REV-01/VAL-06 are now recorded complete/passed in its milestone and execution ledger.

Known agent-resolvable gaps:

None. Automated gates and required human review passed.

External blockers or planning escalations:

None. M0007 is complete.
