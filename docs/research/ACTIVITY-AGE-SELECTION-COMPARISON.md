# M0014 — Activity-Age Selection Displacement

**Execution status:** analysis and evidence package prepared; awaiting human review.

**Decision:** candidate displacement is substantial enough to warrant a bounded blinded semantic assessment. This replay does not show that either policy produces better reviews.

## Question and scope

This analysis compares M0012 activity-age policy **B** with change-only policy **C** at the same repository, source checkpoint, seed, synthetic observation arm, and rule. It measures which BORINGness population units and sampled type candidates each policy selects. It keeps population-unit identity distinct from sampled type identity and counts repeated selections as occurrences. Matching selection totals alone are not treated as identity equivalence.

It is a scheduler analysis. The `all` arm synthetically observes every selected ticket; the `none` arm observes no tickets. These arms are reported separately and are not actual reviews. No semantic labels, correctness judgments, or quality yields were collected.

## Frozen inputs and method

The analysis reuses the M0013 fixed corpus and its five SHA-256-derived seeds per repository (`SHA256(UTF8("M0013-v1|<stable-corpus-id>|seed-01..05"))`). The same seed is paired across B and C. It uses the frozen first-parent source checkpoints and the existing M0012 sampler, populations, rule versions, fixed policies, and batch caps. It does not tune parameters or change production sampling.

| Repository | Frozen history | Source checkpoints |
|---|---|---:|
| `carlrabbit/Private` | `upstream`; exact revision identifiers withheld | 120 |
| `carlrabbit/dotnet-semantic-type-model` | `d95c46079e79155e7b2157e51473f98dab97a514` .. `7997bcdf2ba36388e74e3e6d069b12414715383a` | 77 |
| `carlrabbit/dotnet-ai-code-hygiene` | `5caa1c802098dd8ffa5876a344242be7b2e30fc8` .. `92825010a5afce11646857b2b20e1e0f5ec20174` | 11 |
| `carlrabbit/dotnet-ai-first-2d-game-engine` | `b5130fccc40a6666d898ab269c60c004b6cfd8b9` .. `972b7c47b0215311bbd6b1cf57841bd8d21b2800` | 45 |

The public refs were locally cached M0013 `origin/main` snapshots; their remote freshness was not verified. Private's corrected `upstream` segment was fetched and verified against its local tracking ref at the M0013 freeze. Private SHA values, source paths, candidate identities, and raw traces remain in restricted local storage.

At each checkpoint, pairs are keyed by repository, chronological checkpoint index, seed, arm, and rule. Selection identities are compared exactly as emitted; no path or rename mapping is inferred. Empty and identical pairs remain in the calculation. Jaccard is computed per paired checkpoint and averaged across that seed's paired checkpoints; tables report the median across five seeds and the seed minimum/maximum. The JSONL sidecar preserves public per-checkpoint overlap, displacement, due, selected, and cap rows without candidate identities; Private is published as aggregate distributions only. Displacement counts are repeated selection occurrences. First-selection comparisons use exact identities, report source-checkpoint offsets, and separately count identities selected by only one policy.

### Population-unit identity correction

Schema inspection found that retained M0013 `SelectedIds` and `SelectedCandidates.Identity` do not provide a separate BORINGness population-unit identity. The initial execution-ledger interpretation of `SelectedIds` as population units was incorrect. To avoid inferring units from candidate counts, the M0013 replay helper's diagnostic candidate DTO now also records the existing `PopulationTicket.UnitId`; the helper itself does not alter sampling. Two B/C-only repeats were run for this identity-bearing evidence. The paired population-unit results below use those repeats; sampled type identities are reported separately. The frozen M0013 artifacts are unchanged.

## Selection overlap and displacement

Each cell shows the seed-median mean checkpoint Jaccard, with the range of the five seed means in brackets. A value of 1 means identical identity sets in the paired checkpoint; it does not mean semantic equivalence.

