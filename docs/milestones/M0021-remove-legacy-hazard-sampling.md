# M0021 — Remove Legacy Hazard/Age Sampling

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-human-reviewed |
| Guide system | Agentic Project Guides 0.9.1, base + cli-tool |
| Baseline implementation model | project-established implementation agent |
| Baseline executor readiness | confirmed |
| Decision preservation | confirmed |
| Execution tractability | confirmed |
| Execution ledger | `.execution/M0021-remove-legacy-hazard-sampling.md` planning-seeded |
| Scope size | small, focused removal |
| Implementation autonomy | high; local mechanics only |
| Documentation sync | directly contradicted authority only |
| Focused validation | current Core/CLI gate-review and publication cases |
| Repository validation | source-reference audit, Release build, affected stage receipt checks |
| Integration validation | real Git/review state and selected CLI tests |
| Validation locus/platform | Windows 11 + .NET 11 SDK + Git + PowerShell; no CI substitution |
| Environment authority | existing SDK/PowerShell/Git/NuGet; isolated project/tool paths only; no host provisioning or CI changes |
| Consumer/release validation | representative exact-current packed `dotnet hygiene` installed consumer; no mandatory cold historical full matrix |
| Human review | required and blocking; independent project owner/delegate |
| Baseline repository | `main` `6a59bce64c172d0d4d69b355c22a82efaff67328`; reconcile newer commits |

## Goal

Eliminate the abandoned hazard/age/activity sampling research implementation, its tests and its runtime seams so the supported codebase reflects only the production deterministic gate-local review model. No users or deployed installations exist; compatibility with never-deployed sampler state is explicitly not a requirement.

## Target State

The repository builds and tests its current hygiene behaviors without compiling historical sampling engines, historical persistence/migration logic or test scaffolding. The six semantic-review rules and current gate-local review/acceptance remain intact. Operative authority describes only the supported model; historical research remains readable.

## Scope

- Delete three legacy sampling source files and `SamplingTests.cs`; remove remaining obsolete plumbing and identified legacy-only lifecycle tests/fixtures.
- Simplify affected current tests while preserving real current behavior and atomicity guarantees.
- Correct narrowly affected specification and engineering authority; preserve research/history.
- Validate retained gate-local behavior and exact current installed artifact proportionately.

## Non-goals

- Redesign deterministic gate-local selection or rule architecture.
- Reorganize the test suite, add test tags/feature-selection system, restructure M0020 validator, or optimize test runtime beyond removing obsolete code.
- Implement M0019 `--yolo-all`, change supported rules or rewrite policy, alter review acceptance semantics.
- Add legacy archive tests, migrations, compatibility shims, deployed-user preservation, new CI workflows or external provisioning.
- Rewrite completed research, historical milestone evidence or milestone ledgers.

## Decisions and Constraints

- The abandoned research engine is exactly `Sampling/ProjectActivity.cs`, `Sampling/SamplingMechanics.cs`, `Sampling/SamplingSession.cs`, together with consumers of its types and state; current `Rules/SemanticReview/GateLocalReview.cs` is **not** legacy.
- Current `GateTicket` and `GateLocalReviewStore` are normative and must remain. Remove only old subject/population tickets and their unreachable conditional branches. Change internal representations freely: no deployed consumer/backward compatibility contract exists.
- The old `.hygiene/.state/sampling.json` is not a supported persisted state; do not read, migrate, upgrade or explicitly protect it. Do not introduce destructive cleanup of arbitrary files. Preserve `.hygiene/.state/gate-reviews.json`, latest-run semantics and safe atomic writes.
- Preserve the six independent bounded semantic-review rule contracts, Git gate identity, and positive-only explicit acceptance.
- Historical `docs/research/`, earlier milestones, reports and recorded experiments remain as immutable history. Active specifications must not imply that historical sampler preservation is still product policy.
- Follow M0020's staged validator. Run affected checks during development and prove required evidence at completion using valid current receipts; an unconditional fresh full Tier 4 matrix is **not** required by this removal milestone. Verify relevant source/compiled/consumer changes invalidate appropriate evidence. A representative installed-tool command remains required.

## Baseline Executor Readiness

No unsettled architectural or compatibility choice remains. The executor can use ordinary reference searches and compilation to remove residual dependencies and preserve active tests. Source-level assessment identifies old sampler plumbing in `RuleExecution.cs` and `HygieneEngine.cs` and mixed tests in `LifecycleTests.cs`; precise edits are implementation-owned.

## Decision Preservation

Do not interpret the word 'sampling' itself as legacy: gate-local sampling remains current. No users/deployments means no migration or legacy-state compatibility, but it does not authorize deleting unknown user files. Do not preserve dormant source simply because historical tests mention it.

