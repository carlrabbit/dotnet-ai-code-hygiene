# M0013 — Cross-Repository Sampling Age Experiment

**Execution status:** mechanical replay complete; semantic usefulness inconclusive; milestone awaiting human review.
**Initial corpus frozen:** 2026-10-10 14:21 UTC. **Corrected Private corpus frozen:** 2026-10-10 22:07 UTC. **Private upstream replay pair completed:** 2026-10-11 04:23 UTC.
**Recommendation:** **inconclusive** on retaining, adjusting, or removing activity age. The replay measured scheduler behavior and cost, but no blinded semantic-review judgments were available.

## Question and boundaries

This experiment asks whether the fixed M0012 project-activity policy selects more useful semantic-review work than elapsed-time aging or change-only sampling at comparable review cost. It is a retrospective scheduling simulation: current M0012 rule questions and sampler behavior are replayed over available first-parent source snapshots, using Private's `upstream` branch. It does not claim that reviews actually occurred at those historical revisions.

Production code, public CLI behavior, sampler algorithms, rule versions, fixed hazard policies, and M0012 documentation authority were not changed. Three policies were fixed before the first replay:

- **A — elapsed-time baseline:** apply the M0011 elapsed-day hazard overlay (`deltaSeconds / (365 * 86400)`) to the current review populations; BORINGness uses the previous candidate count as its multiplier.
- **B — activity age:** use the existing M0012 implementation and its project-scoped normalized source-change exposure.
- **C — change-only:** retain the same M0012 populations, versions, sampler, first/change hazards, and budgets while suppressing background age contribution.

Five SHA256-derived seeds were fixed per repository. Each seed was shared across A/B/C. Two separate synthetic observation arms ran for every policy: accept every selected ticket, and observe none. These arms measure scheduler mechanics only.

## Corpus and eligibility

The public corpus was frozen from local cached `refs/remotes/origin/main` snapshots. For Private, the prior run incorrectly used the five-commit `origin/main`; after correction, its `origin/upstream` ref was fetched and matched the local tracking ref at freeze time. Public GitHub fetches timed out, so their endpoints could not be confirmed as current remote heads. Checkpoints are the initial available first-parent revision plus each subsequent first-parent commit changing a tracked non-generated `.cs` path. Every checkpoint was loaded by M0012’s real Roslyn project reader; the replay also performs a repeated unchanged pass at every checkpoint.

| Repository | Frozen range / local revision information | First-parent commits | Source checkpoints | Eligible projects at head | Eligible source records / LOC at head |
|---|---|---:|---:|---:|---:|
| `carlrabbit/Private` | Exact range withheld; upstream branch fetched and tracking ref confirmed at freeze | 143 | 120 | 57 | 796 / 54,502 |
| `carlrabbit/dotnet-semantic-type-model` | `d95c46079e79155e7b2157e51473f98dab97a514` .. `7997bcdf2ba36388e74e3e6d069b12414715383a` | 172 | 77 | 35 | 158 / 27,040 |
| `carlrabbit/dotnet-ai-code-hygiene` | `5caa1c802098dd8ffa5876a344242be7b2e30fc8` .. `92825010a5afce11646857b2b20e1e0f5ec20174` | 15 | 11 | 4 | 34 / 6,212 |
| `carlrabbit/dotnet-ai-first-2d-game-engine` | `b5130fccc40a6666d898ab269c60c004b6cfd8b9` .. `972b7c47b0215311bbd6b1cf57841bd8d21b2800` | 52 | 45 | 26 | 179 / 26,755 |

Eligibility counts are measured project-scoped source records; linked source can occur in more than one project. The corrected current-head populations (BORINGness candidates; summary quality candidates; German summary candidates where enabled) are: Private 746/3,530/3,530; semantic-type-model 447/736/736; hygiene 85/15/disabled; game-engine 842/72/72. These are scope counts, not quality findings.

The first experiment used Private `origin/main` in error and underrepresented the intended stress case. The corrected run uses the repository's `upstream` branch (143 first-parent commits and 120 source checkpoints at fetch); exact revision identifiers remain withheld. Squashed, inaccessible, or non-source intermediate edits cannot be reconstructed. The hygiene default-branch history ends before the later M0011/M0012 implementation, so it is evaluated retrospectively using the fixed current M0012 code.

Private revision identifiers, timestamps, source paths/content, and candidate data remain in a user-restricted local evidence store and are not present in committed artifacts. Public files contain only aggregate Private measurements and the repository name explicitly specified for this experiment.