| Repository | Arm | Population units | Sampled types |
|---|---|---:|---:|
| Private (aggregate only) | all | 0.201 [0.184–0.232] | 0.194 [0.173–0.225] |
| Private (aggregate only) | none | 0.340 [0.265–0.349] | 0.340 [0.265–0.349] |
| `dotnet-semantic-type-model` | all | 0.149 [0.130–0.165] | 0.093 [0.089–0.110] |
| `dotnet-semantic-type-model` | none | 0.267 [0.199–0.271] | 0.267 [0.199–0.271] |
| `dotnet-ai-code-hygiene` | all | 0.571 [0.569–0.664] | 0.499 [0.407–0.559] |
| `dotnet-ai-code-hygiene` | none | 0.909 [0.818–1.000] | 0.909 [0.818–1.000] |
| `dotnet-ai-first-2d-game-engine` | all | 0.178 [0.153–0.213] | 0.101 [0.100–0.117] |
| `dotnet-ai-first-2d-game-engine` | none | 0.237 [0.179–0.287] | 0.237 [0.179–0.287] |

Under `all`, displacement was pronounced in the larger public histories. In semantic-type-model, per-seed repeated occurrences were 299–318 B-only and 257–278 C-only for population units, and 330–343 B-only and 285–310 C-only for sampled types. In the game-engine history, the respective ranges were 159–178 / 136–151 population-unit occurrences and 193–198 / 166–176 sampled-type occurrences. Private's all-arm seed medians were 433 B-only versus 427 C-only unit occurrences, and 440 versus 433 sampled-type occurrences. Hygiene's short history showed greater overlap. These are selection counts, not defects found.

## First-selection timing

The offset is `first C checkpoint - first B checkpoint` for exact identities selected by both policies; positive means B selected that identity earlier. Across seed medians, the median offset was 0 or at most one checkpoint in either direction. In the `all` arm, however, C had more earlier-first identities than B in the two longer public histories: for semantic-type-model, B/C earlier counts ranged 2–7 / 30–35 for population units and 2–5 / 26–28 for sampled types. Game-engine ranges were 2–8 / 6–9 and 2–4 / 7–11. Private's seed-median all-arm counts were B/C 31/87 for units and 27/80 for sampled types. The no-observation arm was much closer, with few identities selected earlier by either policy.

Many exact identities were selected by only one policy during the observed history. For example, in semantic-type-model's all arm, the per-seed B-only/C-only first-selection counts were 4–14 / 87–98 population units and 219–228 / 192–210 sampled types. In Private, the corresponding seed medians were 41/166 units and 146/213 sampled types. These are one-policy-only identities, not candidates inferred to be eligible but never selected. No equivalence is inferred for changed or untrackable identities.

## Due work, selected tickets, and batch caps

The `none` arm leaves due work unconsumed. The public totals below sum over five seeds and all checkpoints. Private values are medians across its five seeds; no seed-specific Private values are published.

| Repository | B due / selected | C due / selected |
|---|---:|---:|
| Private (median per seed) | 191,452 / 583 | 83,302 / 583 |
| `dotnet-semantic-type-model` | 516,719 / 1,893 | 89,324 / 1,893 |
| `dotnet-ai-code-hygiene` | 4,524 / 215 | 1,535 / 214 |
| `dotnet-ai-first-2d-game-engine` | 261,033 / 1,086 | 43,680 / 1,086 |

These due counts are scheduler work items, not unique defects or reviews. The selected counts remain at or near the fixed budget while B accumulates substantially more due work in the longer histories.

For BORINGness, cap saturation is the fraction of active checkpoint/seed pairs selecting at the five-ticket cap. In `none`, both policies saturated 95% of Private pairs, 97.4% of semantic-type-model pairs, 54.5–63.6% of hygiene pairs, and 95.6% of game-engine pairs. In `all`, B saturated 95% / 97.4% / 54.5–63.6% / 95.6% respectively; C saturated 95% / 72.7–80.5% / 36.4–45.5% / 60–80%. Arms and denominators remain separate in the derived metrics.

## Repeatability, runtime, and storage

