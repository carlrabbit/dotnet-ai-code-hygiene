# M0015 — Gate-Local Semantic Sampling Prototype

**Status:** non-production experiment; semantic usefulness inconclusive. This report is research evidence, not production authority.

## Question and recommendation

At one concrete source-change gate, can a deterministic fixed-budget selector allocate BORINGness reviews more usefully than uniform or change-first selection, using individual historical review evidence and bounded local calibration where available?

**Recommendation: INCONCLUSIVE on adoption; refine the evidence before planning a production migration.** The prototype enumerated real eligible types and reproduced same-gate allocations at a fixed budget. Stage C validates owner-submitted judgments for the 16-item amended frame: no actionable findings were observed for any policy. That small convenience sample does not establish policy equivalence, recall, or general semantic-review effectiveness, and no time savings were measured. M0016 should decide whether another independently frozen source-changing gate or later-gate history-transfer study is justified before production planning.

> **Correction status:** The original M0015 observations below are preserved as exploratory evidence. The original comparison JSONs are archived as `tools/M0015/results/exploratory-original-*-comparison.json`. The original displacement field measured theoretical capacity shortfall and was mislabeled as checks displaced; corrected results are in the current `*-comparison.json` files and the frozen three-gate manifest. Use the correction section below for displacement interpretation. The recommendation remains INCONCLUSIVE after Stage C: the submitted judgments yield zero actionable findings in this small frame, which cannot establish effectiveness or equivalence. The Stage A package adds candidates from both source-changing gates and does not alter or replace the original 11-item exploratory sample.

## Provenance and evidence boundary

The question follows the M0015 milestone and the historical M0012–M0014 gate-local pivot. The latter replay was not repeated. Existing production sampling, hazard policies, persistent state, and review commands were left untouched.

Only the following evidence is used here:

- the public source snapshots identified in `tools/M0015/Gates.json` and `tools/M0015/FrozenGateEvidence.json`;
- public candidate identities, fingerprints, and selection results under `tools/M0015/results/`;
- synthetic fixtures for selector, evidence-admission, and calibration split behavior;
- the assessment-ready policy-blinded sample in `tools/M0015/reviewer-sample.json`.

No Private source, trace, revision, or candidate identity was accessed for this experiment. Result JSON contains no source text. SHA-256 digests in `tools/M0015/results/SHA256SUMS.txt` cover the frozen manifest and replay result files.

### Preregistration limitation

The gate revisions, seeds, budget, weights, and finite calibration grid were fixed before the first prototype run. An initial plumbing run exposed that policies were using separate per-candidate random draws. I changed that implementation to shared seed-derived candidate draws before generating the final paired artifacts, so policy differences are compared with common randomness. The final candidate counts and path-diff manifest were written before the final paired comparisons. The initial smoke result had already been seen, however, so this was not a fully blind preregistered experiment. It did not change gates, seeds, budget, or weights, but it is a protocol deviation. Treat the results as reproducible exploratory evidence; a confirmatory study should freeze all inputs and implementation version before any selection output is viewed.

## Experimental protocol

The prototype uses the existing rule's candidate boundary: source-backed classes, structs, records, and interfaces from Roslyn-loaded projects; enums and delegates are excluded, nested types are included. Identity is SHA-256 over the relative project path, source path, fully qualified semantic symbol, and same-symbol declaration ordinal. Declaration text has a separate fingerprint. Path changes and identity collisions are unmatched; no rename equivalence is guessed.

For each head candidate, a missing base identity or changed declaration fingerprint is `new/changed`. An unchanged declaration in a touched source document is `unchanged/context-affected`, a deliberately coarse document-level fallback. An unchanged candidate in another changed project is `unknown` because this prototype does not build a project impact graph. An unchanged candidate in an untouched project is `unchanged/background`. Unknown-impact candidates remain eligible for the unchanged cohort and are not silently reclassified as changed or background.

Each gate's comparison pool contains all new/changed candidates plus at most 25 unchanged or unknown candidates, chosen deterministically from the frozen seed. All policies see the same pool and use the same SHA-256-derived random variate per candidate. They select without replacement at budget 5:

