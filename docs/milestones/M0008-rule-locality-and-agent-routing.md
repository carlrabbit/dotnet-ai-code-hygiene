# M0008 — Rule Locality & Agent Routing Hygiene

**State:** ready
**Mode:** AI-executed, human-reviewed
**Depends on:** completed M0007 modular analysis architecture

## Goal

Make the post-M0007 architecture locally navigable and durable for future rule growth.

The implementation must:

1. restore root `AGENTS.md` to stable repository-level operational routing rather than milestone-specific execution instructions;
2. reflect logical rule modularity in physical source layout;
3. make each product rule the obvious ownership boundary for its descriptor and fixed rule-specific user/reviewer-facing text;
4. preserve all existing product semantics and public output.

This is an architecture/locality cleanup, not a new rule or configuration milestone.

## Current problem

M0007 established independent in-process rule modules over a shared command-scoped session, but the source layout still reflects extraction history:

```text
DotNetAiCodeHygiene.Core/
  RuleModules.cs
  DocumentRuleModules.cs
  SemanticReviewRuleModules.cs
  ProfileRuleModules.cs
  DocumentationRuleEvaluation.cs
  ...
```

Several production rules are grouped into category bucket files. Rule descriptors are stored centrally in `RuleCatalog`, while fixed rule-specific interface text is distributed among modules, helper evaluators, `HygieneEngine`, and profile analysis code.

Separately, root `AGENTS.md` currently contains M0007-specific execution instructions. That is stale after milestone completion and violates the repository's guide-system intent: `AGENTS.md` is a stable operational router, while milestone-specific scope/evidence/completion instructions belong in the milestone, execution ledger, and execution prompt.

## Authority

Implementation starts from this milestone and reads only the authority needed for the work:

- `docs/ARCHITECTURE.md`
- `docs/ENGINEERING.md`
- `docs/TERMINOLOGY.md`
- `docs/specs/HYGIENE.md`
- `docs/specs/DOCUMENTATION.md`
- `docs/specs/SEMANTIC-REVIEWS.md`
- `docs/specs/PROFILE.md`
- `docs/specs/READABILITY.md`
- `docs/specs/REWRITES.md`
- M0006 and M0007 only as behavior/architecture compatibility baselines where needed

`docs/research/` is non-authoritative and is not required for this milestone.

The package's replacement `AGENTS.md`, `ARCHITECTURE.md`, `ENGINEERING.md`, `TERMINOLOGY.md`, and `MILESTONES.md` encode planning decisions for M0008.

## Planning baseline

M0007 is merged and complete.

The M0007 milestone and execution ledger already record the project-owner approval of `REV-M0007-COMPLETION` and the `COMPLETE` terminal state. The merged implementation is recorded there against commit `aa690728d555e5781f455d831a94a1c1ab03857a`; the planning baseline inspected for M0008 is current `main` at `df7e43a96f2fe77d12aa1556cae3c3613407b932`.

Do not reopen, re-review, or rewrite the completed M0007 milestone/ledger as part of M0008.

The remaining stale durable routing/index state on this baseline is:

- root `AGENTS.md` still points specifically at M0007;
- `docs/MILESTONES.md` still describes M0006/M0007 as prerequisite/ready rather than completed.

M0008 corrects those durable routing/index documents while reorganizing rule locality.

## Target source shape

The exact helper filenames remain implementation-owned, but production rule modules must be organized under this stable family layout:

```text
src/DotNetAiCodeHygiene.Core/
  Rules/
    <shared rule execution/catalog files>

    Documentation/
      <one file per production documentation rule>
      <shared documentation facts/helpers>

    SemanticReview/
      <one file per production semantic-review rule>
      <shared review-population helpers where justified>

    Readability/
      <one file per production readability rule>

    Profile/
      <one file per production profile rule>
      <rule-family helpers where justified>
```

The existing single Core project remains the project boundary.

`ProfileManager`, repository/session infrastructure, generic review mechanics, rewrites, and other non-rule product infrastructure need not be moved merely to make the tree look symmetrical. Move supporting code only when locality requires it.

## Resolved decisions

### Stable `AGENTS.md`

Root `AGENTS.md` is permanent repository routing, not the active milestone prompt.

It must:

- point implementation agents to `docs/ENGINEERING.md`, the relevant milestone/task, referenced authority, and relevant source/tests;
- tell ordinary implementation to use repository-local authority;
- identify coordination metadata that is normally ignored;
- preserve constrained-execution guidance at a generic level;
- contain no hard-coded current/completed milestone ID, milestone path, ledger path, work-package state, or milestone-specific non-goal.

