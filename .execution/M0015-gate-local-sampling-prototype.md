# Execution Ledger — M0015 Gate-Local Sampling Prototype

Primary milestone: `docs/milestones/M0015-gate-local-sampling-prototype.md`

**State:** implementation complete; awaiting human review. This operational ledger is not project authority. The milestone controls on conflict. Planner-seeded obligation rows have been preserved verbatim. The ledger may compress work, never obligations.

## Milestone Obligation Registry

| ID | Type | Obligation (exact milestone wording) | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | Freeze and document at least two eligible public base/head gate snapshots, source/candidate population counts, relevant diff evidence, seed(s), budget, and environment before inspecting comparison results; use the prescribed fallback repository if needed. | WP-01, WP-03 | `tools/M0015/Gates.json`; `tools/M0015/FrozenGateEvidence.json`; three gate snapshots including one source-changing and one documentation-only boundary | VAL-02; revisions, source diff, candidate counts, seed, budget, policy weights, and analysis protocol were recorded before comparison emission. The original exploratory run and descriptive-output amendment are disclosed as protocol limitations. | complete with disclosed limitation |
| AC-02 | acceptance | Working prototype enumerates actual rule-eligible head-state type candidates, preserving project identity, disambiguation, eligibility, and auditable changed/unchanged/unknown classification. | WP-02, WP-03 | Roslyn enumerator; hygiene 96, docs-only 420, source-changing Semantic Type Model 396 head candidates; source gate has 4 changed, 18 context-affected, 363 background, 11 unknown | VAL-01, VAL-02 | complete |
| AC-03 | acceptance | A deterministic, budget-respecting, without-replacement selection contract supports priority for changed candidates and possible exploration of unchanged candidates, with explicit selection reason/identity evidence. | WP-02, WP-03 | Three policy result sets per gate; five unique candidates per policy; frozen 20% minimum unchanged exploration | VAL-01, VAL-02 | complete |
| AC-04 | acceptance | Prior evidence affects selection behavior in controlled fixtures without misclassifying aggregate review events, synthetic acceptances, or missing type-level labels as actual quality observations. | WP-02 | Distinct stable-ID 512-seed fixture requires negative > unknown > accepted selection tally; source-fingerprint matching marks evidence current, mismatch stale/neutral, absent or rejected aggregate/synthetic/uncertain evidence missing/neutral | VAL-01 | complete in fixtures; no actual gate labels |
| AC-05 | acceptance | Bounded gate-local parameter search uses a frozen candidate grid and independent assessed labels where valid; otherwise it explicitly follows and proves a stable uncalibrated fallback without claiming learned parameters. | WP-02, WP-03 | `m0015-grid-v1`; fixed identity-fold train/evaluation split; output states both subsets are conditioned on previously selected/labeled candidates and cannot establish general policy optimization; all real gates used fixed fallback | VAL-01, VAL-02 | complete via fallback; no learned parameters |
| AC-06 | acceptance | Equal-budget same-gate comparisons for uniform, change-first, and history-aware policies report candidate identities/overlap and change/background selection/displacement, with separate measures for overhead and any actual labeled yield. | WP-03 | Three comparison JSON files preserve the original docs-only boundary; corrected results report actual omitted/replacement identities, changed-count differences, selected-set symmetric difference, traced selection stage, and separate theoretical capacity shortfall | VAL-02 | complete for scheduling evidence |
| AC-07 | acceptance | Fixed-seed repeatability, changed-input invalidation, zero/short/large-population budgets, and sparse/missing/corrupt history have explicit safe results without changing existing production state. | WP-02, WP-03 | Self-tests cover repeatability, candidate/fingerprint invalidation, budget boundaries, unknown and malformed history; product state untouched | VAL-01, VAL-04 | complete |
| AC-08 | acceptance | Measured selection and calibration costs on both gate snapshots, including cold/warm timing, memory/artifact footprint and reference-platform details; calibration finishes within the declared five-minute ceiling or records a clear overrun/failure. | WP-03 | Three comparison files and report record enumeration, cold/warm selection and explicitly named fallback calibration timings, heap/working set, artifact footprint, and environment | VAL-02 | complete; no-label fallback only, grid not timed on real labels |
| AC-09 | acceptance | Bounded, policy-blinded real semantic judgments are recorded with reviewer provenance when available, and absent/uncertain judgments are left missing; do not claim comparative defect yield without sufficient independent assessments. | WP-04 | 11-token policy-blinded sample; no authorized independent reviewer/judgments available; no labels fabricated | VAL-03 | awaiting reviewer availability; evidence gap disclosed |
| AC-10 | acceptance | Final report gives an evidence-bounded **adopt/refine/reject/inconclusive** recommendation for gate-local selection and explicitly states the production API/state/review-ticket decisions that remain for subsequent planning, without editing production sampling policy. | WP-04, WP-05 | Research report recommends INCONCLUSIVE and lists unresolved production decisions for M0016 | VAL-05 | awaiting human review |
| ART-01 | artifact | Commit reproducible prototype/fixtures plus allowed public result metrics and frozen gate manifest; include commands and integrity checks. Never publish Private candidate/revision/source traces. | WP-02, WP-03, WP-05 | `tools/M0015/`, frozen manifest, public result JSON, `SHA256SUMS.txt`; no Private evidence accessed | VAL-04 | complete |
| DOC-01 | documentation | Write `docs/research/GATE-LOCAL-SAMPLING-PROTOTYPE.md` with question, provenance, observed/inferred/assumed distinctions, experimental protocol, measurements, limitations, decision, and revalidation triggers. Research is **not** production authority. | WP-04 | `docs/research/GATE-LOCAL-SAMPLING-PROTOTYPE.md` | VAL-04 | complete |
| REV-01 | human review | Prepare `.review/REV-M0015-COMPLETION.md` requesting project-owner/delegate evaluation of technical validity, evidence claims and future architecture recommendation; no self-approval. | WP-05 | `.review/REV-M0015-COMPLETION.md`; pending reviewer decision | VAL-05 | awaiting human review |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-02a | AC-02 | Eligible new/changed candidates correctly identified at a real gate | VAL-01 | Hygiene gate: 15 changed candidate identities from Roslyn base/head comparison | complete |
| EC-02b | AC-02 | Unchanged or unknown candidates remain eligible and auditable | VAL-01 | Hygiene gate: 22 context-affected, 3 background, 56 unknown; docs-only gate 420 background; source-changing gate 18 context-affected, 363 background, 11 unknown | complete |
| EC-03a | AC-03 | Changed candidates dominate under change-first policy | VAL-01 | Hygiene gate: change-first selected 4 changed versus 1 unchanged/unknown; uniform selected 3 changed | complete |
| EC-03b | AC-03 | Unchanged exploration remains possible under nonzero eligibility | VAL-01 | Fixture and gate outputs show unchanged selection; history-aware selected 2 unchanged/unknown at hygiene gate | complete |
| EC-04a | AC-04 | Actual individually attributable history has supported effect | VAL-01 | No actual per-type history exists; distinct-ID deterministic 512-seed fixture tallied negative=76, unknown=38, accepted=20 with matching current fingerprints | unavailable for real gates; fixture only |
| EC-04b | AC-04 | Aggregate-only / unlabeled / synthetic evidence does not become quality labels | VAL-01 | Parser rejects aggregate, synthetic, uncertain, missing-fingerprint and malformed records; stale fingerprint is neutral; absent status remains missing | complete |
| EC-05a | AC-05 | Valid independent-labeled case calibrates and keeps evaluation separate | VAL-01 | Synthetic fixture exercises fixed grid and disjoint deterministic folds; no actual independent gate labels | no real-label case available |
| EC-05b | AC-05 | Sparse/no valid labels follows deterministic uncalibrated fallback | VAL-01 | Empty, sparse, malformed history and both real gates use stable fallback | complete |
| EC-06a | AC-06 | Same-gate comparison reports selected identities, not just counts | VAL-02 | Full selected IDs and per-pair overlap/Jaccard in all three comparison files; source-changing gate tests actual changed-code prioritization | complete |
| EC-06b | AC-06 | Changed checks displaced by exploration are counted | VAL-02 | Hygiene gate count difference is 1 (4 vs 3); one named changed candidate omitted and one context-affected replacement entered at weighted-fill. Source-changing gate count difference is 1 (3 vs 2), with named test-class omission and unchanged-background replacement at weighted-fill. Theoretical capacity shortfall 2 is separate. | complete |
| EC-07a | AC-07 | Stable seed and persisted protocol replay identical | VAL-01 | Repeated shared-random replay produced identical selected IDs across all three frozen gates; result equality checked separately from variable timings | complete |
| EC-07b | AC-07 | Changed inputs and malformed/insufficient history fail conservatively | VAL-01 | New/fingerprint-change invalidation and missing/unsupported/corrupt-history fixtures; no production state access | complete |
| EC-09a | AC-09 | Genuine blinded judgments with provenance when available | VAL-03 | 11-item blinded review sample; no authorized assessor was available; no judgments or provenance fabricated | unavailable; sample ready |
| EC-09b | AC-09 | Missing/uncertain judgments explicitly not treated as acceptance | VAL-03 | Report and sample retain missing/uncertain as missing; no quality yield claimed | complete |