- **Uniform:** weight 1 for changed and unchanged candidates.
- **Change-first:** weight 4 for changed, 0.25 for unchanged.
- **History-aware:** frozen no-label fallback weights of 4 and 0.5, with at least `ceil(0.2 × budget)` unchanged exploration slots. Remaining items use weighted ranking.

Ranking uses the exponential-race key `-ln(U)/weight`. The fallback's reserved exploration slot is a minimum; later weighted draws can select additional unchanged candidates. Weights, the 20% exploration fraction, and the 25-candidate unchanged cohort are experimental parameters, not approved product policy.

Only per-type `accepted` or `negative` records with independent reviewer provenance are admitted as quality history. Negative judgments raise the experimental weight; accepted observations lower it. Uncertain, unknown, synthetic, aggregate-event-only, missing, malformed, and unsupported history remain neutral/unknown. Existing production aggregate acceptance does not become an individual type label. No production sampling JSON is read or modified.

The optional isolated history input is versioned JSON with `schemaVersion: 1` and `entries` containing `candidateId`, `kind`, and `provenance`. Only matching stable candidate IDs and `accepted`/`negative` kinds with provenance prefixed `independent:` enter the gate. A missing or corrupt file yields no labels. This schema and its fallback behavior are experimental and are not a new product persistence contract.

The finite calibration grid was fixed as `m0015-grid-v1`: `(changed, unchanged, exploration)` values `(2,1,0.10)`, `(4,0.5,0.20)`, and `(8,0.25,0.20)`. With enough labels, the prototype uses stable candidate-ID folds, chooses parameters using training labels only, and evaluates the chosen values on held-out labels. Fixtures exercise the split and grid path. There are no valid real gate-local labels, so both gate runs used the fixed `(4,0.5,0.20)` fallback; the grid was not run on real outcomes.

## Frozen public gates and observed results

Environment: Windows 11 Home, build `10.0.26200`; PowerShell `7.6.5`; Git `2.55.0.windows.3`; .NET SDK `11.0.100-rc.1.26425.128` (preview); four logical processors. Both Roslyn snapshots loaded with zero workspace diagnostics.

| Gate | Frozen base → head | Changed paths (SHA-256 in manifest) | Base → head eligible types | Head classification | Comparison pool |
|---|---|---:|---:|---|---:|
| `dotnet-ai-code-hygiene` | `48a87618d02278a178cad02f44a924e207ec88d3` → `4d05393b4501a0b4d701f6ba6e1d051e9bd35ec2` | 54; `DD83B0B54359EA2D3BACFF3AF6DF6567955EF6EAC221283445986834A3AD0D91` | 91 → 96 | 15 new/changed; 22 context-affected; 3 background; 56 unknown | 15 changed + 25 unchanged/unknown = 40 |
| `dotnet-semantic-type-model` | `f45a5a2876115defdec06fc3a18fdf131b2ee914` → `4be96e7bf40b5a7a44911b4e64fa94bed7c2b31a` | 6; `6799C6DA8A74419C12303768391E8199832028D8624B81A6AA299B0420DBFE4E` | 420 → 420 | 0 new/changed; 0 context-affected; 420 background; 0 unknown | 25 unchanged = 25 |

The second gate changed only documentation/README files, so it is a useful no-source-change case but does not test changed-code priority. Its 420 eligible candidates remained available; the 25-item unchanged cohort was the only comparison pool.

Each policy selected five unique candidate identities per gate. On the source-bearing hygiene gate, uniform selected 3 changed and 2 unchanged/unknown; change-first selected 4 changed and 1 unchanged/unknown; history-aware selected 3 changed and 2 unchanged/unknown. The original comparison labeled the theoretical two-slot capacity shortfall as displaced checks. That label was incorrect; corrected actual displacement is detailed below. Neither count is evidence that any candidate was valuable or defective.

