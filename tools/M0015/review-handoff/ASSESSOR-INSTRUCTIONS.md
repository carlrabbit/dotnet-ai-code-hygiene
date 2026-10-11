# M0015 Stage A — Independent BORINGness Assessment

## Purpose and blinding

Assess each token independently against the fixed BORINGness questions using only the two frozen source archives. The candidate list intentionally contains no policy labels or control markers. Do not inspect the repository's selector comparison results, prior mapping files, or internal implementation notes before locking the judgments. Do not infer why a candidate was sampled. No automated assessment is part of this handoff.

The submission population is frozen at  24 unique candidate declarations. `sourceKey` in the blinded list identifies which source archive to inspect; each listed path and fully qualified symbol is anchored to the exact head revision in `FROZEN-MANIFEST.json`.

## Source archives

- `source-archives/repository-a-source.zip` — `carlrabbit/dotnet-ai-code-hygiene` at head `4d05393b4501a0b4d701f6ba6e1d051e9bd35ec2`.
- `source-archives/repository-b-source.zip` — `carlrabbit/dotnet-semantic-type-model` at head `88b5ad5092e38e00257d4e327d6606afa2bf4952`.

Extract each archive separately. For every list item, locate its `path`, then read the declaration and enough surrounding callers, registrations, adapters, and owning types to assess the current architecture. Do not assess from the symbol name alone. The `disambiguator` identifies repeated declarations with the same symbol in a file.

## Fixed Q1–Q5 rubric

Answer each question `yes`, `no`, or `uncertain`. “Yes” means the signal is present.

- **Q1 — Speculative abstraction:** Is there an interface, provider, factory, strategy, generic mechanism, extension point, or similar abstraction with only one meaningful production use/implementation and no current requirement for variability?
- **Q2 — Indirection:** To understand what this code actually does, must you follow several forwarding/delegation layers before reaching the behavior?
- **Q3 — Change locality:** Would a small behavioral change require coordinated edits across multiple plumbing types, registrations, adapters, or mappings rather than mostly changing the code that owns the behavior?
- **Q4 — Hidden machinery:** Does the implementation rely on reflection, dynamic discovery, convention-based registration, a custom DSL/framework, or other implicit machinery where ordinary explicit C# could satisfy the current requirement?
- **Q5 — Unclear ownership:** After reading the relevant types, is it difficult to identify the single place that owns the behavior or state being reviewed?

Legitimate boundaries are not failures merely because they currently have one implementation. External-service, persistence, platform, interoperability, or other real boundaries may justify abstraction. Evaluate the current requirement, not hypothetical future extensibility.

Outcome rule: `negative` when Q4 is yes or at least two of Q1/Q2/Q3/Q5 are yes; `accepted` when all five answers are determined and that escalation threshold is not met; `uncertain` when the source evidence is insufficient for a reliable outcome. One non-Q4 signal alone is review evidence, not planner escalation. Record a brief rationale and source-relative evidence pointers. Do not change source code or produce implementation suggestions as if they were findings.

## Complete the judgment template

Copy `JUDGMENT-TEMPLATE.json` to `M0015-judgments.json`. Keep the frozen `schemaVersion`, `kind`, `manifestSha256`, and every token. Fill reviewer identity/role, UTC completion timestamp, confirm independence, and complete each token's Q1–Q5 answers, outcome, rationale, and source-relative evidence. Evidence may be an empty array only when the outcome is `uncertain`; explain what is unavailable in the rationale. Do not add policy or control labels.

Run the included validator from this folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\validate-submission.ps1 -Action validate -PackageRoot . -SubmissionPath .\M0015-judgments.json
```

The validator checks structure, frozen token coverage, rubric/outcome consistency, source-relative evidence references, and hashes. It does not judge the semantic correctness of answers or reveal selection membership.

## Return path

Return only the completed `M0015-judgments.json` to `M0015-review/submissions/` (the project owner’s submission folder). Keep this handoff package unchanged. Do not add judgments to the repository until the owner requests Stage C. Missing or uncertain judgments should remain explicit; do not convert absence into acceptance.
