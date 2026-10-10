# M0013 — Cross-Repository Sampling Age Experiment

**Execution status:** mechanical replay complete; semantic usefulness inconclusive; milestone awaiting human review.
**Frozen:** 2026-10-10 14:21 UTC. **Completed:** 2026-10-10 20:12 UTC.
**Recommendation:** **inconclusive** on retaining, adjusting, or removing activity age. The replay measured scheduler behavior and cost, but no blinded semantic-review judgments were available.

## Question and boundaries

This experiment asks whether the fixed M0012 project-activity policy selects more useful semantic-review work than elapsed-time aging or change-only sampling at comparable review cost. It is a retrospective scheduling simulation: current M0012 rule questions and sampler behavior are replayed over available mainline source snapshots. It does not claim that reviews actually occurred at those historical revisions.

Production code, public CLI behavior, sampler algorithms, rule versions, fixed hazard policies, and M0012 documentation authority were not changed. Three policies were fixed before the first replay:

- **A — elapsed-time baseline:** apply the M0011 elapsed-day hazard overlay (`deltaSeconds / (365 * 86400)`) to the current review populations; BORINGness uses the previous candidate count as its multiplier.
- **B — activity age:** use the existing M0012 implementation and its project-scoped normalized source-change exposure.
- **C — change-only:** retain the same M0012 populations, versions, sampler, first/change hazards, and budgets while suppressing background age contribution.

Five SHA256-derived seeds were fixed per repository. Each seed was shared across A/B/C. Two separate synthetic observation arms ran for every policy: accept every selected ticket, and observe none. These arms measure scheduler mechanics only.

## Corpus and eligibility

The corpus was frozen from local cached `refs/remotes/origin/main` snapshots. GitHub fetch attempts timed out, so these endpoints could not be confirmed as current remote heads. Checkpoints are the initial available first-parent revision plus each subsequent first-parent commit changing a tracked non-generated `.cs` path. Every checkpoint was loaded by M0012’s real Roslyn project reader; the replay also performs a repeated unchanged pass at every checkpoint.

| Repository | Frozen range / local revision information | First-parent commits | Source checkpoints | Eligible projects at head | Eligible source records / LOC at head |
|---|---|---:|---:|---:|---:|
| `carlrabbit/Private` | Exact range withheld; 5 commits on cached default branch | 5 | 5 | 27 | 450 / 25,140 |
| `carlrabbit/dotnet-semantic-type-model` | `d95c46079e79155e7b2157e51473f98dab97a514` .. `7997bcdf2ba36388e74e3e6d069b12414715383a` | 172 | 77 | 35 | 158 / 27,040 |
| `carlrabbit/dotnet-ai-code-hygiene` | `5caa1c802098dd8ffa5876a344242be7b2e30fc8` .. `92825010a5afce11646857b2b20e1e0f5ec20174` | 15 | 11 | 4 | 34 / 6,212 |
| `carlrabbit/dotnet-ai-first-2d-game-engine` | `b5130fccc40a6666d898ab269c60c004b6cfd8b9` .. `972b7c47b0215311bbd6b1cf57841bd8d21b2800` | 52 | 45 | 26 | 179 / 26,755 |

Eligibility counts are measured project-scoped source records; linked source can occur in more than one project. The current-head populations (BORINGness candidates; summary quality candidates; German summary candidates where enabled) were respectively: Private 421/1,431/1,431; semantic-type-model 447/736/736; hygiene 85/15/disabled; game-engine 842/72/72. These are scope counts, not quality findings.

The Private repository was intended as a heavy-change stress case, but its cached default-branch history contains only five commits. Its longer non-main feature history was not substituted. This leaves the intended stress case underrepresented. Squashed, inaccessible, or non-source intermediate edits cannot be reconstructed. The hygiene default-branch history ends before the later M0011/M0012 implementation, so it is evaluated retrospectively using the fixed current M0012 code.

