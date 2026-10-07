# Milestone — M0007 Modular Analysis Architecture

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready after M0006 completion |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Baseline executor readiness | confirmed |
| Decision preservation | confirmed |
| Execution tractability | confirmed |
| Execution ledger | `.execution/M0007-modular-analysis-architecture.md` planning-seeded |
| Scope size | large coherent internal-architecture refactor |
| Implementation autonomy | high within resolved contracts |
| Documentation sync | direct architecture/engineering authority; public docs only if unexpectedly contradicted |
| Focused validation | Tier 1 Core architecture + behavior tests and built CLI process |
| Repository validation | Tier 2 `./eng/validate.ps1` |
| Integration validation | Tier 3 isolated SDK-style Git repositories |
| Validation locus/platform | local Windows 11 + .NET 11 SDK + Git + PowerShell |
| Consumer/release validation | Tier 4 exact current locally packed/installed tool |
| Human review | required |

## Execution prerequisite

M0006 must be complete and accepted before M0007 implementation begins.

The accepted M0006 state is the behavioral baseline, including the fixed-rule/toggle-only policy, rule IDs/versions/order, separate quality/German semantic-review rules, repository German-rule disablement, and rule rationale authority.

If M0006 completion materially changes those assumptions, implementation must reconcile against the accepted M0006 authority before production edits. M0007 must not silently revert or recombine M0006 behavior.

## Goal

Replace the current monolithic rule/rewrite execution shape with a small, BORING architecture in which each rule or rewrite is an independent in-process module over a shared lazy repository-analysis session.

The milestone exists to make additional 1.0 rules and automatic formatters cheap to add without creating either:

- one ever-growing central traversal/switch; or
- an elaborate general-purpose rule framework.

## Target State

The internal architecture has this conceptual shape:

```text
CLI command
-> command policy
-> RepositorySession
   -> target/Git/project/workspace/document context
   -> lazy Roslyn context
   -> lazy immutable session facts

check/bootstrap/update
-> explicit RuleCatalog
-> independent RuleModules
-> host ResultMaterializer / persistence

format/normalize
-> explicit RewriteCatalog
-> independent RewriteModules
-> RewriteTransaction
```

Rules and rewrites share expensive repository infrastructure, not a common semantic base abstraction.

Adding an ordinary future rule should normally require a rule module, explicit catalog registration, tests, and rule authority—not edits to a central Roslyn traversal. Adding a future rewrite should normally require a rewrite module, explicit registration, tests, and transformation authority—not duplicate project loading or edits to a central command switch.

## Scope

- extract one command-scoped shared repository/session substrate used by check and rewrite execution;
- centralize repository root/target resolution, SDK project discovery, Roslyn workspace lifetime, project/document assignment, and lazy expensive context behind that substrate;
- establish explicit static production rule registration with canonical order/metadata;
- move each current rule's diagnosis/evaluation into a dedicated module/evaluator;
- retain fixed optional remediation as a separate rule capability for rules that already own it;
- establish a small rule runner/context/output boundary without a callback/event framework;
- make reporting scope versus broader readable analysis context explicit;
- establish lazy immutable session facts with one concrete shared documentation subject/carrier fact;
- centralize host-owned finding/review materialization, ordering, ignore integration, and persistence mechanics;
- extract shared deterministic semantic-review batch mechanics where the M0006 quality/German rules use the same protocol;
- establish explicit rewrite modules/catalog/runner for current `format` and `normalize` behavior;
- reuse the existing all-or-nothing rewrite transaction through the new rewrite architecture;
- remove duplicated repository/project/workspace machinery between current hygiene and rewrite paths;
- add internal test seams proving module independence and lazy/shared session behavior;
- preserve all accepted M0006 public behavior.

## Non-goals

