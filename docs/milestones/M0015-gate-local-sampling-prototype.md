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

## Amendment — User-Run Semantic Judgments via Google Drive (11 October 2026)

**Status: ready for Stage A preparation; Stage C requires a later user-submitted assessment.** This replaces the earlier assumption that the Luna implementer can invoke independent reviewing agents. **Luna must not spawn, invoke, delegate to, or simulate another AI reviewer**, whether through subagents, Codex CLI, APIs, browser automation, or tools. The project owner will conduct the semantic judgments separately and explicitly resume the implementation run when submissions are present. This is a *two-run, three-stage milestone*, not a single unattended execution.

The original AC-01–AC-10, ART-01, DOC-01, REV-01, EC-02a–EC-09b, and VAL-01–VAL-05 remain unchanged and their executed historical evidence remains valid. Amended AC-11–AC-17, ART-02, DOC-02, EC-11a–EC-17b, and VAL-06–VAL-10 replace the unexecuted 11 October research extension wording. The original milestone completion evidence is not completion evidence for these new obligations.

### Research questions

- **RQ-01 — Semantic yield:** At a fixed source-changing gate and budget, which candidates selected by competing policies yield actionable BORINGness findings under independently supplied Q1–Q5 judgments?
- **RQ-02 — Usable history:** Can independently reviewed, fingerprint-current and temporally prior type observations be used at a later gate without label leakage?
- **RQ-03 — Local calibration:** Is valid past evidence sufficient for bounded outcome-driven calibration, or must the policy use a conservative fallback given selective labels?
- **RQ-04 — Discretionary budget:** Should a documentation-only/no-rule-change gate spend zero semantic reviews unless background surveillance is separately justified?
- **RQ-05 — Exploration opportunity cost:** What changed checks are displaced by background selection, and what independently assessed issues (if any) were traded?

### Stage A — Luna prepares, publishes, and stops

1. Using the existing **two source-changing frozen gates**, construct a policy-blinded assessment frame from the union of all three existing five-candidate policy selections for each gate plus **one independently seeded, uniformly sampled eligible control candidate per gate** outside that union. Retain all policy disagreement subjects. Bound the total to **20 unique candidate assessments**; if the union exceeds the bound, preserve all important comparison identities and declare the coverage constraint rather than silently omit them. The documentation-only gate is used for the budget-policy experiment, not necessarily human semantic review.
2. Before user judgment, freeze a versioned protocol and manifest containing both repository URLs, exact **head revisions**, rule version, Q1–Q5 rubric, subject anchors and fingerprints, token mapping hash, deterministic control-selection seed and procedure, assessment-frame membership, analysis plan, data-schema version, and relevant checksums. Existing gate/policy results are exploratory, not newly preregistered. Keep **token → candidate → policy membership** outside the user-facing handoff; do not disclose it in the blind packet or package names. GitHub research evidence may contain this internal map; direct the judge **not to inspect PR #16 or selection artifacts** before returning judgments.
3. Create **complete, tracked, clean source snapshots** of the two public repositories at the assessment **head commits** using a repository-aware archive method such as `git archive` (no Git metadata, local state, build products, credentials, or Private sources). These snapshots must be sufficiently complete to inspect project dependencies and surrounding code, not merely isolated source excerpts. Validate exact revision, file identity, archive integrity, source-path reachability for every subject, and a basic sensitive-file inventory before delivery.
4. Generate an **assessor-only handoff** explaining how the project owner can run judgments manually using their chosen model or human workflow. Include a portable reviewer instruction prompt, blinded subject token list with repository, archived path and symbol, pinned revision, exact Q1–Q5 decision rubric, uncertainty/abstention rules, expected evidence references, the result JSON schema, a partial-results example and validation tool/command. **Never generate judgments, pre-fill reviewer answers, or invoke a reviewer.**
5. Stage locally, then copy packages into the implementer's connected Drive mount `G:\My Drive\Transfer\M0015-review\` (cloud folder `My Drive/Transfer/M0015-review/`) with the following stable layout:

   ```text
   M0015-review/
     handoff/
       README.md
       JUDGMENT-PROMPT.md
       assessment-manifest.json
       blinded-candidates.json
       judgments-template.json
       SHA256SUMS.txt
       repositories/
         dotnet-ai-code-hygiene-<head12>.zip
         dotnet-semantic-type-model-<head12>.zip
     submissions/
       judgments-<review-session-id>.json    # Written by project owner, never by Luna
   ```

   Package and repository filenames must not encode sampling policy. Manifest references each source archive's exact revision and SHA-256. Write a handoff **receipt** with actual copy destination, file count, size, hashes, and time. Re-read and hash all copied files; if the Drive API is available also verify remote presence, otherwise explicitly state that only the mounted-folder copy was verified and provider sync is not independently confirmed. Do not overwrite existing user submissions or previously frozen handoffs; use immutable versioned subfolders if rerunning an altered protocol.