Private revision identifiers, timestamps, source paths/content, and candidate data remain in a user-restricted local evidence store and are not present in committed artifacts. Public files contain only aggregate Private measurements and the repository name explicitly specified for this experiment.

## Replay and reproducibility

The first corrected full run replayed 138 checkpoints, 30 policy/seed/observation states per checkpoint (4,140 arm states), in 107.40 minutes. A second clean-state corrected run replayed the same 138 checkpoints and 4,140 states in 107.42 minutes. The comparison found **0 mismatching states** across populations, project activity deltas, due and selected counts, accepted observations, post-observation due counts, and serialized state bytes. Wall-clock fields were excluded from equality checks and are reported as distributions.

At each checkpoint, the replay loads the frozen tree, evaluates the actual current semantic-review modules, records due work before observation, observes only selected tickets in the synthetic accept-all arm, persists sampler state externally, and applies the same unchanged checkpoint again. The no-observation arm confirms that absent observation leaves due work unconsumed. State paths are unique per repository/policy/seed/observation arm. Seeds, state, and observations do not cross repository boundaries.

## Measured scheduling workload

The following due and selected counts sum across the source checkpoints and five seeds for the **no-observation** arm. “Due events” are sampler work items, not unique bugs or completed reviews. Selected tickets remain bounded by the production batch policy; completed-review yield is zero in this arm by design.

| Repository | A due / selected | B due / selected | C due / selected | B:C due ratio |
|---|---:|---:|---:|---:|
| `Private` (aggregate only) | withheld / withheld | withheld / withheld | withheld / withheld | 1.21 |
| `dotnet-semantic-type-model` | 38,660 / 1,893 | 516,719 / 1,893 | 89,324 / 1,893 | 5.78 |
| `dotnet-ai-code-hygiene` | 607 / 193 | 4,524 / 215 | 1,535 / 214 | 2.95 |
| `dotnet-ai-first-2d-game-engine` | 23,294 / 1,086 | 261,033 / 1,086 | 43,680 / 1,086 | 5.98 |

The accept-all arm accepted every selected ticket and retained only the due work left after those observations. It does not represent actual reviewer acceptance. In particular, B raised the due queue substantially over C in the three public histories while the bounded selected-ticket total was unchanged or nearly unchanged. This shows added due pressure under the fixed batch cap; it does not show added semantic value.

## Runtime and persistence

Per-checkpoint elapsed time includes process startup and the shared Roslyn project/workspace load for that historical tree plus all 30 states. The sampler-pass measure includes rule evaluation and sampling-state publication but excludes the external copy of each arm state. The state-size figures include sampler and activity state. Times are machine-specific observations, not promises.

| Repository | Checkpoints | Full checkpoint time p50 / p95 / max (ms) | B/no-observation state p50 / p95 / max (bytes) | B/no-observation sampler pass p50 / p95 (ms) |
|---|---:|---:|---:|---:|
| Private (aggregate only) | 5 | 19,797 / 62,287 / 62,287 | 881,177 / 2,717,854 / 2,717,854 | 181 / 602 |
| `dotnet-semantic-type-model` | 77 | 70,834 / 81,475 / 84,569 | 1,707,749 / 2,749,072 / 2,771,930 | 411 / 502 |
| `dotnet-ai-code-hygiene` | 11 | 13,218 / 14,755 / 14,755 | 294,202 / 460,099 / 460,099 | 158 / 268 |
| `dotnet-ai-first-2d-game-engine` | 45 | 36,934 / 42,646 / 43,504 | 205,205 / 1,098,368 / 1,266,922 | 337 / 470 |

The activity state grew beyond the one-megabyte retained-source budget in larger histories because bounded metadata remains recorded for project files. Exact state sizes and timing distributions for all policies/observation arms are in [`ACTIVITY-AGE-RESULTS.json`](ACTIVITY-AGE-RESULTS.json); per-checkpoint aggregates and public frozen revisions are in [`ACTIVITY-AGE-REPLAY.json`](ACTIVITY-AGE-REPLAY.json) and [`ACTIVITY-AGE-CORPUS.json`](ACTIVITY-AGE-CORPUS.json).

