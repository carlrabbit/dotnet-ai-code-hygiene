# REV-M0017-COMPLETION — Semantic Hygiene Rule Expansion

**Status:** Approved by the project owner on 2026-10-13.
**Review scope:** M0017 implementation on `implementation/m0017-semantic-hygiene-expansion`, based on merged `main` at `ec8ec547`; corrective response reviewed at commit `9609ebb`.

## Review request

The first independent review identified comment eligibility and grouping defects plus missing focused identity/lifecycle evidence. Those findings are corrected in this PR update. The project owner reviewed the corrected M0017 changes for contract fidelity, compatibility, source identity/context, and verification evidence and approved completion on 2026-10-13. The PR remains open for another independent GitHub review.

## Implemented scope

- `readability.control-flow.visual-block` is v2 with `review-candidate` classification. The syntax/trivia predicate is unchanged. Its message, suggestion, explanation surface, and CLI agent guidance ask for context-sensitive inspection; v1 ignores do not suppress v2 candidates. `format` and `normalize` do not implement this rule.
- `readability.type-responsibility.review` v1/B-4 reviews eligible source-backed type declarations with the fixed ownership, coupling, and separation questions; planner escalation is declared in the module.
- `tests.quality.review` v1/B-5 recognizes resolved TUnit, xUnit, NUnit, and MSTest method attributes, including derived attributes, and excludes name-only helpers/setup methods. It owns the fixed regression-evidence, contract-focus, and maintainability questions.
- `comments.intent.review` v1/B-6 discovers all ordinary active comment trivia, including copyright, license, legal, and administrative comments. It excludes directives, disabled text, XML docs, generated files, and comment-like literal contents. Contiguous standalone `//` lines group together; trailing comments stay separate from leading blocks. Stable syntax anchors and comment-free owner-code fingerprints support accepted-evidence lifecycle without absolute offsets. Truly identical duplicate blocks with the same owner and adjacent syntax use deterministic discovery ordinals; the ordinal can shift if that indistinguishable set changes. It owns the fixed intent, fidelity, and clarity questions; a comment is not defective only because it does not explain implementation behavior.
- All three new rules are separate explicit modules, default enabled, appended after B-3, and use the existing M0016 selector, tickets, accepted-only store, expansion, handoff, and no-refill lifecycle.
- The CLI/tool package is version 0.9.0. Operative rule specs, agent help, architecture/engineering guidance, and the milestone index are synchronized.

## Verification evidence

- Focused `M0017RuleTests` invocations passed 2/2: `NewRulesDiscoverTheirFixedSourceBackedPopulations` and `CommentDiscoverySeparatesSyntaxBoundariesAndPreservesStableLifecycleEvidence`. They cover legal/copyright eligibility, ordinary nearby comments, XML/directive/inactive/generated/literal exclusions, standalone grouping versus trailing comments, separate statement-owned blocks, accurate locations, deterministic order and current source context, stable identities/fingerprints under unrelated comment insertion/removal, accepted-evidence invalidation after comment edits, stale-ticket rejection, and no-refill acceptance. Duplicate blocks with identical text, owner, and adjacent syntax are documented as ordinal-disambiguated.
- Canonical `eng/validate.ps1` passed on Windows 11/.NET 11. Core 94/94 and CLI 24/24 passed; build had zero warnings/errors. `git diff --check` and repository self-check passed.
- The canonical gate freshly packed and installed `DotNetAiCodeHygiene.Tool` 0.9.0 into an isolated consumer. Its scenarios covered visual-block after format/normalize, zero-work checks, changed/base review batches, acceptance, expansion, handoff with current comment source, copyright/license eligibility, profile operations, and existing rewrite behavior. Package SHA-256: `5F9A821858C7DB1F13B160360D97B6A1977C763650F3E38AB64195DD6AA02AF9`.
- Verification tests tooling behavior only: no model/provider calls, subjective gold labels, reviewer-agreement metrics, or claims of semantic quality certification are used.

## Human decision

**APPROVED — project owner — 2026-10-13.** The decision was provided explicitly in the task conversation after review of the corrected PR branch at `9609ebb`. This records the project owner's decision; it is not an implementer self-approval. PR #18 remains open and unmerged for another independent review.
