# M0012–M0014 Retrospective — From Accumulated Age to Gate-Local Review Selection

**Status:** historical research synthesis, recorded after the decision to pivot. **Not an implementation request, new milestone, or current policy authority.**

## Why these experiments were run

Expensive agent-executed semantic hygiene checks should be sampled instead of re-running every rule on every changed subject at every repository change. The original subject/population hazard models provided stateful selection. M0012 added project-normalized source-churn **activity age** as background hazard; M0013 replayed three policies, and M0014 compared which candidates activity age selected versus change/fingerprint-only sampling.

The *intended* purpose of age was narrower than maximizing due-event pressure: occasional, **low-percentage reinspection of subjects that have not changed for a while**, alongside a dominant first-encounter/change-driven path. A project changing substantially was intended to be context for revisiting otherwise stale evidence, **not a reason for background checks to dominate the primary rule workload**.

## Measured observations

| Evidence | Observation | Interpretation boundary |
| --- | --- | --- |
| M0013, corrected Private `upstream` history | 120 source checkpoints; activity-age B had **960,263** due events vs **420,553** for change-only C in the synthetic no-observation arm (five seeds combined); selected **2,917** vs **2,905** tickets | More accumulated due work is not more completed semantic reviews or better quality |
| M0013, public histories | B:C due-event ratios: semantic-type-model **5.78**, hygiene **2.95**, game-engine **5.98**; selected-ticket totals mostly equal | Fixed selection caps can mask significant queue/ranking changes |
| M0014, Private synthetic accept-all | Sampled-type mean-checkpoint Jaccard overlap (seed median) **0.194** between B and C; no-observation **0.340** | Same number of selected tickets does **not** mean same identities |
| M0014, longer public histories, accept-all | Sampled-type overlap: semantic-type-model **0.093**, game-engine **0.101**; hygiene **0.499** | Substantial allocation/displacement under a shared review cap; not an effectiveness outcome |
| M0014, Private, accept-all | First-selected-earlier shared type identities: B **27**, C **80** (seed medians); median checkpoint offset generally zero or near zero | Additional age hazard did not reliably advance selection of shared identities |
| M0014, no-observation | At five-ticket cap in **95%** of Private, **97.4%** of semantic-type-model and **95.6%** of game-engine active checkpoint/seed pairs, for both B and C | Marginal hazard principally reallocates saturated work, rather than expanding the bounded batch |
| M0013/M0014 validity | Repeatable aggregate/state and recent M0014 candidate rows, but **105 / 7,200** Private M0013-to-M0014 candidate rows differ across 25 checkpoints; no blinded semantic judgments | Cross-run Private discrepancy unresolved; neither benefit nor harm to semantic outcomes was established |

The above are distinct quantities. **Due events** can repeat while unobserved; **selected tickets** are capped scheduler output; **actual completed reviews** and **actionable discoveries** were not measured. The accept-all and no-observation arms were synthetic scheduling scenarios, not historical reviewer behavior.

The experiment did **not** measure attribution to age alone, individual subject staleness at selection, or how often age-only reviews displaced specifically changed-subject reviews. Therefore it cannot prove that age violates a particular percentage budget. Nevertheless the observed displacement and due pressure do **not demonstrate** the proposed low-frequency safety-net behavior.

The studies also exposed operational overhead: the corrected Private activity-age no-observation persisted state reached about **10.6 MB** maximum, and M0014's four B/C identity replay segments took approximately **8.1 hours** combined. This argues against routinely using long historical replays as the product's calibration loop.

## Decision already made: pivot the planning question

**Do not continue treating elapsed/project age accrual as the default optimization direction.** This is a planning decision made *after* M0014; it does not retroactively invalidate those experiments or change the currently implemented M0012 runtime.

The new unit of decision is a **concrete change gate** (for example, a PR ready for review):

1. Start with the gate's changed scope, eligible semantic-review subjects, rule cost and a **bounded agent-review budget**.
2. Use readily available **historical population/observation information** as context for prioritization, including when unchanged subjects genuinely warrant occasional reconsideration. Do not require a continuously aging background process or a long replay over calendar/development time.
3. Optimize or **recalibrate selection parameters locally at each gate**, where useful. Minutes of bounded deterministic/cheap calibration are acceptable when the intended review itself costs roughly an hour; actual utility and runtime must still be established.
4. Preserve an interpretable distinction between **primary checks prompted by the current change** and **rare background/unchanged-subject checks**. Their intended allocation is asymmetric; no numeric percentage or new coefficient has yet been validated.
5. Select the simplest technically adequate mechanism. Existing hazard samplers, age state, and statistical selection algorithms are **not protected architectural commitments**; replacement or removal is acceptable if gate-local sampling works better.

This shifts evaluation from “does age accumulate enough hazard?” to **“given this gate and finite expensive-review budget, which subjects should be checked?”** Historical information may improve that answer without serving as an evolving age clock.

## Implications and remaining evidence needs

- Measure *actual selected identities and attribution*, not just due-event volume. Where justified, compare changed/new, unchanged/background, and previously accepted populations at the same review budget.
- Account for displaced primary checks and the share of capacity assigned to unchanged subjects; test that unchanged candidates provide meaningful benefit rather than merely increasing variance.
- For a concrete gate, separate **software-verifiable correctness** of scope, budgets, replay/reproducibility and state from **empirical model usefulness**, which requires real or independently assessed semantic-review outcomes.
- Prefer bounded, gate-local calibration experiments to hours-long multi-repository historical replay when the decision is gate-local.
- Do not claim the local optimizer, coefficient schedule, review distribution, or a replacement sampling algorithm is already validated or implemented.
- Do not silently adjust M0012/M0013/M0014 acceptance records, historical datasets, reviewer gates, or production sampling authority to match the pivot. Future changes require explicit planning and versioned implementation decisions.

## Evidence provenance

- [M0013 report](ACTIVITY-AGE-EXPERIMENT.md) and [mechanical results](ACTIVITY-AGE-RESULTS.json), including [corrected Private upstream aggregates](ACTIVITY-AGE-PRIVATE-UPSTREAM-RESULTS.json).
- [M0014 report](ACTIVITY-AGE-SELECTION-COMPARISON.md) and [selection metrics](ACTIVITY-AGE-SELECTION-METRICS.json).
- Execution limitations/review state: `.execution/M0013-cross-repository-age-validation.md` and `.execution/M0014-sampling-selection-displacement.md`.
