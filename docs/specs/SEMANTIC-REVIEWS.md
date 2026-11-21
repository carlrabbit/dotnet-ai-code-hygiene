# Semantic review contracts

The deterministic engine discovers candidates; it does not decide whether a candidate is defective. Each semantic rule is an independent, fixed contract with one primary question, a mechanically observable candidate signal, a legitimate-case boundary, and its own escalation policy. Rules can be enabled or disabled only as whole contracts. The CLI invokes no model/provider and applies no semantic correction.

## General semantic portfolio

The 17 general rules share one gate-local budget of at most ten distinct `(rule, subject)` judgment obligations. Presentation grouping does not merge obligations. The portfolio is ordered R01–R17 below; batch IDs B-1–B-17 follow that order.

| Ref / ID | Candidate population and subject | Primary question | Legitimate case and escalation |
|---|---|---|---|
| R01 `architecture.abstraction.necessity` | Changed interface, factory, strategy, or generic abstraction; review the abstraction decision and exposed contract. | Does it serve a current contract, boundary, side effect, or meaningful variability? | A one-implementation persistence, transport, or public-contract boundary may be justified. Escalate only a material contract/design decision unresolved from authority. |
| R02 `architecture.indirection.value` | Changed forwarding method, adapter, or delegation chain; review caller, intermediary, and callee. | Does the intermediary own meaningful transformation, policy, integration, or isolation? | A cross-boundary adapter or authorization boundary may legitimately forward. Escalate unresolved material ownership/boundary decisions. |
| R03 `architecture.responsibility.cohesion` | Changed type responsibilities, related state/operations, or responsibility cluster; review the type and collaborators. | Does the type have a coherent responsibility, or mix separable owners? | Size and member count are not defects. Escalate unresolved material ownership/design decisions. |
| R04 `architecture.boundary.ownership` | Changed cross-component call, dependency on component internals, or public boundary; anchor the changed/reporting side and show provider context. | Does the operation use the component/public contract that owns the behavior and authority? | A composition root may coordinate components. Escalate unresolved material ownership/interface/design decisions. |
| R05 `architecture.authority.uniqueness` | Related configuration, version, or derived/materialized state changed together; review source-of-truth and provenance. | Is a derived value incorrectly treated as an independently authoritative choice? | Caches, snapshots, and versioned materializations are valid with explicit provenance/invalidation. Escalate unresolved material authority decisions. |
| R06 `architecture.lifecycle.binding` | Changed service construction, registration, factory, or request/run/session/resource lifetime; review construction and use. | Are construction-time dependencies and invocation-specific or resource state bound to the intended lifetime? | A per-run processor may capture run state. Escalate unresolved material lifetime/ownership decisions. |
| R07 `architecture.mechanism.explicitness` | Changed reflection/dynamic discovery, locator, convention dispatcher, DSL, or general framework mechanism. | Is the implicit machinery justified by a current need that simpler explicit C# cannot meet? | Framework binding, serialization, interoperability, or stable plugins may justify it. Escalate unresolved material design decisions. |
| R08 `behavior.error.contract` | Changed catch, result conversion, fallback, retry, or exception mapping; review the affected failure-to-outcome path. | Can a failure be misrepresented as success, denial, or a different business outcome? | Intentional domain fallbacks and handled expected errors may be correct. Escalate a missing/conflicting business, security, or data-boundary contract; unsupported runtime context remains uncertain. |
| R09 `behavior.validation.boundary` | Changed endpoint/serializer/DTO/nullability/validation or untrusted-data entry; review input and effective validation. | Is the actual invariant enforced where untrusted data enters without redundant interior checks or unjustified assumptions? | Compiler nullability alone is not runtime JSON validation; inspect configured binders. Escalate a missing/conflicting material input contract; unknown runtime binding remains uncertain. |
| R10 `behavior.state.consistency` | Changed asynchronous request/callback, mutable state, cache, selection, or context transition; review state owner and relevant operations. | Could stale results or state updates apply to the wrong active context? | Sequential or isolated operations may need no extra cancellation machinery. Escalate a missing/conflicting material state contract; unsupported scheduling remains uncertain. |
| R11 `behavior.resource.ownership` | Changed allocation, streaming, event subscription, cancellation, or disposal; review acquisition/use/release paths. | Is the resource owner responsible for valid cleanup and propagation across lifecycle paths? | A consumer need not dispose a borrowed resource. Escalate a missing/conflicting ownership contract; unknown external lifetime remains uncertain. |
| R12 `tests.assertion.effectiveness` | Changed compiler-resolved test method with assertions/observable outcome checks; review test and protected behavior. | Would its assertions detect a material contract violation, not merely non-throwing execution? | Assertion count, mocks, or structure alone are not verdicts. Escalate material uncertainty to frontier. |
| R13 `tests.change-contract.coverage` | Changed production behavior/signature/contract with related test evidence, or changed tests around a contract; review the change-to-test relationship. | Do available tests protect materially changed behavior and relevant failure paths? | No one-to-one test mapping is required; integration/contract tests may be the evidence. Missing deterministic matches are partial context, not a coverage verdict. Escalate material uncertainty to frontier. |
| R14 `tests.isolation.coupling` | Changed tests adding/expanding doubles, setup infrastructure, or implementation-specific assertions; review the test and production seam. | Does the test introduce brittle implementation coupling or unnecessary production abstractions solely for testing? | A fake at a real external side-effect boundary can be appropriate. Escalate material uncertainty to frontier. |
| R15 `docs.summary.quality.review` | Compiler-resolved API summary carriers with nonempty explicit prose, including positional-record property carriers. | Is the summary technically faithful, informative, and clear for this contract? | Concise correct summaries are valid; no mechanical grammar/length scoring. Escalate material uncertainty to frontier. |
| R16 `docs.summary.language.german.review` | Same summary-carrier population when this independently toggleable German policy is enabled. | Is the prose natural, understandable German rather than superficial translation? | English repositories may disable this rule; there is no universal German requirement. Escalate material uncertainty to frontier. |
| R17 `comments.intent.review` | Ordinary active-source comment groups, excluding XML docs, directives, disabled text, and literals; review comment and surrounding code/owner. | Does the comment convey accurate, useful rationale or meaning without narrating syntax? | Legal, administrative, and other valid comments remain eligible. Escalate material uncertainty to frontier. |