On that gate, selected-set overlap was 4/5 between uniform/change-first (Jaccard 0.667), 5/5 between uniform/history-aware (1.0), and 4/5 between change-first/history-aware (0.667). On the documentation-only gate all three policies selected the same five identities (Jaccard 1.0 for each pair). Full selected identities and reasons are in the comparison JSON; see the corrected count and identity analysis below.

All real candidates had unknown individual review history. Consequently the history-aware policy ran only as a change-plus-exploration fallback, not as an evidence-learned policy. The comparison provides no quality yield, correctness improvement, or independently verified savings metric.

## Original two-gate exploratory runtime and footprint

These are the initial exploratory measurements preserved from the first M0015 report. The correction-run timings for all three gates are in the correction section above; use those for the corrected comparison.

Measurements are local single-process timings, not a cross-machine benchmark. “Cold” is a new process/workspace invocation; the operating system file cache may have been warm. Selection and no-label fallback calibration were each run a second time for warm timing.

| Gate | Base/head Roslyn enumeration (cold) | Selection cold/warm | Fallback calibration cold/warm | Compare process working set | Managed heap snapshot |
|---|---:|---:|---:|---:|---:|
| Hygiene | 5,616.5 / 6,545.2 ms | 5.1 / 0.3 ms | 2.3 / 0.02 ms | 31.7 MB | 2.0 MB |
| Semantic type model | 17,391.0 / 17,729.3 ms | 7.5 / 0.3 ms | 3.7 / 0.02 ms | 34.6 MB | 4.2 MB |

The finite calibration search did not run against either gate because no real labels were available; the measured calibration values are the fallback-path cost only. This is well under the five-minute bound but does not establish the cost of a future labeled search on a large corpus. No overrun occurred.

The original six public base/head candidate inventories and two comparisons occupied 587,203 bytes total. `FrozenGateEvidence.json` was 5,084 bytes and the 11-item reviewer sample was 5,508 bytes in that exploratory run. Inventories include IDs, relative project/path/symbol, classification, declaration fingerprint, and workspace diagnostics; they contain no source text. The initial integrity action rewrote checksums; the correction section documents the replacement read-only verifier and explicit regeneration action.

## Correction run: actual displacement, source-changing gate, and evidence integrity

The corrected comparison calculates candidate identity differences between the actual change-first and history-aware five-item sets. It reports (a) change-relevant selected-count difference, (b) changed candidate identities selected by change-first but omitted by history-aware, (c) replacement identities present only in history-aware, (d) full selected-set symmetric difference, and (e) theoretical changed-capacity shortfall as a separate metric. Replacement stage comes from the selection trace; the report does not infer a causal exploration reservation when the candidate entered during weighted fill.

| Frozen gate | Revisions | Changed paths | Base/head candidates | Head changed / context / background / unknown | Pool |
|---|---|---:|---:|---|---:|
| `hygiene-pr15` | `48a87618d02278a178cad02f44a924e207ec88d3` → `4d05393b4501a0b4d701f6ba6e1d051e9bd35ec2` | 54 | 91 / 96 | 15 / 22 / 3 / 56 | 40 |
| `semantic-model-head` (documentation-only boundary) | `f45a5a2876115defdec06fc3a18fdf131b2ee914` → `4be96e7bf40b5a7a44911b4e64fa94bed7c2b31a` | 6 | 420 / 420 | 0 / 0 / 420 / 0 | 25 |
| `semantic-model-source` (source-changing) | `9ba2bf4dc8268b490236d9f287f358d7a840353a` → `88b5ad5092e38e00257d4e327d6606afa2bf4952` | 3 | 396 / 396 | 4 / 18 / 363 / 11 | 29 |

The source gate changes `src/SemanticTypeModel.TestData/SemanticTerminologyProfile.cs`, `src/SemanticTypeModel.TestData/SemanticTestDataGenerator.cs`, and `tests/unit/SemanticTypeModel.TestData.Tests.Unit/TestDataCorrectiveTests.cs`. Frozen revisions, source path diff, candidate populations, seed `M0015-v1-semantic-model-source-seed-01`, budget 5, policy weights, finite grid version, and analysis protocol are captured in `FrozenGateEvidence.json` before comparisons are written. The added gate was chosen by source diff inspection only. An initial corrected replay exposed the expected selections; the output schema was then expanded with explicit symmetric-difference and trace-stage fields and replayed. No gate, population, seed, budget, policy, or outcome changed, but this descriptive-schema amendment after first outcome inspection is a preregistration limitation. Treat all results as exploratory, not confirmatory.

