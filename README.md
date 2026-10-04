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

New M0003 command:

```text
hygiene check
hygiene review expand <batch-handle>
```

`hygiene check` reports deterministic findings and one semantic review batch for each enabled semantic review rule. The first rule, `docs.summary.quality.review`, samples up to five valid, non-empty XML summaries from the selected C# targets. An empty sample is valid and is reported explicitly as `sample 0/0`.

The current implementation agent reviews the normal sample against the four published questions: natural German, technical correctness, information value, and clarity/scope. Confidently acceptable answers need no further action. If any answer materially fails or the implementation agent is uncertain about any question, explicitly expand the batch and hand the complete eligible population to a frontier-capability reviewer:

```text
hygiene review expand B-1
```

Expansion accepts a bare batch ID or a fully qualified latest-run handle such as `R-7K2M9P/B-1`. The CLI only selects and presents review work; it never invokes a model or determines whether prose passes. No semantic review answers or history are stored.

Formatting, normalization, and installed-tool consumer validation move to M0004.
