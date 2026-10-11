# M0015 — Gate-Local Semantic Sampling Prototype

**Status:** non-production experiment; semantic usefulness inconclusive. This report is research evidence, not production authority.

## Question and recommendation

At one concrete source-change gate, can a deterministic fixed-budget selector allocate BORINGness reviews more usefully than uniform or change-first selection, using individual historical review evidence and bounded local calibration where available?

**Recommendation: INCONCLUSIVE on adoption; refine the evidence before planning a production migration.** The prototype enumerated real eligible types and reproduced same-gate allocations at a fixed budget. The correction run adds a source-changing gate, but no independent semantic judgments were collected, so neither the selection policies' review quality nor time savings is measured. A blinded assessment is now ready for the project owner; M0016 should use those judgments to decide whether the evidence supports further planning and whether another source-changing gate is needed.

> **Correction status:** The original M0015 observations below are preserved as exploratory evidence. The original comparison JSONs are archived as `tools/M0015/results/exploratory-original-*-comparison.json`. The original displacement field measured theoretical capacity shortfall and was mislabeled as checks displaced; corrected results are in the current `*-comparison.json` files and the frozen three-gate manifest. Use the correction section below for displacement interpretation. The recommendation remains INCONCLUSIVE because no real semantic judgments exist. A separate Stage A user-run assessment package is now frozen and delivered; it adds candidates from both source-changing gates and does not alter or replace the original 11-item exploratory sample.

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

## Amended research questions — pending independent evidence (11 October 2026)

PR #16 has a **Stage A ready M0015 user-run-judgment amendment**, but the RQ-01–RQ-05 findings have **not yet been produced**. Luna prepares the pinned public repository ZIPs and blind packet in the user-accessible Google Drive Transfer folder, then stops; it may only ingest the project owner's submitted assessments on a separate explicit resumed run. No automated delegation or Luna-performed review judgments are allowed. The original three-gate scheduling experiment above is preserved and exploratory, not evidence of comparative semantic-review value. The authoritative amendment, frozen-assessment requirements, and per-question acceptance criteria are in `docs/milestones/M0015-gate-local-sampling-prototype.md`; the implementation-owned status is in `.execution/M0015-gate-local-sampling-prototype.md`.

- **RQ-01 — Actual semantic yield:** Requires separate, blinded independent Q1–Q5 decisions for selected-union candidates plus independently sampled controls on the two source-changing gates, locked before policy unblinding.
- **RQ-02 — Reusable history:** Requires real prior-gate type labels that match identity and source/rule fingerprint on a later gate; missing chronological evidence must remain an explicit gap.
- **RQ-03 — Locally calibrated parameters:** Must separate test-only grid behavior from real valid-label calibration, address selection-conditioned labels, and retain fallback without unsupported claims.
- **RQ-04 — Review budget as a ceiling:** Compare a no-rule-change gate yielding zero expensive reviews against explicitly justified background selection; do not interpret a budget of five as a mandate to spend five.
- **RQ-05 — Exploration tradeoffs:** Attribute actual changed IDs displaced and compare review outcomes only when independently judged; preserve the difference between reserved exploration and weighted-fill selection.

Stage A is complete only when the two exact-head clean public repository archives, blind prompt/manifest/template and checksum receipt are copied and verified at `G:\My Drive\Transfer\M0015-review\handoff\`. Stage B is project-owner judgment collection in `M0015-review/submissions/`, not Luna orchestration. Stage C validates submitted results before policy unblinding and appends an evidence/provenance/uncertainty/result subsection for **each** RQ. No new actual judgments, cost savings, parameter improvements, or product policy changes are claimed by this planning amendment.

## Semantic assessment and limitations

No authorized independent semantic reviewer was available in the execution environment, and no actual reviewer judgments were provided. `tools/M0015/reviewer-sample.json` preserves the original 11-candidate exploratory sample (six from the hygiene gate, five from the semantic-type-model gate) with policy labels omitted. The separately frozen Stage A package at `tools/M0015/review-handoff/` covers 24 candidates from both source-changing gates, including five independently seeded controls per gate; the documentation-only gate remains a boundary case and is not part of this assessment population. Its exact frozen archives and blinded materials were delivered to `G:\My Drive\Transfer\M0015-review\handoff\`. The token-to-policy mapping remains outside both the repository and handoff. No candidate is marked accepted, negative, or uncertain in this report.

Other limitations:

- Two convenience gates do not establish representative semantic-review quality. One gate is documentation-only.
- The original 11-item exploratory sample was prepared from the prior hygiene and documentation-only heads; it remains unchanged. The Stage A handoff separately covers 24 candidates from the two source-changing gates and retains the documentation-only gate only as a no-source-change boundary.
- The hygiene gate is an earlier merged change set; the pre-final smoke run and shared-random correction are disclosed above.
- Project-level impact is not computed. Same-document unchanged declarations use a coarse context-affected class; cross-document impact stays unknown.
- A 25-item deterministic unchanged cohort is a bounded experiment choice. It can exclude eligible candidates before policy selection.
- The one-slot minimum does not guarantee exactly 20% unchanged selection; weighted remaining draws can add more. The observed hygiene fallback used two unchanged/unknown slots.
- Histories are available only through typed prototype inputs; there is no actual historical source of reusable per-type BORINGness labels in the current product contract.
- Timings include a preview .NET SDK and four-processor Windows environment. They are not production-gate latency forecasts.
- The prototype's deterministic selection tickets are not valid `hygiene review accept` tokens and do not integrate with production review state.

## Direction for M0016 and decisions still open

M0016 should obtain and validate the project owner's Stage A judgments before drawing any quality conclusion, then decide whether another independently frozen source-bearing gate is required. This correction adds one source-changing gate but does not address selection-conditioned evidence or establish review effectiveness. Stage C validation, unblinding, and analysis has not run and requires a separate explicit request. Any further study should lock gates, populations, seed, budget, policy code, grid, and analysis protocol before revealing selections, and report scheduling allocation separately from semantic quality.

Planning must still decide whether production should expose a gate snapshot/selection API, which history sources are valid and whether per-type evidence may be persisted, how existing hazard state is retained or migrated, and whether/how prototype tickets could map to accepted review tickets. This milestone makes none of those production decisions.

Revalidate if the candidate/rubric contract changes, the target population differs materially, a trusted per-type label source becomes available, the selected exploration fraction changes, or an independent review demonstrates a quality effect. Until then, gate-local sampling is a working scheduling prototype with **inconclusive semantic value**.

## Reproduction

From a checkout with the frozen revisions available and a local clone of `carlrabbit/dotnet-semantic-type-model`:

```powershell
.\eng\M0015.ps1 -Action test
.\eng\M0015.ps1 -Action replay -SecondRepositoryPath <clone-path> -EvidenceRoot <evidence-path>
.\eng\M0015.ps1 -Action integrity
```

The replay command resolves the checked-in gate manifest, materializes detached Git worktrees, enumerates both source snapshots with Roslyn, writes the frozen counts/diff manifest before the paired comparisons, and emits the reproducible candidate/result artifacts. Integrity validation checks candidate counts, frozen gate IDs/seeds/budgets, unique selection identities and their membership in the head population, then regenerates the SHA-256 list.
