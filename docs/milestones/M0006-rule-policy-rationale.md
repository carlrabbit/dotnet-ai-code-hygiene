# Milestone — M0006 Rule Policy Rationale & Semantic Review Separation

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | planned |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Baseline executor readiness | confirmed |
| Decision preservation | confirmed |
| Execution tractability | confirmed |
| Execution ledger | `.execution/M0006-rule-policy-rationale.md` planning-seeded |
| Scope size | focused policy/specification correction with small rule-contract change |
| Implementation autonomy | high within resolved contracts |
| Documentation sync | required for affected rule/spec/public guidance |
| Focused validation | Tier 1 Core + built CLI process |
| Repository validation | Tier 2 `./eng/validate.ps1` |
| Integration validation | Tier 3 isolated SDK-style Git repositories |
| Consumer validation | installed current tool when public rule behavior changes |
| Human review | required |

## Prerequisite and motivation

M0005 introduced the supported .NET profile and documentation-hygiene rules. First self-hosting/dogfooding showed that the execution model works, but also exposed ambiguity in how rule intent is documented:

- `docs.summary.quality.review` combines language-neutral summary quality with a fixed German-language requirement even though its ID implies general quality;
- the repository itself is English, so the German policy is useful product behavior for another project but should be disabled here rather than parameterized or weakened;
- `readability.control-flow.visual-block` produced many findings because AI implementation tends to omit deliberate visual control-flow boundaries; this is intended policy, not formatter noise;
- `docs.summary.required` deliberately covers public and internal subjects because its scope is handcrafted for human/agent comprehension, not equivalent to externally shipped package API;
- several rules specify mechanics accurately but do not preserve enough rationale for a future planner/reviewer to distinguish intentional opinion from accidental implementation detail.

The milestone promotes rationale already present in project research, prior milestones, specifications, and dogfooding evidence into durable authority. It must not invent generic configurability to resolve policy differences.

## Goal

Make the rule model and every current rule self-explanatory enough that a future planner, implementer, reviewer, or consuming agent can answer both:

1. **what exactly does this rule mean?**
2. **why does this product deliberately enforce or review that condition?**

At the same time, separate German-language policy from language-neutral summary-quality review so rule IDs continue to denote coherent fixed semantic contracts.

## Product rule model

The existing product direction is retained and made explicit:

```text
one rule ID
-> one fixed semantic contract
-> no repository-supplied rule parameters

repository policy
-> mandatory rule, or
-> optional fixed rule enabled/disabled as a whole
```

For optional rules, “configurable” means **participation only**: enable or disable the fixed rule.

It must not mean configurable:

- language;
- thresholds;
- covered symbol categories;
- severity/classification;
- semantic questions;
- sampling size;
- formatting/readability convention;
- remediation semantics;
- rule-specific modes.

When two independently applicable policies can sensibly be enabled separately, they should be separate rules rather than parameters or an unrelated bundle under one rule ID.

## Summary semantic-review split

### `docs.summary.quality.review`

Advance the existing rule to version 3.

It becomes language-neutral and evaluates only:

1. technical correctness;
2. information value;
3. clarity/scope.

Its summary-subject/carrier population, deterministic ranking, sample maximum, implementer/frontier protocol, and expansion/handoff model remain otherwise compatible unless implementation evidence requires a narrowly documented correction.

The rationale is that these are semantic prose-quality questions that deterministic syntax/structure checks cannot establish reliably.

### `docs.summary.language.german.review`

Introduce version 1 as a separate optional semantic-review rule.

Its fixed policy is:

> The selected documentation summary is natural, comprehensible German rather than awkward literal translation or merely German-looking text.

German is part of this rule's identity and semantics. There is no language parameter.

A repository that does not require German documentation disables this rule.

The dotnet-ai-code-hygiene repository is such a repository: it remains English and must disable this German-language rule in committed Hygiene configuration while retaining `docs.summary.quality.review`.

## Rule rationale audit

Implementation must make rationale explicit for every current rule. The table below records the planning conclusion and existing provenance.