At the hygiene gate, change-first selects 4 changed candidates and history-aware selects 3: **the changed-selection count difference is 1**. Change-first's `SummaryQualityReviewRuleModule` (`src/DotNetAiCodeHygiene.Core/Rules/SemanticReview/SummaryQualityReview.cs`) is omitted by history-aware. The corresponding history-aware-only candidate is `RuleCatalog` (`src/DotNetAiCodeHygiene.Core/Rules/RuleExecution.cs`), classified `UnchangedContextAffected`; its trace says `weighted-fill`. The selected-set symmetric difference is 2 identities (one omitted, one replacement). The separately named theoretical changed-capacity shortfall is 2 because 15 changed candidates existed and only 5 slots were available; it is not a count of actual displaced selections.

At the source-changing Semantic Type Model gate, uniform selects 0 changed / 5 background, change-first selects 3 changed / 2 background, and history-aware selects 2 changed / 3 background. The changed-selection count difference is 1. Change-first's `TestDataCorrectiveTests` (`tests/unit/SemanticTypeModel.TestData.Tests.Unit/TestDataCorrectiveTests.cs`) is omitted by history-aware; its replacement is `JsonSchemaUnsupportedCompositionTransformation` (`src/SemanticTypeModel.JsonSchema/Derivation/JsonSchemaUnsupportedCompositionTransformation.cs`), an `UnchangedBackground` candidate selected during `weighted-fill`. The symmetric difference is 2; theoretical changed-capacity shortfall is 2 (four changed candidates existed with five available slots, while history-aware chose two). This is scheduling allocation evidence only. It does not establish that any selected or omitted declaration deserves a semantic finding.

The documentation-only boundary remains unchanged: all three policies select the same five identities, with zero changed candidates and five background candidates. This confirms the comparison's no-source-change behavior without treating documentation changes as source changes.

History fixtures now use distinct stable candidate IDs. Across 512 deterministic seed trials in the controlled fixture, the target candidate was selected 76 times with negative evidence, 38 with unknown history, and 20 with accepted evidence. The test requires negative > unknown > accepted. These are reproducibility/sensitivity tallies, not empirical probabilities or quality estimates. Individual history records must include a source fingerprint. Matching fingerprints yield `Current`; mismatched fingerprints yield `Stale` and neutral `Unknown` history; absent or rejected aggregate-only, synthetic, uncertain, or malformed evidence yields `Missing` / neutral history. The isolated prototype evidence schema remains separate from production sampling state and `hygiene review accept`. A future production history contract would need explicit subject identity, source/rule version fingerprint, judgment kind, reviewer provenance, observation time, and rules for invalidation, retention, and uncertainty; this prototype does not authorize that contract.

Calibration keeps stable identity-fold training/evaluation separation and the fixed finite grid. Its eligible labels are drawn only from candidates already selected and independently labeled in prior gate-local evidence. Therefore both training and held-out evaluation are conditioned on prior selection and review availability; conditional labeled-subset yield is selection-biased and cannot establish general policy optimization or population semantic quality. No valid real labels exist, so all three current gates report deterministic fallback; no grid search was run on real outcomes. Timing output names the mode (`deterministic-fallback` versus `finite-grid-calibration`) and reports cold/warm calibration separately. The bounded fallback remains when labels are sparse or absent.

