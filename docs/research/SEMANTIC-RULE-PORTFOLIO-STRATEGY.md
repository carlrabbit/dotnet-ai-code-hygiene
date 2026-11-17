# Semantic Rule Portfolio Strategy — Agentic Development and Evaluation

## Status

Planning rationale and evidence context, **not implementation authority**. The resolved project planning policy is [docs/PROJECT-GUIDANCE.md](../PROJECT-GUIDANCE.md); concrete rule behavior is owned by future ready milestones and operative specifications.

## Decision context (November 2026)

This project is pre-1.0 and agent-implemented. Earlier discussions of rule expansion implicitly optimized for human teams: implement a handful of rules, exhaustively test semantic 'good/bad' examples, review them, then repeat. This carries avoidable planning/review overhead and misunderstands what a semantic rule's software can establish.

The new direction is to **implement broad candidate portfolios, compare them against actual work, and consolidate aggressively**, with no backwards-compatibility obligation to preserve experimental rule identities, state formats or public shapes. Broad candidate implementation does **not** imply broad agent work at ordinary production gates.

The current five-question `architecture.boringness.review` mixes speculative abstraction, forwarding layers, change locality, implicit machinery and ownership. `readability.type-responsibility.review` already overlaps its ownership question. They illustrate why product rules should describe specific design decisions with different discovery populations and reviewer contexts, not one omnibus judgment scored by the number of correlated 'yes' answers.

## Sources of candidate failure modes

- Existing semantic rule portfolio and its current real C# candidate populations.
- The Agentic Project Guide's acceptance-evidence, validation lifecycle, decision-authority and implementation responsibilities.
- Project-owner engineering Leitfäden (including the Fin documents used with `carlrabbit/Private`) as *examples of the failure modes that design prescriptions try to prevent*. They are not universal .NET obligations, and private source material is not copied here.
- Actual changes, corrections and code-review findings from authorized repositories.

Promising areas: interfaces without a real boundary, unnecessary forwarding, hidden dynamic mechanisms, duplicated authoritative state, lifecycle-bound state/configuration, responsibility crossing, weak behavioral assertions, missing meaningful failure-path evidence, and misleading comments or documentation.

A useful rule description differentiates: cheap **candidate signal**; required real context; **decision question**; legitimate counterexample; concrete correction/escalation. A candidate is not a defect.

## Why synthetic semantic-verdict testing is the wrong tool

Semantic goodness is not a well-defined compiler predicate. A synthetic class, interface, comment or test case cannot reproduce the real reason the design exists, external contracts, operational constraints, available alternatives and system evolution.

Writing 'bad design' and 'good design' examples with a fixed expected LLM verdict creates an illusion of ground truth. Model responses can vary even for identical text and are sensitive to prompt, context, model version and agent behavior. Repeating such tests consumes tokens and does **not** yield a repeatable semantic quality guarantee; making the prompt rigid merely benchmarks obedience to a curated label.

The project already made this distinction in [Verifiable Review Work](VERIFIABLE-REVIEW-WORK.md): trigger fidelity and review-work fidelity can be mechanically established; external agent judgment cannot. Its M0017 guidance explicitly rejects curated semantic labels, precision scores, model-run gates and economic-return demonstrations as software milestone acceptance.

Historical M0013–M0015 sampling/replay research further illustrates the gap. Deterministic replay can show reproducible *selection identity* and schedule cost without showing that selected subjects contain real defects. The limited M0015 blinded assessment returned zero actionable findings for the assessed subjects; that cannot generalize to candidate-rule quality or yield. This is historical evidence, not a reason to resume expensive repeated semantic benchmarking. See [Gate-local Sampling Prototype](GATE-LOCAL-SAMPLING-PROTOTYPE.md) and [Sampling Gate Pivot](SAMPLING-GATE-PIVOT.md).

**Important distinction:** Controlled compiler fixtures are useful and repeatable for predicate eligibility, syntax/semantic extraction, source spans, cross-file references, state and CLI contracts. Reject only the claim that a fixture-plus-LLM judgment can serve as deterministic proof of subjective semantic correctness.

## Implementation and evaluation economics

Treat implementation and evaluation costs separately:

| Cost surface | Direction |
|---|---|
| Rule source and deterministic regression code | Implement several coherent candidates together; use parallel agent work where independent; accept temporary implementation duplication |
| Acceptance validation and installed-consumer checks | Use existing staged/sharded verification and reuse only proven-current evidence; avoid re-running full gates during unfinished implementation |
| External semantic review | Treat as expensive; keep production admission bounded; spend evaluation effort only where it could change portfolio decisions |
| Planning and independent review | Avoid repeated narrow milestone cycles that create more coordination cost than combined implementation; retain clear independent completion obligations |

A large, meaningful milestone is suitable when its semantic contracts and completion evidence can be planned without new material decisions. If final rule selection depends on unavailable information, the implementer may deliver candidate functionality and bounded observations but **does not silently decide authoritative rule policy**. Planning must settle that boundary explicitly.

## Comparative evaluation: what is informative

Optional exploratory evaluation uses real repository changes and actual source/requirements context, with provenance and candid uncertainty. Record which rules select the same underlying issue, which produce useful action versus appropriate no-change, what context is missing, which changes become worse after purported fixes, and agent-review cost. Prefer a bounded number of meaningful case studies to large repeated model grading or exact accuracy percentages.

Outcomes need not be reproducible model verdicts. Preserve source revision, detector/rule version, selected context and observed result for traceability; report disagreements and uncertainty. Never turn these observations into mandatory frozen semantic labels or product-wide defect-rate guarantees.

Comparative evaluation may legitimately conclude that an implemented candidate should be deleted, merged with an existing concern, kept as an optional review, or made cheaper through narrower discovery. Retaining a rule because of implementation sunk cost is not a product objective.

## Open planning decisions for M0025

These require explicit resolution before production behavior changes, not speculative framework work:

- Which current rules should be split, merged or deleted (especially BORINGness versus type responsibility)?
- Which candidate families have genuinely different subject populations versus better signals for the same existing rule?
- How should a shared architectural review budget allocate scarce agent attention without a five-per-subrule multiplication?
- What bounded real-repository observations, if any, are necessary to choose between alternatives?
- What post-consolidation rule identities, review questions, escalation paths, and state/reset semantics define the next operative contract?

No fixed count of candidate rules, synthetic benchmark suite, precision target or mandatory exhaustive historical replay is prescribed.