## Work Packages

Executor-owned. Before code or analysis, read the milestone and confirm exact ID-set equality for all AC/ART/DOC/REV and EC obligations. Inspect the live repository and map every obligation to one or more bounded work packages. Do not replace the obligation registry with an abbreviated task list.

| ID | Scope | Status | Evidence |
|---|---|---|---|
| WP-01 | Reconcile authority, register milestone, freeze two public gate inputs before selection | complete with disclosed exploratory-run and descriptive-output limitations | Authority/readiness; three frozen gates in `Gates.json` and `FrozenGateEvidence.json`; revisions, source diffs, populations, seeds, budget, weights, and analysis recorded before comparisons; both limitations disclosed |
| WP-02 | Build isolated Roslyn candidate enumerator and deterministic selector/calibration fixtures | complete | `tools/M0015/M0015.csproj`, `Program.cs`, `fixtures/`; `eng/M0015.ps1 -Action test` passed |
| WP-03 | Enumerate and freeze candidate populations; run same-gate policy comparison and timing | complete | `FrozenGateEvidence.json`; three comparison JSONs including source-changing and docs-only boundary gates; exact identity repeat check; corrected metrics and timings in report |
| WP-04 | Prepare blinded reviewer evidence, inspect privacy/integrity, write evidence-bounded research report | complete; semantic labels unavailable | Blinded 11-item sample, public-only outputs, research report; no judgments invented |
| WP-05 | Run focused and repository validation, reconcile criteria/cases, completion audit and review request | complete; human gate pending | Focused fixtures, negative tamper test, read-only checksum verification, replay reproducibility, corrected evidence reconciliation, no product sources touched; review request updated |