## Replay and reproducibility

The two original full-corpus runs took 107.40 and 107.42 minutes; each included 133 public checkpoints and five Private `main` checkpoints, which are now superseded. The extracted public segment contains 3,990 arm states and matched with **0 mismatching states**. After discovering that Private's upstream branch was the correct history, two isolated upstream runs replayed 120 checkpoints and 3,600 states each in 139.05 and 138.85 minutes; their comparison also found **0 mismatching states**. The corrected evidence therefore covers 133 public plus 120 Private-upstream checkpoints, or 7,590 states across the separately compared segments. Comparison fields include populations, project activity deltas, due and selected counts, accepted observations, post-observation due counts, and serialized state bytes. Wall-clock fields were excluded from equality checks and are reported as distributions. Public and Private segment artifacts are linked separately below.

At each checkpoint, the replay loads the frozen tree, evaluates the actual current semantic-review modules, records due work before observation, observes only selected tickets in the synthetic accept-all arm, persists sampler state externally, and applies the same unchanged checkpoint again. The no-observation arm confirms that absent observation leaves due work unconsumed. State paths are unique per repository/policy/seed/observation arm. Seeds, state, and observations do not cross repository boundaries.

## Measured scheduling workload

The following due and selected counts sum across the source checkpoints and five seeds for the **no-observation** arm. “Due events” are sampler work items, not unique bugs or completed reviews. Selected tickets remain bounded by the production batch policy; completed-review yield is zero in this arm by design.

| Repository | A due / selected | B due / selected | C due / selected | B:C due ratio |
|---|---:|---:|---:|---:|
| `Private` (aggregate only) | 220,953 / 2,905 | 960,263 / 2,917 | 420,553 / 2,905 | 2.28 |
| `dotnet-semantic-type-model` | 38,660 / 1,893 | 516,719 / 1,893 | 89,324 / 1,893 | 5.78 |
| `dotnet-ai-code-hygiene` | 607 / 193 | 4,524 / 215 | 1,535 / 214 | 2.95 |
| `dotnet-ai-first-2d-game-engine` | 23,294 / 1,086 | 261,033 / 1,086 | 43,680 / 1,086 | 5.98 |

The accept-all arm accepted every selected ticket and retained only the due work left after those observations. It does not represent actual reviewer acceptance. In particular, B raised the due queue substantially over C in the three public histories while the bounded selected-ticket total was unchanged or nearly unchanged. This shows added due pressure under the fixed batch cap; it does not show added semantic value.

## Runtime and persistence

Per-checkpoint elapsed time includes process startup and the shared Roslyn project/workspace load for that historical tree plus all 30 states. The sampler-pass measure includes rule evaluation and sampling-state publication but excludes the external copy of each arm state. The state-size figures include sampler and activity state. Times are machine-specific observations, not promises.

| Repository | Checkpoints | Full checkpoint time p50 / p95 / max (ms) | B/no-observation state p50 / p95 / max (bytes) | B/no-observation sampler pass p50 / p95 (ms) |
|---|---:|---:|---:|---:|
| Private (aggregate only) | 120 | 66,213 / 121,228 / 136,627 | 4,107,832 / 9,736,725 / 10,594,823 | 759 / 1,811 |
| `dotnet-semantic-type-model` | 77 | 70,834 / 81,475 / 84,569 | 1,707,749 / 2,749,072 / 2,771,930 | 411 / 502 |
| `dotnet-ai-code-hygiene` | 11 | 13,218 / 14,755 / 14,755 | 294,202 / 460,099 / 460,099 | 158 / 268 |
| `dotnet-ai-first-2d-game-engine` | 45 | 36,934 / 42,646 / 43,504 | 205,205 / 1,098,368 / 1,266,922 | 337 / 470 |

The activity state grew beyond the one-megabyte retained-source budget in larger histories because bounded metadata remains recorded for project files. Public-only state sizes and timing distributions are in [`ACTIVITY-AGE-RESULTS.json`](ACTIVITY-AGE-RESULTS.json); the corrected Private aggregate comparison is in [`ACTIVITY-AGE-PRIVATE-UPSTREAM-RESULTS.json`](ACTIVITY-AGE-PRIVATE-UPSTREAM-RESULTS.json). Per-checkpoint public observations are in [`ACTIVITY-AGE-REPLAY.json`](ACTIVITY-AGE-REPLAY.json). The committed Private replay artifact [`ACTIVITY-AGE-PRIVATE-UPSTREAM-REPLAY.json`](ACTIVITY-AGE-PRIVATE-UPSTREAM-REPLAY.json) contains only aggregate coverage and comparison evidence; its per-checkpoint raw records remain in the restricted local evidence store. The frozen corpus manifest is [`ACTIVITY-AGE-CORPUS.json`](ACTIVITY-AGE-CORPUS.json).

