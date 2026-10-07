# Execution Ledger — M0008 Rule Locality & Agent Routing Hygiene

Primary milestone: `docs/milestones/M0008-rule-locality-and-agent-routing.md`

This file is operational implementation state. It is not project authority and does not amend the ready milestone.

Planning seeds the lossless obligation registry, evidence cases, and validation gates. Implementation owns work-package decomposition, concrete mechanics, evidence, status progression, and resume state.

The execution ledger may compress work. It must not compress obligations.

## Milestone Obligation Registry

Planner-owned columns are `ID`, `Type`, and `Obligation`. Implementation must not delete, merge, renumber, paraphrase, or replace planner-seeded obligation rows.

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | Root `AGENTS.md` is a concise repository-level operational router with no hard-coded current/completed milestone ID/path/ledger or milestone-specific execution constraints. | WP-01 | Root AGENTS.md restored as stable repository routing; no milestone-specific path or execution contract. | VAL-06 | satisfied |
| AC-02 | acceptance | `AGENTS.md` routes ordinary implementation to repository-local engineering/milestone authority and does not require the external guide repository or planning conversation. | WP-01 | AGENTS.md routes to repository-local engineering, milestone authority, source, and tests. | VAL-06 | satisfied |
| AC-03 | acceptance | Production rule source is organized under `src/DotNetAiCodeHygiene.Core/Rules/` with `Documentation`, `SemanticReview`, `Readability`, and `Profile` family directories plus shared rule execution/catalog files. | WP-02 | Nine modules live in Rules/Documentation, SemanticReview, Readability, and Profile; Rules/RuleExecution.cs owns shared contracts/catalog. | VAL-06 | satisfied |
| AC-04 | acceptance | Every accepted production rule module has one obvious owning source file; no category bucket source file contains multiple unrelated production rule implementations. | WP-02 | Nine distinct production class files; former category bucket files removed. | VAL-01, VAL-06 | satisfied |
| AC-05 | acceptance | Each registered production rule owns its descriptor at the rule locality boundary, while the catalog preserves explicit static canonical ordering without maintaining a separate manually synchronized descriptor definition table. | WP-02 | Each module declares its descriptor; RuleCatalog.Modules is explicitly ordered and All derives from registered descriptors. | VAL-01, VAL-06 | satisfied |
| AC-06 | acceptance | Fixed finding/purpose/suggestion/observation/reason/constraint text specific to deterministic/profile rules is declared at the corresponding rule locality boundary and not scattered through central host or unrelated shared-helper files. | WP-02 | All fixed XML-rule messages/templates, including empty param/typeparam messages and missing-name observation, are owned by DocumentationXmlConsistencyRuleModule; shared evaluation contains mechanics only. | VAL-01, VAL-06 | satisfied |
| AC-07 | acceptance | Quality and German semantic-review rules each own their fixed questions/rubric and other rule-specific reviewer/escalation text while continuing to use shared deterministic review mechanics. | WP-02 | Quality and German modules each own exact question lists and escalation text; shared batch mechanics remain shared. | VAL-01, VAL-02, VAL-06 | satisfied |
| AC-08 | acceptance | Documentation subjects/facts, semantic-review population/batch mechanics, profile inspection/remediation infrastructure, session infrastructure, and generic host text remain shared where justified rather than being duplicated into rule files. | WP-02 | Documentation facts, review population/batch mechanics, ProfileManager, RepositorySession, host materialization, and rewrites remain shared. | VAL-01, VAL-06 | satisfied |
| AC-09 | acceptance | M0008 introduces no `.resx`, localization service, generic message registry, generated rule metadata, rule-definition DSL/framework, reflection discovery, DI framework, or new runtime dependency. | WP-02 | Source/dependency review found no new framework, resource, registry, reflection, DI, or runtime dependency. | VAL-05, VAL-06 | satisfied |
| AC-10 | acceptance | Rule IDs, versions, order, output kinds, classifications, purposes, configurability, and mandatory/configurable participation exactly match the accepted M0006/M0007 baseline. | WP-02, WP-03 | Built CLI rules JSON returned nine canonical descriptor records; tests and installed workflow passed. | VAL-01..05 | satisfied |
| AC-11 | acceptance | Established deterministic finding text, profile diagnostic text, semantic-review questions/rubrics/escalation behavior, JSON/text output, fingerprints/discriminators, and review batch/expand/handoff behavior remain regression-compatible. | WP-02, WP-03 | Core/CLI behavior suites, isolated fixture suite, and installed-tool lifecycle passed unchanged. | VAL-01..05 | satisfied |
| AC-12 | acceptance | M0007's shared lazy `RepositorySession`, explicit production runner, shared facts, host-owned result mechanics, and distinct rewrite architecture remain intact; the cleanup does not re-centralize rule semantics. | WP-02 | Shared session/facts and host materialization remain; rewrite contracts are untouched; focused session tests passed. | VAL-01, VAL-03, VAL-05, VAL-06 | satisfied |
| AC-13 | acceptance | `docs/MILESTONES.md` records M0006 and M0007 as done, records M0008 as ready, and does not reopen or rewrite the already-complete M0007 milestone/ledger. | WP-01 | On entry, M0007 was already complete; the conditional completion/review reconciliation was already satisfied/no-op. At implementation completion, the index recorded M0006/M0007 done and M0008 ready; after this approval it records M0008 done. M0007 records remain untouched. | VAL-06 | satisfied |
| AC-14 | documentation | Architecture, engineering, terminology, milestone index, and agent-routing documentation consistently describe the rule-locality and stable-agent-routing model. | WP-01, WP-02 | AGENTS.md and Architecture, Engineering, Terminology, Milestones docs describe locality/routing. | VAL-06 | satisfied |
| AC-15 | acceptance | The diff is limited to M0008 locality/routing work, required tests/evidence, and direct documentation/index updates; unrelated hygiene findings remain deferred. | WP-01..03 | Diff is limited to M0008 routing, family moves/text ownership, docs/index, and evidence. | VAL-05, VAL-06 | satisfied |
| REV-01 | review | Project owner/delegate confirms that a future ordinary rule has an obvious small home, its fixed contract text is easy to find, shared infrastructure has not been over-generalized, and `AGENTS.md` is stable rather than milestone-bound. | Human review | Project owner approved REV-M0008-COMPLETION in this task conversation on 2026-10-07 after PR 11 merged to main at `56ce4a5`. | VAL-07 | satisfied |

