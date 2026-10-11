# M0015 — Gate-Local Semantic Sampling: Design and Prototype

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready — bounded diagnostic/prototype, **not** production adoption |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna (project profile 0.9.1) |
| Baseline executor readiness | confirmed for prototype and evidence; product adoption remains a later planning decision |
| Decision preservation | confirmed: this milestone resolves prototype boundaries, not production semantics |
| Execution tractability | bounded gate fixtures, fixed algorithm families and finite calibration grid |
| Execution ledger | `.execution/M0015-gate-local-sampling-prototype.md` (planning-seeded) |
| Scope size | medium; diagnostic software + evidence |
| Implementation autonomy | high for experimental mechanics; no public product contract changes |
| Documentation sync | direct evidence/report only; broad sync deferred |
| Focused validation | deterministic fixtures and test commands documented by prototype |
| Repository validation | `./eng/validate.ps1` when production files are touched; otherwise record why not applicable |
| Integration validation | real local public-repository gate snapshots, Windows 11/.NET 11/PowerShell/Git |
| Validation locus/platform | local Windows 11, .NET 11 SDK, PowerShell, Git, access to checked-out public repositories |
| Consumer/release validation | not applicable: prototype is not packaged as a consumer tool |
| Human review | required and blocking for completion; `REV-M0015-COMPLETION` |

## Goal

Establish whether **budgeted selection at a concrete source-change gate**, informed by prior review evidence and optionally cheap gate-local parameter calibration, provides a simpler and potentially more valuable alternative to persistent hazard scheduling for expensive agent-executed semantic reviews.

Produce a technically working, reproducible **non-production** selector prototype and same-gate evidence. The diagnostic may recommend adoption, refinement, or rejection; do not assume the result. The next production implementation decision belongs to planning after examining this evidence.

## Target State

A prototype consumes an immutable gate snapshot (base/head revisions and explicit scope), the eligible BORINGness candidate population at head, rule-relevant change attribution, *available and clearly typed* historical review evidence, a deterministic random seed, and an integer semantic-review budget. It outputs a reproducible bounded candidate selection, per-candidate selection reasons, gate/budget identifiers, and calibration metadata. A same-gate comparison and durable research report explain its strengths, limits, and evidence of value (or lack thereof).

## Scope

- **One rule:** `architecture.boringness.review` and its actual type-candidate/rubric semantics; other two semantic rules are *not* migrated.
- **One concrete gate:** compare a frozen base and head source tree, normally a PR-ready diff. Select from eligible head-state candidates in changed files plus a small unchanged population for exploration; cheap rule facts may inspect wider project context without expanding the gate's reporting scope.
- **Local experiment:** run at least two reproducible *public* repository gates: this repository and `carlrabbit/dotnet-semantic-type-model`, selecting frozen base/head SHAs before inspecting selection outcomes. If the second does not have a suitable candidate population, document this and use `carlrabbit/dotnet-ai-first-2d-game-engine` as the prescribed fallback. Never silently select only favorable gates.
- Prototype shared selection/calibration logic and fixtures may live under a clearly isolated `tools/M0015/` area or an equivalent non-production test location. Production source changes are out of scope.
- Compare equal-budget baseline (uniform random) and change-first with the history-aware experimental policy. An existing hazard policy may be shown as optional context if reusable without expanding scope or costly replay; it is **not** a required baseline.
- Allow cheap local calibration from **real, independently assessed historical labels when available**. When suitable labels do not exist, use deterministic conservative parameters and label the experiment **uncalibrated**. Do not optimize a synthetic risk proxy and present it as measured semantic benefit.
- Include a bounded policy-blinded semantic assessment where the execution environment provides an authorized actual reviewer; otherwise preserve a ready-to-review sample and report the empirical evidence gap without inventing judgments.

## Non-goals

- Replacing production hazard samplers or migrating existing sampling state.
- Introducing a public sampling DSL, configuration, CLI commands, model provider, online learning system, database, or external telemetry.
- Running M0013/M0014's multi-hour historical replay again, altering their frozen records or resolving their Private cross-run discrepancy.
- Requiring access to Private, publishing any private source material, or asserting statistically representative defect rates from convenience samples.
- Claiming that optimization runs itself are evidence of better semantic review quality.

