# Milestone — M0003 Semantic Review Sampling & Escalation

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | awaiting-human-review |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Baseline executor readiness | confirmed |
| Decision preservation | confirmed |
| Execution tractability | confirmed |
| Execution ledger | `.execution/M0003-semantic-review-sampling.md` planning-seeded |
| Scope size | medium-large coherent feature |
| Validation locus/platform | local Windows 11 + .NET 11 SDK + Git + PowerShell |
| Human review | required |

## Goal

Introduce reusable deterministic semantic sampling so rules can ask the current implementation agent to review a bounded sample, with explicit full-population frontier escalation when the sample is concerning or uncertain. Use XML summary quality as the first semantic review rule.

## Target State

Every enabled semantic review rule evaluates on every `hygiene check`.

`docs.summary.quality.review` selects up to five eligible XML summaries in target scope and emits one run-scoped review batch with a fixed four-question rubric. The implementation agent reviews it. The tool collects no answers.

If any sampled answer is materially negative or uncertain, the caller runs:

```text
hygiene review expand <batch-handle>
```

The CLI revalidates unchanged population and emits the complete eligible population with `reviewerClass=frontier`. No model call or review history exists.

## Scope

- first-class `ReviewBatch`/review-item concepts;
- rule output kind;
- generic deterministic bounded sampler;
- run-local `B-*` batch identities;
- latest-run batch state for expansion;
- `reviewBatches` check text/JSON;
- `hygiene review expand`;
- `docs.summary.quality.review` with fixed rubric;
- explicit implementer -> frontier escalation handoff;
- M0002 regression compatibility;
- public docs.

## Non-goals

- no model API/provider integration or automatic escalation;
- no answer collection/persistence;
- no semantic review history, TTL, scheduler, or audit cadence;
- no generic trigger engine beyond always evaluating enabled rules;
- no random sampling;
- no configurable sample size/rule parameters/severity/order;
- no comment rewrite engine;
- no formatter/normalizer or installed-tool validation;
- no M0004 packaging/release work;
- no cross-platform expansion.

## Decisions and Constraints

- `ReviewBatch` is not a finding subtype.
- One enabled semantic review rule produces exactly one batch per successful run, even `0/0`.
- Summary-quality normal sample maximum is exactly five.
- Sampling is deterministic and SHA-256 based per `docs/specs/SEMANTIC-REVIEWS.md`.
- Identical review-relevant state yields same sample; population/content changes may rotate it.
- Normal reviewer class `implementer`; expanded reviewer class `frontier`.
- CLI never decides sample pass/fail.
- Expansion supports bare/latest and qualified latest-run batch handles.
- Expansion revalidates population fingerprint; stale state => exit 3 + rerun guidance.
- Expanded review contains complete population including sampled items.
- Expansion creates no new run and does not mutate latest-run state.
- Review batches cannot be ignored and create no committed review state.
- Check JSON stays schemaVersion 1 with additive always-present `reviewBatches`.
- Existing M0002 finding/ignore/target/exit behavior remains compatible.
- Canonical rule order: summary required, summary quality review, long line, control-flow visual block.
- M0004 owns formatter/normalizer/installed-tool work.
- No GitHub Actions/workflows.

## Required Authority

- `docs/SPECS.md`
- `docs/specs/HYGIENE.md`
- `docs/specs/SEMANTIC-REVIEWS.md`
- `docs/ARCHITECTURE.md`
- `docs/ENGINEERING.md`
- `docs/TERMINOLOGY.md`
- `docs/PUBLIC-DOCS.md`

## Acceptance Criteria

- **AC-01** — Core models rules with explicit output kind and first-class run-scoped review batches/items, not pseudo-findings or ignore decisions.
- **AC-02** — Canonical rule set/order includes `docs.summary.quality.review` v1 as `review-batch`, participating in existing enable/disable configuration.
- **AC-03** — Generic sampling deterministically derives population fingerprint/ranking from rule/version plus stable subject/content fingerprints; identical state yields identical sample and review-relevant changes recompute ranking.
- **AC-04** — Every successful enabled run emits exactly one batch for the semantic rule, including explicit empty `0/0` batch.
- **AC-05** — Normal sample never exceeds five and includes entire population when population size is five or less.
- **AC-06** — Summary-quality population contains exactly valid non-empty XML summaries on the same public/internal symbol categories as `docs.summary.required`; missing/empty/invalid summaries remain Rule A concerns and are excluded.
- **AC-07** — Every review item exposes item ID, repository-relative path, 1-based location, symbol, summary text, and declaration display/signature without internal ranking/fingerprint metadata.
- **AC-08** — Batch exposes exactly Q1 natural German, Q2 technical correctness, Q3 information value, Q4 clarity/scope plus material-failure/uncertainty escalation guidance.
- **AC-09** — Normal quality batch uses `reviewerClass=implementer`; no deterministic language heuristic is treated as sufficient quality judgment.
- **AC-10** — `check --output json` remains schemaVersion 1, preserves M0002 fields, always adds documented `reviewBatches`, and excludes internal state/hashes.
- **AC-11** — `check --output text` renders compact batch/sample/rubric/escalation info and explicit empty samples.
- **AC-12** — Checks with review batches return exit 0; batches do not alter finding count, ignored count, finding IDs, or ignore matching.
- **AC-13** — Latest-run state atomically persists enough internal batch data for expansion while remaining Git-ignored/engine-owned.
- **AC-14** — `hygiene review expand` supports bare latest and fully-qualified latest-run batch handles, rejects unavailable old-run batches with exit 3, and supports text/JSON.
- **AC-15** — Expansion recomputes population and rejects review-relevant changes with exit 3 plus rerun guidance.
- **AC-16** — Successful expansion emits complete eligible population including sampled items, keeps same rubric, sets `mode=expanded` and `reviewerClass=frontier`, and neither creates a new run nor mutates latest-run state.
- **AC-17** — Batch handles are rejected by finding ignore semantics; no committed semantic-review/history file is created.
- **AC-18** — No model/provider SDK, network model call, automatic model selection/escalation, answer collection, review history, scheduler, or TTL is introduced.
- **AC-19** — Existing M0002 target/finding/text-JSON/explain/ignore/unignore/stale/config/exit behavior remains regression-covered and compatible.
- **AC-20** — `format`/`normalize` remain non-functional and the packaged developer-tool/formatter milestone is deferred to M0004.
- **AC-21** — `./eng/validate.ps1` remains complete thin Windows-local restore/build/test/pack validation and passes.
- **DOC-01** — README/public docs explain batches, empty samples, explicit expansion, implementer/frontier classes, no model/history, and exact summary-quality escalation workflow.
- **DOC-02** — Project authority consistently refers to formatting/normalization/installed-tool validation as M0004 work; no stale M0003 formatter/package claim remains.
- **REV-01** — Human review confirms sample usefulness for the implementation agent, expansion sufficiency for frontier handoff, rubric quality, finding/batch separation, and absence of hidden model/history behavior.