| Gate | Base/head enumeration cold (ms) | Selection cold/warm (ms) | Calibration mode and cold/warm (ms) | Managed heap snapshot | Process working set |
|---|---:|---:|---:|---:|---:|
| Hygiene | 5,900.2 / 5,607.4 | 4.975 / 0.297 | fallback 2.199 / 0.013 | 2.07 MB | 32.19 MB |
| Semantic model docs-only | 14,953.0 / 15,815.4 | 5.696 / 0.197 | fallback 2.467 / 0.015 | 4.33 MB | 35.14 MB |
| Semantic model source-changing | 16,119.9 / 15,132.1 | 6.765 / 0.197 | fallback 2.471 / 0.016 | 4.19 MB | 35.02 MB |

The corrected `tools/M0015/results/` directory contains 11 JSON files totaling 1,138,423 bytes, including the preserved original comparison outputs and three current gate result sets. All 23 non-build files under `tools/M0015/`, including the checksum reference, total 1,190,548 bytes.

The replay tool now writes checksums only through the explicit `-Action regenerate-integrity`. `-Action integrity` compares current files to the existing committed reference and rejects missing, unexpected, changed, or mismatched artifacts without writing. The focused validation includes a tamper test that changes a required artifact after checksum generation and requires verification to fail.

## Amended research questions — Stage A protocol checkpoint (11 October 2026)

At this historical Stage A checkpoint, PR #16 had a **ready M0015 user-run-judgment amendment** and the RQ-01–RQ-05 findings had **not yet been produced**. The amended workflow required freezing the pinned public repository ZIPs and blind packet, then waiting for the project owner to submit results and explicitly resume. No automated delegation or Luna-performed review judgments were allowed. The original three-gate scheduling experiment above remains preserved and exploratory, not evidence of comparative semantic-review value. The authoritative amendment, frozen-assessment requirements, and per-question acceptance criteria are in `docs/milestones/M0015-gate-local-sampling-prototype.md`; the current implementation-owned status is in `.execution/M0015-gate-local-sampling-prototype.md`.

- **RQ-01 — Actual semantic yield:** Requires separate, blinded independent Q1–Q5 decisions for selected-union candidates plus independently sampled controls on the two source-changing gates, locked before policy unblinding.
- **RQ-02 — Reusable history:** Requires real prior-gate type labels that match identity and source/rule fingerprint on a later gate; missing chronological evidence must remain an explicit gap.
- **RQ-03 — Locally calibrated parameters:** Must separate test-only grid behavior from real valid-label calibration, address selection-conditioned labels, and retain fallback without unsupported claims.
- **RQ-04 — Review budget as a ceiling:** Compare a no-rule-change gate yielding zero expensive reviews against explicitly justified background selection; do not interpret a budget of five as a mandate to spend five.
- **RQ-05 — Exploration tradeoffs:** Attribute actual changed IDs displaced and compare review outcomes only when independently judged; preserve the difference between reserved exploration and weighted-fill selection.

Stage A's amended handoff was copied and verified at the immutable versioned path recorded below and in the delivery receipt. Stage B was project-owner judgment collection in `M0015-review/submissions/`. Stage C evidence and per-question findings are appended below. No cost savings, parameter improvements, or product policy changes are claimed.

## Semantic assessment and limitations

At the Stage A checkpoint, no actual judgments had been supplied. `tools/M0015/reviewer-sample.json` preserves the original 11-candidate exploratory sample (six from the hygiene gate, five from the semantic-type-model gate) with policy labels omitted. The separately frozen amended Stage A frame covers 16 candidates from both source-changing gates: the complete three-policy selection union plus one independently seeded outside-union control per gate. The documentation-only gate remains a boundary case and is not part of this assessment population. The exact frozen archives and blinded materials were delivered to the versioned Drive handoff recorded in `tools/M0015/review-handoff-delivery.json`. The token-to-policy mapping is retained in internal research evidence outside the handoff. Stage C results and their provenance are recorded below; the original 11-item sample remains unlabeled.

Other limitations:

