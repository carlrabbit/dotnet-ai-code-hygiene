# dotnet-ai-code-hygiene

AI-first code hygiene tooling for deterministic hygiene analysis and bounded semantic review.

## Status

The project targets Windows 11 and the .NET 11 SDK line. The installable `0.6.0` tool provides a fixed supported profile, rule-owned deterministic formatting/normalization and EditorConfig policy, and installed-consumer validation.

M0003 adds bounded semantic review batches. The CLI selects what should be reviewed; the current implementation agent judges the sample. If the sample is materially poor or uncertain, the caller explicitly expands the batch for frontier-capability review. The CLI itself never invokes a model.

```text
generated or edited code
-> deterministic findings
-> bounded semantic review sample
-> implementer review
-> explicit frontier escalation only when needed
-> remediation by the calling agent
```

Windows 11 and the .NET 11 SDK line are the supported platform. The package is locally validated; public-feed publication is not claimed. No GitHub Actions/workflows are used.

## Install and agent workflow

Install the tool package from the configured NuGet source:

```powershell
dotnet tool install --global DotNetAiCodeHygiene.Tool --version 0.6.0
hygiene --version
hygiene help --agent
```

## Supported profile

Each repository runs `hygiene bootstrap` once to establish the supported `dotnet-11` v2 profile, then `hygiene update` to reconcile its profile-owned state and `hygiene check` to analyze source. Update migrates valid v1 markers. Normal checking requires the committed `.hygiene/profile.json` marker. Bootstrap and update are repository-wide, idempotent operations; they preserve content outside the delimited hygiene blocks in root `.editorconfig` and `Directory.Build.props`.

The generated `.hygiene/profile/Hygiene.props` pins `AnalysisLevel=11`, enables built-in .NET analyzers, and enforces code-style analyzers during builds. By default, the configurable `style.braces.required` and `style.accessibility.explicit` rules project braces (IDE0011) and explicit accessibility (IDE0040) as errors; their managed EditorConfig contributions include visible rule-ID comments and are reconciled immediately when toggled. The profile does not promote other diagnostics or set a global warnings-as-errors policy. Mandatory profile findings cannot be disabled or ignored. StyleCop analyzers are prohibited and are reported for caller resolution; hygiene does not remove them automatically.

## Documentation hygiene

Only a generic summary is required for covered API subjects. Summary is semantic: ordinary declarations use `<summary>`, while positional record and record-struct properties use the matching record `<param name="...">` text. A direct `<inheritdoc/>` satisfies a missing summary without recursively inspecting inherited documentation. Ordinary parameters, type parameters, returns, values, and exceptions remain optional.

Optional XML documentation that is present is checked for structural consistency. Explicit prose in summaries, parameters, type parameters, returns, values, and exceptions must end in `.`, `?`, or `!`. Examples and custom elements are not independently sentence-checked. Deterministic structure and punctuation rules are separate from semantic summary-quality review.

The canonical coding-agent workflow is:

```text
implement/change code
-> run relevant tests
-> hygiene normalize
-> hygiene check
-> resolve deterministic findings and semantic review work
-> rerun tests/check as needed
```

`hygiene format [paths...]` applies enabled format-remediation rules and remains presentation-only. `hygiene normalize [paths...]` applies enabled semantics-preserving normalization rules, validates project compilation before and after, and applies enabled format rules to changed documents. The canonical rule catalogue owns these policies; `hygiene rules` and rewrite results expose capability participation. Both commands support repository-default, explicit file/directory, and `--changed` targets; explicit paths cannot be combined with `--changed`. Both support non-mutating `--check` and text/JSON output. Rewrite plans cover the selected set before mutation, commits roll back on failure, and repeated runs are idempotent. A successful `--check` means the command succeeded even when paths would change.

Commands select enabled rule capabilities: disabling an unrelated diagnostic rule does not disable normalization, and disabling a rewrite rule prevents its own transformation. Findings remain deterministic and can be explained or explicitly ignored. Semantic review remains caller-judged; uncertain or materially negative sample answers can be escalated with `hygiene review expand` or written as a durable handoff with `hygiene review handoff`, locally or to an explicit external file. The CLI invokes no model.

M0003 review workflow:

```text
hygiene check
hygiene review expand <batch-handle>
hygiene review handoff <batch-handle> [--file <path>]
```

`hygiene check` reports deterministic findings and one semantic review batch for each enabled semantic review rule. `docs.summary.quality.review` samples up to five valid, non-empty XML summaries for technical correctness, information value, and clarity/scope. `docs.summary.language.german.review` independently reviews whether summaries are natural, comprehensible German. Each rule is enabled or disabled as a whole; neither has language or other parameters. This English repository disables only the German-language rule in `.hygiene/config.json`.

The implementation agent reviews each normal sample against that rule's published questions. Confidently acceptable answers need no further action. If an answer materially fails or the implementation agent is uncertain, explicitly expand that batch and hand the complete eligible population to a frontier-capability reviewer:

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
