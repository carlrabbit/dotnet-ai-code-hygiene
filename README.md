# dotnet-ai-code-hygiene

AI-first code hygiene tooling for deterministic hygiene analysis and bounded semantic review.

## Status

M0001 and M0002 are complete. M0003 — Semantic Review Sampling & Escalation — is the active milestone.

M0003 adds bounded semantic review batches. The CLI selects what should be reviewed; the current implementation agent judges the sample. If the sample is materially poor or uncertain, the caller explicitly expands the batch for frontier-capability review. The CLI itself never invokes a model.

```text
generated or edited code
-> deterministic findings
-> bounded semantic review sample
-> implementer review
-> explicit frontier escalation only when needed
-> remediation by the calling agent
```

Windows 11 and the .NET 11 SDK line remain the authoritative initial platform. No GitHub Actions/workflows are used.

M0003 review workflow:

```text
hygiene check
hygiene review expand <batch-handle>
hygiene review handoff <batch-handle> [--file <path>]
```

`hygiene check` reports deterministic findings and one semantic review batch for each enabled semantic review rule. The first rule, `docs.summary.quality.review`, samples up to five valid, non-empty XML summaries from the selected C# targets. An empty sample is valid and is reported explicitly as `sample 0/0`.

The current implementation agent reviews the normal sample against the four published questions: natural German, technical correctness, information value, and clarity/scope. Confidently acceptable answers need no further action. If any answer materially fails or the implementation agent is uncertain about any question, explicitly expand the batch and hand the complete eligible population to a frontier-capability reviewer:

```text
hygiene review expand B-1
```

`review expand B-1` is a transient stdout operation for a colocated frontier reviewer. It accepts a bare batch ID or a fully qualified latest-run handle such as `R-7K2M9P/B-1` and emits the full revalidated population.

`review handoff B-1` creates a durable JSON request at `.hygiene/reviews/<handoff-id>/request.json`. This namespaced folder is intentionally not Git-ignored, so a team can include the request in the same branch or PR. The CLI does not commit it; whether to add the artifact is a caller/team decision.

For a completely decoupled frontier reviewer, choose an external destination:

```text
hygiene review handoff B-1 --file "G:\My Drive\Transfer\HR-R7K2M9P-B1-request.json"
```

Both handoff forms use the same latest-run and population revalidation as `review expand`. The request embeds full source text for each file containing a review item, and no unrelated source files. Choose only a destination appropriate for that repository source. The CLI writes the file locally; it does not transmit source, invoke Git, or commit the request.

The CLI never invokes a model or determines whether prose passes. No engine-managed semantic-review history or result ingestion exists; a request is an explicit review work product, not review acceptance state.

Formatting, normalization, and installed-tool consumer validation move to M0004.