- Two convenience gates do not establish representative semantic-review quality. One gate is documentation-only.
- The original 11-item exploratory sample was prepared from the prior hygiene and documentation-only heads; it remains unchanged. The amended Stage A handoff separately covers 16 candidates from the two source-changing gates and retains the documentation-only gate only as a no-source-change boundary.
- The hygiene gate is an earlier merged change set; the pre-final smoke run and shared-random correction are disclosed above.
- Project-level impact is not computed. Same-document unchanged declarations use a coarse context-affected class; cross-document impact stays unknown.
- A 25-item deterministic unchanged cohort is a bounded experiment choice. It can exclude eligible candidates before policy selection.
- The one-slot minimum does not guarantee exactly 20% unchanged selection; weighted remaining draws can add more. The observed hygiene fallback used two unchanged/unknown slots.
- Histories are available only through typed prototype inputs; there is no actual historical source of reusable per-type BORINGness labels in the current product contract.
- Timings include a preview .NET SDK and four-processor Windows environment. They are not production-gate latency forecasts.
- The prototype's deterministic selection tickets are not valid `hygiene review accept` tokens and do not integrate with production review state.

## Direction for M0016 and decisions still open

M0016 should decide whether to collect another independently frozen source-bearing gate and whether a later-gate history-transfer study is worth the cost. Stage C validated and analyzed the project-owner submission below. The observed sample contains no actionable findings for any policy; this is a small convenience sample, not proof of equal policy quality or general effectiveness. Any further study should lock gates, populations, seed, budget, policy code, grid, and analysis protocol before revealing selections, and report scheduling allocation separately from semantic quality.

## Amended research questions — Stage C evidence and limitations

The 11 October amended protocol adds five questions. The Stage A frame remains a 16-item policy-blinded union-plus-control sample (7 hygiene, 9 Semantic Type Model); Stage C validated the complete user-supplied result file before opening the token map. The file SHA-256 is `2276613E4ACB111C1F0CC4220DE1C9E8A71228E0D1B8F91F29213AD111E2D8BA`; it references manifest SHA-256 `286A0FF45640773E9B5572D66D7FC6E5FF1A5881BB7089D86A3603C38CD6577C`. The declared reviewer provenance identifies a policy-blinded GPT-6 review session completed at `2026-10-12T04:30:17Z`. This provenance is as submitted; implementation validation cannot independently audit the assessor's process.

| Question | Observed evidence and protocol | Inference | Status and remaining evidence |
|---|---|---|---|
| **RQ-01 — Semantic yield** | All 16 items passed schema, provenance, manifest, token, source revision/fingerprint, rubric, and evidence-path validation before unblinding. The assessor returned 16 `accepted`, 0 `negative` (actionable), and 0 `uncertain`; each of Q1–Q5 had zero `yes` answers. Each policy selected 5 items per gate (10 slots across two gates), with overlap. Pairwise shared identity counts (uniform/change-first, uniform/history-aware, change-first/history-aware) were 4/5/4 on hygiene and 2/3/4 on the Semantic Type Model source gate. Every assessed identity selected by any policy was accepted. | Observed actionable yield was 0/10 slots for uniform, 0/10 for change-first, and 0/10 for history-aware. This is an observation about these subjects only; it does not establish equal quality, defect rates, recall, or general effectiveness. | **OBSERVED ZERO YIELD; GENERAL EFFECTIVENESS INCONCLUSIVE.** Small N, two convenience gates, model-authored judgments, and unauditable assessor blinding limit inference. |
| **RQ-02 — Usable history** | Prototype fixtures distinguish current, stale, missing, aggregate-only, synthetic, and uncertain evidence. These judgments were collected after the frozen gates. | Stage C labels cannot be treated as temporally prior predictors for either same gate. Fingerprint checks remain necessary, but no genuine later-gate transfer was tested. | **INSUFFICIENT EVIDENCE** until a genuinely earlier judged gate and a subsequent gate are available. |
| **RQ-03 — Local calibration** | The fixed finite-grid implementation retains independent training/evaluation separation and deterministic fallback. Real gates used fallback because no admissible prior labels existed. | Stage C outcomes are post-gate and cannot tune this gate without leakage. Calibration on selected/labeled candidates estimates yield conditional on historical selection and misses unlabeled candidates; it cannot establish population-wide policy optimization. | **NO REAL-LABEL CALIBRATION.** Keep fallback. This sample does not justify parameter changes. |
| **RQ-04 — Discretionary budget** | At the documentation-only boundary (`semantic-model-head`), the frozen head population had 0 new/changed, 420 unchanged/background, and 0 unknown-impact candidates; all three five-slot policies selected 0 changed and 5 background candidates. Source-changing populations had 15 new/changed and 56 unknown candidates for hygiene, and 4 new/changed and 11 unknown for Semantic Type Model. Change-first allocations were 4/1 and 3/2 changed/background-or-unknown; history-aware allocated 3/2 and 2/3. The selector fixture confirms budget 0 selects 0. Positive background surveillance is modeled only as conditional on explicit owner authorization; no authorization is inferred here. | A budget is a ceiling: zero budget produces zero work; a positive budget can fill with background items when no rule-relevant source candidates are found. The documentation-only result does not show background surveillance has value. | **MECHANICALLY TESTED; AUTHORIZATION AND VALUE UNRESOLVED.** Production should allow zero spend and require explicit surveillance authorization. |
| **RQ-05 — Exploration opportunity cost** | Hygiene: change-first selected 4 changed candidates, history-aware 3; count difference and identity displacement are each 1. It omitted `SummaryQualityReviewRuleModule` and replaced it with `RuleCatalog` at `weighted-fill`. Semantic Type Model source gate: 3 versus 2; count difference and identity displacement are each 1. It omitted `TestDataCorrectiveTests` and replaced it with `JsonSchemaUnsupportedCompositionTransformation`, also at `weighted-fill`. All omitted and replacement candidates were judged accepted. | These two traced swaps had no observed actionable finding on either side. This small outcome observation does not show candidates are generally interchangeable or exploration has no cost. | **OBSERVED TRADEOFF: 0 ACTIONABLE FINDINGS ON DISPLACED/REPLACEMENT ITEMS.** Broader quality remains inconclusive. |