## Resolved Decisions and Constraints

### Gate and candidate model

1. A gate is an immutable `(repository, base revision, head revision, selected scope, rule version, budget)` input, plus a deterministic seed. **Source-state change**, not wall time or accumulated project churn, defines current change relevance. A gate is not an elapsed-time trigger.
2. Prototype candidates are **source-backed BORINGness-eligible type declarations** at the head revision (class, struct, record, interface; nested types included). Use a stable project-qualified semantic anchor with a deterministic declaration disambiguator when necessary, and record fingerprint/content evidence separately. Do not use raw span position as the sole identity across revisions. A rename or identity collision must be explicitly treated as unmatched unless equivalence is provable.
3. Classify each candidate with auditable evidence as **new/changed**, **unchanged but context-affected where cheaply determinable**, or **unchanged/background**. Missing impact knowledge is `unknown`, not automatically changed or unaffected. Changed-file status alone is a deliberately coarse signal, not proof that every type inside changed file changed. A safe and cheap fallback may classify the enclosing changed document as change-relevant; report this approximation.
4. Budget is a positive integer count of **expensive semantic reviews**, not a due-event cap. Selection is without replacement and cannot exceed budget or the eligible population. A zero-budget case deterministically selects nothing. Deterministic rules remain exhaustive.
5. Preserve a nonzero exploratory possibility for unchanged/never-reviewed subjects when those candidates are eligible. Exploration is a property of the selector, not a separate time/age queue. Changed candidates are generally prioritized. The prototype may test fixed bounded exploration fractions, but must report if this displaces changed-subject capacity. No production fraction is approved by this milestone.

### History and outcomes

6. History input distinguishes **actual accepted rule observations**, **actual recorded negative/uncertain semantic judgments**, and **unknown/unreviewed**. Never infer a pass from source unchangedness or synthetic accept-all replay. In the existing BORINGness model, aggregate event acceptance does **not** mean that the sampled type has a reusable individual-quality label; represent unavailable type-level history as unknown. Synthetic fixtures may exercise history sensitivity but cannot support quality claims.
7. History should support the current gate without requiring a migration or rewrite of production `.hygiene/.state/sampling.json`. Experimental metadata is isolated and must be safely disposable. No unsafe interpretation of existing hazard thresholds or age totals.
8. Selection tickets for the prototype are non-production evidence, not valid `hygiene review accept` tokens. Do not rewire the current review acceptance, expansion, or handoff commands.

### Selection and calibration

9. Implement a small interpretable weighted-without-replacement selector with a seeded deterministic random source and explicit change/history/exploration influences. No universal risk model, unbounded search, or opaque optimizer. Exact local scoring/sampling mechanics are implementation freedom provided they are documented and their distributions/invariants can be tested.
10. Calibrate only against **independently assessed relevant outcomes** with leakage controls (do not optimize and evaluate on the same observations). The parameter candidate grid must be finite, versioned, and fixed **before viewing measured outcomes**. A few minutes of calibration is acceptable for an hour-scale review, but constrain computational cost: reference target **no more than five minutes per gate**, report warm/cold timings and machine details, and fail/report overrun rather than hiding it. Default/fallback paths should be much cheaper.
11. At sparse/no-label gates, use an explicit documented fallback parameter set. Demonstrate parameter sensitivity on controlled fixtures, not fabricated real labels. Persist seed, frozen input IDs, chosen parameters, candidate selection identities, and selection/calibration timings for reproducibility.
12. Compare uniform random, change-first, and history-aware selector at the **same frozen gate and same completed-review budget**. Report overlap, change/background allocation, displacement of change-relevant candidates, coverage of never-reviewed candidates, and overhead. Quality yield and actual savings require real semantic observations; scheduling metrics do not establish them.

## Baseline Executor Readiness