## Semantic quality and equal-review budgets

No independent reviewer identity or assessment capacity was supplied. No blinded labels were collected, and no actual assessments were fabricated. The pre-registered 25-review per-policy budget (with 50 and 100 only if supported) was therefore not completed for any repository. No actionable/acceptable/uncertain outcome, discovery position, or equal-budget usefulness comparison is available. Synthetic acceptance is not semantic evidence.

The requested blinded union-plus-random judgment pool remains unreviewed and is not included in public artifacts. A reviewer with authorized access and practical capacity is still needed to create/assess it, especially for Private. Unknown outcomes remain unknown. This leaves AC-05/06 and their evidence cases incomplete.

## Failures, limitations, and evidence provenance

- The initial runner attempt failed before replay due to a PowerShell ISO timestamp conversion. The conversion was corrected.
- An instrument-development pass completed but mislabeled the seed field and estimated post-observation due work by subtracting accepted tickets. It is excluded from the corrected results.
- A repeat was stopped after three Private checkpoints when the due-after measurement issue was found. Its partial result is excluded. A subsequent start found the known temporary Private worktree; that worktree was verified and removed, then the full corrected pass ran.
- The two corrected complete runs matched across all 4,140 arm states. No source checkpoint failed or was missing.
- Public `origin/main` refs were locally cached and may be stale. Re-fetch and re-freeze before treating this corpus as current.
- Mainline checkpoints cannot recover squashed edits; Private mainline history is too short to fulfill its intended heavy-change role.
- The no-observation and accept-all arms are synthetic bounds. Current policies applied retrospectively are not historical review logs.
- The candidate pool is not unbiased for repository-wide defect rates. With no labels, effectiveness and statistical uncertainty cannot be estimated.

## Recommendation

**Inconclusive.** M0012 activity age changes scheduling pressure and repeated replay confirms deterministic stateful behavior, but the workload increase did not increase the number of selected tickets under the fixed batch cap in the larger histories. Without blinded semantic judgments at equal completed-review budgets, there is no evidence to retain, tune, or remove the activity policy on usefulness grounds. Preserve the current production policy pending a reviewer-backed experiment; do not treat this operational replay as evidence that it improves review quality.

## Reproduction and revalidation

The frozen inputs and helpers are committed in this repository. From a checkout with the declared SDK and cached dependency availability, run:

```powershell
.\eng\M0013.ps1 -Action freeze -PrivateRepository $env:M0013_PRIVATE_REPO -PrivateEvidenceRoot $env:M0013_PRIVATE_EVIDENCE
.\eng\M0013.ps1 -Action inventory -PrivateRepository $env:M0013_PRIVATE_REPO -PrivateEvidenceRoot $env:M0013_PRIVATE_EVIDENCE
.\eng\M0013.ps1 -Action build-replay
.\eng\M0013.ps1 -Action replay-corpus -PrivateRepository $env:M0013_PRIVATE_REPO -PrivateEvidenceRoot $env:M0013_PRIVATE_EVIDENCE
.\eng\M0013.ps1 -Action analyze -BaselinePath <first-complete-run.json>
```

Set the two environment variables to the authorized local Private checkout and restricted evidence directory before using the corpus commands; their values are intentionally not recorded here. The scripts require a Private checkout parameter and apply a user-only ACL to their temporary Private worktree directory.

Reproduction of Private requires authorized local access. The exact Private revisions, seeds, replay state, and raw candidate/source data are intentionally not included or linked here. The completed experiment's canonical request for milestone-scoped human review is [`REV-M0013-COMPLETION`](../../.review/REV-M0013-COMPLETION.md); its gate is still pending.