## Semantic quality and equal-review budgets

No independent reviewer identity or assessment capacity was supplied. No blinded labels were collected, and no actual assessments were fabricated. The pre-registered 25-review per-policy budget (with 50 and 100 only if supported) was therefore not completed for any repository. No actionable/acceptable/uncertain outcome, discovery position, or equal-budget usefulness comparison is available. Synthetic acceptance is not semantic evidence.

The requested blinded union-plus-random judgment pool remains unreviewed and is not included in public artifacts. A reviewer with authorized access and practical capacity is still needed to create/assess it, especially for Private. Unknown outcomes remain unknown. This leaves AC-05/06 and their evidence cases incomplete.

## Failures, limitations, and evidence provenance

- The initial runner attempt failed before replay due to a PowerShell ISO timestamp conversion. The conversion was corrected.
- An instrument-development pass completed but mislabeled the seed field and estimated post-observation due work by subtracting accepted tickets. It is excluded from the corrected results.
- A repeat was stopped after three Private checkpoints when the due-after measurement issue was found. Its partial result is excluded. A subsequent start found the known temporary Private worktree; that worktree was verified and removed, then the full corrected pass ran.
- The two public runs matched across all 3,990 states; two corrected Private upstream runs matched across all 3,600 states. No source checkpoint failed or was missing in either comparison.
- Public `origin/main` refs were locally cached and may be stale. Re-fetch and re-freeze before treating this corpus as current.
- First-parent checkpoints cannot recover squashed edits. The Private `main` branch was initially selected in error and is superseded; the corrected experiment uses the fetched `upstream` branch, which supplies the intended longer-history segment.
- The no-observation and accept-all arms are synthetic bounds. Current policies applied retrospectively are not historical review logs.
- The candidate pool is not unbiased for repository-wide defect rates. With no labels, effectiveness and statistical uncertainty cannot be estimated.

## Recommendation

**Inconclusive.** M0012 activity age changes scheduling pressure and repeated replay confirms deterministic stateful behavior, but the workload increase did not increase the number of selected tickets under the fixed batch cap in the larger histories. Without blinded semantic judgments at equal completed-review budgets, there is no evidence to retain, tune, or remove the activity policy on usefulness grounds. Preserve the current production policy pending a reviewer-backed experiment; do not treat this operational replay as evidence that it improves review quality.

## Reproduction and revalidation

The frozen inputs and helpers are committed in this repository. From a checkout with the declared SDK and cached dependency availability, run:

```powershell
git -C $env:M0013_PRIVATE_REPO fetch --no-tags origin upstream
.\eng\M0013.ps1 -Action freeze -PrivateRepository $env:M0013_PRIVATE_REPO -PrivateRef refs/remotes/origin/upstream -PrivateEvidenceRoot $env:M0013_PRIVATE_EVIDENCE
.\eng\M0013.ps1 -Action inventory -PrivateRepository $env:M0013_PRIVATE_REPO -PrivateEvidenceRoot $env:M0013_PRIVATE_EVIDENCE
.\eng\M0013.ps1 -Action build-replay
.\eng\M0013.ps1 -Action replay-corpus -PrivateRepository $env:M0013_PRIVATE_REPO -RepositoryId carlrabbit/Private -PrivateEvidenceRoot $env:M0013_PRIVATE_EVIDENCE
.\eng\M0013.ps1 -Action analyze -BaselinePath <private-upstream-first-run.json> -CurrentPath <private-upstream-repeat.json> -OutputPath docs/research/ACTIVITY-AGE-PRIVATE-UPSTREAM-RESULTS.json
```

Set the two environment variables to the authorized local Private checkout and restricted evidence directory before using the corpus commands; their values are intentionally not recorded here. Fetch the Private `upstream` branch before freezing. Public and Private repository segments are replayed and compared separately to remain within the established execution ceiling. The scripts require a Private checkout parameter and apply a user-only ACL to their temporary Private worktree directory.

Reproduction of Private requires authorized local access. The exact Private revisions, seeds, replay state, and raw candidate/source data are intentionally not included or linked here. The completed experiment's canonical request for milestone-scoped human review is [`REV-M0013-COMPLETION`](../../.review/REV-M0013-COMPLETION.md); its gate is still pending.