Milestone-specific instructions remain in `docs/milestones/`, `.execution/`, and the execution prompt.

### One production rule per file

Each accepted production rule module has one obvious source file in its family.

Do not keep several unrelated production rule modules in category bucket files such as:

```text
DocumentRuleModules.cs
SemanticReviewRuleModules.cs
ProfileRuleModules.cs
```

A shared file may contain cohesive shared contracts/helpers, but it must not become another multi-rule bucket.

### Rule-owned descriptor

Each production rule owns its `Rule` descriptor close to its implementation.

`RuleCatalog` remains the explicit static ordered registration point, but it must not require a separate manually synchronized descriptor table when registered modules already expose their descriptors.

Canonical M0006 order, IDs, versions, output kinds, classifications, purposes, and configurability remain unchanged.

A simple shape such as:

```text
RuleCatalog.Modules = [ruleA, ruleB, ...]
RuleCatalog.All = Modules.Select(module => module.Descriptor)
```

is acceptable, but exact implementation is local freedom.

### Rule-owned interface text

Hard-coded C# text is intentional.

The fixed text specific to one rule must be declared at that rule's locality boundary, preferably in the same source file using constants/static data.

This includes, as applicable:

- descriptor purpose;
- finding message templates;
- suggestion templates;
- observation/reason/constraint templates;
- semantic-review questions/rubric;
- rule-specific reviewer/escalation text.

Dynamic evidence values may be interpolated during evaluation.

A shared helper/evaluator may use rule-owned text. It must not become the hidden owner of text belonging to several rules.

For profile rules, shared inspection/remediation infrastructure may remain shared, but the fixed diagnostic text/templates for each profile rule must be discoverable from the corresponding rule file rather than being owned only by a broad profile manager.

### Generic shared text remains shared

Text that is genuinely generic product infrastructure remains with the mechanism that owns it, for example:

- persistence/state corruption errors;
- unavailable source context;
- invalid command/handle errors;
- transaction/concurrency failures;
- generic batch mechanics that are not one rule's semantic policy.

Do not duplicate generic text into every rule merely to satisfy locality.

### No text/metadata framework

Do not introduce:

- `.resx` or localization infrastructure;
- a message registry/service;
- generated rule metadata;
- a generic `RuleDefinition<T>` framework solely for this cleanup;
- attributes/reflection discovery;
- a rule-definition DSL;
- DI;
- new runtime dependencies.

Plain C# and explicit registration are preferred.

### Preserve behavior exactly

M0008 changes locality, not semantics.

Preserve:

- all rule IDs and versions;
- canonical rule order;
- output kind/classification/configurability;
- all established finding/review text;
- fingerprints/discriminators;
- rule enable/disable policy;
- repository German-rule self-host configuration;
- semantic-review questions, batch numbering, ranking/fingerprints, expansion, and handoff;
- profile diagnosis/remediation behavior;
- target resolution;
- format/normalize behavior;
- public JSON/text shape.

If moving text exposes an accidental mismatch between source and current authority, do not silently "improve" wording in M0008. Preserve current accepted behavior and return any semantic wording change to a separate rule-policy milestone.

## Scope

In scope:

- stable `AGENTS.md`;
- `Rules/` family layout;
- one production rule per file;
- rule descriptor ownership/locality;
- rule-specific fixed interface-text ownership/locality;
- small supporting moves/splits required by that locality;
- focused regression tests needed to prove exact behavior;
- direct architecture/engineering/terminology/milestone documentation;
- milestone index reconciliation so M0006/M0007 are recorded as done and M0008 is the ready milestone.

## Non-goals

Out of scope:

- new rules;
- rule semantic changes;
- rule version changes;
- wording improvements;
- new configuration;
- new rewrite transformations;
- localization;
- resource files;
- plugin/discovery systems;
- rule metadata DSLs;
- broad namespace/project decomposition;
- generalized pipeline/DAG work;
- statistical sampling implementation;
- persistent caches/indexes;
- broad test-suite restructuring unrelated to proving this milestone;
- unrelated self-hygiene findings that the product will address separately.

## Acceptance criteria

**AC-01 — Stable agent routing.** Root `AGENTS.md` is a concise repository-level operational router with no hard-coded current/completed milestone ID/path/ledger or milestone-specific execution constraints.