## Evidence Case Registry

Planner-owned columns are `ID`, `Parent obligation`, and `Required evidence case`. Implementation must not delete, merge, renumber, paraphrase, or replace planner-seeded evidence cases.

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-01a | AC-01 | root `AGENTS.md` contains no `M000*`, hard-coded milestone file, or `.execution/M000*` routing and still provides a usable default implementation path | VAL-06 | AGENTS.md content and search show no M000* routing; default path names engineering, task, authority, and source/tests. | satisfied |
| EC-04a | AC-04 | all nine accepted production rule modules are individually locatable as one rule per source file | VAL-01, VAL-06 | Rules tree contains nine production modules, each in its own family-specific file. | satisfied |
| EC-05a | AC-05 | catalog order/descriptor enumeration comes from the registered production modules without a second independent descriptor table | VAL-01, VAL-06 | RuleExecution.cs explicitly registers modules canonically; All derives descriptors; LifecycleTests asserts canonical IDs/order. | satisfied |
| EC-06a | AC-06 | representative documentation/readability/profile rule fixed finding text is owned at its rule locality boundary | VAL-01, VAL-06 | Documentation, readability, and profile rule text resides with modules; DocumentationRuleEvaluation contains only shared traversal/decision mechanics and calls rule-owned text helpers for empty param/typeparam messages and missing-name observation. | satisfied |
| EC-07a | AC-07 | quality and German review questions remain distinct, exact, and rule-owned | VAL-01, VAL-02, VAL-06 | Quality Q1-Q3 and German Q1 remain exact/distinct in respective modules; Core/CLI semantic-review tests pass. | satisfied |
| EC-10a | AC-10 | `rules --output json` / equivalent built-CLI evidence preserves all nine descriptor records and canonical order | VAL-02, VAL-04 | Built Release CLI rules --output json emitted nine ordered records with exact descriptor fields; installed validation passed. | satisfied |
| EC-11a | AC-11 | representative deterministic/profile output preserves exact established message fields and identity/fingerprint behavior | VAL-01..04 | Core/CLI regression, isolated fixture, and installed consumer validation passed; profile messages use rule-owned templates. | satisfied |
| EC-11b | AC-11 | quality/German check -> expand -> handoff preserves questions, batch identity/fingerprint behavior, and positional-record review representation | VAL-01..04 | Core/CLI regression passed review batches, expansion, handoff, fingerprints, positional record representation; Tier-4 passed. | satisfied |
| EC-12a | AC-12 | focused production rule-runner/session tests still prove lazy shared context and shared documentation fact reuse after file moves | VAL-01 | Core module/session tests passed, including lazy context and shared documentation fact tests. | satisfied |
| EC-13a | AC-13 | milestone index records M0006/M0007 as done and M0008 as ready while the completed M0007 milestone/ledger remain completion-history records rather than being reopened | VAL-06 | Original package planner wording/IDs match the milestone and ledger exactly. On entry M0007 was complete and its conditional reconciliation was satisfied/no-op; the index update is present and completed M0007 history remains unchanged. | satisfied |

