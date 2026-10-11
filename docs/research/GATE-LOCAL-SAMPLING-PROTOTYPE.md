# M0015 — Gate-Local Semantic Sampling Prototype

**Status:** non-production experiment; semantic usefulness inconclusive. This report is research evidence, not production authority.

## Question and recommendation

At one concrete source-change gate, can a deterministic fixed-budget selector allocate BORINGness reviews more usefully than uniform or change-first selection, using individual historical review evidence and bounded local calibration where available?

**Recommendation: INCONCLUSIVE on adoption; refine the evidence before planning a production migration.** The prototype enumerated real eligible types and reproduced same-gate allocations at a fixed budget. The history-aware fallback made one or two unchanged slots available and displaced two changed checks at the source-bearing gate. No independent semantic judgments were collected, so neither the selection policies' review quality nor time savings is measured. M0016 should decide whether to obtain a blinded assessment of the ready sample and add a separately pre-registered source-bearing gate before deciding any production redesign.

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

Each policy selected five unique candidate identities per gate. On the source-bearing hygiene gate, uniform selected 3 changed and 2 unchanged/unknown; change-first selected 4 changed and 1 unchanged/unknown; history-aware selected 3 changed and 2 unchanged/unknown. The history-aware exploration displaced two of the five changed checks that could otherwise fit the budget. This is opportunity cost, not evidence that the displaced checks were valuable or defective.

On that gate, selected-set overlap was 4/5 between uniform/change-first (Jaccard 0.667), 5/5 between uniform/history-aware (1.0), and 4/5 between change-first/history-aware (0.667). On the documentation-only gate all three policies selected the same five identities (Jaccard 1.0 for each pair). Full selected identities and reasons are in the comparison JSON. A second run of the final shared-random protocol reproduced all selected identities exactly.

All real candidates had unknown individual review history. Consequently the history-aware policy ran only as a change-plus-exploration fallback, not as an evidence-learned policy. The comparison provides no quality yield, correctness improvement, or independently verified savings metric.

## Runtime and artifact footprint

Measurements are local single-process timings, not a cross-machine benchmark. “Cold” is a new process/workspace invocation; the operating system file cache may have been warm. Selection and no-label fallback calibration were each run a second time for warm timing.

| Gate | Base/head Roslyn enumeration (cold) | Selection cold/warm | Fallback calibration cold/warm | Compare process working set | Managed heap snapshot |
|---|---:|---:|---:|---:|---:|
| Hygiene | 5,616.5 / 6,545.2 ms | 5.1 / 0.3 ms | 2.3 / 0.02 ms | 31.7 MB | 2.0 MB |
| Semantic type model | 17,391.0 / 17,729.3 ms | 7.5 / 0.3 ms | 3.7 / 0.02 ms | 34.6 MB | 4.2 MB |

The finite calibration search did not run against either gate because no real labels were available; the measured calibration values are the fallback-path cost only. This is well under the five-minute bound but does not establish the cost of a future labeled search on a large corpus. No overrun occurred.

The six public base/head candidate inventories and two comparisons occupy 587,203 bytes total. `FrozenGateEvidence.json` is 5,084 bytes and the 11-item reviewer sample is 5,508 bytes. Inventories include IDs, relative project/path/symbol, classification, declaration fingerprint, and workspace diagnostics; they contain no source text. The integrity command writes SHA-256 checksums for the frozen gate manifest, gate protocol, research report, blinded sample, and all JSON result files.

## Semantic assessment and limitations

No authorized independent semantic reviewer was available in the execution environment, and no actual reviewer judgments were provided. `tools/M0015/reviewer-sample.json` contains 11 candidates (six from the hygiene gate, five from the semantic-type-model gate) with policy labels omitted. A separate mapping key remains outside the repository. The sample is ready for a blinded review using the fixed Q1–Q5 rubric; no candidate is marked accepted, negative, or uncertain in this report.

Other limitations:

- Two convenience gates do not establish representative semantic-review quality. One gate is documentation-only.
- The hygiene gate is an earlier merged change set; the pre-final smoke run and shared-random correction are disclosed above.
- Project-level impact is not computed. Same-document unchanged declarations use a coarse context-affected class; cross-document impact stays unknown.
- A 25-item deterministic unchanged cohort is a bounded experiment choice. It can exclude eligible candidates before policy selection.
- The one-slot minimum does not guarantee exactly 20% unchanged selection; weighted remaining draws can add more. The observed hygiene fallback used two unchanged/unknown slots.
- Histories are available only through typed prototype inputs; there is no actual historical source of reusable per-type BORINGness labels in the current product contract.
- Timings include a preview .NET SDK and four-processor Windows environment. They are not production-gate latency forecasts.
- The prototype's deterministic selection tickets are not valid `hygiene review accept` tokens and do not integrate with production review state.

## Direction for M0016 and decisions still open

M0016 should decide whether the project owner/delegate will assess the blinded sample and whether a fresh, independently pre-registered source-bearing gate is required. If further study proceeds, record real reviewer provenance and lock gates, counts, seed, budget, policy code, grid, and analysis before revealing selections. Continue to report scheduling yield separately from semantic quality.

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
