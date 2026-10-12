# Execution Ledger — M0017 Semantic Hygiene Rule Expansion

Primary milestone: `docs/milestones/M0017-semantic-hygiene-rule-expansion.md`

Planning-seeded for Agentic Guide System 0.9.1. The milestone is authoritative; the ledger is mutable implementation evidence. Planner owns the exact ID/type/obligation texts and required evidence-case definitions. Implementer fills work packages, evidence, statuses and resume point without merging, paraphrasing or deleting rows.

## Milestone Obligation Registry

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | The existing `readability.control-flow.visual-block` advances from v1 `finding` to v2 `review-candidate`, retaining its existing precisely defined syntax/trivia trigger predicate and eligibility while explicitly treating a match as an indicator, not a proven readability defect. Positive and negative trigger cases continue to work. | | | | todo |
| AC-02 | acceptance | The versioned visual-block result, `explain` text and CLI/agent guidance invite the caller to inspect conceptual grouping and choose a context-sensitive response; no unconditional blank-line mandate remains. Existing `ignore`/`unignore` behavior works for current candidate occurrences, while old v1 decisions do not silently match v2. | | | | todo |
| AC-03 | acceptance | Neither `format` nor `normalize` automatically fixes the visual-block indicator; no new source formatter, fixer, blanket whitespace normalization or heuristic mechanical readability rewrite is added. Existing rewrite rules and transactions remain unchanged. | | | | todo |
| AC-04 | acceptance | Add `readability.type-responsibility.review` v1 as a configurable source-backed semantic-review rule over eligible class, struct, interface and record declarations (including nested and partial declarations); exclude enums, delegates, generated/ineligible source and nondeclarations, and retain project-qualified disambiguation. | | | | todo |
| AC-05 | acceptance | The type-responsibility rule owns exactly the milestone's fixed three review questions about coherent ownership, unrelated responsibilities and reconstruction burden. A normal implementing agent reviews and escalates material concerns or insufficient context to `planner`, without deriving a verdict from type length/member count or automatically splitting types. | | | | todo |
| AC-06 | acceptance | Add `tests.quality.review` v1 as a configurable semantic-review rule over source-backed test methods recognized by Roslyn-resolved framework test attributes: TUnit `[Test]`, xUnit `[Fact]`/`[Theory]`, NUnit `[Test]`/`[TestCase]` and MSTest `[TestMethod]`/`[DataTestMethod]` (including qualifying framework-derived attributes). Exclude helpers, fixture setup/teardown and name-only false positives; do not add production dependencies on those frameworks. | | | | todo |
| AC-07 | acceptance | The test-quality rule owns exactly the milestone's fixed three review questions on regression-sensitive assertions, observable behavior versus implementation coupling, and failure diagnosis/maintainability. Its normal reviewer is `implementer`, with `frontier` escalation on a material concern or uncertainty. No assertion-count or mocking-count threshold is treated as a quality verdict. | | | | todo |
| AC-08 | acceptance | Add `comments.intent.review` v1 as a configurable semantic-review rule over ordinary source-backed `//` and `/* ... */` implementation comment blocks, grouping contiguous related `//` lines deterministically. Exclude XML documentation trivia, directives, disabled/inactive code, generated sources, license/copyright headers, and comment-like text in strings. | | | | todo |
| AC-09 | acceptance | The comment-intent rule owns exactly the milestone's fixed three review questions on explanatory purpose, correctness relative to code, and clarity/nonredundancy. Its normal reviewer is `implementer`, with `frontier` escalation on a material concern or uncertainty. Do not introduce comment rewrites, requirements for more comments, or fixed good/bad prose scoring. | | | | todo |
| AC-10 | acceptance | Register the three new semantic-review modules explicitly in canonical order after B-3 as B-4 type, B-5 test, B-6 comment, each v1 and default enabled with an independent whole-rule toggle. Preserve all preexisting rule IDs, versions, toggle semantics, B-1/B-2/B-3 identities, and independent rubrics. | | | | todo |
| AC-11 | acceptance | All three new rules reuse M0016 gate-local selection without changes to its algorithm: ordinary `--base`/`--changed` gates prioritize directly affected subjects, reserve at most one touched-file context subject, emit up to five per enabled rule; explicit paths/`--review-all` authorize bounded manual/full inspection, and bare/irrelevant gates select zero. Unrelated background never fills an ordinary gate. | | | | todo |
| AC-12 | acceptance | New subjects use unambiguous project/path/semantic-owner identity plus stable disambiguation for nested/partial declarations, methods and same-owner comment blocks; full review-relevant local content fingerprints suppress accepted unchanged work and invalidate it after material edits. Harmless movement or unrelated leading edits should not create spurious identities when the semantic anchor remains stable. | | | | todo |
| AC-13 | acceptance | Each new rule uses the existing private gate tickets and accepted-only gate-review store; repeat, partial accept, earlier-gate acceptance, stale source/base/population/version, duplicate/nonselected tickets, restart and atomic multi-accept retain M0016 contracts. Negative/uncertain results are never inferred or autoaccepted. | | | | todo |
| AC-14 | acceptance | Successful checks emit an independent normal review batch, including `0/N` and `0/0`, for every enabled new rule, preserve `PopulationCount` and selected-subset meaning, and support full-current-population `review expand` and transport-neutral `review handoff` with source context and no model/provider invocation. | | | | todo |
| AC-15 | acceptance | Public `check`, `rules`, `explain`, `review expand`, `review handoff`, `review accept` text/JSON output and exit codes remain compatible except for the explicitly versioned visual-block reclassification, appended rule descriptors/batches and deliberate tool-version change; internal tickets, rankings and evidence state never become public fields. | | | | todo |
| AC-16 | acceptance | Each rule is a dedicated explicit in-process module owning its fixed text and eligibility; reuse lazy `RepositorySession`/Roslyn session facts and shared selection without a central callback framework, DSL, plugin discovery, general-purpose risk-feature pipeline, persistent index, external model API or per-rule configuration parameters. | | | | todo |
| AC-17 | acceptance | Focused Core and real Git/Roslyn fixtures verify the mechanically specified visual-block trigger, new subject populations/exclusions, fixed question/batch identity, project/symbol/comment-block disambiguation, context sufficiency for truthful review requests and ordinary/zero/manual/full gate mechanics. No test assumes a semantic reviewer produces a particular subjective verdict. | | | | todo |
| AC-18 | acceptance | Lifecycle and CLI process fixtures verify current/older accepted evidence, gate-local budget/nonrefill, invalidation, explicit acceptance-only behavior, expansion/handoff, private state, rule enable/disable and compatibility across the existing three semantic rules and new B-4–B-6 rules. | | | | todo |
| AC-19 | acceptance | The canonical Windows 11/.NET 11 validation completes and the actual freshly packed `DotNetAiCodeHygiene.Tool` version `0.9.0` installs into an isolated consumer and exercises representative new rule discovery, bounded/zero review, visual-block candidate, review accept/expand/handoff and existing rewrite/profile workflows. Do not claim success from a previously installed binary. | | | | todo |
| AC-20 | acceptance | Completion evidence distinguishes mechanical trigger fidelity and review-work fidelity from subjective external model judgment. Do not require model execution, evaluator gold labels, reviewer agreement, precision/recall, economic benefit, historical replay, sampling calibration or a benchmark against a particular model as implementation acceptance. | | | | todo |
| AC-21 | acceptance | Rule-owned context includes the selected declaration/test method/comment block with current enclosing/source text and accurate source locations. When the available source context cannot establish an external contract or dependency, the rule request leaves judgment to the reviewer or escalates uncertainty rather than inventing an assertion. | | | | todo |
| DOC-01 | documentation | After implementation, update the operative rule specifications (especially `docs/specs/READABILITY.md`, `SEMANTIC-REVIEWS.md`, `RULE-CAPABILITIES.md`, `SAMPLING.md` where affected), architecture/engineering authority, CLI/agent help and `docs/MILESTONES.md` to describe live M0017 behavior and v0.9.0, without retrospectively rewriting completed historical milestone evidence. | | | | todo |
| DOC-02 | documentation | Preserve and reflect the planning distinction documented in `docs/research/VERIFIABLE-REVIEW-WORK.md`: a visual-block match is only an indicator; semantic output is review work; software verification does not grade the external agent. Avoid language asserting that triggering proves a defect or that a sampling budget certifies quality. | | | | todo |
| REV-01 | human review | Prepare `.review/REV-M0017-COMPLETION.md` for blocking project-owner/delegate acceptance of scope, compatibility, identity/context correctness, verification-only validation boundary, complete criterion-specific evidence and correct installed-tool provenance. Do not fabricate reviewer approval or merge before the human decision. | | | | todo |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-01a | AC-01 | Existing visual-block positive triggers still yield v2 candidates; no automatic edits | VAL-01 | | todo |
| EC-01b | AC-01 | Existing negative cases and attached-comment boundaries remain nontriggers | VAL-01 | | todo |
| EC-02a | AC-02 | V2 review-candidate metadata/explain guidance and v1-ignore incompatibility | VAL-01;VAL-03 | | todo |
| EC-04a | AC-04 | Nested, partial and ordinary eligible types retain separate source identities | VAL-01 | | todo |
| EC-04b | AC-04 | Enums, delegates and generated/ineligible types excluded | VAL-01 | | todo |
| EC-06a | AC-06 | Roslyn recognition of TUnit, xUnit method-level framework attributes | VAL-01 | | todo |
| EC-06b | AC-06 | Roslyn recognition of NUnit and MSTest method-level attributes | VAL-01 | | todo |
| EC-06c | AC-06 | Helper methods, lifecycle methods and same-named custom attributes not misclassified | VAL-01 | | todo |
| EC-08a | AC-08 | Single-line and contiguous-line groups plus block comments form accurate source-backed subjects | VAL-01 | | todo |
| EC-08b | AC-08 | XML docs, directives, inactive/generated/license comments and strings excluded | VAL-01 | | todo |
| EC-08c | AC-08 | Duplicate same-owner blocks remain disambiguated and stable under unrelated movement | VAL-01 | | todo |
| EC-11a | AC-11 | Real source-change `--base` and `--changed` select changed/context subjects without unrelated backfill | VAL-02;VAL-03 | | todo |
| EC-11b | AC-11 | Bare/irrelevant gates select zero; manual paths and `--review-all` can select up to five | VAL-02;VAL-03 | | todo |
| EC-12a | AC-12 | Type/test nested, partial and method subjects preserve identity under unrelated edits; material edits refresh fingerprints | VAL-01;VAL-02 | | todo |
| EC-12b | AC-12 | Comment blocks with equivalent/different content in one owner do not collide or spuriously replay | VAL-01;VAL-02 | | todo |
| EC-13a | AC-13 | Same-gate partial acceptance has no refill; earlier-gate evidence does not starve new candidates | VAL-02;VAL-03 | | todo |
| EC-13b | AC-13 | Stale base, source, rule version, tampering or concurrent writes reject atomically | VAL-02;VAL-03 | | todo |
| EC-14a | AC-14 | Six independent enabled batches including `0/N`, preserved full population and canonical B-1–B-6 order | VAL-01;VAL-03 | | todo |
| EC-14b | AC-14 | Expand/handoff use full eligible scope with truthful source content; no model invocation | VAL-03;VAL-05 | | todo |
| EC-19a | AC-19 | Full repository build/test and authority checks on Windows 11/.NET 11 | VAL-04 | | todo |
| EC-19b | AC-19 | Freshly packed v0.9.0 installed from exact package into isolated consumer; new and legacy command paths | VAL-05 | | todo |