### Stage A handoff and evidence boundary

The amended handoff is published in the immutable versioned folder `G:\My Drive\Transfer\M0015-review\handoff\m0015-stage-a-amended-20261011-v2\`. The submitted judgment file remains in `G:\My Drive\Transfer\M0015-review\submissions\judgments-gpt6-independent-20261012-e2c3bd6ae5f7.json`. A byte-identical frozen copy is retained outside the repository at `C:\src\m0015-evidence\stage-c-private\judgments-gpt6-independent-20261012-e2c3bd6ae5f7.json`; its hash and submitted provenance are recorded in `tools/M0015/results/stage-c-semantic-assessment.json`. Raw submission notes are not committed. That derived artifact records per-token decisions, policy membership, source identity, and reproducible aggregates.

Planning must still decide whether production should expose a gate snapshot/selection API, which history sources are valid and whether per-type evidence may be persisted, how existing hazard state is retained or migrated, and whether/how prototype tickets could map to accepted review tickets. This milestone makes none of those production decisions.

Revalidate if the candidate/rubric contract changes, the target population differs materially, a trusted per-type label source becomes available, the selected exploration fraction changes, or an independent review demonstrates a quality effect. Until then, gate-local sampling is a working scheduling prototype with **inconclusive semantic value**.

## Reproduction

From a checkout with the frozen revisions available and a local clone of `carlrabbit/dotnet-semantic-type-model`:

```powershell
.\eng\M0015.ps1 -Action test
.\eng\M0015.ps1 -Action replay -SecondRepositoryPath <clone-path> -EvidenceRoot <evidence-path>
.\eng\M0015.ps1 -Action integrity
.\eng\M0015-review-handoff.ps1 -Action validate-submission -SubmissionPath <judgments-file>
```

The replay command resolves the checked-in gate manifest, materializes detached Git worktrees, enumerates both source snapshots with Roslyn, writes the frozen counts/diff manifest before the paired comparisons, and emits the reproducible candidate/result artifacts. Integrity validation compares against the committed SHA-256 list without rewriting it; use `-Action regenerate-integrity` only after an intentional evidence update, then rerun read-only `-Action integrity`.