- no new hygiene rule;
- no new formatter/normalizer transformation;
- no public third-party rule/rewrite/plugin API;
- no separate executable/process per rule;
- no reflection-based rule discovery;
- no dependency injection framework requirement;
- no rule dependency graph;
- no central syntax/symbol callback event bus;
- no declarative rule required-context/capability DSL;
- no parallel execution requirement or scheduler optimization;
- no durable compiler/repository analysis cache;
- no universal repository index;
- no subject-state statistical sampler;
- no aggregate/cohort statistical sampler;
- no new sampling persistence format;
- no new rule parameters or changes to M0006 enable/disable-only configuration;
- no model/provider invocation;
- no cross-platform expansion, IDE/MCP integration, or GitHub workflow.

## Decisions and Constraints

- **Module independence, shared process.** Rules/rewrite implementations are independent logical modules but execute in-process. Process isolation and IPC are explicitly rejected for M0007.
- **One expensive substrate.** Repository/target/project/workspace/document loading is host/session infrastructure shared by rules and rewrites for the command invocation.
- **Lazy cost.** Compilations, semantic models, derived facts, and other expensive context are created only when requested and reused within the session where practical. Capability support does not justify eager work.
- **Rules own traversal.** A rule may independently enumerate documents, syntax nodes, symbols, or shared facts. M0007 does not centralize all syntax traversal into an event-dispatch pipeline.
- **No context declaration mini-language.** Rule implementations request the services/facts they actually use. Required-context metadata is not introduced merely to mirror implementation dependencies.
- **Static explicit catalog.** Production rule/rewrite registration is explicit and code-owned. Deterministic catalog order remains visible; no runtime assembly scanning or user-supplied modules are introduced.
- **Host-owned participation.** Mandatory/enabled rule selection occurs before rule execution. Rule modules do not interpret repository configuration into language/threshold/scope/severity/sampling parameters.
- **Reporting is not reading.** Selected reporting scope remains distinct from broader context a rule may read to evaluate correctly.
- **Facts, not stages.** Shared derived data is extracted as lazy immutable session facts only where multiple consumers need the same semantically significant or materially expensive derivation.
- **Documentation model is first shared fact.** Positional-record property subjects, summary carriers, direct inheritdoc handling, and local review prose must have one coherent producer/representation used by documentation consumers.
- **Session cache is transient.** Session facts/context are command-local performance/consistency state and are not persisted. Existing product persistence remains explicit and separate.
- **Result mechanics are host-owned.** Rules do not assign run/finding handles or bypass canonical ordering/ignore/persistence behavior. Existing fingerprint/discriminator/stale-ignore semantics must remain stable.
- **Semantic-review mechanics can be shared; policy cannot.** Generic population fingerprint/ranking/sample/batch/expansion/handoff machinery may be shared, while each rule keeps its own fixed population semantics, questions/rubric, version, and escalation contract.
- **Diagnosis and remediation remain separate capabilities.** Profile rule diagnosis becomes modular without making ordinary check execution mutate. Existing fixed remediation is invoked only by authorized command policy.
- **Rules and rewrites stay different abstractions.** They may share `RepositorySession`; a rewrite produces a proposed edit plan and participates in transaction validation, while a rule produces findings/review work.
- **Transactional rewrite semantics are invariant.** No architecture simplification may commit a selected source before complete planning/required validation succeeds.
- **Optimize only measured/shared work.** Repeated traversal of an already-loaded syntax tree is acceptable until evidence shows otherwise.
- **No behavior-version churn for refactoring alone.** Existing rule IDs/versions do not advance merely because their implementation moves into modules. Package/version policy remains whatever is established by the accepted prerequisite state unless another authority requires a change.

## Baseline Executor Readiness

Architecture direction, boundaries, negative requirements, compatibility expectations, validation topology, and human-review policy are settled.

Implementation may choose concrete type names, file layout, interfaces, records, constructors, internal visibility, fact-cache keying, and refactoring sequence. It may create small supporting abstractions when needed to satisfy the contract.