This is a self-contained **diagnostic/prototype** contract. The baseline executor chooses file layout, local test mechanics, and algorithm implementation, but may not redefine the gate, use simulated reviews as real labels, integrate with the product, or choose a different optimization target after observing results. No prior private/research conversation is needed: this milestone and project authority define the contract. If obtaining independently assessed judgments requires unavailable external reviewers, complete software/scheduling validation and return `AWAITING HUMAN REVIEW` with the empirical conclusion explicitly inconclusive; do not fake quality evidence.

## Required Authority and Evidence Context

Authority: `AGENTS.md`; `docs/ENGINEERING.md`; `docs/specs/SAMPLING.md`; `docs/specs/SEMANTIC-REVIEWS.md`; `docs/specs/REVIEW-HANDOFFS.md` (current product behavior only). Reference `docs/research/SAMPLING-GATE-PIVOT.md` and `docs/research/ACTIVITY-AGE-SELECTION-COMPARISON.md` **as research provenance, not implementation authority**. M0015 overrides no existing production contract.

## Acceptance Criteria

- **AC-01** — Freeze and document at least two eligible public base/head gate snapshots, source/candidate population counts, relevant diff evidence, seed(s), budget, and environment before inspecting comparison results; use the prescribed fallback repository if needed.
- **AC-02** — Working prototype enumerates actual rule-eligible head-state type candidates, preserving project identity, disambiguation, eligibility, and auditable changed/unchanged/unknown classification.
- **AC-03** — A deterministic, budget-respecting, without-replacement selection contract supports priority for changed candidates and possible exploration of unchanged candidates, with explicit selection reason/identity evidence.
- **AC-04** — Prior evidence affects selection behavior in controlled fixtures without misclassifying aggregate review events, synthetic acceptances, or missing type-level labels as actual quality observations.
- **AC-05** — Bounded gate-local parameter search uses a frozen candidate grid and independent assessed labels where valid; otherwise it explicitly follows and proves a stable uncalibrated fallback without claiming learned parameters.
- **AC-06** — Equal-budget same-gate comparisons for uniform, change-first, and history-aware policies report candidate identities/overlap and change/background selection/displacement, with separate measures for overhead and any actual labeled yield.
- **AC-07** — Fixed-seed repeatability, changed-input invalidation, zero/short/large-population budgets, and sparse/missing/corrupt history have explicit safe results without changing existing production state.
- **AC-08** — Measured selection and calibration costs on both gate snapshots, including cold/warm timing, memory/artifact footprint and reference-platform details; calibration finishes within the declared five-minute ceiling or records a clear overrun/failure.
- **AC-09** — Bounded, policy-blinded real semantic judgments are recorded with reviewer provenance when available, and absent/uncertain judgments are left missing; do not claim comparative defect yield without sufficient independent assessments.
- **AC-10** — Final report gives an evidence-bounded **adopt/refine/reject/inconclusive** recommendation for gate-local selection and explicitly states the production API/state/review-ticket decisions that remain for subsequent planning, without editing production sampling policy.

## Additional Deliverable Obligations

- **ART-01** — Commit reproducible prototype/fixtures plus allowed public result metrics and frozen gate manifest; include commands and integrity checks. Never publish Private candidate/revision/source traces.
- **DOC-01** — Write `docs/research/GATE-LOCAL-SAMPLING-PROTOTYPE.md` with question, provenance, observed/inferred/assumed distinctions, experimental protocol, measurements, limitations, decision, and revalidation triggers. Research is **not** production authority.
- **REV-01** — Prepare `.review/REV-M0015-COMPLETION.md` requesting project-owner/delegate evaluation of technical validity, evidence claims and future architecture recommendation; no self-approval.

## Acceptance Evidence Topology

