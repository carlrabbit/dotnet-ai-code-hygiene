# Execution Ledger — M0014

**Primary milestone:** `docs/milestones/M0014-sampling-selection-displacement.md`  
**State:** awaiting human review; execution and evidence reconciliation complete.

This is operational progress/evidence routing, not a substitute for the milestone contract. Do not collapse acceptance obligations or evidence cases.

## Readiness prerequisites

- [x] Verify retained M0013 candidate identities, discover the missing separate population-unit identity, and verify the corrected Private upstream segment provenance.
- [x] Freeze identity matching, cross-checkpoint comparison, synthetic arms, seed/corpus selection, and missingness conventions before outcome inspection.
- [x] Establish validation commands, execution target/locus, private storage, and milestone-scoped reviewer/review path.
- [x] Confirm that raw trace reconstruction (if needed) does not require changes to M0013 scientific results or product policy.

### Frozen readiness decisions (before outcome inspection)

- Inputs: all three public M0013 `main` corpora from their retained local raw evidence (77, 11, and 45 checkpoints) and the corrected Private `upstream` corpus (120 checkpoints); five fixed seeds; both `all` and `none` arms. Superseded Private `main` is excluded. Public refs remain the locally frozen M0013 refs; no claim of current remote freshness.
- Retained identity completeness: public local raw trace inventory has 133 checkpoint JSON files with 30 variants each; Private restricted local evidence has 120 files with 30 variants each. Committed M0013 summaries lack candidate identities and are not used to reconstruct them. Schema inspection found that M0013 `SelectedIds` and `SelectedCandidates.Identity` do not separately encode the BORINGness `PopulationTicket.UnitId`; the initial ledger interpretation of `SelectedIds` as population units was corrected before final unit-level metric production.
- Pair key: repository ID, chronological checkpoint index, seed ID, observation arm, and rule ID. Within a pair, compare exact `Identity` values emitted by the M0013 runner. No path/name-based rename or structural matching is allowed. A one-sided identity is reported as unmatched; changed IDs are not treated as equivalent.
- Counting: report paired occurrence overlap including empty and identical sets; report unique identity union separately. `SelectedCandidates.Identity` supplies sampled type identities. Population-unit identity is read from the existing `PopulationTicket.UnitId`, recorded by the replay helper in a diagnostic-only field; it is never inferred from `SelectedIds` or counts. Due counts, selected units, and sampled types remain distinct. Repeated selections across checkpoints remain occurrences.
- First-selection: for IDs observed in eligible populations, compare first selected checkpoint per policy, arm, seed, and rule. IDs that change or disappear without exact identity match are unmappable, not inferred. Counts are source checkpoint offsets; no elapsed-time claim.
- Repeatability: a first M0014 B/C-only replay preserved selected candidate identities. A second B/C-only replay added the diagnostic `PopulationUnitId` field. The two M0014 passes match across candidate-identity rows; the unit-ID field itself occurs only in the second run and therefore has a completeness check, not a two-run equality claim. Neither run overwrote M0013 artifacts or changed production behavior.
- Execution: Windows 11 build 26200, PowerShell, .NET SDK `11.0.100-rc.1.26425.128`; canonical helper build via `eng/M0013.ps1 -Action build-replay`; paired analysis via committed `tools/M0014/Analyze-Displacement.ps1`; identity repeat via committed `tools/M0014/Replay-Identity-Corpus.ps1`. Public artifacts go to the repository; Private raw/repeat traces remain in user-restricted `%LOCALAPPDATA%` evidence storage.
- Human gate: `.review/REV-M0014-COMPLETION.md` requests review of the finished report/evidence and records a pending project-owner/delegate decision. No approval is presumed.

## Acceptance obligations

| ID | Exact obligation scope | Work package | Evidence | Validation gate | Status |
| --- | --- | --- | --- | --- | --- |
| AC-01 | Frozen input identity/completeness and B:C comparability | WP-01 | 77/11/45 public plus 120 Private upstream checkpoints; exact revision pairing checked; all expected 30 retained and 20 repeat variants per checkpoint validated | VAL-01 | complete with Private historical discrepancy disclosed |
| AC-02 | Candidate-level checkpoint overlap and displacement | WP-02 | Public per-checkpoint overlap/displacement rows in `ACTIVITY-AGE-SELECTION-CHECKPOINTS.jsonl`; Private aggregate distributions only in metrics JSON; fixture covers empty/partial/identical pairs; unit/type rows separate | VAL-01 | complete |
| AC-03 | Stable-identity first-selection/advancement | WP-02 | Exact identity first-selection counts/offsets; one-policy-only identities separate; no rename mapping | VAL-01 | complete with identity limitations |
| AC-04 | Cap saturation and seed/repository variability | WP-02 | Both arms and five seeds retained separately; due/selected counts and saturation denominators in derived metrics | VAL-02 | complete |
| AC-05 | Candidate-level repeatability and traceability | WP-03 | Two M0014 identity passes match 15,080/15,080 candidate rows; retained Private M0013 differs in 105/7,200 rows; unit-ID completeness validated, independent unit-ID equality not measured | VAL-02 | complete with disclosed limitations |
| AC-06 | Private/public evidence separation | WP-04 | Private raw remains in restricted local evidence; aggregate-only Private fields; staged/public disclosure scan recorded below | VAL-03 | complete |
| AC-07 | Evidence-bounded research report and next-study decision | WP-05 | `docs/research/ACTIVITY-AGE-SELECTION-COMPARISON.md` recommends a bounded blinded assessment; no judgments fabricated | VAL-04 | complete |
| AC-08 | No retroactive M0013 or product policy changes; closure gate | WP-05 | M0013 frozen results unchanged; only replay diagnostic DTO gained existing unit ID; review request submitted, decision pending | VAL-04 | awaiting human review |