Implementation may not replace the selected architecture with a general plugin system, a callback-driven rule pipeline, a rule dependency graph, separate rule processes, or an eager universal repository model.

No external research reconstruction is required. `docs/ARCHITECTURE.md` is authoritative; `docs/research/RULE-APPLICATION-ARCHITECTURE.md` preserves rationale only.

## Required Authority

- `docs/ARCHITECTURE.md`
- `docs/SPECS.md`
- `docs/specs/HYGIENE.md`
- `docs/specs/PROFILE.md`
- `docs/specs/DOCUMENTATION.md`
- `docs/specs/SEMANTIC-REVIEWS.md`
- `docs/specs/READABILITY.md`
- `docs/specs/REWRITES.md`
- `docs/specs/REVIEW-HANDOFFS.md`
- `docs/ENGINEERING.md`
- `docs/TERMINOLOGY.md`
- accepted `docs/milestones/M0006-rule-policy-rationale.md`

## Acceptance Criteria

- **AC-01** — Check and source-rewrite execution use one shared command-scoped repository/session abstraction for repository root, target/change resolution, SDK project discovery, Roslyn workspace lifetime, and project/document assignment rather than maintaining separate implementations of those concerns.
- **AC-02** — Expensive session context is lazy and reused within one command invocation: supporting compilation/semantic/fact access does not eagerly construct all such context, and repeated consumers do not independently rebuild the same session-owned project/fact context.
- **AC-03** — The architecture represents reporting scope separately from readable analysis context, preserving full-repository, explicit-target, and changed-file semantics while allowing rules to read broader project/repository context required for correct evaluation.
- **AC-04** — Production rules are registered in one explicit static catalog that preserves canonical M0006 order, rule identity/version/output/configurability metadata, and mandatory/configurable participation without reflection or runtime plugin discovery.
- **AC-05** — Every current accepted rule has a dedicated evaluator/module, and the central rule runner/engine contains no rule-specific Roslyn traversal or semantic decision branch for those rules.
- **AC-06** — Rule modules own their domain-specific traversal/querying and request session services/facts directly; M0007 introduces no central syntax/symbol callback pipeline, rule dependency DAG, or declarative required-context DSL.
- **AC-07** — A lazy immutable session-fact mechanism exists, and the documentation subject/carrier model is represented once and consumed coherently by applicable documentation finding/review rules, preserving ordinary subjects, positional-record property carriers, direct inheritdoc behavior, and local review-prose eligibility.
- **AC-08** — Session facts are computed at most once per applicable session key/scope, shared read-only by consumers, and discarded with the command session; M0007 adds no durable analysis cache/index.
- **AC-09** — Run/finding/batch identity assignment, canonical ordering, ignore application/stale matching, latest-run persistence, and review persistence/handoff integration remain host-owned and behavior-compatible; rule modules cannot bypass those product mechanics.
- **AC-10** — Shared deterministic semantic-review infrastructure supports the independently enabled M0006 summary-quality and German-language rules without merging their IDs, versions, rubrics, deterministic rankings/population fingerprints, batch handles, or escalation/handoff semantics.
- **AC-11** — Mandatory profile rule diagnosis is modularized consistently with the rule architecture while fixed deterministic remediation remains a separate optional capability invoked only by bootstrap/update policy; normal check remains non-mutating.
- **AC-12** — `format` and `normalize` are represented as distinct explicit rewrite modules behind a rewrite runner/catalog and are not implemented as rule subtypes or as branches of a central transformation switch.
- **AC-13** — Rewrite execution reuses the shared repository/session substrate and preserves the complete-plan, validation, check-only, conflict-detection, atomic replacement, and rollback semantics required by existing rewrite authority.
- **AC-14** — Focused tests prove a test-only rule can execute through the rule runner and a test-only rewrite can execute through the rewrite runner without modifying central production orchestration, dynamic discovery, or repository-loading code.
- **AC-15** — All accepted M0006 externally observable behavior remains regression-compatible, including rule IDs/versions/order, fixed-rule enable/disable policy, repository German-rule disablement, findings/JSON, explain/ignore/stale behavior, review batching/expand/handoff, profile bootstrap/update, target resolution, and format/normalize semantics.
- **AC-16** — Tier-4 validation installs and invokes the exact current locally packed tool from the M0007 build and proves representative check/rules/review plus bootstrap/update and format/normalize consumer workflows, without relying on stale/global installation state.
- **AC-17** — M0007 adds no public plugin API, new runtime framework/dependency solely for orchestration, DI framework, reflection discovery, separate rule process/protocol, uncontrolled parallelism, persistent analysis cache/index, future statistical sampling subsystem, new rule, or new rewrite transformation.
- **AC-18** — The post-refactor coordinator/engine is structurally limited to orchestration and shared product mechanics: current rule-specific analysis logic and duplicated rewrite repository-loading logic are absent from the central coordinator surfaces.
- **DOC-01** — Current architecture, engineering, terminology, milestone, and rule-application research documents describe the modular-rule/shared-session architecture consistently and distinguish authoritative architecture from research rationale.
- **REV-01** — Human completion review confirms the result is BORING and locally comprehensible: a new ordinary rule/rewrite has a small locality boundary, shared infrastructure is not over-generalized, central orchestration no longer owns rule semantics, public behavior is preserved, and no speculative plugin/pipeline/sampling framework was introduced.

