# M0023 — Deterministic C# Cleanup and Sampled Modernization Review

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Execution mode | ai-executed-human-reviewed |
| Guide system | Agentic Project Guides 0.9.3; base + cli-tool |
| Baseline executor | repository-configured baseline implementation model |
| Execution locus | Windows 11, .NET 11 SDK, PowerShell, Git, local test/package environment |
| Baseline | latest merged `main` at execution; reconcile M0019 and subsequent changes; no fixed commit assumption |
| Execution ledger | `.execution/M0023-deterministic-cleanup-and-modernization-reviews.md` |
| Scope | large coherent addition to the existing rule-owned source remediation and bounded review pipeline |
| Review | milestone-scoped independent human review, pending until authorized decision |
| Environment authority | existing SDK, Git, PowerShell, approved local dependencies and isolated local tool installations; no host-wide provisioning or CI changes |

## Goal

Produce predictably formatted, mechanically cleaned and easier-to-read C# before exposing bounded, high-value modernization decisions to an implementation agent. Prefer automatically applied changes only when readability gain is immediate and unambiguous. Keep context-dependent modernization opt-in to agent judgment, not automatic application.

## Target State

The default autonomous `dotnet hygiene` lifecycle first applies enabled deterministic normalization and formatting, verifies a stable clean result, then detects eligible modernization opportunities through compiler-aware analysis and presents a *small shared budget* of rule-specific, file/type/method-scoped work packages. Each occurrence can be individually accepted, rejected, or escalated. All work retains rule ownership, clear provenance, transactional source edits and independent review identities. The CLI remains model-independent.

## Scope

### Deterministic formatting

- Retain `format.csharp.roslyn` as the **only general C# formatter**. Establish explicit behavior and acceptance coverage for comment alignment (`//`, `/* */`, XML docs), line endings, trailing whitespace, final newline, blank lines and Roslyn code indentation.
- Add narrowly scoped formatter extensions only when measured Roslyn output fails the specified layout; preserve comment text, code examples, deliberate diagrams and grouping. Avoid second opinionated formatting engines such as CSharpier.
- Organize `using` directives deterministically without changing necessary imports, respecting repository conventions. Enforce conventional modifier ordering using supported Roslyn style/code fix where available.
- Do not auto-wrap arbitrary long expressions or erase intentional blank-line grouping. Existing visual-block semantic review remains authoritative for judgmental grouping.

### Deterministic normalization

- Preserve existing `style.qualification.this.unnecessary` and `style.qualification.redundant` behavior.
- Add automatic, individual rule-owned code fixes for `style.braces.required` (IDE0011) and `style.accessibility.explicit` (IDE0040), preserving their existing EditorConfig projection and diagnostic contract.
- Add target-typed object creation only when the declared target type is immediately visible (`T x = new T(...)` -> `T x = new(...)` and analogous directly visible typed initializers), syntax is supported by the *effective project compilation language version*, and the rewrite is behavior-preserving. Do not convert method returns or other contextual expressions merely because compiler inference is possible.
- Remove demonstrably unused imports (IDE0005); unnecessary casts (IDE0004); obvious `default(T)` -> `default` when direct type context exists (IDE0034); redundant interpolated string simplification (IDE0071); inferred tuple/anonymous member names when clear (IDE0037). Add unnecessary parentheses (IDE0047) and null-forgiving operator removal (IDE0080) only where contextual intent and readability are not obscured; otherwise leave these for review/diagnosis rather than broadly rewriting.
- Mechanically remove only trivially redundant blank lines (leading/trailing interior block runs and runs of excessive empty lines); preserve meaningful section spacing and comments.
- Prefer existing compiler-aware Roslyn analyzers/code fixes and exact supported diagnostics over new syntax rewriters. Diagnostic IDs are implementation candidates, not permission to apply every fix offered by a diagnostic. Verify SDK/Roslyn diagnostic IDs, availability, safety and public semantics against the actual installed toolchain.

### Sampled semantic modernization

Provide distinct independently configurable diagnostic/review rules for these **deferred**, context-sensitive transformations (do not silently auto-apply):