Compiler-backed discovery is used where available for declared symbols, test-attribute inheritance, trivia, and project references. Dynamic/reflection/unavailable-project paths are reported as incomplete or unknown; no complete call graph or DI reachability is implied. A deterministic signal means only that review may be useful, never that code is defective.

## Review subject and evidence package

Each obligation has a stable project-qualified subject identity, one primary anchor in reporting scope, and zero or more related anchors in readable context. Cross-file subjects retain the changed participating side as primary; an unchanged hidden owner is not substituted as the report anchor. Distinct rules may point to one source anchor and still consume separate budget slots.

Selected obligations include rule question, legitimate-case boundary, observed signal, anchor provenance, and bounded source excerpts. Normal evidence is limited to eight excerpts and 24,000 UTF-8 text bytes per obligation. Missing, unresolved, or truncated necessary context is explicitly `partial` or `unavailable` with a reason. Detailed cross-file excerpts are assembled only for selected work, expansion/handoff, or authorized exhaustive audit. Accepted-observation state stores fingerprints, not raw source excerpts.

The question/rule version and material primary/related evidence participate in identities/fingerprints. Related evidence changes invalidate a prior acceptance. An accepted observation applies only to the same rule, subject, and relevant evidence. Explicit acceptance is required; no signal auto-accepts or auto-remediates.

## Gate selection and acceptance

See [Gate-local semantic review selection](SAMPLING.md) for deterministic selection and invalidation. Normal bare full checks and source-clean gates select zero general semantic obligations. Changed-anchor subjects rank first. A changed related anchor may admit at most two unchanged context-affected subjects, and unrelated background never fills ordinary gates. Explicit paths and `--review-all` use the same ten-obligation total without the context allowance. Full audit is a separately authorized, read-only exhaustive operation.

`dotnet hygiene review accept` accepts only selected obligations after current scope, population, ticket, and fingerprints are revalidated. Partial acceptance never refills the same gate. `expand` and `handoff` revalidate the stored gate; unavailable or truncated context is visible to the reviewer. Legacy `architecture.boringness.review`, `readability.type-responsibility.review`, and `tests.quality.review` acceptance is never translated into a new rule approval. Preserve a copy of old state before removing retired records; the tool reports actionable migration guidance without rewriting legacy bytes.

## Modernization reviews

The nine modernization families retain a separate shared budget of five packages per ordinary gate. Packages contain independently addressable bounded occurrence previews; those details do not spend general semantic slots. Preferred modernization is a source edit. Valid retention uses a narrow source-local suppression, and uncertain work stays outstanding. Modernization verdicts are not persisted as semantic acceptance.

| Rule ID | Analyzer family | Review population |
|---|---|---|
| `modernization.var-style` | IDE0007/IDE0008 | File |
| `modernization.expression-body` | IDE0021–IDE0027 | Type, including partial members |
| `modernization.collection-expression` | IDE0300–IDE0305 | File |
| `modernization.primary-constructor` | IDE0290 | Type and related partial state |
| `modernization.switch-expression` | IDE0066 | Member and enclosing behavior |
| `modernization.conditional` | IDE0045/IDE0046 | Member and control-flow context |
| `modernization.object-initializer` | IDE0017 | Member and construction/mutation context |
| `modernization.readonly-field` | IDE0044 | Type, assignments, and uses |
| `modernization.inline-declaration` | IDE0018 only | Member; concrete inline-declaration fix |

The CLI invokes no model/provider and does not apply modernization previews. Exhaustive read-only audit enumerates every enabled semantic obligation and modernization occurrence without altering normal acceptance state.