## Acceptance Evidence Topology

| ID | Parent obligation | Required evidence case | Why separate evidence is required |
|---|---|---|---|
| EC-02a | AC-02 | a lightweight/test-only rule does not trigger compilation/semantic/fact work it never requests | proves lazy cost rather than only reuse |
| EC-02b | AC-02 | two consumers of the same session-owned expensive/fact context reuse one construction | proves sharing rather than only laziness |
| EC-03a | AC-03 | `--changed` reports only changed targets while a rule can read required unchanged project context | changed-scope boundary |
| EC-03b | AC-03 | explicit file/directory targeting preserves existing reporting containment/de-duplication behavior | explicit-scope boundary |
| EC-07a | AC-07 | shared documentation subjects preserve ordinary summary and direct-inheritdoc semantics | ordinary subject/carrier path |
| EC-07b | AC-07 | shared documentation subjects preserve positional record/record-struct property `<param>` carrier semantics | synthesized subject path |
| EC-10a | AC-10 | quality and German semantic-review rules enabled together produce independent deterministic batches/rubrics and remain independently expandable/hand-offable | multiple semantic rules over shared mechanics |
| EC-11a | AC-11 | check diagnoses profile state without mutation while bootstrap/update retain authorized remediation + re-evaluation | command-policy/remediation boundary |
| EC-13a | AC-13 | format/normalize check-only and successful commit behavior remain compatible | normal rewrite path |
| EC-13b | AC-13 | injected conflict/fault during multi-file rewrite leaves pre-run selected-target state | transactional failure path |
| EC-14a | AC-14 | test-only rule executes through runner without production catalog/engine traversal change | rule locality seam |
| EC-14b | AC-14 | test-only rewrite executes through runner without production command switch/repository-loader change | rewrite locality seam |
| EC-15a | AC-15 | deterministic finding/order/ignore/explain behavior remains compatible | deterministic hygiene surface |
| EC-15b | AC-15 | semantic review sample/expand/handoff behavior remains compatible | review surface |
| EC-15c | AC-15 | repository self-host check retains M0006 German-disable and generic-quality behavior | dogfood policy surface |
| EC-16a | AC-16 | exact packed artifact is installed in isolated consumer and exercises representative lifecycle + rewrite commands | distribution boundary |

## Validation