6. Create a deterministic **result intake validator** and test it using *synthetic format fixtures only* (correct/invalid tokens, duplicate/conflicting answers, wrong revision or manifest hash, missing fields, inconsistent Q1–Q5 escalation decisions, uncertain/abstained answers). It must validate user judgments without treating fixtures as actual observations. A submitted result has a protocol/manifest reference, reviewer/model/session provenance (record actual values), item token, Q1–Q5 yes/no/uncertain answers, decision, pinned source evidence, and optional uncertainty. Treat partial submissions as incomplete, not a pass.
7. Luna can also implement and verify agent-resolvable RQ-04 budget-as-maximum scenarios and RQ-02/RQ-03 temporal/fallback guards now, but **must not use unsubmitted judgments as actual evidence**. Commit reproducible manifests, code, commands, and handoff receipt to PR #16; avoid committing large repository ZIP archives or fabricated labels to GitHub.

**Stage A stopping point:** Once the handoff is durably copied and verified, stop and report **BLOCKED — awaiting project-owner semantic judgments in Drive submissions**. The existing milestone-completion human review is a separate final gate and is **not** what is blocking Stage A. Do not claim completion of actual-judgment AC-12/AC-13 or VAL-07/VAL-08 without submitted results. Do not poll Drive or promise background follow-up.

### Stage B — Project owner runs judgments (not Luna)

The project owner independently reviews the `handoff/` contents and clean frozen repository snapshots with any chosen human/model reviewer. They should not show the judge PR #16, the selector's scores, chosen policy labels, or token mapping. Record the source-based Q1–Q5 yes/no/uncertain answers and reviewer/model/session provenance using the supplied template; an escalation is supported by Q4=yes or at least two yes answers across Q1/Q2/Q3/Q5. Uncertain and missing remain non-assessed, never automatic acceptance. Put one or more completed `judgments-<review-session-id>.json` files in `M0015-review/submissions/`, then explicitly ask Luna to resume M0015. Partial submissions are allowed; remaining items stay pending.

### Stage C — Luna ingests only user-supplied results