## Execution Tractability

Each distinct outcome has an `AC-*`, `DOC-*` or `REV-*` ID below. Distinct success/failure and product/consumer boundaries use `EC-*` cases. The planning-seeded ledger reproduces their exact texts and validation gates, without assigning implementation tasks or claims.

## Required Authority

- `AGENTS.md`; `docs/ENGINEERING.md`.
- `docs/specs/SAMPLING.md`; `docs/specs/SEMANTIC-REVIEWS.md`; `docs/specs/REVIEW-HANDOFFS.md`.
- `docs/specs/AUTONOMOUS-WORKFLOW.md`; `docs/specs/AGENT-HELP.md` where current ticket or CLI contracts apply.
- M0016, M0018, M0020 milestone contracts for historical decisions only when needed; they are not authorization to retain superseded mechanisms.

## Acceptance Criteria

- **AC-01** — Delete obsolete hazard/age sampling implementation from `Sampling/SamplingMechanics.cs`, `Sampling/SamplingSession.cs` and `Sampling/ProjectActivity.cs`; no corresponding production types, schema handlers, thresholds, observations, epochs or activity-age accounting remain compiled.
- **AC-02** — Remove the historical sampling integration from `RuleContext`, `HygieneEngine` and review-subject/ticket association types, including deferred session creation, commit/discard boundary, prepared sampling transaction, old subject/population ticket fields and obsolete test-only injection seams; retain independent current run-publication atomicity.
- **AC-03** — Preserve the production gate-local selector, its state (`gate-reviews.json`), stable identities, independent bounded per-rule selections, ticket revalidation, accepted observations and current review handoff semantics unchanged.
- **AC-04** — Remove `SamplingTests.cs` and the three directly historical `LifecycleTests` methods (`M0012LinkedSourcesAreDeduplicatedAndMultiTargetProjectsShareActivityIdentity`, `ProductionSemanticReviewsLeaveLegacyHazardAndActivityStateUntouched`, `ExpandIgnoreValidationAndIgnoreListingDiscardSamplingMutations`), including orphaned `SamplingCursorRule` fixture and unused imports/helpers.
- **AC-05** — Retain all current behavioral coverage while deleting obsolete fixture setup/assertions from `CheckPublicationFailurePreservesLatestRunAndLegacySamplingState`, `SemanticReviewAcceptanceConsumesOnlyValidatedCurrentTicketsAtomically` and `BoringnessIncludesNestedTypeDeclarationsAndExcludesEnumsAndDelegates`; rename affected tests to describe only current contracts where appropriate.
- **AC-06** — Remove obsolete historical compatibility requirements for `.hygiene/.state/sampling.json`, hazard/age state retention, state version upgrades and historical sampler tickets. No migration, archive-test project, backward-compatibility adapter, or cleanup command for undeployed state is required; do not delete arbitrary user files.
- **AC-07** — Compile successfully with no production or test references to removed hazard/activity sampler types or filenames and no dead `using` directives, constructors, overloads, commit-path branches or stale check/review object shapes.
- **AC-08** — Keep rule catalog, active hygiene rules, authoritative rewrites, repository profile and autonomous `dotnet hygiene` behavior unchanged, with ordinary source checks and semantic gate selection still working.
- **AC-09** — Keep current transactional behavior for latest-run publication, ignored findings, gate-review acceptance and mutation rollback; deleting historical sampler choreography must not introduce partial writes or stale-ticket acceptance.
- **AC-10** — Demonstrate the active Core and CLI review regression paths at the declared Windows 11/.NET 11 locus; use affected validation shards and current receipts rather than requiring unrelated historical suites to rerun without input invalidation.
- **AC-11** — Exercise the current locally packed `dotnet hygiene` artifact through an isolated installed-tool consumer for a representative current check/review command, with actual package/shim provenance; other historical update/migration matrices need not be freshly repeated solely because dead internal sampling code was removed.
- **DOC-01** — Update operative sampling, semantic-review and engineering documentation to describe exclusively current gate-local selection and delete legacy state-preservation promises; retain historical research, milestone records and their evidence untouched.
- **REV-01** — Prepare `.review/REV-M0021-COMPLETION.md` for independent project-owner/delegate completion review; do not assert approval or merge before review.

## Acceptance Evidence Topology