1. `var` versus explicit local types (IDE0007/IDE0008): file population, flagged declarations in file.
2. Expression-bodied members (IDE0021–IDE0027): type population, flagged members in type.
3. Collection expressions (IDE0300 family): file population, flagged expressions in file.
4. Primary constructors (IDE0290): type population including affected state and related partial declarations.
5. Switch expressions (IDE0066): method/member population, candidate switch plus enclosing behavior.
6. Conditional/ternary conversions (IDE0045/IDE0046): method/member population, candidate and control-flow context.
7. Object initializer conversions (IDE0017): method/member population, construction sequence and mutation/evaluation context.
8. `readonly` field proposals (IDE0044): type population, field uses and assignment context.
9. Inlining or merging variables/declarations: method/member population, only when supported compiler-aware detection and a concrete preview exist; do not assume IDE0018 specifically means general inlining.

A modernization rule's occurrence population is derived from compiler-aware candidates; the review-subject population consists of **eligible files, types or methods containing one or more such occurrences**, as listed above. Each grouped work package exposes all bounded candidate occurrences it includes, with original syntax, proposed transformation, surrounding context, candidate identity and rule-specific question. The reviewer judges each occurrence independently: desirable / retain existing code / uncertain. The work package is not one blanket positive verdict. No change is performed by the CLI merely because an occurrence was sampled.

### Shared review capacity and normal gate integration

- Introduce a **shared modernization budget of at most five work packages per ordinary source-change gate across all modernization rules**, not five per modernization rule. Avoid starving a consistently eligible rule: deterministic bounded rotation or similarly transparent fairness using existing gate-local context; no hazard/age reintroduction, random experiments, statistical defect-rate claims or permanent complexity framework.
- Prefer directly changed candidates, then context-affected candidates. Do not introduce background unchanged candidates into ordinary gates. Explicit inspection may request broader candidates while remaining bounded; exhaustive read-only audit (M0019 `--yolo-all`) may inventory all enabled eligible modernization obligations without changing code, preserving its distinct authorization and read-only semantics.
- Bound both number of work packages and occurrences/context per package; for oversized files/types, select coherent bounded method/occurrence subsets and report total, selected and remaining counts honestly, never silently mark unreviewed occurrences complete.
- Preserve six existing semantic rules, their independent populations, selection caps and acceptance semantics. The shared modernization cap **applies only to the new modernization-review family**; do not change the existing per-rule five-item behavior or merge unrelated rule acceptance.
- Stable occurrence-level decisions and content fingerprints must invalidate when material code/context changes and not conflate unrelated occurrences. Rejection means retaining code for this observed gate/context, not permanently suppressing unrelated future opportunities. Explicitly distinguish rejected, accepted/applied and uncertain/escalated; do not treat rejected modernization suggestions as hygiene defects or as positives. Avoid large persistent indexes or cross-gate history machinery.
- Modernization changes are applied by the external implementer using supported normal source-edit flow after judgment, followed by rerun of hygiene. The CLI does not invoke an AI model, author edits itself for judgmental proposals, or assert successful agent remediation.

## Non-goals

- Enable every Roslyn IDE diagnostic/code fix or impose broad C# modernization.
- Force `var`, expression-bodied members, primary constructors, collection expressions, ternaries, inlining, field immutability or object initializers as default style.
- Refactor nested `new()` expressions, introduce new abstractions, or make complexity decisions automatically. Existing/possible semantic complexity rules remain separate.
- Change rule identity/meaning for existing semantic review rules, their per-rule sampling cap, or the public `--yolo-all` mutation policy.
- Introduce an opinionated second formatter, generic analyzer orchestration framework, reflection/DI discovery, external AI provider calls, global warning-as-error policy, new CI, or cross-repository service.
- Claim compiler success proves semantic equivalence; safety must be supported by per-transformation tests and binding/behavior constraints.
- Public-feed publishing or broad release readiness beyond the exact milestone consumer gate.

## Resolved Contract Decisions