## Validation Gates

| ID | Required validation | Target/locus | Proves evidence units | Status | Evidence |
|---|---|---|---|---|---|
| VAL-01 | focused Core tests for catalog descriptors/order, rule runner/session/facts, representative exact finding/review text, and semantic review | Tier 1 / Windows 11 + .NET 11 | AC-04..AC-12; EC-04a, EC-05a, EC-06a, EC-07a, EC-11a, EC-11b, EC-12a | satisfied | eng/validate.ps1 rerun: Release build passed; Core 51/51 passed. Focused XML cases EC16AndEC18OptionalDocumentationAndDeclarationElementsArePresenceBased and EC18bInvalidInlineReferencesAreRejectedWhileEnclosingTypeParametersAreInScope passed 2/2. |
| VAL-02 | built CLI tests including rules/check/review JSON behavior | Tier 1 / Windows 11 + .NET 11 | AC-10, AC-11; EC-10a, EC-11a, EC-11b | satisfied | CLI tests 22/22 passed in canonical validation; descriptor and review JSON behavior passed. |
| VAL-03 | isolated SDK-style Git fixture scenarios covering representative check/profile/review behavior | Tier 3 / Windows 11 + .NET 11 + Git | AC-10..AC-12 | satisfied | eng/validate.ps1 isolated SDK-style Git fixture scenarios passed. |
| VAL-04 | exact locally packed/installed tool representative workflow | Tier 4 / isolated Windows consumer repository | AC-10, AC-11; EC-10a, EC-11a, EC-11b | satisfied | Canonical rerun: exact locally packed/installed 0.5.0 consumer workflow passed after corrections. |
| VAL-05 | `./eng/validate.ps1` including repository self-host validation and `git diff --check` | Tier 2 / complete repository | AC-09..AC-15 | satisfied | Canonical eng/validate.ps1 rerun passed, including Release build, both test suites (73/73), Tier-4, repository self-host, and final git diff --check. |
| VAL-06 | direct source/document review of `AGENTS.md`, `Rules/` layout, text ownership, docs consistency, and M0007 completion bookkeeping | repository review | AC-01..AC-09, AC-13..AC-15; EC-01a, EC-04a, EC-05a, EC-06a, EC-07a, EC-13a | satisfied | Freshly extracted the specified package ZIP (SHA256 52E25A1465BD206ACD700F1EFEF5A644D891991B913CBA40447FA5AF24451D14) and compared planner-owned wording directly: all 16 milestone AC/REV rows, 10 milestone EC rows, 16 ledger AC/REV obligation fields, and 10 ledger EC required-evidence fields match. The alternate AC-13, AC-15, and EC-13a strings supplied in the correction request are not present in this ZIP; the package wording is preserved unchanged. M0007 was already complete on entry, so its conditional AC-13/EC-13a reconciliation path was satisfied/no-op. |
| VAL-07 | `REV-M0008-COMPLETION` | Human / project owner or delegate | REV-01 | passed | Project owner approved REV-M0008-COMPLETION in this task conversation on 2026-10-07 after PR 11 merged to main at `56ce4a5`. |

## Work Packages

- **WP-01 — Routing and authority index:** restore stable root AGENTS.md; reconcile docs/MILESTONES.md; apply the package architecture, engineering, and terminology updates.
- **WP-02 — Rule locality:** split the nine production modules into family directories and one class per file; move descriptors and fixed rule-specific text to the owning modules; keep shared facts, review/profile/session/host mechanisms shared.
- **WP-03 — Compatibility and evidence:** run focused/full canonical validation, inspect source/docs and built CLI descriptor output, reconcile each criterion/evidence case, and record the required human review.
## Resume Point

Current state:

```text
COMPLETE
```

Next action:

None. REV-M0008-COMPLETION is approved and recorded.


Known agent-resolvable gaps:

None; implementation and automated validation are complete.

External blockers or planning escalations:

None.
