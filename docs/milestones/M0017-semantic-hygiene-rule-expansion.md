# M0017 — Semantic Hygiene Rule Expansion

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready — planning complete; implementation pending |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna; Agentic Guide System 0.9.1 |
| Baseline executor readiness | confirmed after M0016 production gate-local surface |
| Decision preservation | confirmed by rule contracts and explicit verification boundary below |
| Execution tractability | one coherent capability expansion; no framework/research prerequisite |
| Execution ledger | `.execution/M0017-semantic-hygiene-rule-expansion.md` (planning-seeded) |
| Scope size | medium-large, four capability strands within one milestone |
| Implementation autonomy | high in Roslyn traversal, tests and local files; no product-policy redesign |
| Documentation sync | directly affected authority only, broad cleanup deferred |
| Focused validation | Core/Roslyn tests plus real Git lifecycle fixtures (VAL-01/02) |
| Repository validation | `.\eng\validate.ps1` and `git diff --check` (VAL-04) |
| Integration validation | built CLI over isolated SDK-style .NET/Git repositories (VAL-03) |
| Validation locus/platform | Windows 11, .NET 11 SDK, Git, PowerShell; real source/project integration |
| Consumer/release validation | exact freshly packed tool 0.9.0 installed in isolated consumer (VAL-05) |
| Human review | blocking `REV-M0017-COMPLETION` (VAL-06) |

## Goal

Broaden AI-first code hygiene without a new rule framework or model-based quality-certification process. Turn the existing visual-block pattern into a truthful *candidate signal* and add three independent semantic-review rules over M0016 gate-local sampling: type responsibility, test quality, and ordinary explanatory comments. The product verifies review-work production and leaves subjective decisions and code correction to external agents.

## Target State

A source change/PR gate can deliver bounded, correctly identified semantic review work for types, test methods and ordinary comments, with accurate source context, stable identities, fixed questions and current review lifecycle. The visual-block indicator gives agent-directed review advice rather than prescribing a mechanical blank-line fix. All earlier rules, formatters, public commands and accepted-state boundaries remain valid except expressly versioned changes.

## Dependency / branch policy

