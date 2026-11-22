# Project Planning and Semantic Rule Evaluation Guidance

## Status and authority

Project-owned **planning authority** for future milestones and rule-portfolio decisions. Planning must apply this alongside the Agentic Project Guide, current specifications, architecture, and engineering constraints. A ready milestone carries the concrete decisions and validation obligations to its implementer; this document is not an instruction to expand an already ready milestone. Historical evidence and unresolved choices belong in [research](RESEARCH.md), not in an implementer's implicit decision space.

## Pre-1.0 product boundary

This is an agent-implemented, pre-1.0 product with no compatibility-dependent production consumers. **Do not preserve an experimental rule ID, batch layout, selector, schema, API, or stored verdict format merely for backwards compatibility.** Prefer coherent replacements, deletions, and clean state/schema breaks when a better future product contract requires them. Source safety, truthful migrations where needed, clean Git checkpoints, and explicit changes to authoritative contracts remain mandatory; permission to break compatibility is not permission to destroy user code or data silently.

BORING is a **design principle**, not necessarily one executable semantic question. Favor separately recognizable failure modes with distinct candidate predicates, bounded context, focused reviewer questions, legitimate counterexamples, and remediation/escalation criteria. Review a design decision, not an arbitrary class-size or interface-count threshold. Consolidate overlapping rules; an accepted or unusual pattern is not automatically defective.

## Agentic development economics

Optimize **total planning + implementation + validation + independent review cost**, not the cost of implementing one individual rule. Agent-written source and focused implementation tests are comparatively cheap; repeated milestone handoffs, expensive aggregate validation, broad semantic judgment, and review coordination may dominate.

- Prefer designing and implementing a **broad, coherent candidate portfolio together** when the relevant policy decisions are sufficiently resolved. Parallel/separable implementation work and shared analysis fixtures can be more economical than sequential three-rule milestones. Do not make small incremental batches the default solely because human teams work that way.
- It is acceptable to implement more candidate rules than the final product should keep. Compare their actual detector coverage, reviewer context, redundancy, operational cost, and fit with the product, then **merge, simplify, or delete aggressively**. A candidate is not a durable feature commitment.
- Prefer existing independent rule modules and shared lazy facts. Avoid prebuilding a universal rule DSL, framework, dependency DAG, or generic plugin system as a prerequisite for portfolio expansion. Extract shared machinery after multiple rules demonstrate a real common need.
- Distinguish **portfolio development breadth** from **ordinary gate cost**. A broad implementation/evaluation pass must not turn into a full semantic review of every candidate at every gate. Final production admission, grouping and budgets should be designed from the resulting failure modes and costs.
- Do not artificially split one executable research/implementation effort into many milestones when agent work and shared validation are more economical together. Conversely, if comparative evaluation must decide a material unresolved product policy, return that decision to planning rather than giving the implementer authority to choose a final rule contract by intuition.

## Semantic rules: what the software can prove

A semantic-review rule has three distinct responsibilities:

1. **Deterministic candidate fidelity:** correctly identify eligible subjects and any documented mechanical signal, with exact exclusions, identities, spans, source/context boundaries and stable behavior.
2. **Review-work fidelity:** emit truthful bounded context, coherent questions, gate selection, escalation, source references, accepted-observation behavior, and stale/tamper protection.
3. **External judgment/remediation:** an agent assesses whether the current code is actually problematic and, if appropriate, changes code or escalates.

**Only the first two are deterministic software acceptance contracts.** The third is inherently contextual and model-dependent. The tool does not have a privileged ground-truth verdict.

### Do not test subjective quality with invented controlled cases

Do **not** create a curated suite of synthetic `good`/`bad` architecture, readability, test-quality, or BORINGness examples and repeatedly ask an LLM to agree with their labels as the rule's unit test or milestone acceptance gate. Such fixtures discard the real codebase, history, intent and constraints on which the judgment depends. The supposed reference labels are debatable, and repeated model judgments are **not reliably repeatable** across sessions, prompts, contexts, or model revisions. Repetition does not turn a subjective judgment into deterministic proof.

Do **not** require model-run snapshots, fixed expected semantic verdicts, model consensus, precision/recall thresholds, benchmark scores, synthetic 'findings corrected' counts, or repeated expensive blinded assessments as routine software tests. A synthetic acceptance test must not be presented as measured semantic effectiveness.

This exclusion concerns **semantic verdicts**, not normal software testing. Small compiled C# fixtures and controlled inputs remain appropriate for mechanically verifiable eligibility, candidate extraction, source spans, identity and ranking, payloads, CLI output, mutation safety, gate behavior, persistence, and real installed-tool integration. Assert the actual deterministic contract; do not encode an artificial judgment about whether a class is 'good'.

### How to learn whether a semantic rule is useful

Use **real repository changes and their available context** for optional, bounded exploratory assessments of rule *usefulness*. Prefer observed actionable findings, no-change/legitimate outcomes, overlap, context insufficiency, cost, and remediation regressions to invented ground truth. Compare candidate rules on the same relevant actual change when practical. Record repository/commit, scope, reviewer/model provenance, question version, observed reasoning, uncertainties and limits; judgments remain provisional and are not guaranteed reproducible.

