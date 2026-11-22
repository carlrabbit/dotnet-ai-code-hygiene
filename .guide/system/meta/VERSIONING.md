# Versioning

## Status

Authoritative for guide-system versioning.

## Versioned artifacts

The following artifacts are versioned independently when useful:

- guide system;
- base guides;
- profiles;
- generic models;
- migrations;
- templates;
- prompt families.

## Semantic versioning

Use semantic versioning.

Until guide-system `1.0.0`, MINOR versions may intentionally include breaking model evolution when accompanied by explicit migration guidance. `0.x` repositories should not assume minor-version compatibility.

From the `1.0.0` stable release:

```text
MAJOR
  Breaking guide model change.
  Existing repositories likely need migration planning.

MINOR
  New optional guide module, profile, maturity stage, building block, prompt family, or guidance.
  Existing repositories do not require immediate migration unless they use the affected workflow.

PATCH
  Clarification, typo, example improvement, or non-semantic wording.
  No repository migration expected.
```

## Git tags and distribution

Guide-system versions are Git source snapshots. Tag the intended upstream commit with `vX.Y.Z` when stable; prereleases may use SemVer tags such as `v1.0.0-rc.1`. Historical draft snapshots are distinguished under `snapshot/` and are not stable adoption versions. A consuming repository records the tag **and peeled commit SHA** and vendors the complete tagged source under `.guide/system/`. The repository itself remains the upstream history source. Do not require NuGet/packages, GitHub Releases, a publication pipeline, or any automated cross-repository propagation; the Git tag is sufficient to identify a version. See [Repository-Local Guide Snapshot Model](LOCAL-GUIDE-SNAPSHOT-MODEL.md).

## Prereleases

Use SemVer prerelease identifiers such as `1.0.0-rc.1` while qualifying a new major methodology. A release candidate is not stable `1.0.0`; migration guidance must name the exact prerelease version. Stable `1.0.0` follows after candidate corrections and must not be implied by a version bump alone.

## Changelog rules

Each minor or major version entry should state:

- migration required: yes/no/conditional;
- affected areas;
- affected product repositories;
- required migration;
- conditional migration;
- deprecated concepts;
- manual review points.

This lets latest-version migration prompts inspect `CHANGELOG.md` and construct repository-specific migration plans.