1. **Readability-first:** automatic transformations require clear local readability gain and proven preconditions; ambiguous transformations are review-only or skipped.
2. **Rule-owned capability model:** retain the static ordered `RuleCatalog` and rule-wide enable/disable. Capabilities may include format, normalize, diagnose and review; preserve profile projection ownership. Do not introduce separate semantic rewrite registries.
3. **Semantic model != semantic judgment:** deterministic rules may use Roslyn binding and code fixes. Review is necessary when *desirability* depends on intent/structure.
4. **Population ≠ work package ≠ occurrence:** analyzer occurrences drive eligibility; file/type/member are sampling identities according to rule; occurrence-level verdicts remain separate.
5. **Bounded and cheap:** analyzer discovery may be exhaustive over the normal eligible changed scope, but model/agent review is strictly bounded across modernization rules. Favor shared caching and avoid repeated compilation.
6. **Source eligibility:** existing supported project compilations, generated-source exclusions, and source identity apply. Ambiguous/missing compilation or unsupported language version must fail safely or skip explicitly with reason; do not blindly text-rewrite.
7. **Execution order:** normalize -> format -> validate (syntax/semantic preconditions and idempotence) -> detect/review modernization candidates. Ensure existing transactional snapshot/rollback semantics and `--check` nonmutation.
8. **Disabled rules:** disabled modernization rules do not diagnose, select or generate work. Existing mandatory profile rules remain mandatory. No arbitrary user rule parameters or per-rule sample-budget configuration.
9. **Audit compatibility:** include modernization obligations in the exhaustive *read-only* path if enabled, without model invocation or auto-fix; use bounded transport for agent judgments while preserving exhaustive report accounting. If M0019 is not merged at implementation start, make compatibility changes against the then-current intended audit contract without silently assuming its CLI already exists.
10. **Scope of optional diagnostics:** IDE0047 and IDE0080 must be validated for unambiguous benefit; do not add unsafe default fixes merely to fill the candidate list. Any unsupported candidate must remain explicitly documented with test evidence and pending escalation, not silently approximated.

## Validation cadence and acceptance boundary

M0023 distinguishes implementation feedback from final acceptance under `docs/ENGINEERING.md` and guide 0.9.3:

- **During implementation:** use the cheapest reliable affected Core/CLI tests, build checks and targeted fixtures per work package. Run an expensive consumer or aggregate stage early only for a documented cross-boundary diagnostic need, not as routine progress verification.
- **When implementation obligations are complete:** reconcile all AC/EC coverage and run/verify VAL-01–VAL-05 against the finished candidate. For the seven-stage aggregate, inspect `./eng/validate.ps1 -PlanJson`, execute missing or stale required stages, and finish with `./eng/validate.ps1 -Verify`. Current fingerprint-valid receipts may be reused; neither a cold full rerun nor reuse of stale evidence is required or allowed by default.
- **After later changes:** invalidate and refresh only affected stage evidence according to `docs/ENGINEERING.md`; where dependency coverage is uncertain, rerun conservatively. Fresh installed-consumer evidence must exercise the current packaged M0023 behavior, including modernization reporting, not merely an earlier partial implementation.
- **Report separately:** suite execution/receipt freshness, actual AC/EC behavioral coverage, and milestone completion/readiness. A green aggregate on an intermediate snapshot is diagnostic evidence and does not satisfy missing behavior or permit a completion claim.

VAL-05 is a **closure gate**, not a request to perform complete release qualification after each work package. Its presence does not prohibit justified early diagnostic runs.

## Required Authority

- `AGENTS.md`, `docs/ENGINEERING.md`, `docs/ARCHITECTURE.md`, `docs/specs/HYGIENE.md`
- `docs/specs/RULE-CAPABILITIES.md`, `docs/specs/REWRITES.md`, `docs/specs/PROFILE.md`, `docs/specs/AUTONOMOUS-WORKFLOW.md`
- `docs/specs/SAMPLING.md`, `docs/specs/SEMANTIC-REVIEWS.md`, `docs/specs/REVIEW-HANDOFFS.md`
- Existing `RuleCatalog`, `Rules/Remediation/`, `Rules/Profile/`, semantic review modules and C# project workspace/transaction pipeline.
- Existing Core/CLI tests and `eng/validate.ps1` (including M0020 stage-plan/verify contract), with current milestone/review conventions.
- M0019 audit milestone only for the applicable full read-only audit boundary; do not treat an unmerged PR as merged functionality.

## Acceptance Criteria

