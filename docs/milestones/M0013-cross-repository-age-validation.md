# M0013 — Cross-Repository Sampling Age Experiment

**State:** proposed (not executed)
**Mode:** AI-executed, human-reviewed
**Depends on:** completed M0012 activity-based sampling age

## Goal

Test whether project-scoped abstract activity age improves useful semantic-review selection, at comparable review cost, beyond the already-existing change/fingerprint hazard. Publish reproducible positive *or negative* evidence. Correct accumulation alone is not evidence of effectiveness.

## Initial consumer corpus

Use these named `carlrabbit` repositories as the pre-registered initial corpus:

1. `carlrabbit/Private` — heavy-change/refactoring stress case; private.
2. `carlrabbit/dotnet-semantic-type-model` — independent .NET library and release history.
3. `carlrabbit/dotnet-ai-code-hygiene` — self-hosted reference and manually interpretable rule intention.
4. `carlrabbit/dotnet-ai-first-2d-game-engine` — contrasting agentic milestone-driven project.

`carlrabbit/dotnet-simple-import-orchestrator` is an optional small-repository control, documented separately if included. Corpus membership describes intended test roles, not measured activity. Before replay, inventory each repository's accessible branch/history, language/project eligibility, eligible review populations, and actual churn. Record exact commit ranges, Git SHA endpoints, excluded scopes, and any replacement/exclusion with reasons **before analyzing outcomes**. A history with little eligible C# remains a documented limitation, not a silently substituted success.

Do not publish source content, project paths, identifiable findings, or commit data from `Private` in public reports unless explicitly approved. Keep raw private evidence in authorized private storage; public documentation includes only aggregate, non-sensitive measures and reproducibility limitations. Public results can carry full commit identifiers and reproduction commands.

## Two independent validation questions

**Scheduling correctness:** Verify that repeated checks and identical checkpoints add no age; inactivity has no effect; project boundaries isolate exposure; changes, renames, source exclusions, shallow/missing Git, and replay/state restoration behave deterministically; measure CPU/time and state footprint. This is mechanical validation and can use fixtures and replay records.

**Review usefulness:** Independently establish whether extra selections find actionable semantic issues sooner or more efficiently than existing fingerprint-only sampling. A policy generating more work is not evidence of better quality.

## Replay methodology

1. Reconstruct meaningful historical **mainline** source checkpoints per repository, preserving the actual chronological sequence and fixed SHA identifiers. Prefer meaningful source-change checkpoints/milestones rather than fixed daily sampling; document aggregation if intermediate history is inaccessible. Do not claim squashed-away edits can be reconstructed.
2. For each repository, start each policy from identical rule populations, compatible initial observation assumptions, and controlled sampler seed(s). Compare: (A) current elapsed-day policy as historical baseline; (B) proposed project-scoped normalized-churn age; (C) change/fingerprint hazard only, with no background age. Keep fixed first-evaluation/fingerprint hazard and batch limits wherever the comparison permits.
3. **Stateful replay:** at every checkpoint load revision, compute exposure, accumulate hazard, determine due and bounded selected work, apply only permitted review observations, persist per-policy state, then advance. Due is not the same as reviewed. Review acceptance changes later thresholds/generations; never automatically treat an emitted ticket as successful.
4. Use a separate reproducible *scheduling replay* with explicitly labeled synthetic acceptance scenarios (e.g., all accepted, none accepted) to test mechanics. Do not represent these scenarios as actual quality evidence.
5. For *quality replay*, build a shared blinded judgment pool from the union of candidates selected by A/B/C, plus an independently selected random supplement. Evaluate the actual fixed semantic rule questions; record acceptable, actionable issue, uncertain, and justified escalation separately. Reviewers must not know which policy selected the subject. Apply equivalent assessments to equivalent subject/revision pairs; missing outcomes remain unobserved rather than invented.
6. Compare at equal **completed semantic-review budgets**, e.g. 25/50/100 reviews per repository when populations support them, alongside natural due workload. If budgets cannot be reached or quality judgments are not available, report this and avoid claims of comparative detection efficiency. Address uncertainty from sampled labeling and unequal-probability selection; a pooled labeled set is not automatically an unbiased estimate of repository-wide quality.
7. Compare several fixed seeds and report variability rather than selecting a favorable run. Fix and publish all non-sensitive sampling/normalization parameters before assessing outcomes.