## Validation Gates

| ID | Required validation | Target/locus | Proves evidence units | Status | Evidence |
|---|---|---|---|---|---|
| VAL-01 | Prototype fixtures / eligibility and sampling tests | local Windows 11 / .NET 11 | EC-02a, EC-02b, EC-03a, EC-03b, EC-04a, EC-04b, EC-05a, EC-05b, EC-07a, EC-07b | passed; real-label cases unavailable | `.\eng\M0015.ps1 -Action test`; build 0 warnings/0 errors; distinct-ID history propensity ordering, current/stale/missing evidence, calibration split/scope and fallback, Roslyn fixtures, and tampering negative test passed |
| VAL-02 | Frozen real public gate inputs | local Windows 11 / .NET 11 / PowerShell / Git | AC-01, EC-06a, EC-06b, AC-08 | passed with disclosed exploratory protocol limitations | `.\eng\M0015.ps1 -Action replay ...`; three frozen public snapshots (source-changing and docs-only boundary included), candidate counts/diffs, allocations, corrected displacement, timings; repeat selected identities match |
| VAL-03 | Blinded reviewer assessment if available | authorized reviewer / evidence | EC-09a, EC-09b, AC-09 | unavailable; review-ready evidence prepared | No authorized reviewer connected/provided; `.review/REV-M0015-COMPLETION.md` and 11-item blinded sample; no semantic labels recorded |
| VAL-04 | Regression / scope / privacy / integrity | Windows 11 / repository review | ART-01, AC-07, DOC-01 | passed | `.\eng\M0015.ps1 -Action integrity` verifies committed checksum references read-only and validates gate identities, counts, displacement arithmetic, eligibility, unique selection; `-Action test` negative tamper case fails as expected. No production files changed; no Private evidence accessed. Full `eng/validate.ps1` not applicable per milestone because no product source was touched. |
| VAL-05 | Milestone-specific human review | project owner/delegate | AC-10, REV-01 | awaiting human review | `.review/REV-M0015-COMPLETION.md` requests decision; no approval recorded |

## Resume Point

Last completed work package: WP-05 implementation/evidence reconciliation

Current work package: human review only

Next concrete action: project owner/delegate reviews the report, blinded sample, and disclosed protocol limitation, then records an explicit decision or requested corrections.

Known agent-resolvable gaps: none. Real per-type labels and a reviewer decision are external evidence, not inferable from the implementation agent.

External blockers or planning escalations: no authorized semantic reviewer or actual labels were available. Original M0015 smoke exposure and a descriptive-output amendment after the first corrected replay are disclosed; the three-gate results are reproducible exploratory scheduling evidence only.

## Final Reconciliation

- [x] Re-read milestone from disk and verify obligation-set equality (13 obligations) and evidence-case-set equality (14 cases).
- [x] Verify concrete evidence for every row and independent behavior path; real label-dependent paths are explicitly unavailable, not claimed complete.
- [x] Verify each required validation gate at declared target/locus; VAL-03 and VAL-05 remain waiting on an authorized human.
- [x] Distinguish actual reviewer labels, synthetic fixtures, and absence of evidence.
- [x] Publish corrected research, fixture, three-gate artifact evidence, current/stale/missing history behavior, read-only integrity verification, and documented fallback calibration cost.
- [x] Create milestone-specific review request; no self-approval.
- [x] Perform completion audit and update compact completion evidence in the milestone; report **AWAITING HUMAN REVIEW** with preregistration, selection-conditioned calibration, and semantic-label limitations disclosed.