## Required evidence cases

| ID | Parent | Distinct evidence | Work package | Evidence | Gate | Status |
| --- | --- | --- | --- | --- | --- | --- |
| EC-01a | AC-01 | Public identities or reconstruction | WP-01 | 133 public retained/repeat checkpoints; exact ranges in M0013 corpus manifest and M0014 report | VAL-01 | complete |
| EC-01b | AC-01 | Private upstream provenance | WP-01 | 120 corrected upstream checkpoints verified against frozen Private manifest; exact IDs withheld; M0013-to-M0014 mismatch disclosed | VAL-01 | complete with limitation |
| EC-02a | AC-02 | Empty/partial/identical overlaps | WP-02 | Deterministic fixture asserts Jaccard/displacement including empty pairs; public JSONL retains per-checkpoint rows | VAL-01 | complete |
| EC-02b | AC-02 | Unit/type and repeat distinction | WP-02 | PopulationUnitId coverage plus separate sampled-type metrics; repeated occurrences and unique identities are distinct | VAL-01 | complete with unit-ID repeat limitation |
| EC-03a | AC-03 | First-selection offsets | WP-02 | Source-checkpoint offsets per identity/arm/seed; seed distributions in report/JSON | VAL-01 | complete |
| EC-03b | AC-03 | Only-one/unmappable | WP-02 | B-only/C-only exact identities counted; no path-based mapping introduced | VAL-01 | complete |
| EC-04a | AC-04 | Accept-all arm | WP-02 | Separate `all` rows for due, selection, identity overlap, and caps | VAL-02 | complete |
| EC-04b | AC-04 | No-observation arm | WP-02 | Separate `none` rows; due workload is not represented as completed reviews | VAL-02 | complete |
| EC-05a | AC-05 | Independent metric identity hashes | WP-03 | Public retained/repeat hashes agree; M0014 repeats match 15,080 candidate rows; Private hashes omitted and aggregate comparison disclosed | VAL-02 | complete with limitation |
| EC-05b | AC-05 | Missing identity evidence detected | WP-03 | `Validate-Trace.ps1` requires every expected policy/arm/seed variant; fixture removes a variant and asserts rejection; analyzer rejects missing population-unit IDs | VAL-02 | complete |
| EC-06a | AC-06 | Restricted private evidence | WP-04 | Raw and identity-bearing Private data remain in user-restricted local evidence store; only seed-distribution aggregates enter report/JSON | VAL-03 | complete |
| EC-06b | AC-06 | Public disclosure scan | WP-04 | Public deliverable scan found no Private SHA, candidate identity, source path, or raw trace | VAL-03 | complete |
| EC-07a | AC-07 | Results and limitations | WP-05 | Research report contains paired results, cost, limits, unresolved discrepancy, and bounded recommendation | VAL-04 | complete |
| EC-07b | AC-07 | Assessment-pool decision | WP-05 | Public equal-budget blinded assessment proposed at 25 per policy/repository; Private deferred until mismatch reconciliation; no pool or judgments invented | VAL-04 | complete |

## Validation gates

| ID | Required target | Coverage | Evidence | Status |
| --- | --- | --- | --- | --- |
| VAL-01 | Deterministic fixture and identity-bearing traces | AC-01 AC-02 AC-03 EC-01a EC-01b EC-02a EC-02b EC-03a EC-03b | Fixture passed; complete frozen checkpoint/variant sets and same-source revisions audited; population-unit IDs complete | passed |
| VAL-02 | Paired seeded arm analysis and independent comparison | AC-04 AC-05 EC-04a EC-04b EC-05a EC-05b | Analyzer completed; public M0013 matches 7,880/7,880; M0014 repeats match 15,080/15,080 candidate rows; Private retained-M0013 discrepancy 105/7,200 disclosed | passed with limitation |
| VAL-03 | Private evidence handling and public output inspection | AC-06 EC-06a EC-06b | Restricted raw storage retained; only aggregate Private values published; disclosure scan recorded under WP-04 | passed |
| VAL-04 | Research report and milestone-scoped human review | AC-07 AC-08 EC-07a EC-07b | Report and `.review/REV-M0014-COMPLETION.md` prepared; human decision is pending | awaiting human review |