## Measures and evidence

For each policy/repository and budget, record: eligible subjects/scopes, source churn and normalization denominators, age units, separate hazard contributions, due and reviewed counts, actionable/acceptable/uncertain outcomes, redundant reviews, discovery position in *development checkpoints* (not calendar days), per-run and total runtime, and persisted-state size. Report whether activity age adds review value **over policy C**, not merely over wall time.

Publish a versioned report under `docs/research/` with experiment protocol, frozen repository/revision corpus, hygiene/rule/model versions, commands and fixtures to reproduce public data, raw/derived non-sensitive metrics (tables/plots), observed failures, confidence/selection caveats, privacy redactions, and decision. Preserve permitted raw evidence in a stable reviewed artifact. The experiment must state explicitly when a conclusion is not supportable.

## Decision gate

Pre-register simple criteria before examining outcomes:
- correctness: no calendar-only drift, no-op idempotence, cross-project isolation, and conservative history fallbacks;
- operations: bounded predictable workload and acceptable overhead;
- usefulness: additional actionable discovery or earlier discovery than change-only (C) at equal completed-review budget, with uncertainty and limitations reported.

Conclude **retain**, **adjust one small fixed coefficient**, or **remove activity age where unhelpful**. A negative result is a valid milestone outcome. No hidden policy tuning during evaluation; subsequent behavior changes require versioned implementation work.

**Not an implementation milestone; no experimental evidence has been collected yet.**


## Research-milestone execution contract

**Lifecycle:** proposed / planning, not ready. This is an evidence-producing investigation milestone, not a production feature implementation. After M0012 completes, planning must verify replay feasibility against the actual implementation, fix validation targets/loci and feasible review budgets, seed the execution ledger, and explicitly mark this milestone ready. The executor must not make material experimental-policy decisions from scratch.

**Required repository authority and context:** read `AGENTS.md`, `docs/ENGINEERING.md`, `docs/specs/SAMPLING.md`, `docs/specs/SEMANTIC-REVIEWS.md`, the completed M0012 milestone and its acceptance evidence, plus `docs/research/ACTIVITY-AGE.md` as non-authoritative provenance. The guide profile is `base + cli-tool`; code-oriented packaging/release validation is not automatically an experiment gate.

**Tooling boundary:** The executor may create bounded reproducible local experimental scripts/fixtures and supporting test utilities, but must not alter production sampling policy, public CLI semantics, rule versions or published specs as a side effect of the experiment. Store reproducible non-sensitive inputs and outputs in reviewable repository artifacts; do not embed private source data or secrets. If accurate replay requires production changes, return that decision to planning.

**Observation integrity:** Synthetic observation scenarios prove scheduling mechanics only, not review value. Quality replay uses actual recorded judgments for the relevant historical subject/revision, with missing judgments left unknown. Record which assessments are AI-generated, independently checked by a human, or not independently validated. Reviewers do not see policy identity during assessment. The bounded pool is not an unbiased estimate of whole-repository defect rates without a justified design-based correction.

## Stable acceptance obligations

| ID | Required outcome |
| --- | --- |
| AC-01 | Freeze the accessible corpus, revision checkpoints, C# eligible populations, exclusions, and non-sensitive protocol before inspecting outcome comparisons. |
| AC-02 | Reconstruct reproducible chronological source checkpoints and clearly document lost/squashed/inaccessible history. |
| AC-03 | Replay all three specified policies under compatible initial conditions and multiple fixed seeds, without changing the policy after results are visible. |
| AC-04 | Demonstrate correct stateful due, selection, observation, and persistence behavior for the replay, including no-op/activity-isolation behavior. |
| AC-05 | Obtain a blinded common assessment pool with fixed semantic questions, independent random supplementation, explicit provenance, and an honest accounting of unknown or uncertain judgments. |
| AC-06 | Compare policies at equal completed-review budgets where supported, and separately present their natural due workloads; do not infer superior quality from more reviews. |
| AC-07 | Measure reproducibility, runtime, and persistent-state overhead at declared execution targets, with failure and incomplete-run records. |
| AC-08 | Protect private repository evidence and verify that public committed artifacts contain no unauthorized private paths, commits, code, findings, or identifiers. |
| AC-09 | Publish a durable research report and permitted reproducibility artifacts with observed/inferred/assumed evidence, provenance, limitations, freshness and revalidation triggers. |
| AC-10 | Produce an evidence-bounded retain/adjust/remove recommendation (including inconclusive outcome where supported), without changing product authority or rule policy. |

