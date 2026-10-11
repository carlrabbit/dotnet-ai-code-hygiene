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

Public docs must also retain installed-tool/format/normalize usage and semantic-review workflow. M0010 documents rule-owned remediation and EditorConfig projection, including the profile v1-to-v2 migration and whole-rule toggles. M0011 documents statistical selection, zero-due batches, explicit accepted observations, full-population expansion/handoff, BORINGness planner escalation, local sampler state, and the no-model/provider boundary. M0012 documents no-age summary sampling and project-scoped source-activity aging for BORINGness, without implying that its nominal scale has been experimentally validated.

Do not claim support for StyleCop, third-party async analyzers, non-.NET languages, or arbitrary analyzer stacks.