**AC-02 — Repository-local execution contract.** `AGENTS.md` routes ordinary implementation to repository-local engineering/milestone authority and does not require the external guide repository or planning conversation.

**AC-03 — Rule-family layout.** Production rule source is organized under `src/DotNetAiCodeHygiene.Core/Rules/` with `Documentation`, `SemanticReview`, `Readability`, and `Profile` family directories plus shared rule execution/catalog files.

**AC-04 — One production rule per file.** Every accepted production rule module has one obvious owning source file; no category bucket source file contains multiple unrelated production rule implementations.

**AC-05 — Rule-owned descriptors.** Each registered production rule owns its descriptor at the rule locality boundary, while the catalog preserves explicit static canonical ordering without maintaining a separate manually synchronized descriptor definition table.

**AC-06 — Rule-owned deterministic text.** Fixed finding/purpose/suggestion/observation/reason/constraint text specific to deterministic/profile rules is declared at the corresponding rule locality boundary and not scattered through central host or unrelated shared-helper files.

**AC-07 — Rule-owned semantic-review text.** Quality and German semantic-review rules each own their fixed questions/rubric and other rule-specific reviewer/escalation text while continuing to use shared deterministic review mechanics.

**AC-08 — Shared code remains shared.** Documentation subjects/facts, semantic-review population/batch mechanics, profile inspection/remediation infrastructure, session infrastructure, and generic host text remain shared where justified rather than being duplicated into rule files.

**AC-09 — No localization/metadata framework.** M0008 introduces no `.resx`, localization service, generic message registry, generated rule metadata, rule-definition DSL/framework, reflection discovery, DI framework, or new runtime dependency.

**AC-10 — Exact rule catalog compatibility.** Rule IDs, versions, order, output kinds, classifications, purposes, configurability, and mandatory/configurable participation exactly match the accepted M0006/M0007 baseline.

**AC-11 — Exact interface/output compatibility.** Established deterministic finding text, profile diagnostic text, semantic-review questions/rubrics/escalation behavior, JSON/text output, fingerprints/discriminators, and review batch/expand/handoff behavior remain regression-compatible.

**AC-12 — Existing architecture preserved.** M0007's shared lazy `RepositorySession`, explicit production runner, shared facts, host-owned result mechanics, and distinct rewrite architecture remain intact; the cleanup does not re-centralize rule semantics.

**AC-13 — Milestone index reconciled.** `docs/MILESTONES.md` records M0006 and M0007 as done, records M0008 as ready, and does not reopen or rewrite the already-complete M0007 milestone/ledger.

**AC-14 — Documentation matches locality.** Architecture, engineering, terminology, milestone index, and agent-routing documentation consistently describe the rule-locality and stable-agent-routing model.

**AC-15 — No unrelated cleanup.** The diff is limited to M0008 locality/routing work, required tests/evidence, and direct documentation/index updates; unrelated hygiene findings remain deferred.

**REV-01 — Human completion review.** Project owner/delegate confirms that a future ordinary rule has an obvious small home, its fixed contract text is easy to find, shared infrastructure has not been over-generalized, and `AGENTS.md` is stable rather than milestone-bound.

## Required evidence cases

| ID | Parent | Required evidence case |
|---|---|---|
| EC-01a | AC-01 | root `AGENTS.md` contains no `M000*`, hard-coded milestone file, or `.execution/M000*` routing and still provides a usable default implementation path |
| EC-04a | AC-04 | all nine accepted production rule modules are individually locatable as one rule per source file |
| EC-05a | AC-05 | catalog order/descriptor enumeration comes from the registered production modules without a second independent descriptor table |
| EC-06a | AC-06 | representative documentation/readability/profile rule fixed finding text is owned at its rule locality boundary |
| EC-07a | AC-07 | quality and German review questions remain distinct, exact, and rule-owned |
| EC-10a | AC-10 | `rules --output json` / equivalent built-CLI evidence preserves all nine descriptor records and canonical order |
| EC-11a | AC-11 | representative deterministic/profile output preserves exact established message fields and identity/fingerprint behavior |
| EC-11b | AC-11 | quality/German check -> expand -> handoff preserves questions, batch identity/fingerprint behavior, and positional-record review representation |
| EC-12a | AC-12 | focused production rule-runner/session tests still prove lazy shared context and shared documentation fact reuse after file moves |
| EC-13a | AC-13 | milestone index records M0006/M0007 as done and M0008 as ready while the completed M0007 milestone/ledger remain completion-history records rather than being reopened |

## Validation gates

