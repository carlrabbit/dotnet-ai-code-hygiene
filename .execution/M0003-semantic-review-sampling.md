# Execution Ledger — M0003 Semantic Review Sampling & Escalation

Primary milestone: `docs/milestones/M0003-semantic-review-sampling.md`

Operational implementation state; not project authority.

The execution ledger may compress work. It must not compress obligations.

## Milestone Obligation Registry

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | Core models rules with explicit output kind and first-class run-scoped review batches/items, not pseudo-findings or ignore decisions. | WP-01 | ReviewBatch, ReviewItem and CheckResult are separate from Finding. | VAL-01; see validation gates. | done |
| AC-02 | acceptance | Canonical rule set/order includes `docs.summary.quality.review` v1 as `review-batch`, participating in existing enable/disable configuration. | WP-01 | Canonical Rules order; SetRule/ListRules; rules JSON outputKind. | VAL-01, VAL-02; see validation gates. | done |
| AC-03 | acceptance | Generic sampling deterministically derives population fingerprint/ranking from rule/version plus stable subject/content fingerprints; identical state yields identical sample and review-relevant changes recompute ranking. | WP-01 | BuildReviewBatch SHA-256 fingerprints/ranking; Core repeat/content-change test. | VAL-01; see validation gates. | done |
| AC-04 | acceptance | Every successful enabled run emits exactly one batch for the semantic rule, including explicit empty `0/0` batch. | WP-01 | Check emits one enabled batch, including empty; Core and CLI fixtures. | VAL-01, VAL-02; see validation gates. | done |
| AC-05 | acceptance | Normal sample never exceeds five and includes entire population when population size is five or less. | WP-01 | BuildReviewBatch limit 5; Core verifies 8->5 and 5->5. | VAL-01, VAL-03; see validation gates. | done |
| AC-06 | acceptance | Summary-quality population contains exactly valid non-empty XML summaries on the same public/internal symbol categories as `docs.summary.required`; missing/empty/invalid summaries remain Rule A concerns and are excluded. | WP-01 | Summary eligibility helpers; fixture covers valid/empty/malformed/non-doc/missing. | VAL-01, VAL-03; see validation gates. | done |
| AC-07 | acceptance | Every review item exposes item ID, repository-relative path, 1-based location, symbol, summary text, and declaration display/signature without internal ranking/fingerprint metadata. | WP-01 | ReviewItem and PublicBatch expose required public subject fields. | VAL-01, VAL-02; see validation gates. | done |
| AC-08 | acceptance | Batch exposes exactly Q1 natural German, Q2 technical correctness, Q3 information value, Q4 clarity/scope plus material-failure/uncertainty escalation guidance. | WP-01 | SummaryQuestions and ReviewEscalation implement fixed rubric/escalation. | VAL-01, VAL-02; see validation gates. | done |
| AC-09 | acceptance | Normal quality batch uses `reviewerClass=implementer`; no deterministic language heuristic is treated as sufficient quality judgment. | WP-01 | BuildReviewBatch sets implementer; no language heuristic. | VAL-01; see validation gates. | done |
| AC-10 | acceptance | `check --output json` remains schemaVersion 1, preserves M0002 fields, always adds documented `reviewBatches`, and excludes internal state/hashes. | WP-02 | RenderCheck adds reviewBatches; projection excludes internal fingerprints. | VAL-02; see validation gates. | done |
| AC-11 | acceptance | `check --output text` renders compact batch/sample/rubric/escalation info and explicit empty samples. | WP-02 | RenderBatchText renders rubric/escalation and explicit 0/0. | VAL-02; see validation gates. | done |
| AC-12 | acceptance | Checks with review batches return exit 0; batches do not alter finding count, ignored count, finding IDs, or ignore matching. | WP-02 | Findings alone numbered/ignored; review collection separate. | VAL-01, VAL-02, VAL-03; see validation gates. | done |
| AC-13 | acceptance | Latest-run state atomically persists enough internal batch data for expansion while remaining Git-ignored/engine-owned. | WP-02 | Atomic RunSnapshot stores batches/targets; byte-equivalent expansion test. | VAL-01, VAL-03; see validation gates. | done |
| AC-14 | acceptance | `hygiene review expand` supports bare latest and fully-qualified latest-run batch handles, rejects unavailable old-run batches with exit 3, and supports text/JSON. | WP-02 | ExpandReview and process tests cover bare/qualified/old-run handles. | VAL-02; see validation gates. | done |
| AC-15 | acceptance | Expansion recomputes population and rejects review-relevant changes with exit 3 plus rerun guidance. | WP-02 | Fingerprint revalidation and stale-population test. | VAL-01, VAL-03; see validation gates. | done |
| AC-16 | acceptance | Successful expansion emits complete eligible population including sampled items, keeps same rubric, sets `mode=expanded` and `reviewerClass=frontier`, and neither creates a new run nor mutates latest-run state. | WP-02 | Expanded output returns full population/frontier; sample inclusion and no-write test. | VAL-01, VAL-03; see validation gates. | done |
| AC-17 | acceptance | Batch handles are rejected by finding ignore semantics; no committed semantic-review/history file is created. | WP-02 | Finding-only ignore lookup; B-1 rejection and no-history artifact test. | VAL-01, VAL-02; see validation gates. | done |
| AC-18 | acceptance | No model/provider SDK, network model call, automatic model selection/escalation, answer collection, review history, scheduler, or TTL is introduced. | WP-04 | No model/network dependency, answer persistence, history, or scheduler; repo inspection. | VAL-04; see validation gates. | done |
| AC-19 | acceptance | Existing M0002 target/finding/text-JSON/explain/ignore/unignore/stale/config/exit behavior remains regression-covered and compatible. | WP-03 | Core/CLI existing lifecycle passes; disabling review preserves findings. | VAL-03, VAL-04; see validation gates. | done |
| AC-20 | acceptance | `format`/`normalize` remain non-functional and the packaged developer-tool/formatter milestone is deferred to M0004. | WP-04 | format/normalize remain non-functional; M0004 docs/code scope checked. | VAL-02, VAL-04; see validation gates. | done |
| AC-21 | acceptance | `./eng/validate.ps1` remains complete thin Windows-local restore/build/test/pack validation and passes. | WP-04 | ./eng/validate.ps1 restore/build/test/pack passed on Windows 11/.NET 11. | VAL-04; see validation gates. | done |
| DOC-01 | documentation | README/public docs explain batches, empty samples, explicit expansion, implementer/frontier classes, no model/history, and exact summary-quality escalation workflow. | WP-04 | README documents workflow, empty sample, reviewer roles, escalation, no model/history. | VAL-05; see validation gates. | done |
| DOC-02 | documentation | Project authority consistently refers to formatting/normalization/installed-tool validation as M0004 work; no stale M0003 formatter/package claim remains. | WP-04 | Project authority assigns format/normalize/consumer validation to M0004. | VAL-04; see validation gates. | done |
| REV-01 | review | Human review confirms sample usefulness for the implementation agent, expansion sufficiency for frontier handoff, rubric quality, finding/batch separation, and absence of hidden model/history behavior. | WP-05 | REV-M0003-COMPLETION requested; awaiting project owner/delegate. | VAL-06; see validation gates. | awaiting human review |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-03a | AC-03 | identical population/content yields identical sample | VAL-01 | Core test repeats population/content and sample. | passed |
| EC-03b | AC-03 | review-relevant population/content change recomputes fingerprint/ranking | VAL-01 | Core test changes summary content, observes new fingerprint and expansion rejection. | passed |
| EC-04a | AC-04 | eligible population exists | VAL-01 | Core fixture 8 eligible; CLI populated JSON. | passed |
| EC-04b | AC-04 | zero eligible population | VAL-01 | Core empty fixture; CLI empty JSON/text. | passed |
| EC-05a | AC-05 | population <=5 | VAL-01 | Core fixture exactly 5 gives sample 5. | passed |
| EC-05b | AC-05 | population >5 | VAL-01 | Core fixture 8 gives sample 5. | passed |
| EC-06a | AC-06 | valid summary subjects included | VAL-01, VAL-03 | Core fixture includes valid XML summary subjects. | passed |
| EC-06b | AC-06 | missing/empty/non-doc summaries excluded | VAL-01, VAL-03 | Core fixture excludes empty, malformed, non-doc, and missing summaries. | passed |
| EC-10a | AC-10 | populated batch JSON | VAL-02 | CLI populated batch JSON schema and hash omission. | passed |
| EC-10b | AC-10 | empty batch JSON | VAL-02 | CLI empty batch JSON schema. | passed |
| EC-14a | AC-14 | bare latest-run batch handle | VAL-02 | CLI process expands bare B-1. | passed |
| EC-14b | AC-14 | fully-qualified latest-run batch handle | VAL-02 | CLI process expands qualified latest-run handle. | passed |
| EC-14c | AC-14 | old/unavailable run handle | VAL-02 | CLI process rejects R-OLD/B-1 with exit 3. | passed |
| EC-15a | AC-15 | unchanged population expands | VAL-01, VAL-03 | Core expands unchanged population. | passed |
| EC-15b | AC-15 | population changed after check | VAL-01, VAL-03 | Core rejects changed population with rerun guidance. | passed |
| EC-16a | AC-16 | expanded output contains entire population including sample | VAL-01, VAL-03 | Core verifies all 8 expanded items include all sampled subjects. | passed |
| EC-16b | AC-16 | expansion leaves latest-run state byte-equivalent | VAL-01, VAL-03 | Core compares latest-run bytes before and after expansion. | passed |
| EC-17a | AC-17 | batch handle rejected by `ignore` | VAL-01, VAL-02 | CLI process rejects B-1 passed to ignore. | passed |
| EC-17b | AC-17 | no committed review-history artifact after check/expand | VAL-01, VAL-02 | CLI process checks no review-history artifact after check/expand. | passed |
| EC-19a | AC-19 | deterministic finding/ignore lifecycle works with review rule enabled | VAL-03 | Core/CLI finding-ignore lifecycle with review enabled passes. | passed |
| EC-19b | AC-19 | disabling review rule removes batch without altering deterministic findings | VAL-03 | CLI disables review: no batch and same deterministic findings. | passed |