## Work packages and execution evidence

| ID | Scope | Status | Evidence |
| --- | --- | --- | --- |
| WP-01 | Reconcile frozen public and Private-upstream identity inputs and readiness assumptions | complete with disclosed schema correction | M0013 corpus manifest, exact public ranges, 120 Private upstream checkpoints; M0013 selected IDs are not treated as population-unit IDs |
| WP-02 | Produce deterministic paired overlap, displacement, discovery timing, and cap metrics | complete | `tools/M0014/Analyze-Displacement.ps1`; `tools/M0014/Validate-Trace.ps1`; public per-checkpoint rows in `docs/research/ACTIVITY-AGE-SELECTION-CHECKPOINTS.jsonl`; Private aggregate rows in `ACTIVITY-AGE-SELECTION-METRICS.json`; deterministic fixture |
| WP-03 | Recover population-unit IDs, repeat candidate traces, validate completeness | complete with limitations | Two B/C-only runs match candidate identity rows; second run reports complete unit IDs; Private retained-M0013 mismatch (105/7,200 rows) remains unresolved; no independent unit-ID repeat |
| WP-04 | Keep Private traces restricted and inspect publication surface | complete | Raw identity traces remain under restricted `%LOCALAPPDATA%`; report and aggregate JSON omit private candidate IDs, SHAs, paths, seeds, and trace files |
| WP-05 | Write evidence-bounded report, complete audit, and request human review | awaiting human review | `docs/research/ACTIVITY-AGE-SELECTION-COMPARISON.md`; `.review/REV-M0014-COMPLETION.md`; no reviewer decision recorded |

## Execution record

- `.\eng\M0013.ps1 -Action build-replay` — succeeded with .NET SDK `11.0.100-rc.1.26425.128`; 0 warnings, 0 errors (SDK preview informational notice).
- `.\tools\M0014\Test-DisplacementFixture.ps1` — passed empty, partial, identical, unit/type, advancement, one-policy, cap, and incomplete-trace cases.
- PowerShell AST parser passed for all M0014 scripts, including `Validate-Trace.ps1`.
- First identity pass: 133 public checkpoints in 102.39 minutes; Private upstream 120 checkpoints in approximately 119.70 minutes. Second pass with `PopulationUnitId`: public 133 in 123.05 minutes; Private 120 in 139.42 minutes. Each checkpoint had 20 B/C × arm × seed variants. The analyzer verifies 30 retained M0013 variants and 20 repeat variants at every checkpoint, exact source revision pairing, and nonmissing population-unit IDs.
- Two M0014 candidate-identity traces match 15,080/15,080 rows. Public retained M0013 identities match the M0014 trace 7,880/7,880 rows. Private retained M0013 to M0014 differs in 105/7,200 rows across 25 checkpoints; both M0014 repeats match each other. Exact mismatch identities remain local and the discrepancy is unresolved.
- Second-pass raw JSON storage: 53,717,863 public bytes and 70,749,868 Private bytes. Total runtime for all four repository-segment runs was 484.56 minutes. Raw outputs are not committed.
- M0013 report, ledgers, and frozen result artifacts are unchanged. Production sampling algorithms, rule policies, and CLI are unchanged; the replay helper only emits the existing `PopulationTicket.UnitId` in its diagnostic candidate DTO.
- Canonical metric command: `.\tools\M0014\Analyze-Displacement.ps1` — final run completed; JSON records 120/77/11/45 checkpoints, 30/20 variant completeness, exact source pairing, 0 missing unit IDs, and repeat comparisons; public JSONL contains 5,270 per-checkpoint rows.
- Privacy inspection: inspected the report, aggregate JSON, public checkpoint JSONL, ledger, request, milestone index, and tracked M0014/helper tools. Comparison against all Private manifest revision SHAs returned 0 matches; Private rows have no seed labels or non-null hashes; the public JSONL has 0 Private rows; no tracked raw files exist; Private metrics contain only aggregate distributions and counts. Raw trace files are not staged.

## Completion reconciliation

Audit performed after execution by rereading the M0014 milestone and comparing each AC-01..08, EC-01a/b..07a/b, and VAL-01..04 row against concrete evidence above. The fixture, complete-variant validator, real trace analyzer, repeat comparison, report, and privacy scan are separate evidence loci. The M0013 Private-to-M0014 105-row discrepancy and lack of an independent second unit-ID schema run are preserved as limitations. `.review/REV-M0014-COMPLETION.md` requests project-owner/delegate review and records no decision. State: **AWAITING HUMAN REVIEW**, not complete.