- M0017 depends on production M0016 (PR #17). Its planning branch starts from PR #17's implementation head because PR #17 is not yet merged at the time of planning. When PR #17 is squash-merged, rebase the M0017 implementation branch onto that merged `main` before opening the final merge PR; do not unintentionally replay M0016 as new M0017 work.
- The planning branch is `planning/m0017-semantic-hygiene-expansion`. This is a ready **contract**, not a claim that M0017 source functionality has been implemented.

## Scope

- Versioned visual-block candidate semantics and agent guidance (no new formatter).
- Three source-backed, rule-owned semantic review modules using current M0016 gate-local selector and review workflow.
- Focused verification of subject discovery, identity, context, selection, acceptance/expansion/handoff and installed product behavior.
- Directly affected product authority, agent help, tests and current tool version 0.9.0.

## Non-goals

- No visual-block automatic rewriting, generalized readability formatting, speculative restructuring, code generation or semantic correction in the CLI.
- No model/provider invocation, independent judgment evaluation, curated subjective gold labels, precision/recall, model-versus-model experiments or requirement to demonstrate an external model's competence.
- No new hazard, subject-age, historical replay, probabilistic coverage claim, learned sampling policy or per-rule sampling controls.
- No generalized rule callback pipeline, external plugin, broad rule discovery framework, new analyzer/test framework dependencies, IDE/MCP integration or unrelated 1.0 cleanup.
- No claim that detected candidates are defects, that the three rubrics are exhaustive, or that accepted evidence certifies the repository.

## Decisions and Constraints

### Visual-block signal: explicit contract correction

- Existing `readability.control-flow.visual-block` v1 returns a deterministic `finding` with a mandatory blank-line suggestion. M0017 **intentionally changes its semantic contract**: keep its present source/trivia trigger predicate (ordinary statement followed by selected control flow with no blank line and the documented exclusions), but version it to v2 with classification `review-candidate`.
- A matching syntactic pattern is an inspection hint, not proof of poor layout. The v2 message/explanation asks an agent to examine conceptual grouping and choose whether to leave code unchanged, add whitespace or restructure it. Never mechanically declare a blank line mandatory or call the candidate a proven problem.
- Keep lightweight deterministic candidate materialization and current `check`/`explain`/`ignore`/`unignore` surfaces; preserve fixed rule toggles. Old v1 persisted ignore occurrences do not silently suppress a v2 candidate. Source rewrite commands do not consume this signal.
- `readability.long-line.review` remains independent, also a review candidate. Do **not** migrate either visual/long-line candidate into the sampled semantic-review batch framework solely for this milestone.

### Three rule-owned semantic review contracts

| Capability | Fixed rule ID | Version | Batch | Review path |
|---|---|---|---|---|
| Type responsibility | `readability.type-responsibility.review` | v1 | B-4 | implementer → planner |
| Test quality | `tests.quality.review` | v1 | B-5 | implementer → frontier |
| Ordinary comment intent | `comments.intent.review` | v1 | B-6 | implementer → frontier |

Each question is fixed text owned by its module; no configurable rubric, severity or thresholds. A material concern or uncertainty escalates through the stated class; the CLI does not itself answer the questions, label defects or perform remediation.

**Type responsibility — Q1–Q3:**
- Q1 — Ownership: Does this type mix responsibilities without one coherent ownership/purpose?
- Q2 — Coupling: Does understanding or changing ordinary behavior require reconstructing unrelated state or workflows within this type?
- Q3 — Separation: Is there a concrete responsibility that should be owned elsewhere without creating needless abstraction or fragmentation?

Eligible subjects: source-backed class, struct, interface and record declarations (including nested/partial parts) in discoverable C# projects. Exclude enums, delegates, generated/ineligible source and nondeclarations. Each source-backed declaration is independently disambiguated; the reviewer may inspect related parts through current source context. Do not use class length/member count as a verdict. A valid cohesive coordinator is not automatically a bad abstraction.

**Test quality — Q1–Q3:**
- Q1 — Regression evidence: Could the assertions plausibly detect a meaningful violation of the tested behavior rather than merely successful execution or a non-null result?
- Q2 — Contract focus: Does the test primarily examine observable behavior rather than unnecessarily binding itself to implementation details?
- Q3 — Maintainability: Is the setup/assertion structure clear enough to diagnose failure without unjustified fragility, coupling or mock choreography?

Eligible subjects: actual test methods with semantically resolved known test attributes. Explicit coverage: TUnit Test, xUnit Fact/Theory, NUnit Test/TestCase, MSTest TestMethod/DataTestMethod and qualifying framework-derived attributes. Method names, test folder names, random attributes named `Test`, fixtures and setup/teardown alone are not evidence of a test method. Include accurate containing type/current test body/nearby test context without new production references to test frameworks. This rule does not require more assertions or mocks by formula.

**Comment intent — Q1–Q3:**
- Q1 — Intent: Does the comment provide useful non-obvious rationale, constraint or meaning rather than narrate nearby syntax?
- Q2 — Fidelity: Is the comment consistent with current implementation and relevant local context?
- Q3 — Clarity: Is the explanation sufficiently specific, scoped and concise to help a reader without redundant narrative?

Eligible subjects: ordinary source `//` lines grouped into contiguous same-owner blocks, or ordinary `/* ... */` comment blocks, anchored to their nearest relevant enclosing source declaration when available. Exclude XML docs, disabled/inactive source, directives, generated source, license/copyright banners, comment-like text in literals. No automatic requirement to write comments or remove them. A comment may be useful despite stating an apparent implementation detail if the context justifies it.

### Gate selection, identity, context and acceptance

- New semantic rules are independently enabled by default, appended after B-3 as B-4/B-5/B-6. Preserve existing descriptors and question order; use the same rule-owned module pattern and truthful `ReviewItem` fields (empty non-applicable summary fields rather than fabricated XML prose).
- Reuse M0016 ordinary changed/context/full/manual gate policy, SHA-256 deterministic ranking, maximum five normal items per rule, fixed same-gate allocation, cross-gate accepted suppression, current population fingerprinting, expand/handoff, private tickets and explicit accepted-only state. Each rule owns its own eligible subject content and stable fingerprint; do not introduce a second selector.
- Stable project/path/semantic-owner identity and rule-specific disambiguation must avoid collisions for nested/partial types, overloaded tests and repeated comment blocks. Movement of unrelated declarations/trivia should not routinely mint a different subject for unchanged semantic anchors; content changes trigger new review fingerprints.
- Keep the selected source text and sufficient truthful containing-file context for meaningful review; current multi-file dependencies need not become a new universal index. An external reviewer may inspect additional code or escalate uncertainty instead of inventing unseen contracts.
- Normal `check` continues deterministic full-scope checks, but bare full checks emit zero semantic review work. An empty relevant rule population still produces its normal zero batch. Existing B-1/B-2/B-3 and all mechanical checks work without participating in the new rule's context if disabled.

### Verification and validation policy

- **Verify the tool's contract:** trigger predicates, eligible/excluded subjects, identities and fingerprints, context/source accuracy, deterministic gate admission, bounded selection, rule/questions/order, stale acceptance, transport, rollback and actual installed-tool behavior.
- **Do not grade external intelligence as a software acceptance test:** no required model call, golden semantic review judgment, false-positive/false-negative metric, subjective usefulness benchmark, aggregate statistical power or economic return. The agent and model are replaceable external consumers.
- Verification may include intentionally constructed source examples to assert whether an item is *eligible* or *selected*; those examples must not assert that a chosen agent would judge the source as good or bad.
- Implementer owns focused TUnit fixtures and exact in-repository source edits. Follow canonical Windows/.NET 11 test/pack/installed-tool validation. Human milestone review remains separate from model-quality benchmarking.

## Baseline Executor Readiness

The rule IDs, eligibility boundaries, four capability strands, fixed questions, reviewer classes, versions, M0016 compatibility, verification boundary, consumer version and execution topology are decided. The baseline executor may determine local Roslyn helpers and test design without inventing new product policy. Any truly material incompatible public contract or missing framework-attribute edge must be escalated rather than silently changed.

## Decision Preservation

Preserve the distinction `deterministic indicator != proven defect`, `review work != external review judgment`, and `software verification != subjective model certification`. Do not turn visual-block findings into an unconditional formatter, and do not make M0017 completion depend on an agent providing preferred semantic answers. A planning conversation is not required to recover these decisions.

## Execution Tractability

Implement as one coherent milestone. There are four functional strands, but the executor owns its actual work-package decomposition. New modules use M0007 rule locality and M0016 sampling; no architecture redesign or research study is necessary. The planning-seeded ledger retains each separately verifiable obligation and required evidence path without prescribing every source edit.

## Required Authority

- `AGENTS.md`, `docs/ENGINEERING.md`, `docs/ARCHITECTURE.md`, `docs/specs/RULE-CAPABILITIES.md`, `docs/specs/READABILITY.md`, `docs/specs/SEMANTIC-REVIEWS.md`, `docs/specs/SAMPLING.md`, `docs/specs/REVIEW-HANDOFFS.md`, `docs/specs/REWRITES.md`.
- M0016 source and milestone for active gate selection and accepted-only review lifecycle.
- Planning context: `docs/research/VERIFIABLE-REVIEW-WORK.md`, `docs/research/HUMAN-CONSUMABILITY.md`, `docs/research/PRODUCT-DIRECTION.md`. Non-authoritative research cannot override this milestone or current production specifications.

## Acceptance Criteria

- **AC-01** — The existing `readability.control-flow.visual-block` advances from v1 `finding` to v2 `review-candidate`, retaining its existing precisely defined syntax/trivia trigger predicate and eligibility while explicitly treating a match as an indicator, not a proven readability defect. Positive and negative trigger cases continue to work.
- **AC-02** — The versioned visual-block result, `explain` text and CLI/agent guidance invite the caller to inspect conceptual grouping and choose a context-sensitive response; no unconditional blank-line mandate remains. Existing `ignore`/`unignore` behavior works for current candidate occurrences, while old v1 decisions do not silently match v2.
- **AC-03** — Neither `format` nor `normalize` automatically fixes the visual-block indicator; no new source formatter, fixer, blanket whitespace normalization or heuristic mechanical readability rewrite is added. Existing rewrite rules and transactions remain unchanged.
- **AC-04** — Add `readability.type-responsibility.review` v1 as a configurable source-backed semantic-review rule over eligible class, struct, interface and record declarations (including nested and partial declarations); exclude enums, delegates, generated/ineligible source and nondeclarations, and retain project-qualified disambiguation.
- **AC-05** — The type-responsibility rule owns exactly the milestone's fixed three review questions about coherent ownership, unrelated responsibilities and reconstruction burden. A normal implementing agent reviews and escalates material concerns or insufficient context to `planner`, without deriving a verdict from type length/member count or automatically splitting types.
- **AC-06** — Add `tests.quality.review` v1 as a configurable semantic-review rule over source-backed test methods recognized by Roslyn-resolved framework test attributes: TUnit `[Test]`, xUnit `[Fact]`/`[Theory]`, NUnit `[Test]`/`[TestCase]` and MSTest `[TestMethod]`/`[DataTestMethod]` (including qualifying framework-derived attributes). Exclude helpers, fixture setup/teardown and name-only false positives; do not add production dependencies on those frameworks.
- **AC-07** — The test-quality rule owns exactly the milestone's fixed three review questions on regression-sensitive assertions, observable behavior versus implementation coupling, and failure diagnosis/maintainability. Its normal reviewer is `implementer`, with `frontier` escalation on a material concern or uncertainty. No assertion-count or mocking-count threshold is treated as a quality verdict.
- **AC-08** — Add `comments.intent.review` v1 as a configurable semantic-review rule over ordinary source-backed `//` and `/* ... */` implementation comment blocks, grouping contiguous related `//` lines deterministically. Exclude XML documentation trivia, directives, disabled/inactive code, generated sources, license/copyright headers, and comment-like text in strings.
- **AC-09** — The comment-intent rule owns exactly the milestone's fixed three review questions on explanatory purpose, correctness relative to code, and clarity/nonredundancy. Its normal reviewer is `implementer`, with `frontier` escalation on a material concern or uncertainty. Do not introduce comment rewrites, requirements for more comments, or fixed good/bad prose scoring.
- **AC-10** — Register the three new semantic-review modules explicitly in canonical order after B-3 as B-4 type, B-5 test, B-6 comment, each v1 and default enabled with an independent whole-rule toggle. Preserve all preexisting rule IDs, versions, toggle semantics, B-1/B-2/B-3 identities, and independent rubrics.
- **AC-11** — All three new rules reuse M0016 gate-local selection without changes to its algorithm: ordinary `--base`/`--changed` gates prioritize directly affected subjects, reserve at most one touched-file context subject, emit up to five per enabled rule; explicit paths/`--review-all` authorize bounded manual/full inspection, and bare/irrelevant gates select zero. Unrelated background never fills an ordinary gate.
- **AC-12** — New subjects use unambiguous project/path/semantic-owner identity plus stable disambiguation for nested/partial declarations, methods and same-owner comment blocks; full review-relevant local content fingerprints suppress accepted unchanged work and invalidate it after material edits. Harmless movement or unrelated leading edits should not create spurious identities when the semantic anchor remains stable.
- **AC-13** — Each new rule uses the existing private gate tickets and accepted-only gate-review store; repeat, partial accept, earlier-gate acceptance, stale source/base/population/version, duplicate/nonselected tickets, restart and atomic multi-accept retain M0016 contracts. Negative/uncertain results are never inferred or autoaccepted.
- **AC-14** — Successful checks emit an independent normal review batch, including `0/N` and `0/0`, for every enabled new rule, preserve `PopulationCount` and selected-subset meaning, and support full-current-population `review expand` and transport-neutral `review handoff` with source context and no model/provider invocation.
- **AC-15** — Public `check`, `rules`, `explain`, `review expand`, `review handoff`, `review accept` text/JSON output and exit codes remain compatible except for the explicitly versioned visual-block reclassification, appended rule descriptors/batches and deliberate tool-version change; internal tickets, rankings and evidence state never become public fields.
- **AC-16** — Each rule is a dedicated explicit in-process module owning its fixed text and eligibility; reuse lazy `RepositorySession`/Roslyn session facts and shared selection without a central callback framework, DSL, plugin discovery, general-purpose risk-feature pipeline, persistent index, external model API or per-rule configuration parameters.
- **AC-17** — Focused Core and real Git/Roslyn fixtures verify the mechanically specified visual-block trigger, new subject populations/exclusions, fixed question/batch identity, project/symbol/comment-block disambiguation, context sufficiency for truthful review requests and ordinary/zero/manual/full gate mechanics. No test assumes a semantic reviewer produces a particular subjective verdict.
- **AC-18** — Lifecycle and CLI process fixtures verify current/older accepted evidence, gate-local budget/nonrefill, invalidation, explicit acceptance-only behavior, expansion/handoff, private state, rule enable/disable and compatibility across the existing three semantic rules and new B-4–B-6 rules.
- **AC-19** — The canonical Windows 11/.NET 11 validation completes and the actual freshly packed `DotNetAiCodeHygiene.Tool` version `0.9.0` installs into an isolated consumer and exercises representative new rule discovery, bounded/zero review, visual-block candidate, review accept/expand/handoff and existing rewrite/profile workflows. Do not claim success from a previously installed binary.
- **AC-20** — Completion evidence distinguishes mechanical trigger fidelity and review-work fidelity from subjective external model judgment. Do not require model execution, evaluator gold labels, reviewer agreement, precision/recall, economic benefit, historical replay, sampling calibration or a benchmark against a particular model as implementation acceptance.
- **AC-21** — Rule-owned context includes the selected declaration/test method/comment block with current enclosing/source text and accurate source locations. When the available source context cannot establish an external contract or dependency, the rule request leaves judgment to the reviewer or escalates uncertainty rather than inventing an assertion.

## Acceptance Evidence Topology

Evidence cases are reserved for behaviors with different code paths or failure surfaces. No subjective model-quality outcome is a required case.

| ID | Parent obligation | Required evidence case | Why separate evidence is required |
|---|---|---|---|
| EC-01a | AC-01 | Existing visual-block positive triggers still yield v2 candidates; no automatic edits | Changed classification must not accidentally erase the trigger. |
| EC-01b | AC-01 | Existing negative cases and attached-comment boundaries remain nontriggers | Trivia and excluded sibling paths differ. |
| EC-02a | AC-02 | V2 review-candidate metadata/explain guidance and v1-ignore incompatibility | Public contract and persisted version transition differ. |
| EC-04a | AC-04 | Nested, partial and ordinary eligible types retain separate source identities | Source-backed type forms differ. |
| EC-04b | AC-04 | Enums, delegates and generated/ineligible types excluded | Exclusion paths differ from ordinary discovery. |
| EC-06a | AC-06 | Roslyn recognition of TUnit, xUnit method-level framework attributes | Two independent framework families. |
| EC-06b | AC-06 | Roslyn recognition of NUnit and MSTest method-level attributes | Other framework metadata paths. |
| EC-06c | AC-06 | Helper methods, lifecycle methods and same-named custom attributes not misclassified | False-positive/negative paths. |
| EC-08a | AC-08 | Single-line and contiguous-line groups plus block comments form accurate source-backed subjects | Different trivia/grouping mechanisms. |
| EC-08b | AC-08 | XML docs, directives, inactive/generated/license comments and strings excluded | Excluded trivia/input categories. |
| EC-08c | AC-08 | Duplicate same-owner blocks remain disambiguated and stable under unrelated movement | Comment identity distinct from simple extraction. |
| EC-11a | AC-11 | Real source-change `--base` and `--changed` select changed/context subjects without unrelated backfill | Git scope versus hand-picked candidates. |
| EC-11b | AC-11 | Bare/irrelevant gates select zero; manual paths and `--review-all` can select up to five | Different admission modes. |
| EC-12a | AC-12 | Type/test nested, partial and method subjects preserve identity under unrelated edits; material edits refresh fingerprints | Stable identity separate from changed content. |
| EC-12b | AC-12 | Comment blocks with equivalent/different content in one owner do not collide or spuriously replay | Comment granularity and duplicates. |
| EC-13a | AC-13 | Same-gate partial acceptance has no refill; earlier-gate evidence does not starve new candidates | Two distinct acceptance-budget mechanisms. |
| EC-13b | AC-13 | Stale base, source, rule version, tampering or concurrent writes reject atomically | Negative state and invalidation paths. |
| EC-14a | AC-14 | Six independent enabled batches including `0/N`, preserved full population and canonical B-1–B-6 order | Materialization versus selection. |
| EC-14b | AC-14 | Expand/handoff use full eligible scope with truthful source content; no model invocation | Escalation/consumer surface. |
| EC-19a | AC-19 | Full repository build/test and authority checks on Windows 11/.NET 11 | Repository regression target. |
| EC-19b | AC-19 | Freshly packed v0.9.0 installed from exact package into isolated consumer; new and legacy command paths | Distribution boundary not proven by Core tests. |

## Validation

| ID | Depth/target | Locus/platform | Command/check | Proves | Expected evidence |
|---|---|---|---|---|---|
| VAL-01 | Tier 1 — focused Roslyn Core tests for v2 visual-block candidate and all three new populations/rubrics, including exclusion/identity/context cases | local Windows 11/.NET 11 SDK; compile representative in-memory or temporary projects | Focused Core/CLI/TUnit fixture invocation through canonical repository validation | EC-01a, EC-01b, EC-02a, EC-04a, EC-04b, EC-06a, EC-06b, EC-06c, EC-08a, EC-08b, EC-08c, EC-12a, EC-12b, EC-14a, AC-05, AC-07, AC-09, AC-10, AC-21 | Focused TUnit Core tests; exact named fixtures and observed selectors/fields |
| VAL-02 | Tier 1/Tier 3 — gate-local selection, acceptance and staleness regression for new rules | local Windows 11/.NET 11; real persisted state and Git fixtures | Focused Core/CLI/TUnit fixture invocation through canonical repository validation | EC-11a, EC-11b, EC-12a, EC-12b, EC-13a, EC-13b, AC-13, AC-18 | Positive/negative selection and acceptance fixtures; stable identities/state bytes |
| VAL-03 | Tier 3 — built CLI in isolated real SDK-style .NET/Git repositories, changed/base/manual/all scope and handoff | local Windows 11/.NET 11 with Git and PowerShell; actual Roslyn workspace and built CLI process | Focused Core/CLI/TUnit fixture invocation through canonical repository validation | EC-02a, EC-11a, EC-11b, EC-13a, EC-13b, EC-14a, EC-14b, AC-02, AC-03, AC-15, AC-17, AC-18, AC-21 | Text/JSON contracts, return codes, selected contexts, expanded complete populations |
| VAL-04 | Tier 2 — canonical repo regression, docs and version consistency | local Windows 11/.NET 11/PowerShell; `./eng/validate.ps1` and `git diff --check` | `.\\eng\\validate.ps1`, `git diff --check` | EC-19a, AC-03, AC-10, AC-15, AC-16, AC-17, AC-18, AC-20, DOC-01, DOC-02 | Release build/test/pack receipts; authority vs behavior checks |
| VAL-05 | Tier 4 — freshly built and installed actual v0.9.0 dotnet tool in isolated consumer | local Windows 11/.NET 11 PowerShell/Git; extend `eng/validate.ps1` isolated tool install and consumer commands | Current packed `dotnet tool install`/installed consumer from canonical validation | EC-19b, EC-14b, AC-19 | Exact package SHA/version, isolated tool shim outputs, new rule/candidate and legacy compatibility receipts |
| VAL-06 | Human — independent owner/delegate completion review | milestone PR and `.review/REV-M0017-COMPLETION.md` | Human review record | REV-01 | Dated recorded review decision; no self-approval |

## Human Review

Applicability: required, blocking. Canonical review ID: `REV-M0017-COMPLETION`. Reviewer: project owner or delegated human. Subject: coherent addition of three independently selected semantic-review rules; nonprescriptive v2 visual-block candidates; faithful context/identity/state; fixed question contracts; sound project/installed-tool verification without grading the model. Waiver: none without planning authority change. Implementer prepares `.review/REV-M0017-COMPLETION.md` and retains the milestone in awaiting-human-review state until the reviewer explicitly decides; use the repository's actual documented milestone-scoped review check if available, do not invent one.

## Documentation Policy

Planning updates are already in research and index documents. Implementation synchronizes operative specs and agent help after behavior exists. Historical M0009–M0016 evidence is preserved. Do not perform a broad documentation refactor.

## Additional Completion Obligations

- **DOC-01** — After implementation, update the operative rule specifications (especially `docs/specs/READABILITY.md`, `SEMANTIC-REVIEWS.md`, `RULE-CAPABILITIES.md`, `SAMPLING.md` where affected), architecture/engineering authority, CLI/agent help and `docs/MILESTONES.md` to describe live M0017 behavior and v0.9.0, without retrospectively rewriting completed historical milestone evidence.
- **DOC-02** — Preserve and reflect the planning distinction documented in `docs/research/VERIFIABLE-REVIEW-WORK.md`: a visual-block match is only an indicator; semantic output is review work; software verification does not grade the external agent. Avoid language asserting that triggering proves a defect or that a sampling budget certifies quality.
- **REV-01** — Prepare `.review/REV-M0017-COMPLETION.md` for blocking project-owner/delegate acceptance of scope, compatibility, identity/context correctness, verification-only validation boundary, complete criterion-specific evidence and correct installed-tool provenance. Do not fabricate reviewer approval or merge before the human decision.

## Completion Expectations

The implementer maps the exact seeded obligations and evidence cases into execution work packages, runs required verification on the declared Windows and installed-tool targets, and performs criterion-by-criterion final reconciliation. Passing tests is not a substitute for each obligation's actual evidence. Record compact durable completion evidence only after implementation; do not mark planning placeholders as passed.

## Completion Evidence

Pending implementation. The executor fills a compact obligation/evidence-case-to-proof mapping here after final reconciliation. Do not fabricate validation or reviewer judgments during planning.

## Escalation Boundary

The implementer may choose local files, syntax helpers and tests. Return to planning only for a material unresolved compatibility/scope/public contract decision. Do not defer this milestone for another sampling experiment or external model-quality study.
