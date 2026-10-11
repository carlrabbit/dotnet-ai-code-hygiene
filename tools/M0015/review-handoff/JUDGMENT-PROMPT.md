# M0015 independent BORINGness assessment

You are the project owner conducting a manual, policy-blinded assessment. Assess only the candidate declarations in `blinded-candidates.json` against the exact repository head snapshots. Do not inspect PR #16, comparison outputs, selection artifacts, or any token-to-policy mapping before locking and returning your judgments. Do not infer why an item was included. This handoff contains no model-generated or prefilled semantic judgments.

## Frozen sources and candidates

Each list item identifies a source archive by `sourceKey`, repository URL, exact head revision, archived path, project, fully qualified symbol, and declaration disambiguator. Extract the matching ZIP from `repositories/` and inspect the declaration plus enough callers, registrations, adapters, and owning types to assess the actual architecture. Do not assess from a symbol name alone. The matching declaration fingerprint and revision are frozen in `assessment-manifest.json` and the template.

## Q1–Q5 rubric

Answer `yes`, `no`, or `uncertain`; “yes” means the signal is present.

- **Q1 — Speculative abstraction:** Is there an interface, provider, factory, strategy, generic mechanism, extension point, or similar abstraction with only one meaningful production use/implementation and no current requirement for variability?
- **Q2 — Indirection:** To understand what this code actually does, must you follow several forwarding/delegation layers before reaching the behavior?
- **Q3 — Change locality:** Would a small behavioral change require coordinated edits across multiple plumbing types, registrations, adapters, or mappings rather than mostly changing the code that owns the behavior?
- **Q4 — Hidden machinery:** Does the implementation rely on reflection, dynamic discovery, convention-based registration, a custom DSL/framework, or other implicit machinery where ordinary explicit C# could satisfy the current requirement?
- **Q5 — Unclear ownership:** After reading the relevant types, is it difficult to identify the single place that owns the behavior or state being reviewed?

Legitimate external-service, persistence, platform, and interoperability boundaries are not failures merely because they currently have one implementation. Assess the current requirement, not hypothetical future extensibility.

## Decision and abstention

`negative` when Q4 is yes or at least two of Q1/Q2/Q3/Q5 are yes. `accepted` when every answer is determined and the threshold is not met. `uncertain` when source evidence is insufficient; any uncertain question requires an uncertain decision. Cite source-relative `path`, optional line/anchor, and a short note. Use `uncertainty` to explain abstention. Missing items in a partial submission remain incomplete; do not convert them to accepted.

## Return format

Copy `judgments-template.json`, retain `schemaVersion`, `kind`, `protocolVersion`, `manifestSha256`, and tokens. Record actual reviewer identity, role, model or human workflow, session ID, UTC completion time, source revision and fingerprint for each judged item, Q1–Q5 answers, decision, source evidence, and uncertainty where applicable. Partial submissions may include only completed items; do not add a status or fabricate absent items.

Format-only partial example (no judgment answers):

```json
{"schemaVersion":1,"kind":"m0015-semantic-review-judgments","protocolVersion":"<copy from manifest>","manifestSha256":"<copy SHA-256>","reviewer":{"reviewerId":"<actual>","role":"<actual>","model":"<actual workflow>","sessionId":"<actual session>","completedAtUtc":"<UTC timestamp>"},"items":[]}
```

Validate the file locally with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\validate-judgments.ps1 -Action validate -PackageRoot . -SubmissionPath .\judgments-<review-session-id>.json
```

The validator reports `incomplete` for a valid partial submission and does not infer judgments. Return the completed JSON under `M0015-review/submissions/`. Keep the handoff unchanged. Do not convert missing or uncertain items to acceptance.