| ID | Parent | Required distinct evidence case | Why independent |
|---|---|---|---|
| EC-02a | AC-02 | Eligible new/changed candidates correctly identified at a real gate | Changed candidate extraction |
| EC-02b | AC-02 | Unchanged or unknown candidates remain eligible and auditable | Background/context path |
| EC-03a | AC-03 | Changed candidates dominate under change-first policy | Primary budget path |
| EC-03b | AC-03 | Unchanged exploration remains possible under nonzero eligibility | Background budget path |
| EC-04a | AC-04 | Actual individually attributable history has supported effect | Genuine prior-evidence path |
| EC-04b | AC-04 | Aggregate-only / unlabeled / synthetic evidence does not become quality labels | Missing-evidence path |
| EC-05a | AC-05 | Valid independent-labeled case calibrates and keeps evaluation separate | Calibration path |
| EC-05b | AC-05 | Sparse/no valid labels follows deterministic uncalibrated fallback | Fallback path |
| EC-06a | AC-06 | Same-gate comparison reports selected *identities*, not just counts | Identity-sensitive comparison |
| EC-06b | AC-06 | Changed checks displaced by exploration are counted | Opportunity-cost path |
| EC-07a | AC-07 | Stable seed and persisted protocol replay identical | Determinism path |
| EC-07b | AC-07 | Changed inputs and malformed/insufficient history fail conservatively | Failure/invalidation path |
| EC-09a | AC-09 | Genuine blinded judgments with provenance when available | Real reviewer evidence |
| EC-09b | AC-09 | Missing/uncertain judgments explicitly not treated as acceptance | Inconclusive evidence path |

AC-09 / EC-09a is conditional on reviewer availability. If unavailable, record proof of unavailability and an assessment-ready blinded sample; the research outcome must remain explicitly inconclusive on semantic usefulness. Do not assert `not-applicable` merely because collecting labels is inconvenient.

## Validation Gates

| ID | Depth | Target | Locus/platform | Command/check | Proves | Evidence |
|---|---|---|---|---|---|---|
| VAL-01 | focused | Prototype fixtures / eligibility and sampling tests | local Windows 11, .NET 11 | Prototype's documented focused test launcher | EC-02a, EC-02b, EC-03a, EC-03b, EC-04a, EC-04b, EC-05a, EC-05b, EC-07a, EC-07b | Executed test cases and result artifacts |
| VAL-02 | integration | Frozen real public gate inputs | local Windows 11, .NET 11, PowerShell, Git | Prototype's documented replay/check command for both frozen gates | AC-01, EC-06a, EC-06b, AC-08 | Frozen manifest, identities, distributions and timing evidence |
| VAL-03 | evidence | Recorded/blinded reviewer assessments, where available | authorized reviewer + recorded evidence | Fixed BORINGness rubric with blinded policy label | EC-09a, EC-09b, AC-09 | Reviewer provenance, unanswered items and caveats |
| VAL-04 | hygiene/evidence | Product regression boundary and privacy/integrity audit | Windows 11 + repository review | `./eng/validate.ps1` if product sources touched; otherwise diff/fixture integrity and public-output inspection | ART-01, AC-07, DOC-01 | Scope diff, checksums, validation receipt, sensitive-data inspection |
| VAL-05 | human | Milestone-specific architectural/evidence review | project owner/delegate | Review request `REV-M0015-COMPLETION`; record decision; use installed guide review-check only if available in repository | AC-10, REV-01 | Review request, reviewer decision, final reconciliation |

A prototype-generated command must be stable, checked into `tools/M0015/` with its fixtures, and recorded in the evidence; it need not exist before implementing the prototype. Do not invent a command as having already run. No Tier 4 installed-tool validation is required unless product/distribution code is changed (which is outside this milestone).

## Human Review

- Applicability: required / blocking.
- Review class: milestone completion; architectural pivot and evidence integrity.
- Canonical ID: `REV-M0015-COMPLETION`.
- Subject: whether software contract, local calibration, historical-evidence interpretation, and same-gate findings justify a subsequent production redesign.
- Reviewer: project owner/delegate; decision must be recorded by them.
- Waiver: none by implementation agent.
- Completion check: consult actual repository guide review tooling when available; do not invent `eng/review-check.ps1` where absent.

## Completion Expectations