## Work Packages

Implementation-owned. Derive work packages after rereading the milestone; do not replace any individually seeded obligation/evidence case with a broader task summary.

## Validation Gates

| ID | Required validation | Target/locus | Proves evidence units | Status | Evidence |
|---|---|---|---|---|---|
| VAL-01 | Tier 1 — focused Roslyn Core tests for v2 visual-block candidate and all three new populations/rubrics, including exclusion/identity/context cases | local Windows 11/.NET 11 SDK; compile representative in-memory or temporary projects | EC-01a, EC-01b, EC-02a, EC-04a, EC-04b, EC-06a, EC-06b, EC-06c, EC-08a, EC-08b, EC-08c, EC-12a, EC-12b, EC-14a, AC-05, AC-07, AC-09, AC-10, AC-21 | todo | |
| VAL-02 | Tier 1/Tier 3 — gate-local selection, acceptance and staleness regression for new rules | local Windows 11/.NET 11; real persisted state and Git fixtures | EC-11a, EC-11b, EC-12a, EC-12b, EC-13a, EC-13b, AC-13, AC-18 | todo | |
| VAL-03 | Tier 3 — built CLI in isolated real SDK-style .NET/Git repositories, changed/base/manual/all scope and handoff | local Windows 11/.NET 11 with Git and PowerShell; actual Roslyn workspace and built CLI process | EC-02a, EC-11a, EC-11b, EC-13a, EC-13b, EC-14a, EC-14b, AC-02, AC-03, AC-15, AC-17, AC-18, AC-21 | todo | |
| VAL-04 | Tier 2 — canonical repo regression, docs and version consistency | local Windows 11/.NET 11/PowerShell; `./eng/validate.ps1` and `git diff --check` | EC-19a, AC-03, AC-10, AC-15, AC-16, AC-17, AC-18, AC-20, DOC-01, DOC-02 | todo | |
| VAL-05 | Tier 4 — freshly built and installed actual v0.9.0 dotnet tool in isolated consumer | local Windows 11/.NET 11 PowerShell/Git; extend `eng/validate.ps1` isolated tool install and consumer commands | EC-19b, EC-14b, AC-19 | todo | |
| VAL-06 | Human — independent owner/delegate completion review | milestone PR and `.review/REV-M0017-COMPLETION.md` | REV-01 | todo | |

## Resume Point

Last completed work package: none (planning only).

Current work package: implementation has not started.

Next concrete action: reread the ready M0017 milestone and operative authority, confirm M0016 base/PR merge status, verify obligation/evidence/gate ID equality, then decompose implementation.

Known agent-resolvable gaps: none identified by planning.

External blockers or planning escalations: M0016 base branch is unmerged at planning time; rebase onto merged `main` when PR #17 lands, preserving intended M0017-only diff.

## Final Reconciliation

- [ ] Reread primary milestone from repository at completion.
- [ ] Verify exact obligation-ID set equality and exact planner-owned wording.
- [ ] Verify exact evidence-case-ID set equality; no sibling cases collapsed.
- [ ] Verify each validation gate covers only actually exercised behavior and specified target/locus.
- [ ] Attach criterion-specific implementation and validation evidence, not only aggregate passing tests.
- [ ] Verify no requirement depends on a model's subjective answers or fabricated review evidence.
- [ ] Verify current packed/installed v0.9.0 tool consumer behavior and stable existing rules.
- [ ] Prepare human review request and leave milestone awaiting human approval when required.
- [ ] Run the repository's supported review-check if available after the human decision.
- [ ] Complete durable milestone Completion Evidence reconciliation before declaring COMPLETE.
