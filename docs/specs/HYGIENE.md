# Hygiene Behavior

## Rule model

A rule ID denotes one fixed semantic contract with no repository-supplied parameters. Optional-rule configuration means whole-rule enable/disable only. It cannot set language, thresholds, scope, severity, review questions, sampling, conventions, remediation, or modes. Independently applicable policies use separate rule IDs instead of parameter bags or bundled semantics. Rule-set membership never changes a rule's meaning.

M0005 distinguishes:

```text
mandatory profile rules
configurable hygiene/review rules
```

Mandatory profile rules cannot be disabled or ignored.

Configurable rules retain the existing `.hygiene/config.json` `disabledRules` model.

Canonical normal rule order and documentation rule semantics are defined in:

```text
docs/specs/DOCUMENTATION.md and docs/specs/SEMANTIC-REVIEWS.md
```

Profile rule semantics and bootstrap/update sets are defined in:

```text
docs/specs/PROFILE.md
```

## Check prerequisite

Beginning with M0005, normal `hygiene check` requires the current supported hygiene profile marker.

Missing/unsupported profile state returns exit `3` with bootstrap/update guidance rather than silently selecting an unrecorded profile.

## Check result ordering

Enabled/applicable rules execute in canonical rule order.

Reported findings remain ordered by:

```text
rule order
-> repository-relative path ordinal
-> source span start
-> deterministic local discriminator
```

Finding IDs are assigned only after final ordering.

Profile findings participate in normal ordering but cannot be ignored.

## Check output and exits

Existing text/JSON `hygiene check` schema/version and finding/review-batch separation remain compatible unless the M0005 milestone explicitly requires an additive public field.

Findings, including mandatory profile findings, do not by themselves change successful exit code `0`.

Malformed profile/config/state and unsupported profile versions use product exit `3`.

## Explain

`hygiene explain` continues to operate on latest-run findings. Mandatory profile findings can be explained normally even though `ignore` rejects them.

## Rules/configuration

`hygiene rules` exposes at least:

- rule ID/version;
- output kind/classification;
- whether the rule is configurable.

`enable`/`disable` remain idempotent for configurable rules.

Attempting to disable/enable a mandatory profile rule as if it were configurable returns exit `3`.

A persisted `disabledRules` entry naming a mandatory profile rule is invalid product configuration.

## Ignore behavior

Existing finding ignore/stale behavior remains for configurable deterministic findings.

`ignore` rejects a mandatory profile finding with exit `3`.

Rule-version matching remains part of occurrence identity. `docs.summary.required` changes from v1 to v2 in M0005, so old v1 ignore decisions are not silently treated as v2 ignores.

## Target behavior

Existing M0002/M0004 repository discovery, project-aware Roslyn context, target de-duplication, broad exclusions, explicit target containment, and `--changed` behavior remain unchanged for normal source/rewrite commands.

Bootstrap/update are repository-wide profile commands and do not accept source targets or `--changed`.

Readability rule contracts are defined in `docs/specs/READABILITY.md`.

## Persistence

Committed product state includes:

```text
.hygiene/config.json
.hygiene/decisions.json
.hygiene/profile.json
```

Namespaced profile-generated artifacts live under:

```text
.hygiene/profile/
```

Existing local latest-run state remains under `.hygiene/.state/` and Git-ignored.

Review handoff artifacts remain `.hygiene/reviews/` and are not profile state.
