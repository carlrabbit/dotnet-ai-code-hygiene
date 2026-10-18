# Public Documentation

At M0005 completion, README/public docs must explain the supported-profile lifecycle:

```text
hygiene bootstrap
hygiene update
hygiene check
```

Document:

- the supported profile is `dotnet-11` v1;
- normal checking requires a current profile marker;
- bootstrap/update are repository-wide and idempotent;
- profile-managed artifacts/sections and their ownership boundary;
- `AnalysisLevel=11`, built-in analyzer enablement, and code-style-in-build;
- braces and explicit accessibility are enforced as analyzer errors;
- global warnings-as-errors repository configuration is prohibited;
- other diagnostics remain at platform/analyzer defaults from the hygiene profile;
- StyleCop is prohibited and not automatically removed;
- mandatory profile findings cannot be disabled/ignored;
- no arbitrary external analyzer compatibility is promised.

Documentation policy must state:

- only a generic documentation summary is required;
- "summary" is semantic, not `<summary>`-tag-only;
- positional record property summaries use matching record `<param>` elements;
- direct `<inheritdoc/>` satisfies missing summary without recursive parent inspection;
- ordinary params/typeparams/returns/etc. remain optional;
- optional documentation present must be structurally consistent;
- selected explicit prose must end with `.`, `?`, or `!`;
- `<example>` and other non-governed/custom tags are not forbidden;
- semantic summary-quality review remains separate from deterministic structure/punctuation checks.

Public docs must also retain installed-tool/format/normalize usage and semantic-review workflow. M0010 documents rule-owned remediation and EditorConfig projection, including the profile v1-to-v2 migration and whole-rule toggles. M0011–M0015 remain documented as historical decisions and evidence. Current M0016-facing docs describe gate admission, bounded risk-stratified selection, explicit accepted observations, full-population expansion/handoff, BORINGness planner escalation, legacy-state separation, and the no-model/provider boundary. They make no defect-rate, population-coverage, or defect-free acceptance claim.

Do not claim support for StyleCop, third-party async analyzers, non-.NET languages, or arbitrary analyzer stacks.

## M0018 public interface

The current release is package `DotNetAiCodeHygiene.Tool` version `0.10.0`, distributed with `ToolCommandName=dotnet-hygiene`; the only supported public entrypoint is `dotnet hygiene`. README onboarding is organized around First Bootstrap, Run on Gates, Manual Inspection and Debugging, and Updating to a New Release. The no-argument workflow owns bootstrap and supported profile reconciliation, creates a report/snapshot-bound approval-required baseline, detects a safe local change gate, and presents selected-only grouped review work. Documentation must explain the report hash approval command, fail-closed feature-base behavior, ancestor local-manifest updates, trusted out-of-process package update steps, current formatter allowlists, and terminal status meanings. `--yolo-all` is deferred to M0019; do not present `--review-all` as exhaustive.

There is no pre-1.0 compatibility requirement for the legacy bare `hygiene` command, CLI layouts, JSON/state formats, or experimental internal interfaces. Keep source and user configuration preservation as correctness requirements. No public feed publication may be implied unless it has actually occurred.
