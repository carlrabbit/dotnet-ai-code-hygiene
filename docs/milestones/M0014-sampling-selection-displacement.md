# M0014 — Sampling Selection Displacement Analysis

**State:** planned (not executed; readiness verification required)
**Mode:** AI-executed, human-reviewed; investigation/evidence milestone
**Depends on:** M0013 mechanical replay evidence and M0012 implementation; does not require M0013's semantic-effectiveness gate to be waived

## Goal

Determine whether M0012 project-activity age (policy **B**) changes **which** BORINGness review candidates are selected, relative to fingerprint/change-only sampling (**C**), under identical review budgets. Measure identity overlap, displacement and discovery advancement before proposing additional semantic-labeling work. Extra due events or equal total selected-ticket counts do not establish either equivalence or usefulness.

This is an **analysis of previously observed selection behavior**, not a new production feature or a retrospective rewrite of M0013's frozen protocol.

## Evidence inputs and execution scope

- Read the M0013 report, public corpus/replay/results manifests, completed execution ledger, and replay tooling. Use the corrected Private `upstream` segment (120 source checkpoints); the previous five-checkpoint Private `main` segment is **superseded**, not pooled with it.
- Compare policies B and C **within the same repository, fixed seed, observation arm, chronological checkpoint and rule**. Keep accept-all and no-observation scenarios separate. A (elapsed-time) is optional contextual reference, not required for the core B:C decision.
- Prefer retained per-checkpoint candidate selections from the M0013 restricted raw replay evidence; verify that identity, revision, seed, arm, and source provenance are complete before analysis. If identities are missing, perform only the minimum necessary reproducible candidate-level replay, reusing frozen source revisions/seeds and scripts. Do not repeat the full four-repository replay without a demonstrated gap.
- The existing public aggregate JSON cannot establish selection overlap. Never reconstruct or infer candidate identity from selected-count totals.
- Private candidate identities, source paths, revision identifiers, code excerpts, labels and unredacted selection traces **remain only in authorized private evidence storage**. Publish only privacy-safe aggregate values for Private.
- No production sampler changes, rule-policy tuning, policy/CLI/version changes, or retroactive edits to M0013's result protocol. Temporary/reproducible analysis scripts are allowed.

## Definitions and comparison rules

A **selection occurrence** is a (repository, rule, checkpoint, seed, observation arm, selected review unit/candidate identity) tuple. Identity comparisons must use the actual sampled target identity at that revision, not simply a file path or a count. Distinguish aggregate BORINGness **population-unit** selection from its **sampled type candidate**; report both where necessary to interpret displacement.

For every paired checkpoint/seed/arm:

1. **Overlap:** `|B ∩ C|`, `|B only|`, `|C only|`, union size, and Jaccard overlap. Include identical and empty selection sets rather than silently omitting them.
2. **Selection displacement:** show candidates selected by one policy but not the other at the **same checkpoint and budget**, with aggregate proportions and counts. Count repeated selections separately from unique candidates; identify whether the same selection is merely shifted to a different checkpoint.
3. **First selection / advancement:** for a candidate with a stable cross-checkpoint identity, compare the first checkpoint at which each policy selects it. Record B-earlier, C-earlier, same-checkpoint, and only-one-policy cases. Report differences in **source checkpoints**, not elapsed days. Do not treat renamed/moved/structurally reidentified candidates as equivalent without a documented deterministic mapping.
4. **Budget saturation:** record due-event counts separately from selected-ticket counts, actual per-checkpoint selection caps, and the percentage of checkpoint/seed pairs at the cap. Never equate due events with completed reviews or actionable findings.
5. **Seed and repository dispersion:** show per-seed and per-repository distributions as well as aggregate summaries; do not let larger histories obscure short or low-population histories.

Selection occurs in both synthetic arms, but their histories differ. Therefore compare B versus C **within** each arm; do not combine arms into a single imagined review policy. If the replay records multiple selections of one candidate across generations, preserve generation semantics rather than deduplicating indiscriminately.

No semantic quality labels are available from M0013. This milestone may generate a **policy-blinded, bounded proposal** for later assessment, preferentially sampling B-only/C-only pairs and including matching selections as controls; it does **not** require or invent semantic correctness judgments. Any assessment of real effectiveness requires a separate reviewer-supported gate or future milestone.

## Stable acceptance obligations

