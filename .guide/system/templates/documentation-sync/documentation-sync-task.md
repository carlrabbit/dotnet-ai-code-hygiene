# Documentation Sync Task

## Purpose

Normalize repository documentation after implementation or migration work.

## Inputs

- selected guide profiles from `.guide-profile.json`
- recent milestone(s)
- changed files
- repository docs

## Rules

- Do not duplicate guide documents in project-owned documentation; the pinned `.guide/system/` tree is installed and updated only by the guide-version workflow.
- Do not add external guide documents to implementation-agent required reading.
- Localize rules into repository docs.
- Prefer deleting stale meta-process references over preserving them.

## Review areas

- README
- AGENTS
- terminology
- specs
- architecture
- decisions
- engineering
- milestones
- public docs, if active