| Rule | Rationale status before M0006 | Rationale to preserve/promote | Existing provenance |
|---|---|---|---|
| `profile.dotnet.analysis.required` | mostly explicit, but distributed | Hygiene owns one predictable tested .NET analyzer/code-style baseline so agents do not depend on ambient/default analyzer state; only explicitly selected diagnostics are hardened, avoiding indiscriminate warnings-as-errors policy. | `docs/SPECS.md`; `docs/specs/PROFILE.md`; M0005 decisions |
| `profile.stylecop.prohibited` | mostly explicit, but concise | Hygiene owns the supported policy instead of composing overlapping style-policy stacks; StyleCop is therefore rejected rather than silently coexisting. Automatic removal is unsafe because dependency/policy impact requires caller judgment. | `docs/SPECS.md`; `docs/specs/PROFILE.md`; M0005 goal/non-goals |
| `docs.summary.required` | mechanics explicit, rationale insufficient | Summaries reduce reconstruction cost for humans and agents at important source-level API subjects. Public and internal coverage is deliberate and is not a proxy for externally shipped API surface. Only a generic summary is required to avoid StyleCop-like completeness/boilerplate volume. | M0005 goal/decisions; `docs/research/HUMAN-CONSUMABILITY.md`; `docs/research/PRODUCT-DIRECTION.md` |
| `docs.xml.consistent` | mechanics explicit, rationale insufficient | Optional XML documentation stays optional, but prose/structure that does exist should not be misleading, orphaned, duplicated, or structurally invalid. Presence-based validation preserves useful documentation without creating completeness pressure. | M0005 documentation policy/non-goals; `docs/specs/DOCUMENTATION.md` |
| `docs.text.sentence` | mechanics explicit, rationale insufficient | A cheap deterministic prose baseline catches visibly unfinished documentation without pretending to judge semantic quality. It is intentionally narrow and mechanical. | M0005 deterministic/semantic separation; `docs/specs/DOCUMENTATION.md`; product direction “deterministic before semantic” |
| `docs.summary.quality.review` v3 | misleading combined contract | Semantic review exists for correctness/value/clarity because deterministic proxies would be brittle. Language policy is removed so this rule's name and contract match. | M0003 semantic-review model; product direction; current M0005 review rubric minus German question |
| `docs.summary.language.german.review` v1 | new split from existing Q1 | Some consuming projects deliberately require German documentation quality. That fixed project policy remains supported, but as an independently toggleable rule rather than a parameter or hidden part of generic quality. | existing `docs.summary.quality.review` Q1; fixed-rule/no-parameter product model; self-hosting evidence |
| `readability.long-line.review` | semantics explicit, rationale partly implicit | Very long physical lines can hide multiple concepts/structures and raise reconstruction cost, but length alone does not prove a defect. Therefore the rule selects review candidates and explicitly rejects mechanical wrapping. | M0002 rule contract; `docs/research/HUMAN-CONSUMABILITY.md`; product direction deterministic-candidate/semantic-judgment pattern |
| `readability.control-flow.visual-block` | semantics explicit, rationale missing from current authority | Blank-line separation acts as structural punctuation between linear work and a control-flow group. AI-generated code often remains syntactically formatted while visually dense; explicit boundaries improve scanning and human catch-up. This is an intentional readability policy, not formatter output. | M0002 rule contract; `docs/research/HUMAN-CONSUMABILITY.md`; BORING/local readability direction; M0005 dogfooding observation |

No current rule should be dropped or weakened merely because self-hosting exposes many occurrences. A high finding count is evidence about repository conformance and rule usability, not by itself evidence that the fixed policy is wrong.

## Documentation authority changes

Implementation must establish the following authority shape.

### `docs/SPECS.md`

Clarify that:

- rule IDs have fixed semantics;
- optional-rule configuration means enable/disable only;
- rules have no user parameters;
- independently applicable policies should use separate rule IDs.

Update canonical rule order for the summary-review split.

### `docs/specs/HYGIENE.md`

Make the fixed-semantics/toggleable-participation distinction normative.

The existing term `Configurable` may remain in the public schema for compatibility, but its specification meaning must be unambiguous: it denotes whether the whole fixed rule may be enabled/disabled.

### `docs/specs/DOCUMENTATION.md`

Add concise rationale alongside each documentation rule and make deliberate public/internal summary coverage explicit.

Move generic quality and German-language review into separate rule contracts/references.