| ID | Independently verifiable outcome |
| --- | --- |
| AC-01 | Verify frozen/recovered input identity, completeness and comparability across repositories, seeds, checkpoints and arms; record gaps and assumptions before metrics. |
| AC-02 | Produce deterministic per-checkpoint B:C overlap and displacement metrics, separating due, selected units, sampled candidates and repeated occurrences. |
| AC-03 | Produce stable-identity first-selection timing / advancement metrics and explicitly quantify unmappable or one-policy-only candidates. |
| AC-04 | Quantify batch-cap saturation and per-seed/per-repository variability without conflating synthetic observations with real reviews. |
| AC-05 | Demonstrate independent repeatability of candidate-level metrics and traceability to retained source/replay evidence; distinguish aggregate-only checks from identity checks. |
| AC-06 | Maintain public/private evidence separation, performing an explicit public-artifact disclosure inspection. |
| AC-07 | Publish a durable, evidence-bounded report and permitted reproducibility artifacts with an explicit decision on whether blinded semantic assessment is justified. |
| AC-08 | Preserve M0013's frozen findings, production policy, and human-review boundaries; reconcile milestone-scoped completion evidence and human-review gate. |

## Distinct required evidence cases

| ID | Parent | Evidence case |
| --- | --- | --- |
| EC-01a | AC-01 | Public historical candidate-identity evidence, or documented reconstruction path |
| EC-01b | AC-01 | Corrected Private upstream segment, privately traceable and publicly aggregate-only |
| EC-02a | AC-02 | Pairwise overlap/displacement for empty, partially overlapping and identical selection sets |
| EC-02b | AC-02 | BORINGness population units versus sampled types; repeated occurrences versus unique candidates |
| EC-03a | AC-03 | Stable candidate selected by both policies at different checkpoints |
| EC-03b | AC-03 | Candidate selected by only one policy or identity not safely trackable |
| EC-04a | AC-04 | Accept-all synthetic arm |
| EC-04b | AC-04 | No-observation synthetic arm |
| EC-05a | AC-05 | Independent rerun produces identical candidate-identity metric hashes |
| EC-05b | AC-05 | Incomplete raw identity evidence is detected rather than replaced by count equality |
| EC-06a | AC-06 | Private raw material kept in authorized local store |
| EC-06b | AC-06 | Explicit public output/privacy inspection |
| EC-07a | AC-07 | Measured selection findings and limitations |
| EC-07b | AC-07 | Bounded proposed blinded assessment pool or justified conclusion that it is unnecessary/unavailable |

## Validation gates

| Gate | Target / locus | Obligations and evidence |
| --- | --- | --- |
| VAL-01 | Deterministic replay-identity fixture and real retained candidate traces on the M0013 recorded environment (Windows 11, PowerShell, .NET 11); verify available tooling during readiness | AC-01–AC-03; EC-01a/b, EC-02a/b, EC-03a/b |
| VAL-02 | Paired arm/seed/corpus aggregate analysis and bounded second-run comparison | AC-04–AC-05; EC-04a/b, EC-05a/b |
| VAL-03 | Public-artifact inspection and permitted private evidence attestation | AC-06; EC-06a/b |
| VAL-04 | Written research review and milestone-scoped human decision | AC-07–AC-08; EC-07a/b |

## Deliverables

- `docs/research/ACTIVITY-AGE-SELECTION-COMPARISON.md`: question, frozen inputs and provenance, observed results, separate synthetic arms, overlap/displacement/advancement metrics, variability, saturation, uncertainty, selection-identity limitations, conclusions, and revalidation triggers.
- A compact public derived metrics JSON and reproducible script/commands, with no Private candidate/revision/source identifiers.
- An optional blinded **assessment candidate manifest** in authorized locations, only if candidate-level differences justify a quality study. Its presence does not count as actual judgments.
- `.execution/M0014-sampling-selection-displacement.md`: planner-seeded AC/EC/VAL coverage, implementation-owned work mapping, evidence and final reconciliation.
- Milestone-specific human review request; do not invent approval or bypass an unavailable reviewer.

## Readiness and completion boundary

Before execution, verify the M0013 retained raw evidence is accessible, state the precise candidate identity and cross-checkpoint mapping policy, establish reproducibility command/target, input freeze, private evidence location and human review gate. If candidate identities were not retained or comparable, document the minimal replay requirement and return material changes to planning. **Do not inspect B:C selection outcome metrics before fixing analytical choices.**

Successful execution may find no meaningful displacement, significant displacement, or insufficient evidence. All are legitimate findings when supported. This milestone **cannot conclude semantic usefulness without actual labels** and cannot alter M0012 production policy. Complete only after lossless AC/EC/VAL reconciliation and the required human decision; otherwise report `AWAITING HUMAN REVIEW` or `BLOCKED` truthfully.