The initial M0014 B/C identity replay took 102.39 minutes for 133 public checkpoints and about 119.70 minutes for 120 Private checkpoints. The second replay, which added `PopulationUnitId`, took 123.05 and 139.42 minutes respectively. Both had every expected checkpoint and the same source revisions as the frozen input. The two M0014 outputs match exactly across 15,080 candidate-identity rows: 4,620 semantic-type-model, 560 hygiene, 2,700 game-engine, and 7,200 Private. In the second replay, all selected B/C BORINGness candidates in all four repositories had a population-unit ID. The unit-ID field itself was only available in that second schema, so independent unit-ID equality across two unit-aware runs was not measured.

The retained M0013 trace matches the M0014 repeat for all 7,880 public rows. For Private, it differs in 105 of 7,200 compared rows across 25 checkpoints; the two current M0014 repeats match each other exactly. This Private cross-run discrepancy is unresolved and is not hidden by the repeatability result. Private hashes and mismatch identities are not published.

Raw identity-bearing JSON from the population-unit replay occupied 53,717,863 bytes across public repositories (34,005,499 semantic-type-model; 1,424,467 hygiene; 18,287,897 game-engine) and 70,749,868 bytes for Private. These files remain in local evidence storage. The public derived metrics artifact contains aggregate Private distributions only. Both full B/C identity passes together required 484.56 minutes across public and Private, about 8.1 hours; this is a material cost for recurring full-history analysis.

## Subsequent planning decision (post-experiment)

After this report was produced, planning decided to pivot toward **gate-local review selection and bounded parameter calibration at a concrete change gate**, with historical population information serving as context. The experiment's own findings, limits and original proposal below remain unchanged as historical evidence; the original suggestion to proceed directly to a broad blinded replay assessment is not a standing roadmap decision. See [M0012–M0014 retrospective and pivot](SAMPLING-GATE-PIVOT.md).

## Interpretation and follow-up

Selection displacement is reproducible within the two M0014 runs and large in the longer histories, particularly for the synthetic `all` arm. The first-selection results do not show a consistent activity-age discovery lead: C often selected shared identities earlier in `all`, while the median offset was generally zero. The fixed cap converts increased B due work mainly into queue pressure rather than more selected tickets.

**A bounded blinded semantic assessment is warranted**, because the policies repeatedly select different candidates under comparable caps. It should use equal completed-review budgets, prioritize the reproducible public candidate sets, include blinded B-only/C-only selections and shared-selection controls, and start at 25 reviews per policy per repository. Expand only if reviewer capacity supports 50 or 100. Private assessment should use authorized local traces and first reconcile the 105-row retained-to-M0014 discrepancy. No judgment pool or labels were created in this milestone.

This recommendation justifies asking whether displacement changes review value. It does not establish that activity age is useful, harmful, or better than change-only sampling. Production policy remains unchanged; M0013 remains awaiting its own human gate.

## Limitations

- Synthetic observation arms are scheduler bounds, not historical review behavior.
- No semantic-quality labels, actual review completion, defect yield, or uncertainty estimates are available.
- The exact identity rule deliberately treats renamed or structurally changed identities as different.
- Public branch snapshots may be stale because remote freshness was unavailable at the M0013 freeze.
- Private raw identities, revisions, seeds, and per-seed values are withheld; its historical M0013 comparison has the unresolved discrepancy described above.
- The short hygiene history yields fewer paired checkpoints and should not be generalized to larger repositories.

## Reproduction and evidence

From the repository root on the recorded Windows 11 / PowerShell / .NET 11 environment:

```powershell
.\eng\M0013.ps1 -Action build-replay
.\tools\M0014\Replay-Identity-Corpus.ps1 -RepositoryId <public-repository-id> -RepeatEvidenceRoot <local-evidence-root>
.\tools\M0014\Analyze-Displacement.ps1
.\tools\M0014\Test-DisplacementFixture.ps1
```

The Private replay uses the authorized local Private checkout and restricted evidence root; those paths and all raw traces are intentionally omitted. `docs/research/ACTIVITY-AGE-SELECTION-METRICS.json` contains aggregate outputs, and `docs/research/ACTIVITY-AGE-SELECTION-CHECKPOINTS.jsonl` contains public per-checkpoint rows only. `.execution/M0014-sampling-selection-displacement.md` maps each acceptance obligation and validation case to its evidence. The request for human review is `.review/REV-M0014-COMPLETION.md`.
