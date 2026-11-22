# Engineering

This document contains project engineering truth. Fill only sections that are active for this repository.

## Test strategy

State the repository's actual strategy when it is a material project rule, for example:

```text
integration-first
balanced
unit-focused
scenario/artifact-first
```

Do not inherit a generic test pyramid implicitly.

## Validation intent, cost and investigation

When material to this project, distinguish focused development feedback, investigative simulations/probes, milestone integration checks, and release qualification. Avoid mandatory gate matrices for ordinary milestones. Investigations are excluded from normal test discovery, canonical validation and CI unless expressly required. Define concrete experiment entry points separately.

For genuinely expensive suites only, consider independent stage commands and documented safe reuse/invalidation. Receipt fingerprints and fast verification are optional mechanisms, not an expectation. Measure stage and setup duration; avoid unnecessary repeated restore, process and fixture startup without breaking test isolation. Cheap suites may use a single direct command.

Investigative findings may be discarded, retained as non-authoritative research when valuable, or deliberately promoted into bounded deterministic regression checks.

## Validation topology

Only for integration boundaries where the detail materially changes execution, record the applicable:

| Depth | Target | Locus | Platform/capability | Command | Evidence |
|---|---|---|---|---|---|
| <Tier> | <real boundary> | <local/CI/remote/mixed> | <requirements> | <command> | <evidence> |

Integration depth does not imply CI execution.

Installed/package-consumer and broad compatibility qualification should normally be planned at release readiness, not imposed on each milestone.

## Project-specific integration targets

When correctness depends on an external runtime, installed application, service, real database, browser, native subsystem, or similar target, define or reference the project-local specialization needed by implementation and validation.

Resolve as applicable:

- target and supported version/range;
- availability and required platform/capabilities;
- provisioning/connection;
- isolation and reset/cleanup;
- identity/secrets/permissions;
- repository command used to invoke validation;
- expected evidence;
- infrastructure versus product failure semantics;
- fallback validation when the target is unavailable;
- release and consumer-surface relationship.

Use a focused `docs/engineering/<topic>.md`, spec, architecture document, or decision record when this becomes too detailed for this file. Do not duplicate the pinned guide's specialization methodology in project-owned engineering docs; `.guide/system/` is the separate, versioned methodology snapshot.

## Research boundary

If the repository uses `docs/research/`, treat it as non-authoritative planning knowledge.

Research may preserve evidence, experiments, provenance, uncertainty, comparisons, and revalidation triggers. Operative engineering rules must live here or in another appropriate project-authority document.

Ordinary implementation must not need to infer an engineering rule from research. If a planning conclusion changes implementation, validation, compatibility, supported environments, or another durable engineering contract, promote that conclusion into project authority before implementation depends on it.

Do not use research as an archive for planning transcripts or copied guide methodology.

## Human review

Ordinary engineering review can take place through existing PR discussion. If a milestone explicitly requires human acceptance, record the subject, relevant evidence, real reviewer, and outcome in that PR or a concise milestone decision record. Do not require a `.review/` directory or new command host by default.

If this project deliberately adopts the **advanced structured review subsystem**, document its repository-local records, supported `review-*` commands, and actual completion behavior here. Only then expose the relevant launchers, scoped to the active milestone. Completed decisions are historical; they are not reapproved after unrelated changes.
