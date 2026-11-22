# Base Setup Guide

This base guide defines the minimum repository documentation model shared by most projects.

It is not copied into product repositories.

Required base repository docs:

```text
README.md
AGENTS.md
docs/ENGINEERING.md
```

Add other project-authority docs only when selected profiles, maturity stage, repository role, or concrete project authority needs require them.

Optionally add `docs/PROJECT-GUIDANCE.md` when the project has durable agentic-development meta objectives or local planning methodology worth preserving. Optionally add `.guide/profiles/<id>/PROFILE.md` when a project-authored engineering shape needs a registered local profile; record its `local:<id>` selection in `.guide-profile.json`. Neither layer is default scaffolding or a substitute for concrete engineering policy.

Add `docs/research/` and an optional `docs/RESEARCH.md` discovery surface only when durable planning evidence actually exists and is materially useful to future planning. Do not create research scaffolding by default.

Product repositories contain localized project truth and concrete specializations, may retain selective non-authoritative planning research, and vendor **one pinned, unmodified upstream guide snapshot** at `.guide/system/` for self-contained planning. Do not copy shared guide documents into other project documentation or research.
