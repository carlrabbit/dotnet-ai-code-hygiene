# Execution Ledger — M0006 Rule Policy Rationale & Semantic Review Separation

Primary milestone: `docs/milestones/M0006-rule-policy-rationale.md`

This file is mutable operational execution state, not project authority.

Planning seeded the obligation/evidence registry only. Implementation owns work packages, concrete evidence, statuses, and resume state.

## Milestone Obligation Registry

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | Fixed rule semantics and enable/disable-only optional participation are explicit. |  |  |  | planned |
| AC-02 | acceptance | Independently applicable policies use separate rule IDs rather than parameters/bundles. |  |  |  | planned |
| AC-03 | acceptance | docs.summary.quality.review v3 is language-neutral with correctness/value/clarity only. |  |  |  | planned |
| AC-04 | acceptance | docs.summary.language.german.review v1 is a separate fixed German-language semantic rule. |  |  |  | planned |
| AC-05 | acceptance | Both semantic rules preserve independent deterministic bounded sampling/expansion/handoff. |  |  |  | planned |
| AC-06 | acceptance | Canonical order/rules/help expose both rules correctly. |  |  |  | planned |
| AC-07 | acceptance | This repository disables only the German-language rule while retaining generic quality review. |  |  |  | planned |
| AC-08 | acceptance | Summary-required rationale makes deliberate public/internal coverage and generic-only scope explicit. |  |  |  | planned |
| AC-09 | acceptance | XML-consistency and sentence-rule rationales are explicit. |  |  |  | planned |
| AC-10 | acceptance | Canonical READABILITY.md preserves semantics and rationale for both readability rules. |  |  |  | planned |
| AC-11 | acceptance | Mandatory profile rules retain semantics and gain explicit rationale. |  |  |  | planned |
| AC-12 | acceptance | Product-direction research records fixed opinion / optional applicability rationale. |  |  |  | planned |
| AC-13 | acceptance | Disabling German suppresses only the German review batch. |  |  |  | planned |
| AC-14 | acceptance | Both enabled rules produce independent batches in a German-policy fixture. |  |  |  | planned |
| AC-15 | acceptance | Multiple semantic batches remain independently expandable/handoff-capable. |  |  |  | planned |
| AC-16 | acceptance | Prior deterministic/profile/rewrite/review behavior remains compatible except explicit changes. |  |  |  | planned |
| DOC-01 | documentation | Public/agent guidance describes generic quality and German-language policy separately. |  |  |  | planned |
| DOC-02 | documentation | Every current rule has authoritative rationale or direct rationale reference. |  |  |  | planned |
| REV-01 | review | Human completion review confirms intended rule model and rationale fidelity. |  |  |  | planned |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-03a | AC-03 | English technically correct/value-adding/clear summary passes generic quality without language judgment. | VAL-01, VAL-03 | | planned |
| EC-04a | AC-04 | English summary fails enabled German-language review. | VAL-01, VAL-03 | | planned |
| EC-04b | AC-04 | Natural German summary passes German-language review. | VAL-01, VAL-03 | | planned |
| EC-05a | AC-05 | Same eligible population independently feeds both enabled semantic rules deterministically. | VAL-01, VAL-03 | | planned |
| EC-07a | AC-07 | Repository configuration emits no German-language batch. | VAL-02, VAL-04 | | planned |
| EC-07b | AC-07 | Repository configuration retains generic quality review. | VAL-02, VAL-04 | | planned |
| EC-13a | AC-13 | Disabling German leaves quality population/ranking/rubric unchanged. | VAL-01, VAL-03 | | planned |
| EC-14a | AC-14 | Both rules enabled produce separately identifiable batches. | VAL-01, VAL-02, VAL-03 | | planned |
| EC-15a | AC-15 | Review expand selects intended batch when multiple batches exist. | VAL-02, VAL-03 | | planned |
| EC-15b | AC-15 | Handoff persists intended rule ID/version/rubric for each rule. | VAL-02, VAL-03 | | planned |

## Validation Registry

| ID | Depth | Target | Evidence | Status |
|---|---|---|---|---|
| VAL-01 | Tier 1 | Core rule catalog, sampling, questions, configuration | | planned |
| VAL-02 | Tier 1 | Built CLI rules/check/review expand/handoff | | planned |
| VAL-03 | Tier 3 | Isolated English/German SDK-style Git fixtures | | planned |
| VAL-04 | Tier 2 | Complete repository via `./eng/validate.ps1` | | planned |
| VAL-05 | documentation | Authority/spec/research/public consistency | | planned |
| VAL-06 | human | Completion review | | planned |

## Resume State

Planning complete. No implementation work started.