On **a subsequent explicit execution request** (not in the Stage A run), read results from `G:\My Drive\Transfer\M0015-review\submissions\` or the same cloud Drive folder via an available connected app. Validate receipt hashes, manifest reference, schema, token set, pinned source revision/fingerprint, verdict/rubric consistency, provenance, and conflicts. Do not silently overwrite, invent, or correct reviewer answers. Freeze copies of accepted submitted evidence with provenance before unblinding policy mapping. Commit allowable research evidence/derived aggregates; keep reviewer-sensitive raw records at authorized locations if necessary. Perform identity-paired policy findings comparison at fixed budget only for assessed subjects, with coverage/missingness and shared-candidate accounting; avoid unsupported extrapolation. Study temporal history reuse only on valid later-gate evidence and perform calibration only if admissible prior labels suffice. Update all AC/EC/VAL evidence and RQ-01–RQ-05 report conclusions. If results are partial, document remaining evidence obligations and report **BLOCKED — incomplete submissions** rather than fabricate completion. Once all agent-resolvable evidence is satisfied, request the distinct milestone human completion review `REV-M0015-COMPLETION`.

### Research boundaries and economical controls

- A five-review budget is a **ceiling**, not necessarily a quota. Compare zero-cost/no-rule-change versus deliberately justified background surveillance, and report hypothetical unspent review slots separately from measured reviewer time.
- Preserve fixed-seed selection traces; distinguish policy-selected identity displacement from theoretical capacity shortfall, and reserved exploration from weighted-fill replacements.
- Treat labels obtained from this study as **post-gate outcomes**; using them to tune the same gate is invalid. If no valid later gate exists, explicitly report insufficient evidence for historical transfer/calibration.
- The user decides which judge or model performs Stage B. Luna must neither orchestrate nor masquerade as that judge.
- No Private repository material, production hazard or CLI changes, new model-provider integration, long M0013/M0014 replay, or invented semantic yield.

## Baseline Executor Readiness

This is a self-contained **diagnostic/prototype** contract. The baseline executor chooses file layout, local test mechanics, and algorithm implementation, but may not redefine the gate, use simulated reviews as real labels, integrate with the product, or choose a different optimization target after observing results. No prior private/research conversation is needed: this milestone and project authority define the contract. Luna's Stage A is baseline-executable using local Git, .NET, PowerShell and the configured Google Drive mount; it prepares assessor materials but never conducts or launches judgment sessions. The project owner supplies the external judgments in a later stage. If the handoff cannot be copied to the declared Drive target, return `BLOCKED` with the unavailable capability. After a verified handoff, return `BLOCKED — awaiting project-owner judgments` until explicitly resumed. Do not fake quality evidence.

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

### Additional acceptance criteria — research extension

- **AC-11** — Freeze a policy-blinded, source-revision- and fingerprint-pinned assessment frame on both source-changing gates, covering each policy-selected union plus independently seeded eligible control(s), with versioned protocol and token mapping isolated from the judge.
- **AC-12** — Generate and verify complete clean frozen public repository archives plus an assessor-only blinded handoff, result template, review instructions and read-only integrity receipt; copy them to the specified Google Drive Transfer folder without running or spawning any reviewer, and preserve a resumable Stage A checkpoint pending project-owner judgments.
- **AC-13** — After the project owner supplies actual independent Q1–Q5 judgments in the Drive submissions folder and explicitly resumes work, validate and freeze submitted results before policy unblinding; compare same-gate observed actionable findings at equal review budgets, accounting for overlapping identities, missing/uncertain judgments and small-N limitations. Do not complete without real submissions.
- **AC-14** — Assess whether genuinely prior, fingerprint-current type-level judgments can inform a later gate without leakage; demonstrate a temporally valid transfer if available or establish precisely why the current study cannot test it.
- **AC-15** — Analyze outcome-driven local calibration's valid-label sufficiency, selection-conditioned bias, bounded runtime and deterministic fallback; never reuse current-gate submitted judgments as prior labels for the same gate or claim population-wide quality from selected-only labels.
- **AC-16** — Compare maximum-budget/zero-review, explicitly authorized background-surveillance, and change-driven selection at documentation-only and source-changing gates, including zero-work, unknown impact and bounded review count; do not equate a budget with a fill target.
- **AC-17** — Attribute exploration effects using actual omitted changed IDs, replacement IDs and selection-stage evidence; when valid user-submitted judgments are available evaluate observed actionable-value tradeoffs, otherwise report the unresolved outcome evidence without claiming quality benefit.

## Additional Deliverable Obligations

- **ART-01** — Commit reproducible prototype/fixtures plus allowed public result metrics and frozen gate manifest; include commands and integrity checks. Never publish Private candidate/revision/source traces.
- **DOC-01** — Write `docs/research/GATE-LOCAL-SAMPLING-PROTOTYPE.md` with question, provenance, observed/inferred/assumed distinctions, experimental protocol, measurements, limitations, decision, and revalidation triggers. Research is **not** production authority.
- **REV-01** — Prepare `.review/REV-M0015-COMPLETION.md` requesting project-owner/delegate evaluation of technical validity, evidence claims and future architecture recommendation; no self-approval.

### Additional deliverables — research extension

- **ART-02** — Commit frozen experiment protocol, blinded candidate manifest, token mapping and reproducible intake/analysis tooling plus a verified Google Drive handoff receipt. Put the two source snapshot archives and assessor-only handoff in Transfer, not GitHub; incorporate actual user-submitted judgments only after their receipt and validation, preserving privacy, provenance, and integrity. Never invent judgments.
- **DOC-02** — Extend `docs/research/GATE-LOCAL-SAMPLING-PROTOTYPE.md` with a separate evidence-backed RQ-01–RQ-05 section stating question, protocol, observed/inferred/assumed evidence, result or blocker, uncertainty, and implications for production planning. Preserve original exploratory findings.
- **REV-01 (existing)** — The pending M0015 human review must examine the *amended* obligations and evidence before approval; do not open a duplicate review or fabricate a decision.

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

### Additional evidence cases — research extension

| ID | Parent | Required distinct evidence case | Why independent |
|---|---|---|---|
| EC-11a | AC-11 | Both source-changing gates contribute complete selected-union identities and pinned source/fingerprint references. | Real gate coverage |
| EC-11b | AC-11 | Independent deterministic eligible controls, blinded tokens, locked assessor-independent mapping and protocol hash. | Prevent selection-only sample |
| EC-12a | AC-12 | Both clean frozen public repository ZIPs and assessor-only handoff are verifiably copied to the configured Drive Transfer destination; manifest/checksum receipt and no policy leaks. | Real semantic outcome path |
| EC-12b | AC-12 | Stage A stops at a resumable external-judgments checkpoint without launching any agent or fabricating submitted review outcomes. | Missing/uncertain path |
| EC-13a | AC-13 | Actual submitted Q1–Q5 results pass schema, identity, source, provenance and decision checks before unblinding; conflicts and invalid submissions fail. | Outcome comparison |
| EC-13b | AC-13 | Equal-budget outcome comparison accounts for shared identities, unknown/missing cases, small sample and explicitly exploratory evidence. | Inference guardrail |
| EC-14a | AC-14 | Genuine earlier-gate, fingerprint-current judgments inform a later gate, or the absence of valid chronological evidence is explicit. | Temporal history path |
| EC-14b | AC-14 | Current-gate, stale or fingerprint-mismatched observations are excluded as same-gate historical predictors. | Leakage prevention |
| EC-15a | AC-15 | Valid prior-label calibration runs when admissible or records concrete insufficiency without fabricated labels. | Calibration path |
| EC-15b | AC-15 | Bounded fallback/time measurements and selection-bias qualifications without unsupported yield claims. | Fallback/cost path |
| EC-16a | AC-16 | No relevant source change and no authorized surveillance produce zero semantic review selections. | No-work path |
| EC-16b | AC-16 | Change-bearing gate and optional explicitly justified background review remain bounded and observable. | Positive-work path |
| EC-17a | AC-17 | Omitted changed identities, replacements, changed-count differences and actual selection stages reconcile. | Mechanical displacement path |
| EC-17b | AC-17 | Actual submitted semantic outcomes quantify observed tradeoffs, or clearly identify missing assessment coverage. | Semantic-value path |

## Validation Gates

| ID | Depth | Target | Locus/platform | Command/check | Proves | Evidence |
|---|---|---|---|---|---|---|
| VAL-01 | focused | Prototype fixtures / eligibility and sampling tests | local Windows 11, .NET 11 | Prototype's documented focused test launcher | EC-02a, EC-02b, EC-03a, EC-03b, EC-04a, EC-04b, EC-05a, EC-05b, EC-07a, EC-07b | Executed test cases and result artifacts |
| VAL-02 | integration | Frozen real public gate inputs | local Windows 11, .NET 11, PowerShell, Git | Prototype's documented replay/check command for both frozen gates | AC-01, EC-06a, EC-06b, AC-08 | Frozen manifest, identities, distributions and timing evidence |
| VAL-03 | evidence | Recorded/blinded reviewer assessments, where available | authorized reviewer + recorded evidence | Fixed BORINGness rubric with blinded policy label | EC-09a, EC-09b, AC-09 | Reviewer provenance, unanswered items and caveats |
| VAL-04 | hygiene/evidence | Product regression boundary and privacy/integrity audit | Windows 11 + repository review | `./eng/validate.ps1` if product sources touched; otherwise diff/fixture integrity and public-output inspection | ART-01, AC-07, DOC-01 | Scope diff, checksums, validation receipt, sensitive-data inspection |
| VAL-05 | human | Milestone-specific architectural/evidence review | project owner/delegate | Review request `REV-M0015-COMPLETION`; record decision; use installed guide review-check only if available in repository | AC-10, REV-01 | Review request, reviewer decision, final reconciliation |

A prototype-generated command must be stable, checked into `tools/M0015/` with its fixtures, and recorded in the evidence; it need not exist before implementing the prototype. Do not invent a command as having already run. No Tier 4 installed-tool validation is required unless product/distribution code is changed (which is outside this milestone).

### Additional validation gates — research extension

| ID | Depth | Target | Locus/platform | Command/check | Proves | Required evidence |
|---|---|---|---|---|---|---|
| VAL-06 | focused / evidence | Frozen two-gate policy-blinded assessment frame and reproducible source archives | Local Windows 11/.NET 11/Git | Verify frozen revision/fingerprint, selected-union/control coverage, token separation and SHA-256 | EC-11a, EC-11b, ART-02 | Reproducible frozen manifest, blind frame, mapping hash, checked ZIP source paths |
| VAL-07 | external handoff / evidence | Assessor-only repository snapshots, prompt, template, verified Google Drive copies | Implementer Windows Drive mount \`G:\\My Drive\\Transfer\\M0015-review\` and user-accessible cloud folder | Verify local staging/copy/hashes/receipt and assessor isolation; return BLOCKED awaiting user-run judgments | EC-12a, EC-12b, AC-12 | Two ZIP archives, handoff, source hashes, Drive receipt, no auto reviewer launch |
| VAL-08 | independent supplied evidence / analysis | User-submitted judgments, then identity-paired fixed-budget policy results | Separate later Luna run; mounted Google Drive submissions folder | Validate schema/rubric/provenance/pinned sources; freeze submissions before unblind; analyze coverage and yield | EC-13a, EC-13b, EC-17a, EC-17b, AC-13 | Actual validated submitted judgments and analyses; BLOCKED if missing |
| VAL-09 | diagnostic | Historical evidence admissibility and bounded calibration | Windows 11/.NET 11 fixtures plus truly earlier-gate evidence when present | Reject current-gate and stale labels; compare grid/fallback and measure costs | EC-14a, EC-14b, EC-15a, EC-15b | History or insufficiency evidence, benchmark/validation receipt |
| VAL-10 | focused / reporting | Zero-change/unknown-impact and changed-code gates; completed research documentation | Windows 11/.NET 11 and repository review | Verify maximum-budget versus fill and optional background work; reconcile DOC-02 and human review request | EC-16a, EC-16b, DOC-02 | Selection-count evidence and RQ report, with staged blocker status |

## Human Review

- Applicability: required / blocking.
- Review class: milestone completion; architectural pivot and evidence integrity.
- Canonical ID: `REV-M0015-COMPLETION`.
- Subject: whether software contract, local calibration, historical-evidence interpretation, and same-gate findings justify a subsequent production redesign.
- Reviewer: project owner/delegate; decision must be recorded by them.
- Waiver: none by implementation agent.
- Completion check: consult actual repository guide review tooling when available; do not invent `eng/review-check.ps1` where absent.

## Completion Expectations

Use the project's guide 0.9.1 execution workflow. Reconcile every stable obligation and evidence case with `.execution/M0015-gate-local-sampling-prototype.md`, actual experiment outputs and validation gates. Preserve a compact obligation-to-proof summary in this milestone's completion evidence before any ledger cleanup. If only external human judgment is outstanding, return `AWAITING HUMAN REVIEW`. For the amended AC-11–AC-17 sequence, a completed handoff without user-submitted judgments is `BLOCKED — awaiting project-owner judgments`, not `AWAITING HUMAN REVIEW` or `done`. Only after Stage C reconciles all obligations does the distinct milestone-level human review become the remaining gate. If labels are still unavailable, empirical superiority is unestablished.

## Completion Evidence

**Historical notice (M0015 staged user-judgment amendment):** The evidence table below records the already executed prototype and correction phases only. It does not establish the appended AC-11–AC-17, ART-02, DOC-02, or EC-11a–EC-17b. The implementer must append their individual evidence rows after executing the amendment; milestone completion and REV-M0015-COMPLETION remain pending.


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
| AC-09 | Stage A user-run handoff is published to `G:\My Drive\Transfer\M0015-review\handoff\` with 24 blinded candidates from both source-changing gates, independently seeded controls, full frozen source archives, assessor rubric/instructions, judgment template/schema, and validator. No judgments are included; owner results belong in `M0015-review/submissions/`. |
| AC-10 | Research recommends INCONCLUSIVE and names API, state, history, and review-ticket decisions for M0016. |
| ART-01 / DOC-01 | Public-only result inventories and SHA-256 reference are committed; `integrity` verifies read-only and `regenerate-integrity` is explicit. Tamper-negative test and reproducible commands are recorded; report is research, not production authority. No Private traces or source were accessed. |
| REV-01 | `.review/REV-M0015-COMPLETION.md` requests project-owner/delegate review and records no decision. |

The Stage A handoff's ten delivered files were verified at the destination against its SHA-256 manifest (`4D7B1571559162A61AEC770DF79C0434F25AE96E96EE9F088DA8A93D5ACDEA71`); both source archive hashes match the frozen manifest. Six synthetic-only validator fixtures passed. The prior focused prototype, read-only integrity verification, negative tampering case, and three-gate reproducibility validations passed. No product source, production sampler, policy, persistent-state contract, or review command changed. **State: BLOCKED — awaiting project-owner semantic judgments**; semantic usefulness remains inconclusive. Stage C validation/unblinding/analysis requires an explicit subsequent request. Completion review remains pending with no approval recorded.

## Escalation Boundary

Material changes to policy, prototype-to-production integration, persistence compatibility, public review semantics, scope, validity of the optimization objective, or mandatory validation require returning to planning. Local files/functions/test algorithms within this contract are implementation-owned.