| ID | Depth | Target | Locus/platform | Command/check | Proves | Expected evidence |
|---|---|---|---|---|---|---|
| VAL-01 | Tier 1 | RepositorySession, rule runner/catalog, fact store, result materializer, rewrite runner/catalog | Windows 11 + .NET 11 | focused TUnit | AC-01..AC-14, AC-18 + EC-02a/b, EC-07a/b, EC-14a/b | focused architecture tests with observable lazy/reuse/module behavior |
| VAL-02 | Tier 1 | existing Core + built CLI process behavior | Windows 11 + .NET 11 | focused/full Core and CLI tests | AC-09..AC-15 + EC-11a, EC-13a/b, EC-15a/b | passing behavior/regression tests with case-specific assertions |
| VAL-03 | Tier 3 | isolated SDK-style Git repositories | Windows 11 + .NET 11 + Git | full/explicit/changed check fixtures plus profile/review/rewrite scenarios | AC-03, AC-07, AC-10..AC-15 + EC-03a/b, EC-07a/b, EC-10a, EC-11a, EC-13a/b, EC-15a/b | fixture evidence from real Roslyn/MSBuild/Git boundaries |
| VAL-04 | Tier 4 | exact current locally packed/installed tool | isolated Windows consumer repo | package, install exact artifact, invoke installed `hygiene` | AC-15, AC-16 + EC-16a | artifact hash/provenance + installed consumer command evidence |
| VAL-05 | Tier 2 | complete repository | Windows 11 | `./eng/validate.ps1` | AC-15..AC-18 | complete current validation pass |
| VAL-06 | documentation | architecture/milestone/engineering/terminology/research consistency | repository | authority consistency review | DOC-01 | direct document reconciliation |
| VAL-07 | human | completed architecture and evidence | project owner/delegate | `REV-M0007-COMPLETION` | REV-01 | explicit approval or recorded review outcome |

## Human Review

Applicability: required before milestone completion.

Review class: architecture/completion.

Canonical review ID: `REV-M0007-COMPLETION`.

Review subject:

- post-refactor source/module layout;
- representative ordinary rule locality;
- profile rule/remediation locality;
- format/normalize module locality;
- repository/session/fact abstractions;
- central runner/materializer responsibilities;
- validation evidence proving compatibility.

Required evidence:

- compact before/after architecture summary;
- file/type map for session, catalog/runner, representative rule modules, facts, result materialization, rewrite modules/transaction;
- validation result summary including Tier 4 installed-tool evidence;
- confirmation that no new plugin/context-DAG/statistical-sampling framework was introduced.

Acceptance criteria:

- a future ordinary rule/rewrite has an obvious small home;
- shared infrastructure is simple enough to understand without framework archaeology;
- rule semantics are no longer embedded in central orchestration;
- expensive shared context has one clear owner and lazy lifetime;
- public behavior remains coherent with M0006.

Reviewer role: project owner/delegate.

Waiver policy: explicit project-owner decision only; otherwise milestone remains awaiting review.

Milestone completion command/check: use the repository's current review mechanism where available, then record the human decision as durable completion evidence.

## Documentation Policy

Architecture documentation is part of this milestone because the architecture decision is the deliverable, not incidental implementation detail.

Implementation updates directly contradicted architecture/engineering/terminology authority and keeps `docs/research/RULE-APPLICATION-ARCHITECTURE.md` aligned as rationale/provenance. Broad public-document cleanup is not required unless the refactor unexpectedly changes a public claim.

## Completion Expectations

Implementation owns execution decomposition and milestone closure:

```text
read milestone + planning-seeded ledger
-> verify obligation/evidence-case equality
-> inspect accepted M0006 live repository
-> map obligations to implementation work packages
-> implement shared session and module boundaries incrementally
-> keep behavior tests passing during extraction
-> focused + integration + installed-artifact validation
-> freshly reread milestone
-> reconcile every obligation/evidence case <-> ledger <-> repository/evidence
-> completion audit
-> durable completion evidence
-> required human review
```

Passing the existing test suite alone does not establish M0007 completion. The internal modularity/laziness/locality obligations require focused evidence in addition to public behavior regression.