### `docs/specs/SEMANTIC-REVIEWS.md`

Define `docs.summary.quality.review` v3 and `docs.summary.language.german.review` v1 independently, including questions, population, ranking/sample behavior, and escalation.

### `docs/specs/READABILITY.md`

Create this file as canonical authority for:

- `readability.long-line.review`;
- `readability.control-flow.visual-block`.

Move/retain their complete current semantics and add the rationale derived above. `docs/specs/HYGIENE.md` should reference this authority rather than leaving the rules historically anchored to M0002 prose.

### `docs/specs/PROFILE.md`

Add concise explicit rationale for both mandatory profile rules. Do not change their semantics.

### `docs/research/PRODUCT-DIRECTION.md`

Strengthen the existing “Opinionated before configurable” direction with the established fixed-rule/toggleable-participation distinction. Research remains rationale/provenance, not implementation authority.

### Repository self-hosting policy

Commit the repository's own Hygiene configuration so:

- `docs.summary.language.german.review` is disabled;
- `docs.summary.quality.review` remains enabled;
- `docs.summary.required` remains enabled;
- `readability.control-flow.visual-block` remains enabled;
- no rule semantics are changed to make self-hosting pass.

Document the reason briefly in engineering/self-hosting guidance.

## Scope

- split the combined summary semantic-review contract into language-neutral quality and fixed German-language review;
- version the changed quality rule appropriately;
- add the new German-language rule;
- update canonical rule order and rule discovery/help/output as required;
- preserve deterministic sampling/expansion/handoff behavior for each semantic rule;
- establish explicit fixed-rule/toggleable-participation semantics;
- audit and document rationale for every current rule;
- create canonical readability-rule specification;
- add repository self-hosting rule selection that disables only the German-language rule;
- update focused tests and installed-tool evidence affected by rule IDs/order/batches;
- update public/agent documentation where current wording implies generic quality includes German.

## Non-goals

- no rule parameters;
- no language selector;
- no threshold configuration;
- no per-project severity matrix for Hygiene rules;
- no weakening/removal of `docs.summary.required` internal coverage;
- no weakening/removal of visual-block policy because of current repository finding volume;
- no automatic semantic-review result ingestion;
- no model/provider invocation inside the CLI;
- no broad cleanup of pre-existing repository findings as part of this milestone;
- no new readability rules;
- no redesign of the rule-application pipeline;
- no change to mandatory profile-rule semantics;
- no general localization framework.

## Acceptance Criteria

- **AC-01** — Product/spec authority states unambiguously that one rule ID has one fixed semantic contract and optional-rule configuration means whole-rule enable/disable only; repository-supplied rule parameters are unsupported.
- **AC-02** — Authority states that independently applicable policies should use separate rule IDs rather than parameters or unrelated bundled semantics.
- **AC-03** — `docs.summary.quality.review` advances to v3 and contains only technical correctness, information value, and clarity/scope questions; no language requirement remains in this rule.
- **AC-04** — `docs.summary.language.german.review` v1 exists as a separate optional semantic-review rule whose fixed question is natural/comprehensible German; it exposes no language parameter.
- **AC-05** — The two summary semantic-review rules use the established summary-subject/carrier population model and deterministic bounded review protocol, with stable independent batches/handles and normal escalation/handoff behavior.
- **AC-06** — Canonical normal rule order is updated deterministically to include both semantic-review rules, and rule listing/help accurately identifies their fixed purpose and enable/disable capability.
- **AC-07** — The dotnet-ai-code-hygiene repository commits configuration disabling only `docs.summary.language.german.review` for language policy while retaining language-neutral summary-quality review.
- **AC-08** — `docs.summary.required` authority explicitly records that public/internal coverage is deliberate human/agent source-comprehension policy rather than an externally shipped API filter, and that generic-summary-only scope avoids boilerplate completeness pressure.
- **AC-09** — `docs.xml.consistent` and `docs.text.sentence` each have explicit rationale consistent with presence-based optional documentation and deterministic-before-semantic product direction.
- **AC-10** — A canonical `docs/specs/READABILITY.md` defines both existing readability rules, preserving their current semantics and recording why long lines are review candidates while visual control-flow blank lines are deterministic structural punctuation.
- **AC-11** — `profile.dotnet.analysis.required` and `profile.stylecop.prohibited` retain current behavior and gain concise explicit rationale consistent with the supported-profile/product-ownership model.
- **AC-12** — Product-direction research explicitly preserves “fixed opinion, optional applicability” as the rationale against thresholds/language/scope/severity parameter bags.
- **AC-13** — Tests prove disabling the German-language rule suppresses only its review batch while language-neutral summary quality remains active.
- **AC-14** — Tests prove a German-language project can enable both rules and receive independent review batches with their respective rubrics.
- **AC-15** — Existing review expansion/handoff remains compatible when multiple semantic-review rules produce batches in one check.
- **AC-16** — Existing deterministic rules, profile behavior, ignores, format/normalize, targeting, and prior semantic-review mechanics remain regression-compatible except for the explicit rule-version/ID/order changes.
- **DOC-01** — README/agent-facing guidance no longer describes German as an intrinsic question of generic `docs.summary.quality.review`; it explains fixed German policy as a separate disableable rule.
- **DOC-02** — Every current rule's authoritative specification contains or directly references a concise rationale sufficient to distinguish intentional policy from implementation accident.
- **REV-01** — Human completion review confirms the split reflects the intended no-parameter rule model, the repository disables rather than changes German policy, internal-summary and visual-block policies remain intentional, and rationale text accurately reflects existing project direction rather than introducing new policy.