### Required evidence cases

| ID | Parent | Distinct proof required |
| --- | --- | --- |
| EC-02a | AC-02 | Fully reproducible public source-history checkpoint sequence |
| EC-02b | AC-02 | Documented missing, squashed, or inaccessible history and its effect |
| EC-04a | AC-04 | Accepted observation consumes ticket, advances generation, persists and changes subsequent selection |
| EC-04b | AC-04 | Failed/uncertain or absent observation leaves due work unconsumed |
| EC-04c | AC-04 | Deferred due work, repeated identical checkpoints, and resumed state do not double-account exposure |
| EC-04d | AC-04 | Project-isolated activity and independent per-unit accounting |
| EC-05a | AC-05 | Common union-plus-random candidate pool and blinded fixed-rubric labeling |
| EC-05b | AC-05 | Unavailable or inconclusive labels remain missing, not fabricated acceptance |
| EC-06a | AC-06 | Equal-budget useful-review comparison across feasible checkpoints |
| EC-06b | AC-06 | Unconstrained due workload separately reported, including unreachable budget limits |
| EC-08a | AC-08 | Private raw inputs retained only in authorized private locations |
| EC-08b | AC-08 | Explicit public-artifact redaction inspection and aggregate-only private measures |
| EC-09a | AC-09 | Reproduction manifest, commands, fixed tool versions/seeds/revisions and permitted measurements |
| EC-09b | AC-09 | Written report with contrary evidence, inferential caveats and follow-up implications |

No synthetic scenario may satisfy an evidence case requiring actual human/model review judgments. An inability to obtain labels may produce a defensible **inconclusive effectiveness** outcome under AC-05/06/10 if fully documented; it must not be marked as a successful comparative quality finding.

## Validation gates and evidence targets

| Gate | Target / locus | Obligations |
| --- | --- | --- |
| VAL-01 | Deterministic local replay fixtures, at an environment established in post-M0012 readiness planning | AC-02, AC-03, AC-04; EC-02a, EC-02b, EC-04a–d |
| VAL-02 | Corpus replay and assessment evidence at a documented reproducible environment and reviewer process | AC-01, AC-05, AC-06; EC-05a–b, EC-06a–b |
| VAL-03 | Recorded performance measurements and second-run reproducibility check using frozen inputs | AC-03, AC-07, AC-09; EC-09a |
| VAL-04 | Manual inspection of committed/public artifacts for private disclosures; private raw material stays in authorized storage | AC-08; EC-08a–b |
| VAL-05 | Milestone-scoped human review of evidence/recommendation, after ledger reconciliation | AC-09, AC-10; EC-09b |

Before marking ready, planning shall resolve exact available execution locus, reproducible command/fixture targets, reviewer identity/assessment capacity, intended observation scenario, fixed seeds, available history windows, feasible review budgets, measurement ceilings, private evidence storage location, and applicable review-request path. This readiness pass should choose bounded mechanics, **not** revise the three scientific comparison policies based on outcomes.

## Artifacts and completion

Required non-sensitive committed outputs are a frozen corpus/protocol manifest, a compact replay dataset/summary (or reproducible commands to regenerate it), and a completed report at `docs/research/ACTIVITY-AGE-EXPERIMENT.md`. The report must give question, scope, provenance, observed measurements, inferred interpretation, assumptions, limitations, and date/revalidation trigger. Link private evidence only by an authorized opaque evidence identifier if needed; never publish the underlying location or credentials.

Planning must create `.execution/M0013-cross-repository-age-validation.md` before the milestone becomes ready. Seed **each** applicable AC-/EC- obligation separately, together with VAL gates and their explicit proof targets. The executor owns work packages and evidence attachment, not obligation invention. Preserve the full mapping into the milestone completion evidence at closure; do not treat an aggregate successful run as proof for every criterion.

Completion is **AWAITING HUMAN REVIEW** until the milestone-specific human reviewer accepts the evidence and the canonical review gate passes. A negative or inconclusive experimental result is a valid research outcome, provided evidence and limitations satisfy the obligations. Any resulting policy change must return to planning for a separate, versioned implementation decision.