## Completion Evidence

Implementation records verified repository evidence and automated validation here. Human review remains pending.

| Obligation / evidence case | Evidence | Validation gate / target | Result |
|---|---|---|---|
| AC-01 | Shared `RepositorySession` is consumed by check and rewrite; one root/target/project/workspace/document implementation. | VAL-01..03; Tier 1/3 | satisfied |
| AC-02 | Production LongLineReviewRuleModule runs through RuleCatalog.Runner and requests only text; its focused test leaves workspace/projects/compilation absent. Registered production summary/quality/German consumers reuse one compilation and one DocumentationSubjectFact computation. | VAL-01; Tier 1 | satisfied |
| AC-03 | Reporting documents are selected independently from loaded project context; changed/explicit scope regressions pass. | VAL-03; Tier 3 | satisfied |
| AC-04 | RuleCatalog.Modules explicitly registers each production IRuleModule in canonical order; RuleCatalog.All retains descriptors and metadata. | VAL-01/02; Tier 1 | satisfied |
| AC-05 | XML and sentence modules use separate evaluators; summary, profile, quality/German review, long-line and visual-block logic is module-owned. HygieneEngine.Check only selects/runs modules and handles generic results. | VAL-01/02; Tier 1 | satisfied |
| AC-06 | Production modules receive RuleContext over RepositorySession and request only their needed resources/facts; the production lightweight-rule test demonstrates unloaded Roslyn resources. | VAL-01; source review | satisfied |
| AC-07 | Shared immutable `DocumentationSubjectFact` handles summary carriers, positional records, direct inheritdoc, and review eligibility. | VAL-01/03; Tier 1/3 | satisfied |
| AC-08 | Session fact and compilation factories are cached once per session key; no durable analysis cache. | VAL-01; focused tests | satisfied |
| AC-09 | Host retains finding identity/order/ignore/stale/run/review persistence paths. | VAL-02/03; Tier 1/3 | satisfied |
| AC-10 | Quality and German review modules share deterministic batch mechanics while retaining separate descriptors and behavior. | VAL-02/03; Tier 1/3 | satisfied |
| AC-11 | Profile diagnosis modules are separate from bootstrap/update remediation; check remains diagnostic only. | VAL-02/03; Tier 1/3 | satisfied |
| AC-12 | RewriteCatalog registers distinct format and normalize modules; normalization validation belongs to NormalizeRewriteModule hooks, and RewriteEngine has no command-specific recognition. | VAL-01/02; Tier 1 | satisfied |
| AC-13 | Rewrite uses `RepositorySession`; transaction planning, validation, check-only, conflicts, atomic replacement, and rollback regressions pass. | VAL-02/03; Tier 1/3 | satisfied |
| AC-14 | Test-only rule/rewrite modules run through the same runner contracts used by production without central orchestration/loader changes. | VAL-01; focused tests | satisfied |
| AC-15 | Core 51/51 and CLI 22/22 pass; exact positional-record ReviewItem fields are asserted in Core and across CLI check/expand/handoff; self-host preserves M0006 quality active/German disabled. | VAL-02/03/05; Tier 1/2/3 | satisfied |
| AC-16 | Exact locally packed package installed in isolated consumer; canonical Tier-4 workflow marker emitted. | VAL-04; Tier 4 | satisfied |
| AC-17 | Source/project inspection confirms no prohibited framework, discovery, process, scheduler, persistent cache, sampler, rule, or rewrite. | VAL-05; source/dependency review | satisfied |
| AC-18 | HygieneEngine.Check has no summary-specific analysis branch/subject construction; RewriteEngine has no format/normalize semantic branch and reuses RepositorySession. | VAL-01/05; source review | satisfied |
| DOC-01 | Architecture, engineering, terminology, milestone and rationale docs are consistent; architecture remains authority. | VAL-06; repository review | satisfied |
| REV-01 | Required BORING/local-comprehensibility completion review. | VAL-07; project owner/delegate | pending — AWAITING HUMAN REVIEW |
| EC-02a | Production LongLineReviewRuleModule runs through RuleCatalog.Runner; short text requests no workspace/project/compilation and constructs only text. | VAL-01; ProductionLightweightRuleUsesLazyProductionContextWithoutLoadingRoslyn | satisfied |
| EC-02b | Registered production summary/quality/German modules reuse one compilation and one DocumentationSubjectFact construction for their document. | VAL-01; RegisteredProductionRulesShareLazyDocumentationFactAndCompilation | satisfied |
| EC-03a | Changed-target reporting remains scoped while project context is loaded. | VAL-03; isolated Git/MSBuild fixture | satisfied |
| EC-03b | Explicit file/directory containment and de-duplication remain stable. | VAL-03; isolated Git/MSBuild fixture | satisfied |
| EC-07a | Ordinary summary and direct-inheritdoc behavior pass shared-subject fixture. | VAL-01/03; focused/fixture tests | satisfied |
| EC-07b | Record and record-struct positional `<param>` carriers pass shared-subject fixture. | VAL-01/03; focused/fixture tests | satisfied |
| EC-10a | Simultaneous quality/German rules preserve independent deterministic batches, expansion and handoff. | VAL-03; isolated CLI fixture | satisfied |
| EC-11a | Check diagnosis and authorized bootstrap/update repair plus re-evaluation pass. | VAL-02/03; Tier 1/3 | satisfied |
| EC-13a | Rewrite check-only and successful format/normalize operations pass. | VAL-02/03; Tier 1/3 | satisfied |
| EC-13b | Compiler rejection, conflict and multi-file rollback preserve original selected targets. | VAL-02/03; Tier 1/3 | satisfied |
| EC-14a | Test-only IRuleModule executes via the production RuleModuleRunner/context seam without production catalog/engine edits. | VAL-01; TestOnlyRuleAndRewriteModulesRunThroughTheirRunners | satisfied |
| EC-14b | Test-only IRewriteModule executes via RewriteRunner without production engine command branching or loader edits. | VAL-01; TestOnlyRuleAndRewriteModulesRunThroughTheirRunners | satisfied |
| EC-15a | Ordering, JSON, explain, ignore/stale and target compatibility regressions pass. | VAL-02/03; Tier 1/3 | satisfied |
| EC-15b | Review fingerprints, independent expansion and handoff rubrics remain compatible. | VAL-02/03; Tier 1/3 | satisfied |
| EC-15c | Repository self-host retains generic quality and disables German review. | VAL-05; Tier 2 | satisfied |
| EC-16a | Exact packed artifact passes representative isolated lifecycle and rewrite workflow. | VAL-04; Tier 4 | satisfied |

Automated validation: `eng/validate.ps1` exited 0 on Windows 11 with .NET SDK 11.0.100-rc.1.26425.128; Release build succeeded; Core 51/51 and CLI 22/22 passed; exact installed consumer and repository self-host markers passed. Exact positional-record semantic-review fields passed Core and CLI check/expand/handoff assertions. `git diff --check` passed after evidence updates. Detailed case mapping and resume state are in `.execution/M0007-modular-analysis-architecture.md`.

Prerequisite record: the project owner approved M0006 REV-01 in this task conversation on 2026-10-06. The acceptance is now recorded in the M0006 milestone and execution ledger before M0007 PR publication.

Current milestone outcome: **AWAITING HUMAN REVIEW** (`REV-M0007-COMPLETION`).

## Escalation Boundary

Implementation owns concrete interfaces, type names, folders, internal test seams, and refactoring sequence that satisfy this contract.

Return to planning if completion would require a material change to:

- independent-module/shared-session architecture;
- M0006 rule semantics/configuration;
- public behavior or schema;
- statistical-sampling product semantics;
- validation topology;
- package/version policy beyond the accepted prerequisite authority;
- plugin/extensibility scope.
