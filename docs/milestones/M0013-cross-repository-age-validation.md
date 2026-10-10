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