## Work Packages

Implementation owns this section.

| ID | Bounded work | Obligations / cases | Gates |
|---|---|---|---|
| WP-01 | Add first-class review models, deterministic population fingerprint/ranking/sample, summary eligibility and rubric. | AC-01..AC-09; EC-03a..EC-06b | VAL-01 |
| WP-02 | Integrate review batches with check ordering/output/latest-run state and implement non-mutating latest-run expansion. | AC-10..AC-17; EC-10a..EC-17b | VAL-01, VAL-02 |
| WP-03 | Add Core/CLI and isolated repository fixture coverage, including M0002 mixed lifecycle/config regression. | AC-19; EC-19a, EC-19b | VAL-01..VAL-03 |
| WP-04 | Reconcile scope, docs, and full Windows validation; preserve M0004 boundary and no-model boundary. | AC-18, AC-20, AC-21, DOC-01, DOC-02 | VAL-04, VAL-05 |
| WP-05 | Obtain required project-owner review and reconcile completion evidence. | REV-01 | VAL-06 |

Obligation mapping: AC-01..AC-09 -> WP-01; AC-10..AC-17 -> WP-02; AC-19 -> WP-03; AC-18, AC-20, AC-21, DOC-01, DOC-02 -> WP-04; REV-01 -> WP-05. Evidence cases map to the corresponding WP and validation gates in the registry above.

