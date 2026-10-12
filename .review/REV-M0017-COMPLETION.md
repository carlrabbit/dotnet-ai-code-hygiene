# REV-M0017-COMPLETION — Semantic Hygiene Rule Expansion

**Status:** Prepared for independent project-owner/delegate review; no human decision recorded.  
**Review scope:** M0017 implementation on `implementation/m0017-semantic-hygiene-expansion`, based on merged `main` at `ec8ec547`.

## Review request

Please review the M0017 changes for contract fidelity, compatibility, source identity/context, and verification evidence. This is a blocking completion review; implementation verification does not constitute approval. The branch must remain unmerged until the project owner or delegate records a decision.

## Implemented scope

- `readability.control-flow.visual-block` is v2 with `review-candidate` classification. The syntax/trivia predicate is unchanged. Its message, suggestion, explanation surface, and CLI agent guidance ask for context-sensitive inspection; v1 ignores do not suppress v2 candidates. `format` and `normalize` do not implement this rule.
- `readability.type-responsibility.review` v1/B-4 reviews eligible source-backed type declarations with the fixed ownership, coupling, and separation questions; planner escalation is declared in the module.
- `tests.quality.review` v1/B-5 recognizes resolved TUnit, xUnit, NUnit, and MSTest method attributes, including derived attributes, and excludes name-only helpers/setup methods. It owns the fixed regression-evidence, contract-focus, and maintainability questions.
- `comments.intent.review` v1/B-6 groups ordinary active comment trivia, with exclusions for directives, disabled text, XML docs, generated files, license/copyright headers, and literal contents. It owns the fixed intent, fidelity, and clarity questions.
- All three new rules are separate explicit modules, default enabled, appended after B-3, and use the existing M0016 selector, tickets, accepted-only store, expansion, handoff, and no-refill lifecycle.
- The CLI/tool package is version 0.9.0. Operative rule specs, agent help, architecture/engineering guidance, and the milestone index are synchronized.

## Verification evidence

- Focused `M0017RuleTests`: 2/2 pass. Fixtures cover nested/partial and excluded type forms, all listed framework attribute families and derived attributes, false-positive helpers/setup/custom attributes, comment grouping/exclusions/duplicate identity, identity stability after unrelated leading edits, fixed questions and batches, explicit accept/nonrefill, visual-block v2 metadata, legacy v1 ignore incompatibility, and current ignore/unignore.
- Full Core and CLI suites passed in the canonical validation; final aggregate evidence is recorded in `.execution/M0017-semantic-hygiene-rule-expansion.md` after that run completes.
- The canonical `eng/validate.ps1` gate packs and installs the exact freshly built `DotNetAiCodeHygiene.Tool` 0.9.0 package into an isolated consumer. Its scenarios cover visual-block after format/normalize, zero-work checks, changed/base review batches, acceptance, expansion, handoff, profile operations, and existing rewrite behavior.
- Verification tests tooling behavior only: no model/provider calls, subjective gold labels, reviewer-agreement metrics, or claims of semantic quality certification are used.

## Human decision

**Not recorded.** Project owner/delegate: please record an explicit accept/reject decision and date here after reviewing the implementation branch and its evidence. Do not infer approval from this prepared request or from passing software validation.
