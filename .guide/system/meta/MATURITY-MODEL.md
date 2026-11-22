# Maturity Model

Maturity stages prevent premature documentation, release, and validation obligations.

| Stage | Meaning |
|---|---|
| `exploration` | Ideas, spikes, research, unstable design. |
| `design-ready` | Terminology, specs, and key decisions are normalized enough for implementation planning. |
| `implementation-ready` | Milestones and validation strategy are clear enough for implementation. |
| `integration-ready` | Representative integration validation protects important real system boundaries. |
| `public-preview` | External-facing docs, samples, and compatibility expectations are emerging. |
| `release-ready` | Public API, packages, docs, samples, and release validation are complete enough for release. |
| `published-maintenance` | Compatibility, migration, deprecation, and release discipline dominate. |

Integration readiness does not require CI. Required integration validation may run locally, in CI, against remote services, or through a mixed topology according to project authority.

Do not apply release-ready obligations to exploration or implementation-ready repositories.