## Validation Gates

| ID | Required validation | Target/locus | Status | Evidence |
|---|---|---|---|---|
| VAL-01 | focused TUnit Core sampler/review tests | Windows 11 + .NET 11 | passed | Release Core TUnit: 18 passed, 0 failed; ./eng/validate.ps1. |
| VAL-02 | built CLI process tests | Windows 11 + .NET 11 | passed | Release CLI process TUnit: 18 passed, 0 failed; ./eng/validate.ps1. |
| VAL-03 | isolated SDK-style fixture scenarios | Windows 11 + .NET 11 + Git | passed | Isolated SDK-style Git fixtures in Core/CLI process tests passed. |
| VAL-04 | `./eng/validate.ps1` + dependency/workflow/docs inspection | complete repo / Windows 11 | passed | ./eng/validate.ps1 restore/build/test/pack passed; dependency/workflow/scope inspection. |
| VAL-05 | README/public docs vs live behavior | local repo | passed | README and public behavior cross-checked against CLI process tests. |
| VAL-06 | human completion review | project owner/delegate | awaiting human review | Requested REV-M0003-COMPLETION; decision pending. |

## Resume Point

Last completed work package:
WP-04
Current work package:
WP-05 human review remains.
Next concrete action:
Project owner/delegate completes `REV-M0003-COMPLETION`.
Known agent-resolvable gaps:
None; all automated gates passed.
External blockers or planning escalations:
Human completion review is pending.

## Final Reconciliation

- [x] reread milestone from disk;
- [x] verify exact obligation-ID set equality;
- [x] verify exact evidence-case-ID set equality;
- [x] preserve planner-owned rows/wording;
- [x] reconcile every obligation/case with concrete evidence;
- [x] run all gates on declared locus;
- [x] confirm no hidden model/history/scheduler behavior;
- [x] confirm M0004 scope not pulled forward;
- [ ] obtain `REV-M0003-COMPLETION`;
- [x] write durable completion reconciliation into milestone.