| ID | Parent obligation | Required evidence case | Why separate evidence is required |
|---|---|---|---|
| EC-02a | AC-02 | Current check publication succeeds and latest-run state is written without any old sampling session or prepared commit. | Success state path differs from failures. |
| EC-02b | AC-02 | Injected current latest-run publication failure retains previous latest-run and leaves no temporary/rollback debris. | Failure atomicity differs from success. |
| EC-03a | AC-03 | Gate-local changed-source/merge-base selection and bounded independent review batches remain stable. | Gate selection distinct from persistence. |
| EC-03b | AC-03 | Gate review acceptance persists only current valid tickets and rejects stale or mixed/invalid acceptance. | Acceptance/revalidation distinct from selection. |
| EC-05a | AC-05 | Current review acceptance and BORINGness population tests still prove their original active claims after removal of legacy fixture assertions. | Active behavior within mixed historical/current tests. |
| EC-06a | AC-06 | No code path loads, migrates, preserves as a compatibility contract or writes legacy sampling.json, while unrelated files are not deleted. | Obsolete compatibility boundary distinct from data safety. |
| EC-08a | AC-08 | Normal `dotnet hygiene` check/rules/review flow and active formatter/profile rules remain functional. | CLI orchestration distinct from internal engine compile. |
| EC-09a | AC-09 | Current gate-review state publication and rollback remain atomic under injected failure or concurrent-write conflict. | Current gate state is separate from latest-run state. |
| EC-11a | AC-11 | Freshly packed current tool installs in isolation and performs a real check/review-facing invocation, not an internal DLL-only test. | Consumer artifact boundary. |

## Validation

| ID | Depth | Target | Locus/platform | Command/check | Proves | Expected evidence |
|---|---|---|---|---|---|---|
| VAL-01 | Tier 0/1 | Source reference and compiled build | Local Windows 11 / .NET 11 SDK | Search production+test references to removed symbols/files; `dotnet build DotNetAiCodeHygiene.slnx -c Release` | AC-01, AC-04, AC-07 | No obsolete compiled references; build success |
| VAL-02 | Tier 1/3 | Current Core gates and atomicity fixtures | Local Windows 11 / .NET 11, Git | `eng/validate.ps1 -Shard solution`; focused Core tests by supported `-TestFilter`; then `-Shard core-tests` if required evidence stale | EC-02a, EC-02b, EC-03a, EC-03b, EC-05a, EC-09a | Named current behavioral cases pass; no historical sampler tests |
| VAL-03 | Tier 1/3 | CLI integration and current product behavior | Local Windows 11 / .NET 11, Git | `eng/validate.ps1 -Shard cli-tests` or focused CLI filter; explicit check of active rule and review flows | AC-08, AC-10, EC-08a | Named process and rule/regression results |
| VAL-04 | Tier 4 (representative) | Exact current packed/installed tool | Isolated local Windows 11 / .NET 11, NuGet tool path | `eng/validate.ps1 -Shard pack-current` plus an isolated install of that exact nupkg and representative current command invocation; use existing consumer script only when it offers suitably scoped checking | EC-11a | Package hash, isolated install, shim and invocation |
| VAL-05 | Tier 0/2 | Documentation and current evidence reconciliation | Local repository / Windows PowerShell | Review operative docs, removed compatibility claims and preserved research; `git diff --check`; current receipt verification for any claimed reused stages | AC-06, DOC-01, EC-06a | No contradictory live policy; no research deletion; traceable current receipts |
| VAL-06 | Tier 5 | Independent M0021 review | PR / project owner or delegate | Submit review packet and await independent decision | REV-01 | Pending reviewer decision, never self-approved |

## Human Review

Applicability: mandatory independent completion review.
Review class: milestone completion.
Canonical review ID: `REV-M0021-COMPLETION`.
Review subject: obsolete code/test elimination, retained production contracts, source and installed-tool evidence.
Reviewer role: project owner or explicitly delegated reviewer.
Waiver policy: none; a pending reviewer decision means `AWAITING HUMAN REVIEW`, not `done`.
Milestone completion command: repository has no generic review-check implementation; use the prepared `.review` packet and authorized human decision.

## Documentation Policy

`DOC-01` updates directly contradicted operative authority. Do not retrospectively rewrite research, earlier milestones or historical results.

## Completion Expectations

Reread the milestone and seeded ledger, derive bounded work packages, retain one row per obligation and distinct evidence case, implement and validate, and attach actual evidence. Before claiming software complete, confirm exact ID/text/parent correspondence and all live gate proofs, perform the mandatory completion audit and write compact durable evidence. Do not self-approve.

## Completion Evidence

Pending implementation and independent verification. Do not assert results before execution.

## Escalation Boundary

Escalate only if a genuinely active production rule depends on obsolete hazard/age algorithms in a way that would require redefining a settled active product contract, or if the declared Windows/installed consumer validation locus is unavailable without permitted remedy. Routine compile errors and stale test setup are implementer-owned corrections.
