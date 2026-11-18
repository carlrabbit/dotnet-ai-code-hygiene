# M0025 — Semantic Rule Portfolio and Relationship-Based Review Work

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | **ready**; handoff overlay applies to the exact planning-policy baseline below |
| Mode | `ai-executed-human-reviewed` (broad, coherent implementation under one milestone) |
| Guide selection (planning) | Agentic Project Guides v0.9.3; repository-scoped `base` + `cli-tool` |
| Project role / maturity | developer-tool / greenfield, pre-1.0, no external compatibility consumers |
| Baseline implementation model | Project-configured **GPT-5.6 Luna**; no model tier escalation is assumed |
| Baseline executor readiness | confirmed; complete rule contracts, budgets, failure modes and validation topology below |
| Decision preservation | confirmed: independent semantic decisions, agent cost, no model-verdict oracle and reset safety fixed here |
| Execution tractability | confirmed: one large scope with planner-seeded lossless ledger, implementer-owned bounded work packages, resumable validation |
| Execution ledger | `.execution/M0025-semantic-rule-portfolio.md` |
| Source baseline | `carlrabbit/dotnet-ai-code-hygiene` at **d8ae424b8f3bfdab74d930058f8306e933dbfb86**, branch `docs/agentic-rule-portfolio-planning-and-evaluation` (PR #27). **Do not apply to a checkout that lacks this commit/its project guidance.** If PR #27 has merged, apply onto a descendant that contains this commit. |
| Target runtime | Windows 11, .NET 11 SDK, Git, PowerShell; SDK-style C# projects; real packed `dotnet tool` consumer |
| Environment authority | Use existing development host/tools and project-authorized isolated Git worktrees, temp NuGet feeds/tool paths, restored declared dependencies. **No** machine-wide/admin provisioning, new CI/workflows, secret changes or remote infrastructure without separate approval. |
| Validation | Focused feedback while implementing; current milestone-targeted stages + exact local installed consumer after stable implementation; review gate after reconciliation |
| Human review | Blocking independent milestone-scoped `REV-M0025-COMPLETION` |
| Documentation policy | Update directly contradicted authority/help now; unrelated sync deferred |

## Goal

Replace omnibus semantic BORINGness/type/test questions with **17 focused, independently understandable C# semantic-review contracts** and the minimum relation-aware review-work capabilities they require. Make normal agent review more useful without multiplying expensive reviewer tasks. Build the portfolio together under agent-driven economics; establish deterministic software correctness without pretending external model judgments are repeatable tests.

## Target State

An ordinary change gate discovers exact mechanically eligible review candidates for the 17 fixed rules, selects **at most ten distinct rule-subject judgment obligations in total** using a deterministic shared gate policy, lazily assembles bounded, truthful cross-file evidence, and supplies concise independent questions to the external agent. The nine Roslyn modernization families retain their separate **five-package** shared budget. Normal acceptance, handoff, exact installed-tool behavior and explicitly authorized exhaustive audit remain sound. Obsolete experimental BORINGness/type/test contracts no longer operate. The reviewer can accept, change source, or escalate; the CLI never claims to decide architectural quality.

## Scope

- 17 executable general semantic rule contracts below: seven architecture, four behavior, three test effectiveness, three documentation/comment concerns. These are the milestone's **fixed provisional production catalog**; do not silently discard rules because an implementer subjectively judges one unhelpful. Future product decisions may merge/delete them without compatibility obligation. Shared helpers may be consolidated freely.
- Replace the three old overlapping rules, preserving or adapting useful deterministic detector mechanics. Keep existing unrelated mechanical/style, profile, modernization, documentation structural, and visual-block contracts unless necessarily affected by shared infrastructure changes.
- F1 relation/behavior review subjects, F2 bounded multi-file evidence, F3 shared semantic-selection budget, F4 cheap discovery and lazy enrichment.
- Exact deterministic CLI/audit/handoff/acceptance/invalidation behavior, output schemas, documentation and installed consumer distribution; target next local package **0.12.0** without public NuGet publication.
- Report-only bounded observations on real authorized repository changes to demonstrate work-item construction, overlap and cost; these observations **do not certify semantic defect detection**.

## Non-goals

- No synthetic 'good/bad' semantic gold corpus, repeated LLM grades, model consensus, precision/recall acceptance threshold, benchmark-oracle testing, or automated semantic refactoring.
- No embedded model client/provider, planner agent integration, persistent behavioral review-grade database, new language coverage, Razor/JS/TS implementation, universal program graph, rule DSL, plugin registry, external service or CI workflow.
- No arbitrary per-rule tuning; rules stay fixed-policy and independently toggleable. No multi-year history replay or calibrated hazard/age policy.
- No public NuGet publication, platform expansion, broad profile/SDK formatter redesign, or change to ordinary source mutation and audit authorization rights.
- No autonomous acceptance or automated finding remediation based solely on a candidate signal. No subjective rule deletion/consolidation during implementation without a separate planning decision.

## Resolved Design Decisions and Constraints

### 1. Baseline, scope and authority

The new planning authority is on PR #27 at the exact baseline above. This milestone is intentionally based on that state; if PR #27 is not merged, apply the repository-relative overlay to its exact source branch/commit. M0024 is already merged into `main`. The guide metadata selects `base` and `cli-tool`, with the normal `ai-executed-human-reviewed` execution profile and project baseline model. The completed repo guidance (not external guide copies or planning chats) governs implementation. No pre-1.0 persisted/API compatibility is owed, but user/source data may not be silently destroyed.

### 2. Rule semantics and review outcome

Every rule below has a deterministic **candidate signal**, a source-backed review **subject**, exactly one primary **question** (supporting instructions may be included), an explicit legitimate **counterexample**, and an escalation policy. Presence of the signal means review is potentially useful; it **never proves a semantic defect**. An external agent chooses acceptable (no change), concrete correction, or uncertain/escalate. Any corrective edit must respect actual requirements. There is no checklist scoring, correlated-question threshold, or automatic 'fix' merely because code seems non-BORING.

- Architecture questions escalate to **planner** only if a *material* contract, ownership, interface, or design decision cannot be resolved from current authority. Purely local justified refactoring is implementer-owned.
- Behavior questions escalate to **planner** when a missing or conflicting business/security/data boundary contract requires a policy decision; unsupported runtime context remains **uncertain**, not an invented answer.
- Test and documentation questions escalate to **frontier** on material uncertainty or inability to establish the observable contract; local corrections are implementer-owned. These are reviewer-routing policies, not model selection/configuration requirements.
- Each rule is a separate enable/disable unit; no parameterized thresholds, language toggles or repository-defined questions. `docs.summary.language.german.review` is optional and disabled in hygiene's own English repository.

### 3. Full candidate rule catalog

| Ref | Fixed rule ID | Deterministic signal / eligibility | One focused review question |
|---|---|---|---|
| R01 | `architecture.abstraction.necessity` | New/changed interface, factory, strategy or generic abstraction; preserve real definitions and users. | Does it serve a current contract, boundary, side effect or meaningful variability? |
| R02 | `architecture.indirection.value` | New/changed forwarding method, adapter or delegation chain with little local behavior. | Does the intermediary own meaningful transformation, policy, integration or isolation? |
| R03 | `architecture.responsibility.cohesion` | Changed type responsibilities, related operations/state or a changed responsibility cluster. | Does the type have a coherent responsibility, or mix separable owners? |
| R04 | `architecture.boundary.ownership` | Changed cross-component call, dependency on another component internal, or altered public boundary. | Does the operation use the component/public contract that owns the behavior and authority? |
| R05 | `architecture.authority.uniqueness` | Related configuration, version or materialized/derived state changed together. | Is a derived value incorrectly treated as an independently authoritative choice? |
| R06 | `architecture.lifecycle.binding` | Changed service construction, DI registration, factory, request/run/session state or lifetime. | Are construction-time dependencies and invocation-specific or resource state bound to the intended lifetime? |
| R07 | `architecture.mechanism.explicitness` | Changed reflection/dynamic discovery, service locator, convention dispatcher, DSL or general framework mechanism. | Is the implicit machinery justified by a current need that simpler explicit C# cannot meet? |
| R08 | `behavior.error.contract` | Changed catch, result conversion, fallback, retry or exception mapping. | Can a failure be misrepresented as success, denial or a different business outcome? |
| R09 | `behavior.validation.boundary` | Changed endpoint/serializer/DTO/nullability/validation or untrusted-data entry. | Is the actual invariant enforced where untrusted data enters without redundant interior checks or unjustified assumptions? |
| R10 | `behavior.state.consistency` | Changed asynchronous request/callback, mutable state, cache, selection or context transition. | Could stale results or state updates apply to the wrong active context? |
| R11 | `behavior.resource.ownership` | Changed allocation, streaming, event subscription, cancellation or disposal. | Is the resource owner responsible for valid cleanup and propagation across lifecycle paths? |
| R12 | `tests.assertion.effectiveness` | Changed compiler-resolved test method with assertions or observable outcome checks. | Would its assertions detect a material contract violation, not merely non-throwing execution? |
| R13 | `tests.change-contract.coverage` | Changed production behavior/signature/contract with associated test evidence, or changed tests around a contract. | Do available tests protect the materially changed behavior and relevant failure paths? |
| R14 | `tests.isolation.coupling` | Changed tests introducing/expanding doubles, setup infrastructure or implementation-specific assertions. | Does the test introduce brittle implementation coupling or unnecessary production abstractions solely for testing? |
| R15 | `docs.summary.quality.review` | Compiler-resolved API summary carriers with nonempty explicit prose (including positional-record property carriers). | Is the summary technically faithful, informative and clear for this contract? |
| R16 | `docs.summary.language.german.review` | Same summary-carrier population when the independently toggleable German policy is enabled. | Is the prose natural, understandable German rather than superficial translation? |
| R17 | `comments.intent.review` | Ordinary active-source comment groups (not XML docs/directives/disabled text/string literals). | Does the comment convey accurate, useful rationale or meaning without narrating syntax? |

#### Subject boundaries and legitimate cases

**R01 — `architecture.abstraction.necessity`.** Family: Architecture. *Subject:* The actual abstraction decision and the exposed contract, rather than an isolated type. *Legitimate counterexample:* One-implementation interface may legitimately isolate a persistence, transport or public contract. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R02 — `architecture.indirection.value`.** Family: Architecture. *Subject:* A relationship/chain showing caller, intermediary and callee. *Legitimate counterexample:* A cross-boundary adapter or authorization boundary can legitimately forward. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R03 — `architecture.responsibility.cohesion`.** Family: Architecture. *Subject:* One type with supporting collaborators and affected operations. *Legitimate counterexample:* Type size, member count and multiple methods are not defects. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R04 — `architecture.boundary.ownership`.** Family: Architecture. *Subject:* The consumer-to-provider relation and owners of each capability. *Legitimate counterexample:* A legitimate application composition root may coordinate several components. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R05 — `architecture.authority.uniqueness`.** Family: Architecture. *Subject:* The source-of-truth and derived/copy relationship. *Legitimate counterexample:* Caches, snapshots and versioned materializations can be correct if provenance and invalidation are explicit. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R06 — `architecture.lifecycle.binding`.** Family: Architecture. *Subject:* A construction/operation lifetime relationship with consumers and ownership context. *Legitimate counterexample:* A truly per-run processor may legitimately capture run state in its constructor. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R07 — `architecture.mechanism.explicitness`.** Family: Architecture. *Subject:* The concrete mechanism plus the ordinary explicit alternative boundary. *Legitimate counterexample:* Framework binding, serialization, interoperability and stable plugin ecosystems can justify dynamic mechanisms. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R08 — `behavior.error.contract`.** Family: Behavior. *Subject:* One affected failure-to-outcome path across relevant calls. *Legitimate counterexample:* Intentional domain fallbacks and handled expected errors may be correct. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R09 — `behavior.validation.boundary`.** Family: Behavior. *Subject:* An input boundary plus the consumer invariant and effective validation mechanism. *Legitimate counterexample:* Compiler nullability alone is not runtime JSON validation; actual configured binders matter. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R10 — `behavior.state.consistency`.** Family: Behavior. *Subject:* A state owner and the relevant concurrent/context-changing operations. *Legitimate counterexample:* Sequential code or isolated concurrent operations need no extra cancellation machinery. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R11 — `behavior.resource.ownership`.** Family: Behavior. *Subject:* A resource acquisition-use-release path with success/failure/cancellation coverage. *Legitimate counterexample:* Borrowed resources must not necessarily be disposed by their consumer. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R12 — `tests.assertion.effectiveness`.** Family: Tests. *Subject:* A test operation and the behavior its assertions claim to protect. *Legitimate counterexample:* Test structure, mocks or assertion count alone are not a verdict. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R13 — `tests.change-contract.coverage`.** Family: Tests. *Subject:* A change-to-observable-contract relationship, with available tests and provenance of missing matches. *Legitimate counterexample:* No one-to-one test mapping is required; integration or contract tests may be the real evidence. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R14 — `tests.isolation.coupling`.** Family: Tests. *Subject:* The test and its double/fixture relationship with the production seam. *Legitimate counterexample:* A fake at a real external side-effect boundary may be appropriate. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R15 — `docs.summary.quality.review`.** Family: Documentation. *Subject:* The documented symbol, summary carrier and relevant declaration. *Legitimate counterexample:* A concise summary can be correct; mechanical grammar or length scoring is not authorized. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R16 — `docs.summary.language.german.review`.** Family: Documentation. *Subject:* The documented symbol and summary carrier. *Legitimate counterexample:* The hygiene repository may disable this rule; no universal German requirement. The detector must establish the named source-backed indicator and context, not the subjective answer.

**R17 — `comments.intent.review`.** Family: Documentation. *Subject:* A comment block and its actual surrounding code or owner. *Legitimate counterexample:* Legal, administrative and other valid non-code explanatory comments remain eligible. The detector must establish the named source-backed indicator and context, not the subjective answer.

Compiler-backed discovery is mandatory where available (especially declared symbol identity, test attribute inheritance, Roslyn trivia, actual cross-project references). A relation detector must avoid claiming complete call-graph or DI reachability when reflection, dynamic binding, unavailable projects or external packages prevent certainty. In that case produce only grounded evidence and explicit unknown context or omit an unsupported inferred relation; do not manufacture a defect verdict. Duplicate candidates may share a source path but have independent rule identities.

### 4. F1 — source-backed relationship/behavior identity

Each review obligation has an exact rule identity, project-qualified stable subject key, one **primary anchor in the requested reporting scope** (file+line+symbol/span where available), and zero or more related anchors anywhere in readable project context. A cross-file relation's primary anchor is the changed/requested participating side, not an arbitrary unchanged hidden owner. If only an out-of-scope primary could represent the case, do not emit it as an in-scope ordinary finding; report any in-scope related aspect truthfully. Cross-project, shared-file, generic, nested and partial declarations must not collide or multiply obligations accidentally. Distinct rule-subject decisions remain distinct even if they share an anchor.

### 5. F2 — bounded, explicit multi-file evidence

A selected obligation includes relevant source excerpts plus their exact project/path/symbol/span origins, the concrete mechanically observed signal, the rule's question, and what contextual evidence was inspected. Maximum normal payload per obligation: **8 source excerpts and 24,000 UTF-8 text bytes** (including metadata text; UTF-8 byte bound), excluding a small fixed-size omission summary. If required evidence exceeds those bounds or an external/dynamic relation is unresolved, set `contextStatus=partial|unavailable` with identifiable missing anchors/reason and recommend further inspection; never silently truncate into apparent completeness. An external reviewer may seek missing repository context before an explicit acceptance; the tool must not infer acceptance. Avoid persisting raw source content in accepted-observation state.

### 6. F3 — one bounded general semantic budget

- The 17 general rules together select **at most ten rule-subject judgment obligations per ordinary gate**. Zero or fewer than ten is correct when few eligible subjects exist. A presentation group may contain several obligations but **each distinct (rule, subject)** costs one slot; grouping cannot evade the cap or imply shared approval.
- Changed-anchor obligations rank ahead of context-affected obligations. Context-affected obligations require a concrete changed *related* source anchor; at most **two** consume the ten-slot ordinary budget. Unrelated background never fills ordinary gates. There is no per-rule quota.
- Within each stratum (direct, then related-context), select by deterministic rotating round-robin across nonempty enabled rule IDs using the gate/version/fingerprint identity; deterministic ranking within each rule. Explicit-path/`--review-all` inspection uses the same ten-obligation general cap without the two-slot context reserve. A bare full check and a source-clean gate schedule zero general semantic review as today.
- Acceptances only suppress unchanged *same-rule, same-subject, same-relevant-evidence* observations. Partial acceptance never replenishes the same gate's selected slots. All rule candidates and unselected remainders stay discoverable through explicit expansion and authorized full audit.
- **Modernization stays separate:** the nine modernization families retain their existing aggregate **five review packages**, bounded per-occurrence detail, narrow source exceptions, and no new verdict persistence. None of those consumes a general semantic slot.

### 7. F4 — two-level discovery and lazy enrichment without an engine rewrite

Normal source change processing must discover the complete eligible deterministic candidate identities for relevant enabled rule populations, with cheap syntactic/symbol checks and only facts necessary for eligibility. **Do not construct full multi-file handoff evidence for every unselected candidate.** After shared selection, enrich only chosen obligations; explicit expand/handoff and authorized exhaustive audit may deliberately enrich larger populations in bounded returned pages. Cached analysis facts remain immutable and command-scoped; duplicate expensive graph walks on the same source should be reused where justified, but a universal index/plugin/analysis pipeline is not authorized.

Separate the cheap **discovery identity/fingerprint** used for selection/population from the **review evidence fingerprint** used for acceptance. The latter incorporates material primary and related evidence so changes affecting a previously accepted relationship make that judgment stale; question/rule-version changes likewise invalidate it. Never claim an unchanged full-population semantic-quality snapshot from cheap anchor hashes alone. When necessary to identify whether a related source change makes a candidate context-affected, use bounded, deterministic cross-file anchors rather than eagerly building the final review prose for every candidate.

### 8. State, audit, public protocol and legacy cleanup

Version the affected API/serialized reports and accepted-observation structures as needed; pre-1.0 breakage is allowed. Keep explicit approval semantics, atomic state writes, stale/tampered ticket rejection, run-specific handles, full eligible population counts, truthful source/reporting scope, old-source edits preserved, and audit read-only/noninterfering. Do not silently accept a legacy `architecture.boringness.review` or `tests.quality.review` observation as acceptance of a new rule. Preserve old bytes if invalidating/resetting legacy state and present actionable cleanup/archival guidance; do not require automated migration. Delete obsolete code/docs/tests that claim the old semantics are still active. Preserve exhaustive audit attestation and snapshot caveats.

### 9. Evidence versus semantic model correctness

Software acceptance verifies Roslyn candidate eligibility and mechanical predicates, relationship anchors, output context and omissions, deterministic selection, state invalidation, formatter safety, CLI/audit and exact installed consumer. **Do not** write expected subjective correctness verdicts for synthetic test snippets or repeatedly call a model to get an 'accuracy' score. Compiler fixtures *are* appropriate for deterministic signal and work-order tests. A small real-repository report-only inspection may summarize actual discovered candidates/overlap/context/cost with revisions and uncertainty, without using LLM labels as correctness tests. Its observations cannot veto this fixed provisional catalog; a later planning decision may remove rules based on usefulness.

### 10. Workflow and documentation boundaries

The implementation may reorganize the source-backed review DTO/batches and CLI output to achieve the above fixed contract, but must retain a callable `dotnet hygiene`, rule-list/help and explicit review workflow, no embedded model, and current consumer availability. Update only directly contradicted operative specifications, user-facing help and README; track broader cleanup as a separate documentation sync. No new guide-system files, profiles or dependencies are necessary.

## Baseline Executor Readiness

`ready`: the fixed candidate portfolio, primary semantic questions, legitimate counterexamples, 10/5 budget policy, state safety, evidence bound, cross-file reporting, exclusions, provider boundary and validation topology are resolved above. Implementation owns algorithms, source types, test mechanics, versioned schema representations, work-package order and performance refinements within these contracts. If actual compiler/project capabilities disprove an assumed rule's *mechanical eligibility*, preserve the observed evidence and return the material rule-contract decision to planning rather than fabricating certainty.

## Decision Preservation

The planning chat and external guide are not required for implementation. The rule portfolio and negative requirements above are the operative source of truth for M0025 even where pre-M0025 specs still describe the old six independent budgets. The implementer must update those specs at execution to match the completed product. `docs/PROJECT-GUIDANCE.md` is planning authority and is *not* an ordinary implementation reading dependency beyond the constraints explicitly reproduced here.

## Execution Tractability

This large milestone is coherent because rule detectors and shared relation/context mechanics can be implemented in bounded work packages while a planning-seeded ledger preserves every acceptance/evidence obligation. Full validation is sharded/resumable; model-verdict benchmarking is excluded. The implementer owns work-package decomposition and closure, not project-level portfolio culling.

## Required Authority

- `AGENTS.md` — implementer routing, review boundaries and local commands.
- `docs/ARCHITECTURE.md` — shared lazy `RepositorySession`, independent in-process rule modules and reporting scope.
- `docs/ENGINEERING.md` — Windows/.NET 11 validation topology, current receipt/shard workflow and deterministic semantic-testing policy added with PR #27.
- `docs/SPECS.md`, `docs/specs/SEMANTIC-REVIEWS.md`, `docs/specs/SAMPLING.md`, `docs/specs/AUTONOMOUS-WORKFLOW.md`, `docs/specs/PROFILE.md`, `docs/specs/AGENT-HELP.md` — current contracts being intentionally superseded where M0025 says so.
- `docs/TERMINOLOGY.md`, `README.md` — directly affected definitions and public/agent-facing guidance.
- `docs/PROJECT-GUIDANCE.md` — **planning baseline authority in PR #27**; implementation-relevant decisions are also explicitly preserved in this milestone.

## Acceptance Criteria


- **AC-01** — `architecture.abstraction.necessity` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-02** — `architecture.indirection.value` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-03** — `architecture.responsibility.cohesion` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-04** — `architecture.boundary.ownership` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-05** — `architecture.authority.uniqueness` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-06** — `architecture.lifecycle.binding` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-07** — `architecture.mechanism.explicitness` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-08** — `behavior.error.contract` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-09** — `behavior.validation.boundary` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-10** — `behavior.state.consistency` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-11** — `behavior.resource.ownership` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-12** — `tests.assertion.effectiveness` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-13** — `tests.change-contract.coverage` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-14** — `tests.isolation.coupling` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-15** — `docs.summary.quality.review` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-16** — `docs.summary.language.german.review` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-17** — `comments.intent.review` is an independently toggleable executable semantic review rule with its declared candidate population, subject, focused question, legitimate-case boundary, source evidence, and no automatic semantic verdict.
- **AC-18** — The legacy omnibus `architecture.boringness.review` and superseded `readability.type-responsibility.review` and `tests.quality.review` implementations are removed/replaced; unaffected mechanical, comment/documentation and nine modernization contracts retain their intended behavior without accidental duplicate review work.
- **AC-19** — Review subjects can represent a stable source-backed relation or behavior spanning more than one C# declaration, including project-qualified identities, one in-scope primary anchor and related source anchors; cross-project, nested, partial and unresolved-reference cases are represented truthfully.
- **AC-20** — A selected semantic work item carries bounded multi-file excerpts, the relevant concrete relationship, rule question, provenance and explicit available/incomplete context status; omitted evidence is identified rather than silently replaced with guessed data.
- **AC-21** — The 17 general semantic rules share a deterministic gate-local ceiling of ten *individually judged rule-subject obligations*, including when items are grouped for display; there is no per-rule five-item multiplication, forced quota or silent dropping of distinct rule obligations.
- **AC-22** — Normal gate admission prioritizes changed source-backed decisions, then genuine affected context; unrelated background is not admitted; normal no-change/no-eligible gates select zero; explicit paths, `--review-all` and independent full audit follow their declared boundaries.
- **AC-23** — Candidate discovery is mechanically complete for each enabled rule within its eligible reporting scope without eagerly enriching all unselected candidates; expensive relationship/source evidence is assembled lazily on selection, expansion or audit and reused as command-local facts.
- **AC-24** — Selected-item and accepted-observation identities/fingerprints invalidate when material primary or related evidence changes, including question/rule contract changes; stale, malformed or forged acceptance cannot silently mark new work acceptable.
- **AC-25** — `check`, autonomous grouped work order, rule enable/disable, `review expand`/`handoff`/`accept` and the explicit read-only audit expose truthful per-rule counts, selected obligations, unknown/truncated context, suppressed observations and unreviewed remainder without silently changing normal acceptance state.
- **AC-26** — The modernization nine-family five-package budget, SDK formatter/profile v3, source-local modernization exceptions, safe Git checkpoint execution, mandatory deterministic findings and read-only audit authorization remain semantically intact.
- **AC-27** — The obsolete experimental rule identities and acceptance state are not treated as 1.0 compatibility obligations or imported into new semantic approvals; old persisted bytes are never silently destroyed, and any incompatible state/reset is reported with actionable steps.
- **AC-28** — The current package version and documented CLI contracts are coherent (target 0.12.0), and the exact newly packed locally installed `dotnet hygiene` tool exercises representative relationship review, shared budgeting, acceptance and audit in an isolated consumer without depending on source-runner paths.
- **AC-29** — Deterministic regression and integration evidence establishes the 17 candidate/review-work contracts and framework semantics without treating synthetic good/bad source labels, repeat LLM grades or subjective precision scores as acceptance proofs.
- **AC-30** — A bounded report-only inspection of actual authorized repository changes records candidate incidence, cross-rule overlap, available/incomplete context and runtime characteristics with source revision provenance; it neither exposes private source nor asserts semantic defects or measured model accuracy.

## Completion Obligations

- **DOC-01** — The current operative architecture, sampling, semantic-review, rule capabilities, engineering, CLI/help and applicable README/index authority accurately describes the new portfolio and accepted state/reset behavior; obsolete descriptions are corrected without unrelated document synchronization.
- **REV-01** — The authorized independent M0025 completion reviewer evaluates the current implementation, deterministic evidence coverage, bounded review costs and safety, records an actual decision in `.review/REV-M0025-COMPLETION.md`, and the milestone review checker passes only after genuine approval.

## Acceptance Evidence Topology

Evidence cases distinguish genuinely separate execution paths. Each parent criterion without a listed case remains an independently provable evidence unit. Tests for candidate detection assert **mechanical** behavior, never a synthetic 'correct semantic verdict'.

| ID | Parent | Required evidence case | Why independent proof matters |
|---|---|---|---|
| EC-01a | M0025ArchitectureRuleTests: interface implementation and consumer symbol anchors; PartialAbstractionKeepsTheChangedDeclarationAndOneSymbolDecision: changed later partial remains primary. | Interfaces/implementations, including one implementation | complete |
| EC-01b | M0025ArchitectureRuleTests: factory return contract, resolved implementation and consumer anchors. | Factory/strategy abstraction discovery | complete |
| EC-02a | M0025ArchitectureRuleTests: compiler-resolved caller, intermediary and callee chain anchors. | Simple forwarding chain and alternate boundary | complete |
| EC-04a | M0025ArchitectureRuleTests cross-project provider-contract anchor; installed consumer cross-project review. | Cross-project external component access | complete |
| EC-05a | M0025ArchitectureRuleTests: direct RHS state provenance, with transitive-flow limitation. | Configuration copied versus derived provenance | complete |
| EC-06a | M0025ArchitectureRuleTests: constructor dependency type/source relationship anchor. | Construction-time dependency | complete |
| EC-06b | M0025ArchitectureRuleTests: resolved DI registration and invocation/run-state relationship evidence. | Invocation/run-specific state | complete |
| EC-07a | M0025ArchitectureRuleTests: dynamic/reflection candidate and explicit unresolved graph limitation. | Explicit dynamic mechanisms and absence of reliable graph | complete |
| EC-08a | M0025BehaviorRuleTests: catch/fallback and ?? throw candidates; ordinary ?? excluded. | Technical failure mapped to success/denial | complete |
| EC-09a | M0025BehaviorRuleTests: real ASP.NET HttpPost/FromBody and Required symbols, DTO relationship anchor, and explicit unknown effective runtime binder/options. | Effective HTTP serializer/binder boundary and null/missing inputs | complete |
| EC-10a | M0025BehaviorRuleTests: await count, resolved mutable state write/owner anchor, and scheduler limitation. | Stale async completion after context change | complete |
| EC-11a | M0025BehaviorRuleTests: event subscription and same-owner acquisition/release anchors; arithmetic +=/-= excluded. | Acquisition/cleanup across exceptions and cancellation | complete |
| EC-12a | M0025TestDocumentationRuleTests: framework-resolved test methods with observable assertion and reference evidence. | Framework-resolved test with positive observable assertions | complete |
| EC-13a | M0025TestDocumentationRuleTests: exact changed-symbol test reference distinguished from sibling and containing-type-only references. | Production change with related tests | complete |
| EC-13b | M0025TestDocumentationRuleTests: changed production method with no direct test edge and explicit integration/contract coverage limitation. | Production change with no deterministically discovered related tests | complete |
| EC-14a | M0025TestDocumentationRuleTests: FakeStore compiler-resolved IStore seam anchor; recognized test-framework setup calls. | Fixture/double change with production seam | complete |
| EC-15a | M0025TestDocumentationRuleTests: ordinary API summary and positional-record property carrier. | Ordinary summary and positional-record `<param>` carrier | complete |
| EC-16a | LifecycleTests.PortfolioRulesRemainIndependentAndGermanPolicyCanBeDisabled: shared population and independent disable behavior. | Enabled and disabled German review participation | complete |
| EC-17a | M0025TestDocumentationRuleTests: ordinary comment candidates and exclusion of directives, disabled trivia, XML docs and literals. | Ordinary, leading/trailing and excluded trivia | complete |
| EC-19a | M0025ArchitectureRuleTests: changed-side primary anchor, implementation/consumer edges, later partial and project-qualified identity. | In-project relation and stable primary reporting anchor | complete |
| EC-19b | M0025ArchitectureRuleTests and installed consumer: cross-project provider edge; unresolved/dynamic paths remain explicit limitations. | Cross-project relation with partial or unresolved references | complete |
| EC-20a | M0025SharedSemanticBudgetTests.MissingPrimarySourceIsUnavailableAndNecessaryMethodBodyIsIncluded plus selected related-source package. | Complete bounded context package | complete |
| EC-20b | M0025SharedSemanticBudgetTests: full serialized UTF-8 bound, Unicode, many anchors, eight-excerpt cap, missing and truncation metadata. | Oversized or incomplete evidence package | complete |
| EC-21a | M0025SharedSemanticBudgetTests: duplicate path relationships remain independent rule-subject obligations. | Multiple rules hit the same source relationship | complete |
| EC-21b | M0025SharedSemanticBudgetTests.SharedBudgetCountsEachRuleSubjectAndDoesNotRefillAfterPartialAcceptance: ten-slot ceiling and stable rotation. | More than ten eligible decisions across families | complete |
| EC-22a | M0025SharedSemanticBudgetTests.RelatedContextIsLimitedToTwoAndComesAfterChangedSubjects plus closed/zero-work analysis counter. | Changed versus context-affected subject, plus zero-work gate | complete |
| EC-22b | LifecycleTests.CheckBaseAndReviewAllAreExplicitGateModesWithProcessErrors and explicit-path CLI assertions. | Explicit paths and `--review-all` | complete |
| EC-23a | M0025ArchitectureRuleTests.ClosedAndZeroWorkGatesSkipRelationshipEnrichmentUntilWorkIsSelected: candidate discovery, zero selection, zero relationship scans. | No selected candidates and bounded ordinary gate | complete |
| EC-23b | LifecycleTests expansion paging and exhaustive-audit tests; explicit expansion selects/enriches and audit preserves normal state. | Expansion and exhaustive audit | complete |
| EC-24a | M0025RelationshipAcceptanceTests.AcceptedProductionTestRelationshipInvalidatesWhenLateAssertionChanges and related-method edit regression. | Related evidence edit invalidates prior accepted review | complete |
| EC-24b | M0025SharedSemanticBudgetTests partial acceptance/no refill; current-ticket tamper and stale-ticket CLI cases. | Same unchanged gate, partial acceptance and tampered ticket | complete |
| EC-25a | Core/CLI process coverage for check, accept, expand and handoff; exact installed-consumer relationship and budget scenarios. | Autonomous and manual CLI outputs | complete |
| EC-25b | LifecycleTests/CLI exhaustive-audit authorization, resume, and normal accepted-state isolation cases. | Read-only audit and normal accepted-state isolation | complete |
| EC-26a | Core and CLI SDK formatter/profile-v3, modernization population and source-local exception cases. | SDK/modernization normal behavior and source exceptions | complete |
| EC-26b | CLI Git checkpoint and authenticated read-only exhaustive audit process scenarios. | Git checkpoint and audit authorization/read-only invariant | complete |
| EC-27a | LifecycleTests legacy-state preservation/refusal plus installed v2-to-v3 migration; no retired acceptance translation. | Old persisted accepted-review records vs new rule names | complete |
| EC-28a | Exact 0.12.0 package in local NuGet feed and isolated dotnet-hygiene consumer installation. | Packed tool installation and ordinary CLI review | complete |
| EC-28b | Installed-consumer audit/acceptance failure and protected-source/state hash checks. | Installed audit and acceptance failure exits | complete |
| EC-29a | Compiled M0025 architecture, behavior, test/docs, evidence, acceptance, identity and budget fixtures. | Mechanical fixtures exercise eligibility and graph correctness | complete |
| EC-29b | Default deterministic suite contains no synthetic semantic-good/bad labels, LLM verdicts or consensus thresholds. | No synthetic labeled LLM verdict tests in default validation | complete |
| EC-30a | Current implementation read-only in-process census on both source SHAs; counts, overlap, context sufficiency, timing and provenance recorded below. | At least two source-bearing historical changes with actual source provenance | complete |

## Validation

Use project-approved Windows 11 with .NET 11 SDK, Git and PowerShell. Normal execution can restore existing declared packages and create isolated, user-scoped temporary test repos/feed/tools; persistent host provisioning, CI/workflows and shared infrastructure modification are **not** authorized. If the required Windows/.NET/installed consumer target is unavailable and no authorized equivalent locus exists, mark the affected gate **BLOCKED**; Linux-only source tests do not replace it.

Follow `./eng/validate.ps1 -PlanJson` for current stage IDs. During implementation run the cheapest relevant focused Core/CLI checks and do not issue a complete aggregate qualification while substantial affected implementation is unfinished. Some independent stages may run earlier once their own dependencies stabilize. At the final candidate, run missing/stale required stages and reuse only fingerprint-proven current stage receipts, then verify the aggregate. Cases cited below must be proved by actual focused/consumer scenarios; a stage receipt is never permission to infer sibling evidence that was not executed.

| ID | Depth / target | Required locus/capabilities | Concrete command/check and execution trigger | Proves | Evidence |
|---|---|---|---|---|---|
| VAL-01 | Tier 1 — 17 rule detectors and deterministic payload fidelity | Windows 11 / .NET 11 / Roslyn Core, isolated SDK-style C# fixtures | Focused Core TUnit/MTP filters for new rule modules during development; current `./eng/validate.ps1 -Shard core-tests` at stable final candidate | AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07, AC-08, AC-09, AC-10, AC-11, AC-12, AC-13, AC-14, AC-15, AC-16, AC-17, EC-01a, EC-01b, EC-02a, EC-04a, EC-05a, EC-06a, EC-06b, EC-07a, EC-08a, EC-09a, EC-10a, EC-11a, EC-12a, EC-13a, EC-13b, EC-14a, EC-15a, EC-16a, EC-17a, AC-29, EC-29a, EC-29b | Per-rule compiler predicate identities, question/context, exclusions, recorded test names and actual current Core receipt; no model-grade assertions. |
| VAL-02 | Tier 1/3 — relationships, bounded context, lazy work | Windows 11 / .NET 11 / Git, multi-project SDK fixtures | Real Core/CLI project-aware tests: in-project, cross-project/partial, context truncation/unknown, selected/no-selected and expand/audit; run focused during editing, current affected Core/CLI shards at closure | AC-19, AC-20, AC-23; EC-19a, EC-19b, EC-20a, EC-20b, EC-23a, EC-23b | Exact source anchors, context lengths/status, lazy fact instrumentation and absence of invented relation evidence. |
| VAL-03 | Tier 1/3 — gate selection, accepted state and legacy cleanup | Windows 11 / .NET 11 / Git / isolated repositories | Focused Core/CLI gate/acceptance state tests for changed and context-only, >10 cross-rule candidates, same-source distinct obligations, zero-work, tampered/stale tickets, old state; canonical current Core/CLI shards at closure | AC-18, AC-21, AC-22, AC-24, AC-27; EC-21a, EC-21b, EC-22a, EC-22b, EC-24a, EC-24b, EC-27a | Counted selected obligations (not presentation groups), deterministic ordering, stale rejection, no backfill, old bytes preserved and user-facing migration action. |
| VAL-04 | Tier 1/3 — CLI, audit and non-regression | Windows 11 / .NET 11 / Git / PowerShell, existing SDK/profile test substrates | Actual CLI `dotnet hygiene check`, no-arg work order, rules/help, review expand/handoff/accept and authenticated `--yolo-all` tests; compare modernization SDK/profile and Git safety regressions | AC-25, AC-26, DOC-01; EC-25a, EC-25b, EC-26a, EC-26b | Reported selected/unselected obligations, exhaustive audit entries, accurate exit semantics, byte-equal audit source/state, unchanged SDK formatting and modernizations. |
| VAL-05 | Tier 2 — final repository validation | Windows 11 / .NET 11 / Git / PowerShell; existing canonical validator | Discover `./eng/validate.ps1 -PlanJson`; run current required shards (solution, Core, CLI, README policy, self-host and others as declared) when affected surfaces are stable; verify `./eng/validate.ps1 -Verify` after all mandatory stages are current | AC-28, AC-29, DOC-01 (aggregate code/test/document receipts only; individual rule cases remain VAL-01–04) | Current fingerprint-proven receipts; exact AC/EC trace remains independent of an aggregate green summary. |
| VAL-06 | Tier 4 — exact locally packed and installed tool | Windows 11 / .NET 11 / Git / PowerShell / isolated local NuGet feed+tool path | `./eng/validate.ps1 -Shard pack-current`, then `./eng/validate.ps1 -Shard installed-consumers`; execute representative changed-source/relation work, ten-budget, legacy state refusal, acceptance, error and audit against the *newly packed installed shim* | AC-25, AC-26, AC-27, AC-28; EC-25a, EC-25b, EC-27a, EC-28a, EC-28b | Package and shim hashes, actual installed commands, source/state snapshots and final current consumer receipt. |
| VAL-07 | Tier 3 investigative observation — real repository candidate census | Authorized local Git clones of hygiene + other existing authorized local .NET repo source revisions where available; no credential provisioning | Use a bounded representative `dotnet hygiene check --base <trusted-ref>`/explicit-path invocation on at least two recorded source-bearing revisions using the actual CLI when already initialized/approved, or via a read-only in-process candidate-census path on disposable copies without bypassing baseline approval; record only rule counts/overlap/context status/timing and source SHAs; no model grades or correctness thresholds | AC-30; EC-30a | Report-only non-authoritative trace, no private source contents/identifiers in public output; no asserted semantic defect yield. |
| VAL-08 | Tier 5 — independent M0025 human review | Authorized human/delegate using current PR, milestone, ledger, recorded current receipts and installed tool | Reviewer records actual approved/changes-requested decision in `.review/REV-M0025-COMPLETION.md`; then `./eng/review-check.ps1 -Milestone M0025` | REV-01 | Independent signed/identified reviewer decision with date/scope and checker status; pending => `AWAITING HUMAN REVIEW`. |

The consumer test must invoke the installed tool from the **exact current 0.12.0 package** produced by this branch, not a globally installed binary or direct `dotnet run`. Project validation-specific commands that are absent or materially changed must be reconciled against the canonical `-PlanJson` output; no invented substitute may count as passing. All validation results and file identities are implementation-owned ledger evidence, not prefilled by planning.

## Human Review

- **Applicability:** blocking independent milestone completion review.
- **Canonical review ID:** `REV-M0025-COMPLETION`.
- **Review record:** `.review/REV-M0025-COMPLETION.md`, initially pending; not preapproved by planning.
- **Reviewer:** project owner or authorized independent delegate; implementation agent cannot self-approve.
- **Review subject:** correctness and usability of 17 focused rule boundaries, relationship/context fidelity, ten/five budget safety, legacy-state handling, honest partial context, deterministic test separation, runtime/consumer evidence and direct documentation.
- **Required evidence:** final PR/diff and commit identity, AC/EC reconciliation, current validator receipts, exact installed package provenance, real-repository report-only observations and any unresolved/uncertain material contracts.
- **Allowed decision:** approved or changes-requested by an actual authorized reviewer; pending remains blocking. No implicit waiver.
- **Checker:** `./eng/review-check.ps1 -Milestone M0025` after actual decision; report blocked if unavailable rather than faking it.

## Documentation Policy

Directly update `docs/ARCHITECTURE.md`, `docs/SPECS.md`, `docs/specs/SEMANTIC-REVIEWS.md`, `docs/specs/SAMPLING.md`, affected `RULE-CAPABILITIES.md`, `AUTONOMOUS-WORKFLOW.md`, `AGENT-HELP.md`, `docs/ENGINEERING.md`, terminology, CLI help, README and milestones index only where runtime/policy changes make them inaccurate. The new `docs/PROJECT-GUIDANCE.md` already exists in the baseline and is planning authority; the implementation may reference it but need not reread non-authoritative research. Do not rewrite unrelated research, past milestone history, external guide, or all public docs as a general sync pass. If a broad follow-up is genuinely needed, preserve a focused deferred-sync hint.

## Completion Expectations

The implementer reads this milestone and the seeded ledger, verifies exact ID sets, creates its own bounded work packages, implements with focused feedback, captures AC/EC and stage evidence, rereads this milestone at finalization, reconciles every obligation and evidence case against live code/evidence, performs the completion audit, and writes concise durable completion evidence below. Green aggregate tests are not completion. The expected terminal execution outcome is **`AWAITING HUMAN REVIEW`** until actual approval and review-check; otherwise `BLOCKED` for an unavailable required external prerequisite or material planning decision.

## Completion Evidence

*Implementation-owned evidence recorded below after final reconciliation. The independent review decision remains pending.*

| Obligation / evidence case | Concrete implementation evidence | Gate/target | Result |
|---|---|---|---|
| AC-01 | M0025ArchitectureRuleTests.ArchitectureRulesDiscoverTheirMechanicalSignalsIndependently; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-02 | M0025ArchitectureRuleTests.ArchitectureRulesDiscoverTheirMechanicalSignalsIndependently; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-03 | M0025ArchitectureRuleTests.ArchitectureRulesDiscoverTheirMechanicalSignalsIndependently; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-04 | M0025ArchitectureRuleTests.ArchitectureRulesDiscoverTheirMechanicalSignalsIndependently; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-05 | M0025ArchitectureRuleTests.ArchitectureRulesDiscoverTheirMechanicalSignalsIndependently; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-06 | M0025ArchitectureRuleTests.ArchitectureRulesDiscoverTheirMechanicalSignalsIndependently; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-07 | M0025ArchitectureRuleTests.ArchitectureRulesDiscoverTheirMechanicalSignalsIndependently; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-08 | M0025BehaviorRuleTests.BehaviorRulesExposeMechanicalChangedSourceSubjectsAndSingleQuestions; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-09 | M0025BehaviorRuleTests.BehaviorRulesExposeMechanicalChangedSourceSubjectsAndSingleQuestions; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-10 | M0025BehaviorRuleTests.BehaviorRulesExposeMechanicalChangedSourceSubjectsAndSingleQuestions; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-11 | M0025BehaviorRuleTests.BehaviorRulesExposeMechanicalChangedSourceSubjectsAndSingleQuestions; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-12 | M0025TestDocumentationRuleTests.TestAndDocumentationRulesExposeTheirMechanicalPopulations; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-13 | M0025TestDocumentationRuleTests.TestAndDocumentationRulesExposeTheirMechanicalPopulations; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-14 | M0025TestDocumentationRuleTests.TestAndDocumentationRulesExposeTheirMechanicalPopulations; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-15 | M0025TestDocumentationRuleTests.TestAndDocumentationRulesExposeTheirMechanicalPopulations; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-16 | M0025TestDocumentationRuleTests.TestAndDocumentationRulesExposeTheirMechanicalPopulations; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-17 | M0025TestDocumentationRuleTests.TestAndDocumentationRulesExposeTheirMechanicalPopulations; corresponding rule module; VAL-01. | VAL-01–06 | complete |
| AC-18 | Legacy modules removed; CLI rules catalog and M0024V2MigrationReportsFormerMechanicalDisablesAndPreservesSemanticDisables tests; VAL-03. | VAL-01–06 | complete |
| AC-19 | Relationship anchors and BoundaryOwnershipReview; M0025ArchitectureRuleTests cross-project fixture; installed-consumer cross-project scenario; VAL-02. | VAL-01–06 | complete |
| AC-20 | Bounded ReviewEvidenceBuilder; SelectedEvidenceIsBoundedAndReportsMissingOrTruncatedAnchors; DurableReviewHandoffEmbedsOnlyCurrentRelevantSourcesAndIsAtomic; VAL-02. | VAL-01–06 | complete |
| AC-21 | Ten-slot GeneralSemanticBudget; SharedBudgetCountsEachRuleSubjectAndDoesNotRefillAfterPartialAcceptance; VAL-03. | VAL-01–06 | complete |
| AC-22 | RelatedContextIsLimitedToTwoAndComesAfterChangedSubjects; ClosedGateSelectsNothingAndBroadInspectionIncludesUnchangedSubjects; base/review-all CLI tests; VAL-03. | VAL-01–06 | complete |
| AC-23 | SessionFactsAreLazyAndSharedWithinTheSession; SessionLoadsProjectAndCompilationOnceForMultipleConsumers; expansion/audit tests; VAL-02. | VAL-01–06 | complete |
| AC-24 | ChangedMergeBaseRejectsOldExpansionHandoffAndAcceptanceWithSamePopulation; relationship fingerprint and stale-ticket tests; VAL-03. | VAL-01–06 | complete |
| AC-25 | CLI help, catalog, check, accept, expand, handoff and audit process tests; installed consumer scenarios; VAL-04/06. | VAL-01–06 | complete |
| AC-26 | Roslyn modernization/profile/source-exception tests and read-only audit tests; installed consumer; VAL-04/06. | VAL-01–06 | complete |
| AC-27 | M0024V2MigrationReportsFormerMechanicalDisablesAndPreservesSemanticDisables; retired-state refusal and byte-preservation tests; VAL-03/06. | VAL-01–06 | complete |
| AC-28 | 0.12.0 local package and isolated installed dotnet-hygiene consumer; package/shim hashes in provenance; VAL-06. | VAL-01–06 | complete |
| AC-29 | Compiled M0025 rule, relation and budget fixtures; no synthetic model-grade expectations; VAL-01/03. | VAL-01–06 | complete |
| AC-30 | Read-only in-process census on source-bearing commits f128195ee4e99590c47e31939e0aed932613d679 and 400f140b6764c560fe9f4d96437d8b8a6b73243d; VAL-07. | VAL-07 | complete |
| DOC-01 | README, architecture, engineering, terminology, operative specs, CLI help reconciled; README policy and self-host receipts. | VAL-04/05 | complete |
| REV-01 | Pending authorized independent reviewer decision; reviewer evidence not self-authored. | VAL-08 | pending independent review |
| EC-01a | M0025ArchitectureRuleTests: interface implementation and consumer symbol anchors; PartialAbstractionKeepsTheChangedDeclarationAndOneSymbolDecision: changed later partial remains primary. | VAL-01–07 | complete |
| EC-01b | M0025ArchitectureRuleTests: factory return contract, resolved implementation and consumer anchors. | VAL-01–07 | complete |
| EC-02a | M0025ArchitectureRuleTests: compiler-resolved caller, intermediary and callee chain anchors. | VAL-01–07 | complete |
| EC-04a | M0025ArchitectureRuleTests cross-project provider-contract anchor; installed consumer cross-project review. | VAL-01–07 | complete |
| EC-05a | M0025ArchitectureRuleTests: direct RHS state provenance, with transitive-flow limitation. | VAL-01–07 | complete |
| EC-06a | M0025ArchitectureRuleTests: constructor dependency type/source relationship anchor. | VAL-01–07 | complete |
| EC-06b | M0025ArchitectureRuleTests: resolved DI registration and invocation/run-state relationship evidence. | VAL-01–07 | complete |
| EC-07a | M0025ArchitectureRuleTests: dynamic/reflection candidate and explicit unresolved graph limitation. | VAL-01–07 | complete |
| EC-08a | M0025BehaviorRuleTests: catch/fallback and ?? throw candidates; ordinary ?? excluded. | VAL-01–07 | complete |
| EC-09a | M0025BehaviorRuleTests: real ASP.NET HttpPost/FromBody and Required symbols, DTO relationship anchor, and explicit unknown effective runtime binder/options. | VAL-01–07 | complete |
| EC-10a | M0025BehaviorRuleTests: await count, resolved mutable state write/owner anchor, and scheduler limitation. | VAL-01–07 | complete |
| EC-11a | M0025BehaviorRuleTests: event subscription and same-owner acquisition/release anchors; arithmetic +=/-= excluded. | VAL-01–07 | complete |
| EC-12a | M0025TestDocumentationRuleTests: framework-resolved test methods with observable assertion and reference evidence. | VAL-01–07 | complete |
| EC-13a | M0025TestDocumentationRuleTests: exact changed-symbol test reference distinguished from sibling and containing-type-only references. | VAL-01–07 | complete |
| EC-13b | M0025TestDocumentationRuleTests: changed production method with no direct test edge and explicit integration/contract coverage limitation. | VAL-01–07 | complete |
| EC-14a | M0025TestDocumentationRuleTests: FakeStore compiler-resolved IStore seam anchor; recognized test-framework setup calls. | VAL-01–07 | complete |
| EC-15a | M0025TestDocumentationRuleTests: ordinary API summary and positional-record property carrier. | VAL-01–07 | complete |
| EC-16a | LifecycleTests.PortfolioRulesRemainIndependentAndGermanPolicyCanBeDisabled: shared population and independent disable behavior. | VAL-01–07 | complete |
| EC-17a | M0025TestDocumentationRuleTests: ordinary comment candidates and exclusion of directives, disabled trivia, XML docs and literals. | VAL-01–07 | complete |
| EC-19a | M0025ArchitectureRuleTests: changed-side primary anchor, implementation/consumer edges, later partial and project-qualified identity. | VAL-01–07 | complete |
| EC-19b | M0025ArchitectureRuleTests and installed consumer: cross-project provider edge; unresolved/dynamic paths remain explicit limitations. | VAL-01–07 | complete |
| EC-20a | M0025SharedSemanticBudgetTests.MissingPrimarySourceIsUnavailableAndNecessaryMethodBodyIsIncluded plus selected related-source package. | VAL-01–07 | complete |
| EC-20b | M0025SharedSemanticBudgetTests: full serialized UTF-8 bound, Unicode, many anchors, eight-excerpt cap, missing and truncation metadata. | VAL-01–07 | complete |
| EC-21a | M0025SharedSemanticBudgetTests: duplicate path relationships remain independent rule-subject obligations. | VAL-01–07 | complete |
| EC-21b | M0025SharedSemanticBudgetTests.SharedBudgetCountsEachRuleSubjectAndDoesNotRefillAfterPartialAcceptance: ten-slot ceiling and stable rotation. | VAL-01–07 | complete |
| EC-22a | M0025SharedSemanticBudgetTests.RelatedContextIsLimitedToTwoAndComesAfterChangedSubjects plus closed/zero-work analysis counter. | VAL-01–07 | complete |
| EC-22b | LifecycleTests.CheckBaseAndReviewAllAreExplicitGateModesWithProcessErrors and explicit-path CLI assertions. | VAL-01–07 | complete |
| EC-23a | M0025ArchitectureRuleTests.ClosedAndZeroWorkGatesSkipRelationshipEnrichmentUntilWorkIsSelected: candidate discovery, zero selection, zero relationship scans. | VAL-01–07 | complete |
| EC-23b | LifecycleTests expansion paging and exhaustive-audit tests; explicit expansion selects/enriches and audit preserves normal state. | VAL-01–07 | complete |
| EC-24a | M0025RelationshipAcceptanceTests.AcceptedProductionTestRelationshipInvalidatesWhenLateAssertionChanges and related-method edit regression. | VAL-01–07 | complete |
| EC-24b | M0025SharedSemanticBudgetTests partial acceptance/no refill; current-ticket tamper and stale-ticket CLI cases. | VAL-01–07 | complete |
| EC-25a | Core/CLI process coverage for check, accept, expand and handoff; exact installed-consumer relationship and budget scenarios. | VAL-01–07 | complete |
| EC-25b | LifecycleTests/CLI exhaustive-audit authorization, resume, and normal accepted-state isolation cases. | VAL-01–07 | complete |
| EC-26a | Core and CLI SDK formatter/profile-v3, modernization population and source-local exception cases. | VAL-01–07 | complete |
| EC-26b | CLI Git checkpoint and authenticated read-only exhaustive audit process scenarios. | VAL-01–07 | complete |
| EC-27a | LifecycleTests legacy-state preservation/refusal plus installed v2-to-v3 migration; no retired acceptance translation. | VAL-01–07 | complete |
| EC-28a | Exact 0.12.0 package in local NuGet feed and isolated dotnet-hygiene consumer installation. | VAL-01–07 | complete |
| EC-28b | Installed-consumer audit/acceptance failure and protected-source/state hash checks. | VAL-01–07 | complete |
| EC-29a | Compiled M0025 architecture, behavior, test/docs, evidence, acceptance, identity and budget fixtures. | VAL-01–07 | complete |
| EC-29b | Default deterministic suite contains no synthetic semantic-good/bad labels, LLM verdicts or consensus thresholds. | VAL-01–07 | complete |
| EC-30a | Current implementation read-only in-process census on both source SHAs; counts, overlap, context sufficiency, timing and provenance recorded below. | VAL-01–07 | complete |
| VAL-01 | Current core-tests receipt: 111/111 passed on Windows/.NET 11.0.100-rc.3.26522.148, 300.17 s. | Core rule suite | complete |
| VAL-02 | Current Core/CLI receipts: cross-project relations, bounded evidence, lazy work and page expansion passed. | Core/CLI | complete |
| VAL-03 | Current Core/CLI receipts: shared budget, gate priority, acceptance, invalidation and legacy-state scenarios passed. | Core/CLI | complete |
| VAL-04 | Current CLI 36/36; README policy receipt reused as current; repository self-host passed in 205.688 s. | CLI/repository | complete |
| VAL-05 | Canonical ./eng/validate.ps1 -Verify passed; all seven fingerprint-matched receipts reused, zero additional executions. | aggregate | complete |
| VAL-06 | Exact local 0.12.0 package SHA-256 d6dfbe0ade48bc080c2920c68bcea357cd056e618495e4a45b15facebff64532; installed shim SHA-256 4e1f1df3d5c811e751a9be0ead0419f6c8cbbb8640b1dd851513bf454e61f6da; installed-consumer receipt passed. | installed consumer | complete |
| VAL-07 | Read-only census on f128195ee4e99590c47e31939e0aed932613d679 and 400f140b6764c560fe9f4d96437d8b8a6b73243d: 1,415/1,605 candidates, 75/88 multi-rule overlaps, 6/10 selected evidence packages available, 149.333/151.607 s; detail and limitations in ledger. | real source snapshots | complete |
| VAL-08 | Wait for authorized independent reviewer, then run ./eng/review-check.ps1 -Milestone M0025. | human review | pending |

## Escalation Boundary

Implementation owns source/test mechanics, methods, local code structure, rule-specific Roslyn queries, staging and supporting changes that fit the resolved contract. Return a material issue to planning if a rule cannot be mechanically defined without subjective detection, a reliable relationship is impossible in the supported workspace, the agreed 17/10/5 rule/budget policy needs changing, external acceptance semantics would change, environment authority must expand, or a newly discovered product-level ambiguity changes scope or acceptance. Do not silently drop a hard rule or change the budget to get green tests.