| ID | Required validation | Target/locus | Proves |
|---|---|---|---|
| VAL-01 | focused Core tests for catalog descriptors/order, rule runner/session/facts, representative exact finding/review text, and semantic review | Tier 1 / Windows 11 + .NET 11 | AC-04..AC-12; EC-04a, EC-05a, EC-06a, EC-07a, EC-11a, EC-11b, EC-12a |
| VAL-02 | built CLI tests including rules/check/review JSON behavior | Tier 1 / Windows 11 + .NET 11 | AC-10, AC-11; EC-10a, EC-11a, EC-11b |
| VAL-03 | isolated SDK-style Git fixture scenarios covering representative check/profile/review behavior | Tier 3 / Windows 11 + .NET 11 + Git | AC-10..AC-12 |
| VAL-04 | exact locally packed/installed tool representative workflow | Tier 4 / isolated Windows consumer repository | AC-10, AC-11 |
| VAL-05 | `./eng/validate.ps1` including repository self-host validation and `git diff --check` | Tier 2 / complete repository | AC-09..AC-15 |
| VAL-06 | direct source/document review of `AGENTS.md`, `Rules/` layout, text ownership, docs consistency, and M0007 completion bookkeeping | repository review | AC-01..AC-09, AC-13..AC-15; EC-01a, EC-04a, EC-05a, EC-06a, EC-07a, EC-13a |
| VAL-07 | `REV-M0008-COMPLETION` | human / project owner or delegate | REV-01 |

`eng/validate.ps1` remains the canonical aggregate validation command. If its current Tier-4 workflow already exercises the required installed-tool surface, do not create a second parallel validation harness.

## Implementation guidance

Prefer moves/splits over rewrites.

A likely sequence is:

```text
confirm completed M0007 milestone/ledger remain untouched
-> establish stable AGENTS.md
-> create Rules/ family layout
-> move/split rule modules
-> move descriptor ownership into rule files
-> move rule-specific fixed text into rule files
-> keep/relocate shared family helpers as needed
-> make catalog derive descriptor enumeration from registered modules
-> strengthen exact-output tests where current coverage is weak
-> run focused validation
-> run canonical full validation
-> reconcile docs/evidence
-> human review
```

This sequence is guidance, not a mandatory work-package decomposition.

## Human review

Canonical review ID:

```text
REV-M0008-COMPLETION
```

Review subject:

- root `AGENTS.md`;
- `Rules/` physical tree;
- one representative deterministic rule;
- one semantic-review rule;
- one profile rule;
- catalog/runner;
- shared documentation/review/profile helpers;
- validation evidence proving behavior/text compatibility.

Acceptance questions:

1. Can a future implementer find one rule and its fixed contract text without searching central engine/bucket files?
2. Is the directory structure obvious without requiring framework archaeology?
3. Did the implementation avoid a resource/message/metadata framework?
4. Is shared infrastructure still shared for semantic rather than cosmetic reasons?
5. Is `AGENTS.md` stable and useful for the next milestone rather than bound to M0008?
6. Is public behavior unchanged?

Waiver policy: explicit project-owner decision only.

## Completion expectations

Implementation must:

```text
read milestone + planning-seeded ledger
-> verify obligation/evidence-case equality
-> confirm M0007 completion evidence is already durable
-> inspect live merged M0007 implementation
-> derive bounded work packages
-> implement locality/routing cleanup
-> run focused + canonical validation
-> freshly reread milestone
-> reconcile every obligation/evidence case <-> ledger <-> repository/evidence
-> durable completion evidence
-> required human review
```

Passing tests alone does not establish the locality obligations; source/document review is required.

## Completion evidence

Implementation and automated validation are complete. Nine production rules now have individual family-local source files that own their descriptors and fixed rule-specific interface text. Root AGENTS.md is stable repository routing; the M0006/M0007 milestone index is reconciled, and completed M0007 history remains unchanged.

Validation: eng/validate.ps1 passed on Windows 11/.NET 11.0.100-rc.1.26425.128; Release build succeeded; Core 51/51 and CLI 22/22 passed; exact locally packed/installed consumer validation and repository self-host checks passed. Built Release rules --output json returned all nine descriptors in canonical order. Final git diff --check passed. Detailed criterion/evidence mapping is in .execution/M0008-rule-locality-and-agent-routing.md.

Human completion review REV-M0008-COMPLETION remains pending.

Current milestone outcome:

```text
AWAITING HUMAN REVIEW
```