Such evaluation informs portfolio prioritization and can justify replacing a rule or improving candidate discovery. It is **not** a required gate for implementing deterministic rule machinery and does not certify stable semantic accuracy. Avoid costly historical replay, model-label calibration or large adjudication exercises unless a concrete planning decision genuinely depends on that information.

A real observed failure may motivate a **deterministic** regression for a faulty detector, handoff or workflow contract. Do not freeze the reviewer agent's one-time subjective conclusion as an immutable expected value.

## Validation strategy and completion boundary

Use focused deterministic checks and shared fixtures during broad implementation. Keep investigative execution separate from routine regression tests. Shard costly validations, reuse proven-current receipts, and run only the milestone's declared complete acceptance gates at the final boundary; if a release/consumer gate is required, its evidence remains necessary. Aggregate green tests do not prove individual acceptance criteria unless the scenarios establish those criteria.

Review cost is a separate production constraint. Rule inclusion and gate budgets must consider expensive agent attention, not just fast C# detector execution. A legitimate `acceptable; no code change` outcome is useful evidence, not a failed experiment.

## Local planning ownership and guide adoption

This project uses its pinned agentic guide as shared methodology, while `docs/PROJECT-GUIDANCE.md` and operative engineering/specification documents own **project-specific interpretations and decisions**. Local observations do not become shared guide requirements automatically.

- The **planner/project owner** owns guide adoption, local profile/pin selection, and reconciliation of project-level normative agent/planning authority. Apply straightforward methodology/metadata/routing updates directly in a distinct authorized project change; do not manufacture an implementation milestone and hand methodology interpretation to an ordinary product implementer. Only a substantial, already resolved coordinated migration warrants its own implementation handoff.
- Keep guide adoption conceptually separate from a product correction or exploratory experiment. The product implementer executes the ready contract and updates directly affected product code, tests, and documentation, but does **not** decide how shared methodology should change or silently reclassify project authority.
- Historical experiments and completed milestones remain evidence. Promote enduring conclusions into applicable **project-owned** authority for future planning; do not silently expand an existing ready milestone or retroactively rewrite completed obligations.

## Autoformat and SDK-backed validation economics

Autoformat combines **project-owned** Roslyn canonicalization/layout and orchestration with **SDK-owned** formatting and style fixes (including unused-import removal through `IDE0005`). Maintain this ownership boundary: inspect and test our command arguments, project/restore context, phase integration, source safety, and check-versus-mutate behavior; do not independently reproduce the SDK's unused-import algorithm or test its entire ruleset. If SDK behavior fails under our invocation, investigate the SDK execution context first rather than inventing a replacement analyzer.

For each proposed fixture, integration run, real-repository exercise, or test retained in the default suite, ask **what distinct material failure it can reveal beyond checks already planned or performed**, and account for both one-time and recurring cost. Agent-generated tests are not free to execute: repeated `dotnet restore`, MSBuild/Roslyn workspace initialization, analyzer loading, and `dotnet format` subprocesses can dominate normal feedback.

- Prefer fast deterministic tests for our custom transformations, configuration, argument forwarding, and orchestrator decisions. Use a real SDK invocation only where the SDK/project boundary is material to correctness or the implementation is otherwise not credibly established.
- An explicitly authorized, reproducible real-repository experiment **may supply the milestone's real SDK integration evidence**. Do **not** also require a synthetic SDK-style fixture for substantially the same behavior by default. A separate fixture or permanent integration regression is warranted when it contributes distinct failure isolation, repeatability, future regression detection, or consequence coverage sufficient to justify its continuing cost.
- Keep exploratory real-repository workloads **outside the default automated suite**. If an existing SDK integration test is expensive, avoid running it unconditionally during every narrow change unless its diagnostic value warrants that cost. Do not split test projects, add categories or create new orchestration infrastructure solely to enforce a conceptual unit/integration/release taxonomy.
- Package creation, installed `dotnet-autoformat` invocation, and consumer-distribution checks belong to **release qualification** unless the milestone itself changes that boundary or a demonstrated material risk demands earlier testing. A source-project diagnostic build can explain formatter-induced breakage without becoming an unconditional Autoformat runtime or milestone gate.

Future milestone planners must select the **cheapest sufficient non-duplicative evidence** for the actual suspected defects. This guidance applies prospectively; it does not amend the already ready M0029 acceptance contract.

## Application to the next semantic portfolio milestone

M0025 is a planning candidate, **not declared ready by this document**. Candidate families include abstraction necessity, delegation overhead, mechanism explicitness, type responsibility, duplicated authority, lifecycle boundaries, behavioral contract/error handling, and test evidence quality. Inventory and critically reassess existing rules before expanding; BORINGness may be decomposed, merged with type-responsibility, or removed without compatibility preservation.

Useful background: [Semantic Rule Portfolio Strategy](research/SEMANTIC-RULE-PORTFOLIO-STRATEGY.md), [Verifiable Review Work](research/VERIFIABLE-REVIEW-WORK.md), [Product Direction](research/PRODUCT-DIRECTION.md), and the operative [Engineering](ENGINEERING.md) and [Semantic Review](specs/SEMANTIC-REVIEWS.md) specifications. Existing production contracts remain operative until a ready milestone deliberately replaces them.