## Acceptance Evidence Topology

| ID | Parent | Required evidence case | Why separate |
|---|---|---|---|
| EC-03a | AC-03 | identical population/content yields identical sample | determinism |
| EC-03b | AC-03 | review-relevant population/content change recomputes fingerprint/ranking | invalidation/rotation |
| EC-04a | AC-04 | eligible population exists | normal batch |
| EC-04b | AC-04 | zero eligible population | empty batch |
| EC-05a | AC-05 | population <=5 | complete small population |
| EC-05b | AC-05 | population >5 | bounded sample |
| EC-06a | AC-06 | valid summary subjects included | eligibility |
| EC-06b | AC-06 | missing/empty/non-doc summaries excluded | exclusion |
| EC-10a | AC-10 | populated batch JSON | populated schema |
| EC-10b | AC-10 | empty batch JSON | empty schema |
| EC-14a | AC-14 | bare latest-run batch handle | shorthand |
| EC-14b | AC-14 | fully-qualified latest-run batch handle | qualified |
| EC-14c | AC-14 | old/unavailable run handle | failure path |
| EC-15a | AC-15 | unchanged population expands | success |
| EC-15b | AC-15 | population changed after check | stale rejection |
| EC-16a | AC-16 | expanded output contains entire population including sample | full-population |
| EC-16b | AC-16 | expansion leaves latest-run state byte-equivalent | non-mutating |
| EC-17a | AC-17 | batch handle rejected by `ignore` | finding/batch separation |
| EC-17b | AC-17 | no committed review-history artifact after check/expand | persistence separation |
| EC-19a | AC-19 | deterministic finding/ignore lifecycle works with review rule enabled | mixed-output regression |
| EC-19b | AC-19 | disabling review rule removes batch without altering deterministic findings | config regression |

## Validation

| ID | Depth | Target/locus | Proves |
|---|---|---|---|
| VAL-01 | Tier 1 | Core semantic-review/sampler tests / Windows 11 + .NET 11 | AC-01..AC-09, AC-13, AC-15..AC-18 and mapped ECs |
| VAL-02 | Tier 1 | built CLI process / Windows 11 + .NET 11 | AC-10..AC-12, AC-14, AC-17, AC-20 and mapped ECs |
| VAL-03 | Tier 3 | isolated SDK-style fixture repos / Windows 11 + .NET 11 + Git | AC-06, AC-15, AC-16, AC-19 and mapped ECs |
| VAL-04 | Tier 2 | complete repo / `./eng/validate.ps1` | AC-18, AC-19, AC-21, DOC-02 |
| VAL-05 | Tier 2 | README/public docs vs live behavior | DOC-01 |
| VAL-06 | Human | semantic workflow usability | REV-01 |

## Human Review

Review ID: `REV-M0003-COMPLETION`.

Request status: requested; awaiting project owner/delegate review. This implementation does not self-approve the review.

Confirm sample usefulness, four-question rubric, expanded frontier handoff, finding/batch separation, absence of hidden model/history behavior, and preservation of M0004 scope.

## Completion Evidence

Implementation evidence is reconciled below. Human approval remains pending.

| Obligation/evidence case | Concrete evidence | Validation gate/target | Result |
|---|---|---|---|
| AC-01..AC-21, DOC-01..DOC-02 | Per-ID implementation and evidence-case records in `.execution/M0003-semantic-review-sampling.md`; Windows 11 `./eng/validate.ps1` passed with 36 TUnit tests. | VAL-01..VAL-05 | Passed; REV-01 pending human review |

Completion reconciliation (2026-10-04, Windows 11, .NET 11 SDK 11.0.100-rc.1.26425.128): obligation IDs AC-01..AC-21, DOC-01..DOC-02, REV-01 and EC cases exactly match the planning-seeded ledger. WP-01..WP-04 and EC-03a..EC-19b are implemented and evidenced by ./eng/validate.ps1 (restore/build, 36 TUnit tests passed, pack), focused Core/CLI/isolated Git fixture coverage, and repository/dependency/docs inspection; detailed case evidence is in .execution/M0003-semantic-review-sampling.md. No model integration, answer/history persistence, scheduler, or M0004 formatting/normalization/consumer work was introduced. REV-M0003-COMPLETION requested; awaiting project owner/delegate review.
