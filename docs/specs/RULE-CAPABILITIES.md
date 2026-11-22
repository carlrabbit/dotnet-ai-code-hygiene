# Hygiene Rule Capabilities

`dotnet hygiene` owns source-backed findings and semantic-review workflow. It does not own source formatting or modernization rewrites. The independent `dotnet autoformat` executable owns mechanical `.editorconfig` policy, SDK formatting, and opinionated C# canonicalization. See [Autoformat rewrite behavior](REWRITES.md), [readability policy](READABILITY.md), and [M0027](../milestones/M0027-opinionated-autoformat-canonicalization.md) for those contracts.

## Hygiene rules

Hygiene's static rule catalog contains active deterministic diagnostics and semantic-review candidate signals. Diagnostic rules produce source-backed findings. Review rules select bounded obligations for explicit human or agent judgment; a candidate signal is not a defect verdict. Rule questions, legitimate cases, evidence limits, selection, and acceptance behavior are defined in [Semantic Reviews](SEMANTIC-REVIEWS.md) and [Sampling](SAMPLING.md).

`dotnet hygiene rules` reports the currently registered Hygiene rules and their capabilities. `check` evaluates diagnostic rules and selects eligible review obligations according to the requested scope. Review acceptance is explicit and applies only to the selected rule, subject, and current evidence. Hygiene does not invoke a model/provider or automatically apply semantic corrections.

## Profile and configuration ownership

Hygiene profile commands maintain Hygiene's analysis and StyleCop profile infrastructure and its review state. Autoformat initializes and maintains its own root mechanical `.editorconfig` block through its normal policy service. Hygiene does not project per-rule formatter settings, adopt a Hygiene-owned formatter block, or use Hygiene toggles to control Autoformat transformations.

Hygiene rule configuration applies only to active Hygiene rules. Retired pre-M0027 mechanical formatter identifiers and projections are not runtime contracts and do not require migration support. User-authored content outside the files or sections explicitly owned by a command remains preserved.

## Reporting

Hygiene reports findings, review batches, selected rule identities, and source paths from the actual Hygiene execution. Autoformat separately reports its formatter phases, canonicalization identities, changed paths, and notices. Neither tool's output implies that the other tool ran.