- **AC-01** — Existing `format.csharp.roslyn` remains sole general formatter; formatting reliably aligns conventional statements/comments/XML docs, applies whitespace/newline conventions, and retains intended comment text and intentional grouping; a fallback handles only demonstrated Roslyn gaps.
- **AC-02** — `using` organization and modifier-order formatting are deterministic, project-aware, idempotent, and do not drop required imports or reorder semantics-relevant constructs.
- **AC-03** — Existing qualification simplifiers retain current behavior; braces and explicit accessibility style policies gain source auto-fixes while retaining current diagnostic/editorconfig owners and rule-wide disabling.
- **AC-04** — Target-typed `new()` applies only in immediately type-visible, supported and proven-equivalent expressions; it does not rewrite return expressions or materially obscure contextual types.
- **AC-05** — Safe cleanup candidates (unused imports, redundant casts, obvious typed default, interpolated simplification, inferred names, trivial blank-line redundancy) run under individually owned rule identities and preserve semantics, comments, original trivia and unrelated source.
- **AC-06** — Parentheses/null-forgiving proposals apply automatically only under documented proven-safe, obvious readability conditions; nonqualifying candidates remain unmodified rather than over-eagerly normalized.
- **AC-07** — Mechanical remediation is bounded, source-aware, transactional, idempotent, respects effective language version and repository/profile enablement, and the no-mutation check modes match the proposed actual changes without writing.
- **AC-08** — All nine deferred modernization families have compiler-aware eligibility, fixed question/preview contract, and the appropriate file/type/member review population; technical convertibility alone never triggers an automatic change.
- **AC-09** — Selected modernization work packages contain bounded individually addressable candidate occurrences and adequate context; implementer can accept, decline or mark uncertain for each, and package-level completion does not manufacture occurrence-level judgments.
- **AC-10** — One shared max-five modernization work-package budget applies per ordinary gate across modernization families, with honest per-rule eligible/selected/unreviewed accounting, changed-first selection and transparent nonstarvation; existing six semantic-rule per-rule sampling behavior remains unchanged.
- **AC-11** — Modernization verdict persistence/revalidation uses stable occurrence and enclosing-context fingerprints, supports stale/rejected/uncertain results without falsely asserting fixes, and does not suppress unrelated obligations or resurrect removed hazard/age scheduling.
- **AC-12** — Rule catalog capabilities, enable/disable behavior, JSON/text reporting, rule explanations, review/handoff and autonomous lifecycle expose the new rules consistently; disabled rules contribute neither transformations nor review tasks.
- **AC-13** — Explicit exhaustive read-only audit remains nonmutating and inventories modernization obligations when enabled; normal mode remains bounded, and `--review-all` cannot silently become unbounded agent work.
- **AC-14** — Project authority documents are directly reconciled, explaining formatting vs mechanical normalization vs judgmental modernization, rule inventory, sampling units/budget, exclusion/rollback and source safety.
- **AC-15** — Required Windows/.NET 11 Core+CLI and installed-tool consumer checks demonstrate the real changed public workflow, with current stage-fingerprinted evidence and no substituted unsupported validation locus.
- **AC-16** — Milestone-specific independent review records an actual authorized decision on rule safety, agent-work bound, semantic-review independence and evidence fidelity; do not self-approve.

## Distinct Evidence Cases

| ID | Parent | Independently observable scenario |
|---|---|---|
| EC-01a | AC-01 | Ordinary and trailing `//`, `/* */` and XML docs align without textual corruption, including odd original indentation |
| EC-01b | AC-01 | Intentional diagrams/code snippets and visual spacing are preserved; repeated format makes no edits |
| EC-02a | AC-02 | Organize usings and modifiers in normal projects; preserve imports used through aliases/global/conditional contexts |
| EC-03a | AC-03 | Required braces and explicit accessibility: enabled fixes rewrite source, disabled rules neither rewrite nor project policy |
| EC-04a | AC-04 | Obvious typed local/initializer converts to `new()` and compiles with equivalent binding |
| EC-04b | AC-04 | Contextual method return, ambiguous overload/typing and unsupported language target do not convert |
| EC-05a | AC-05 | Each safe cleanup family has at least one positive and one ineligible/negative focused scenario |
| EC-06a | AC-06 | Grouping parentheses and nullability assertion conveying intent remain unchanged |
| EC-07a | AC-07 | `normalize --check` and `format --check` are read-only and produce matching actionable changed-path plans |
| EC-07b | AC-07 | Compiler/validation failure, cancellation/conflict and second-run idempotence cause safe rollback/no further churn |
| EC-08a | AC-08 | Each modernization family identifies correct eligible occurrences, correct scope, and excludes syntactically suggestible but semantically unsupported contexts |
| EC-09a | AC-09 | Multiple occurrences in selected file/type/method can receive independent mixed verdicts; long package reports remainder |
| EC-10a | AC-10 | Several simultaneously eligible families never exceed shared five-package normal budget; six legacy semantic batches unaffected |
| EC-10b | AC-10 | Changed candidates outrank context; repeated gates permit fair selection without hazard/age and do not create unbounded reviews |
| EC-11a | AC-11 | Repeated same-content gate does not duplicate accepted results; material surrounding change makes required decisions stale |
| EC-11b | AC-11 | Rejected/uncertain candidate not falsely fixed/accepted; unrelated candidate remains separately eligible |
| EC-12a | AC-12 | Rule toggles consistently affect projected settings, actual rewrite, diagnostics and work orders in CLI JSON/text |
| EC-13a | AC-13 | Explicit full audit reports all eligible obligations without source, state or profile mutation, unlike capped normal gate |
| EC-15a | AC-15 | Exact freshly built+installed tool performs end-to-end deterministic cleanup and modern review reporting in a fixture repository |

