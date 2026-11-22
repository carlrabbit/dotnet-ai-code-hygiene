# 0025 — Separate Historical Research Adoption from Guide Compliance

## Status

Accepted.

## Context

Guide-system 0.8 introduces a first-class non-authoritative research layer for durable planning knowledge.

Repositories created or planned before that model may still have valuable historical planning evidence in chats, uploaded documents, notes, experiments, issue/PR history, or other external material.

Requiring every repository to reconstruct that history during migration would make guide adoption depend on evidence that may be unavailable, low-value, stale, or expensive to curate.

Ignoring historical material entirely would lose an opportunity to preserve costly planning knowledge when it is still available and likely to matter again.

## Decision

Treat historical research adoption as a reusable planning/research workflow that is independent of required guide-system compliance.

Research adoption uses one of three modes for a chosen scope:

```text
none
selective
reconstructive
```

The mode is chosen from expected planning value and available evidence, including rediscovery cost, evidence quality, project maturity and expected change, unusual/fragile constraints, and revalidation cost.

Do not derive the mode mechanically from maturity.

Historical chats/files are source material. They are curated into bounded research findings/evidence when worth retaining; they are not copied wholesale into `docs/research/`.

Implementation-affecting conclusions recovered during adoption are promoted into normal project authority.

The selected adoption mode is workflow history, not stable `.guide-profile.json` configuration.

When a migration first introduces the research model, it explicitly evaluates historical research adoption. `none` remains a valid result and unavailable historical evidence does not block guide migration.

Small selective adoption may be embedded in project creation, guide adoption, or version migration. Substantial reconstructive adoption should normally use the dedicated `templates/prompts/special/adopt-research.md` workflow and may occur after guide migration completes.

## Consequences

- Repositories can adopt 0.8 without reconstructing unavailable or low-value planning history.
- Valuable old chats/files can still be converted into curated planning knowledge when worthwhile.
- Guide compliance and historical research completeness remain separate concepts.
- Migration prompts remain delta-driven instead of becoming broad historical-archaeology workflows by default.
- `.guide-profile.json` remains stable coordination metadata rather than accumulating one-time research-adoption state.
- Ordinary implementation remains isolated from planning research and consumes promoted project authority instead.
