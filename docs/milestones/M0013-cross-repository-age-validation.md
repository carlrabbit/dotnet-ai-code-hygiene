# M0013 — Cross-Repository Sampling Age Experiment

**State:** proposed (not executed)
**Mode:** AI-executed, human-reviewed
**Depends on:** completed M0012 activity-based sampling age

## Goal
Test whether the abstract project-activity age heuristic produces useful, comprehensible and affordable semantic-review selection across *other* repositories. Preserve the results as versioned documentation so product quality claims can be supported by evidence rather than intuition.

## Experiment design
Use a small, intentionally varied set of repositories with obtainable license/access: idle-with-old-code, bursty-agentic, steady-activity, small libraries, and multi-project repos. Pre-register the corpus, selected history windows, inclusion/exclusion rules, available reviewer labels, and limitations; do not cherry-pick only successful runs.

Replay/reconstruct observed project state at reproducible checkpoints. Compare:
- previous wall-clock accrual policy;
- new project-scoped activity-age policy;
- change/fingerprint-only policy without background aging.

Keep a fixed sampler seed where possible for paired comparisons and report sensitivity across multiple seeds. Record project/scopes, changed LOC, normalized age, hazard contributions, review eligibility, due events, actual bounded review workload, runtime and persistence overhead, and review outcomes where legitimately assessed. Differentiate sampling-scheduling probabilities from actual defect probabilities; avoid calling samples unbiased quality estimates.

## Durable evidence and documentation
Produce a concise report under `docs/research/` with methodology, repository revisions, tool/rule/model versions, data/command reproducibility instructions, anonymization/license constraints, plots or compact tables, observed strengths/failures, and recommendations. Store compact raw/derived evidence in a reviewable form or link to stable permitted artifacts. Record excluded or inaccessible histories and avoid inventing review judgments.

Define measurable success/failure criteria *before* observing outputs: no-op idempotence; no calendar-only drift; cross-project isolation; useful review yield relative to workload; bounded execution cost; and robustness to bursty histories. Explicitly discuss where 365 age units per project-equivalent churn proves too sensitive or too insensitive.

## Decision gate
Conclude with one of: retain baseline, adjust a small fixed coefficient/unit interpretation with justification, or remove activity-age from rules lacking evidence of value. Any consequential rule/model change is a subsequent implementation/versioned milestone, not an unrecorded tuning performed inside the report.

**Not an implementation milestone and not evidence of proven effectiveness until it has actually run.**