## Validation

| Gate | Target/depth | Locus | Required invocation / verification | Obligations / evidence |
|---|---|---|---|---|
| VAL-01 | Targeted Core tests; Roslyn real compilations/workspaces | Windows 11/.NET 11 | Focused TUnit/MTP via repository's `eng/validate.ps1` stage plan or supported focused tests | AC-01–AC-08; EC-01a/b, 02a, 03a, 04a/b, 05a, 06a, 07a/b, 08a |
| VAL-02 | CLI fixture gates, review persistence and shared budgeting | Windows 11/.NET 11 + Git | Focused CLI test stages, fresh isolated repositories, actual JSON and work orders | AC-09–AC-13; EC-09a, 10a/b, 11a/b, 12a, 13a |
| VAL-03 | Failure-path/idempotence mutation checks | Windows 11/.NET 11 | Existing transaction/rollback fixtures and repeated normalize/format; `git diff --check` | AC-01–AC-07; EC-01b, 07a/b |
| VAL-04 | Exact installed package consumer integration | Windows 11/.NET 11 + Git + isolated local NuGet feed | Current `eng/validate.ps1` plan/shard/verify stage(s) for freshly packed and installed tool; don't substitute source-level smoke test | AC-12, AC-13, AC-15; EC-12a, 13a, 15a |
| VAL-05 | Required authoritative aggregate for milestone | Windows 11/.NET 11 + PowerShell | `eng/validate.ps1` current required plan/shards and `-Verify` with current fingerprints, plus `git diff --check`; full cold only if current project authority requires it | AC-01–AC-15; evidence mapped to actual test scenarios rather than generic green |
| VAL-06 | Independent milestone review | authorized reviewer on M0023 PR | `.review/REV-M0023-COMPLETION.md` and project review-check after real decision | AC-16 |

Focused checks are for iterative development; current valid evidence receipts and planned stage identity are required for final claims. Missing Windows/tool prerequisites are BLOCKED, not a simulated pass. Do not provision CI or host-wide SDKs. Before final acceptance, update the milestone's `Completion Evidence` with per-AC/EC links to actual recorded receipts and pending human decisions.

## Human Review

Human review is milestone-scoped, blocking, and independent. `.review/REV-M0023-COMPLETION.md` is prepared with links to the rule diff, current Core/CLI/consumer evidence, modernization cap and negative-case proof, and outstanding limitations. Leave the decision pending for the authorized reviewer; run the existing review-check only after an actual decision.

## Documentation Policy

Update directly affected specs (especially RULE-CAPABILITIES, REWRITES, SAMPLING, SEMANTIC-REVIEWS, AUTONOMOUS-WORKFLOW), CLI usage and engineering guidance so no existing operative rule contract contradicts the implementation. Keep research optional/non-authoritative. No broad guide-repository or unrelated website synchronization.

## Baseline Executor Readiness and Tractability

Ready: product-level decisions, negative boundaries, population strategy, budget and validation locus are fixed above. Implementation may select exact source structures, Roslyn APIs, diagnostic availability handling, fixture architecture and bounded work packages after inspecting the live repository. Planned obligation IDs, evidence-case IDs and validation target/locus must survive execution intact. If a mandatory modernization candidate cannot be supported safely by the installed Roslyn implementation, document concrete evidence and return the material scope decision to planning rather than silently substituting regex rewriting or claiming success.

## Completion Evidence

## Execution evidence status (2026-10-21)

Implementation and the technical validation gates are complete on the current branch snapshot. Windows 11/.NET 11 current receipts pass: Release solution build (0 warnings/errors), Core 112/112, CLI 34/34, README policy, current package/installed-consumer, and self-host. `eng/validate.ps1 -Verify` returned `status: verified` and reused all seven stage receipts; current package SHA-256 is `426799A5C245613DF8CA85489B1CE2DB5D52FAF9EB2E681780F6D645D1E5E5CC`. The execution ledger retains criterion-specific partial/pending evidence where the planned negative-context, mixed-verdict, new-gate replay, and all-family audit fixtures are not yet complete. AC-16/VAL-06 remains blocking: `.review/REV-M0023-COMPLETION.md` is prepared, and the authorized independent human decision is pending. No approval or full milestone acceptance is claimed.