## Evidence cases

| ID | Parent | Required evidence case |
|---|---|---|
| EC-03a | AC-03 | English technically correct/value-adding/clear summary passes quality review without any language judgment |
| EC-04a | AC-04 | English summary is eligible for German-language review and fails German question when that rule is enabled |
| EC-04b | AC-04 | natural German summary passes the German-language question |
| EC-05a | AC-05 | one population can independently feed both enabled semantic rules with deterministic samples |
| EC-07a | AC-07 | repository config disables German rule and `hygiene check` emits no German batch |
| EC-07b | AC-07 | repository check still emits generic quality review work when eligible prose exists |
| EC-13a | AC-13 | disabling German does not alter generic-quality population/ranking/rubric |
| EC-14a | AC-14 | both enabled rules produce separately identifiable batches |
| EC-15a | AC-15 | expand by qualified/bare latest-run batch handle selects the intended rule batch |
| EC-15b | AC-15 | handoff persists the intended rule ID/version/rubric for each rule |

## Validation

| ID | Depth | Target | Command/check | Proves |
|---|---|---|---|---|
| VAL-01 | Tier 1 | Core rule catalog, sampling, questions, configuration | focused TUnit | AC-01..AC-06, AC-13..AC-16 |
| VAL-02 | Tier 1 | CLI rules/check/review expand/handoff | built CLI process tests | AC-05..AC-07, AC-13..AC-16 |
| VAL-03 | Tier 3 | isolated English and German fixture repositories | real SDK/Git fixtures | AC-03..AC-07, AC-13..AC-15 |
| VAL-04 | Tier 2 | complete repository | `./eng/validate.ps1` | aggregate regression and self-hosting config |
| VAL-05 | documentation | specs/research/public docs | authority consistency review | AC-01, AC-02, AC-08..AC-12, DOC-01, DOC-02 |
| VAL-06 | human | completion evidence | rule-by-rule rationale and dogfood-policy review | REV-01 |

## Required Authority

- `docs/SPECS.md`
- `docs/specs/HYGIENE.md`
- `docs/specs/DOCUMENTATION.md`
- `docs/specs/SEMANTIC-REVIEWS.md`
- `docs/specs/PROFILE.md`
- `docs/specs/REVIEW-HANDOFFS.md`
- `docs/ARCHITECTURE.md`
- `docs/ENGINEERING.md`
- `docs/TERMINOLOGY.md`
- `docs/research/PRODUCT-DIRECTION.md`
- `docs/research/HUMAN-CONSUMABILITY.md`
- M0002 and M0005 milestone authority as rationale provenance only

## Direct documentation impact

The milestone intentionally changes authority as well as code. Documentation is not cleanup after implementation; the rule rationale and semantic boundaries are part of the deliverable.

The implementation must avoid copying long historical milestone prose into current specs. Promote only the stable rationale necessary to explain the current contract.