Use the project's guide 0.9.1 execution workflow. Reconcile every stable obligation and evidence case with `.execution/M0015-gate-local-sampling-prototype.md`, actual experiment outputs and validation gates. Preserve a compact obligation-to-proof summary in this milestone's completion evidence before any ledger cleanup. If only external human judgment is outstanding, return `AWAITING HUMAN REVIEW`. If independent semantic labels are unavailable, the technical prototype may be valid, but empirical superiority is **not** established; the human gate judges whether to request more research.

## Completion Evidence

**Implementation evidence:** `tools/M0015/` contains the isolated Roslyn enumerator, deterministic common-random selector, finite calibration grid and fallback, controlled fixtures, three frozen public gates (including a source-changing gate and a documentation-only boundary), corrected comparison result sets, explicit read-only checksum verification, and a policy-blinded review sample. `docs/research/GATE-LOCAL-SAMPLING-PROTOTYPE.md` records original exploratory findings, corrected displacement, the new source gate, measurements, limitations, and INCONCLUSIVE recommendation. `.execution/M0015-gate-local-sampling-prototype.md` contains the full obligation/evidence reconciliation.

| Obligation | Completion evidence |
|---|---|
| AC-01 | Three public base/head gates, candidate counts, path diffs, seeds, equal budget, policy weights, analysis protocol, and environment are in `tools/M0015/FrozenGateEvidence.json`; includes source-changing Semantic Type Model gate and docs-only boundary. Original exploratory exposure and descriptive-output amendment are disclosed. |
| AC-02 | Roslyn matches rule eligibility; head populations are 96, 420, and 396, with changed/context/background/unknown distinctions recorded. |
| AC-03 | Three fixed-budget policies emit five unique selected identities and reasons per gate; fixtures cover bounds, priority, and unchanged exploration. |
| AC-04 | Distinct stable-ID deterministic seed fixtures prove accepted/negative/unknown weighting order. Fingerprint-matched evidence is current; stale evidence is neutral; missing, aggregate, synthetic, uncertain, and corrupt evidence is excluded. No real gate labels existed. |
| AC-05 | The finite `m0015-grid-v1`, independent identity folds, conditional selected/labeled-subset scope, and fallback are fixture-tested. All three gates used fixed uncalibrated 4/.5/.20 fallback; no learned parameter is claimed. |
| AC-06 | Three result JSONs contain equal-budget identities, overlaps, allocation, actual omitted/replacement candidates, count difference, symmetric difference, traced selection stage, separate theoretical capacity shortfall, and timing; no semantic yield is claimed. |
| AC-07 | Fixtures cover repeatability, changed identity/fingerprint, zero/short/large budgets, and missing/corrupt history. Repeat gate runs reproduced the same selected identities. |
| AC-08 | Result and research artifacts contain per-gate cold/warm selection and mode-labeled fallback timing, enumeration timing, memory snapshots, and artifact sizes; all measured fallback work is below five minutes. |
| AC-09 | No authorized independent reviewer or real judgments were available. `tools/M0015/reviewer-sample.json` contains 11 policy-blinded candidates with labels absent. |
| AC-10 | Research recommends INCONCLUSIVE and names API, state, history, and review-ticket decisions for M0016. |
| ART-01 / DOC-01 | Public-only result inventories and SHA-256 reference are committed; `integrity` verifies read-only and `regenerate-integrity` is explicit. Tamper-negative test and reproducible commands are recorded; report is research, not production authority. No Private traces or source were accessed. |
| REV-01 | `.review/REV-M0015-COMPLETION.md` requests project-owner/delegate review and records no decision. |

The focused prototype, read-only integrity verification, negative tampering case, and three-gate reproducibility validations passed. No product source, production sampler, policy, persistent-state contract, or review command changed. **State: AWAITING HUMAN REVIEW**; semantic usefulness remains inconclusive, and the exploratory protocol/schema limitations are disclosed for reviewer evaluation.

## Escalation Boundary

Material changes to policy, prototype-to-production integration, persistence compatibility, public review semantics, scope, validity of the optimization objective, or mandatory validation require returning to planning. Local files/functions/test algorithms within this contract are implementation-owned.
