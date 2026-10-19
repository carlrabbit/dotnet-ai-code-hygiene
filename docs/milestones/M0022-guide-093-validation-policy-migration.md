# M0022 — Guide 0.9.3 Validation Policy Migration

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Baseline executor readiness | confirmed |
| Decision preservation | confirmed |
| Execution tractability | confirmed |
| Guide | Agentic Project Guides 0.9.3; base + cli-tool |
| Execution ledger | `.execution/M0022-guide-093-validation-policy-migration.md` planning-seeded |
| Scope size | small, documentation and metadata only |
| Implementation autonomy | high within fixed boundary |
| Documentation sync | directly contradicted authority only |
| Focused validation | static metadata/routing/policy audit |
| Repository validation | read-only validator entrypoint audit; git diff --check |
| Integration validation | not applicable: no runtime changes |
| Validation locus/platform | local repo checkout, Git and text/JSON tools; established runtime locus Windows 11/.NET 11/PowerShell remains unchanged |
| Environment authority | existing commands and disposable isolated project/tool paths only; no persistent host provisioning or new CI/remote infrastructure |
| Consumer/release validation | not applicable for metadata-only migration; preserve full release qualification |
| Human review | independent owner/delegate, blocking |
| Baseline repository | main `f5a47f250198d20e0ca2bd816224b59c4354c0f2`; reconcile newer commits |

## Goal

Adopt guide 0.9.3 and codify proportionate validation without rebuilding M0020. Long release qualification is acceptable; unnecessary repetition is not. Keep current proven resume/fingerprint implementation intact.

## Target State

Guide metadata selects 0.9.3. Agents follow milestone-first repository-local authority. Milestone closure verifies declared evidence only; comprehensive, safely resumable release qualification stays available as the separate full validator.

## Scope

Metadata, AGENTS entrypoint, operative engineering validation policy, read-only audit of investigative script participation, completion evidence and independent review packet.

## Non-goals

Runtime changes; C# tests or their organization; new test selection, CI, caching, profiles, project-guidance scaffolding; rewrite/decomposition of `eng/validate.ps1`, `eng/ValidationStages.ps1` or installed-consumer suites; edits to completed research, milestones, ledgers or past receipts; forced cold full suite.

## Decisions and Constraints

- 0.9.1→0.9.2 introduces optional project guidance/local profiles: neither is needed here. Schema 2 and `base + cli-tool` remain valid.
- 0.9.3 is identified as draft at planning time; if a final authoritative release differs materially, return for planning resolution.
- `AGENTS.md` is routing, not another substantive implementer manual. Implementers need only the milestone, ledger and referenced local authority, never external guide repository.
- Development feedback is provisional and focused. Investigative simulations/profiling are separate and not default regression. Milestone acceptance is the set of explicitly declared gates, current and evidenced; it is not automatically the entire historical suite.
- Long comprehensive release validation is acceptable. Reuse independent successful stages when input, command, toolchain, dependency, artifact and relevant environment provenance proves freshness. Stale/unknown evidence must be rerun; final current artifact consumer evidence cannot be inherited incorrectly.
- Keep M0020's existing seven-stage engine and immutable prior-version fixture; do not split the installed-consumer stage merely for speed.
- Distinguish permission to execute existing tools and restore project dependencies from permission to provision persistent host software or modify CI/remote infrastructure. Neither latter permission is granted.
- No product or distributable artifact surface is changed, so no full suite/installed artifact test is required to establish *this* milestone.

## Baseline Executor Readiness

Material scope, non-goals, evidence and environment authority are resolved. Implementation chooses exact local edits and work packages only.

## Decision Preservation

Do not replace the critical distinction (declared milestone gates versus full release qualification) with a generic instruction to "run validation." M0020 remains authoritative for the full release path.

## Execution Tractability

One coherent migration. The ledger below seeds each obligation and required evidence gate; implementation owns concrete work packages, evidence and completion audit.

## Required Authority

- `.guide-profile.json`
- `AGENTS.md`
- `docs/ENGINEERING.md`
- `docs/ARCHITECTURE.md` (read-only system boundaries)
- `eng/validate.ps1` and `eng/ValidationStages.ps1` (read-only validation implementation)
- Guide migration chain: `carlrabbit/agentic-project-guides/migrations/guide-system-v0.9.1-to-v0.9.2.md`, `.../guide-system-v0.9.2-to-v0.9.3.md` (planning provenance; implementation-facing decisions are completely preserved here)

## Acceptance Criteria

- **AC-01** — Update `.guide-profile.json` to guide system and existing `base`/`cli-tool` profile versions 0.9.3; preserve schema version 2, scopes, role and other metadata.
- **AC-02** — Route `AGENTS.md` milestone-first: ready milestone, planning-seeded execution ledger, referenced operative authority, then relevant source/tests; no ordinary implementer dependency on external guides.
- **AC-03** — Remove contradictory `AGENTS.md` reading order and overly rigid canonical-command restriction while preserving useful project-local instructions and truthful evidence reporting.
- **AC-04** — In `docs/ENGINEERING.md`, distinguish cheap affected development feedback, investigative simulation/profiling, and authoritative milestone/release acceptance without new tiers/profiles.
- **AC-05** — Make milestone completion depend on fresh/current evidence for its *declared* acceptance gates, not universal complete-suite or historical consumer runs; focused checks do not masquerade as full-suite receipts.
- **AC-06** — Preserve M0020 canonical comprehensive release validation, independent stages, fingerprints, locks, fail-closed aggregate verifier and current prerequisite checks without rewriting or decomposing scripts.
- **AC-07** — State current release-candidate evidence must be refreshed when relevant inputs change, but trustworthy immutable prior-version fixtures and other unaffected evidence may be reused; stale/missing evidence fails closed.
- **AC-08** — Audit ordinary tests, canonical validator and legacy research script entrypoints: exploratory simulations must not accidentally be default regression/CI; do not delete independent historical research just to simplify the migration.
- **AC-09** — Clarify execution versus provisioning versus infrastructure authority: existing Windows 11/.NET 11, Git, PowerShell and disposable isolated tools allowed; no new persistent host installs or CI/remote infrastructure authorized.
- **AC-10** — Keep C# production code, current tests, CLI/product contracts, installed-consumer scripts, M0020 validator and completed milestone/research evidence unchanged; no full build or release validation for documentation-only migration.

## Acceptance Evidence Topology

| ID | Parent obligation | Required evidence case | Why separate |
|---|---|---|---|
| EC-05a | AC-05 | Focused tests may prove specific milestone acceptance only when its required gate names those cases. | Distinct from full-suite evidence. |
| EC-05b | AC-05 | Full suite/consumer validation remains mandatory whenever a milestone specifically declares it. | Prevents loss of required consumer assurance. |
| EC-07a | AC-07 | Changed current-candidate output invalidates dependent current-package validation. | Candidate-sensitive identity. |
| EC-07b | AC-07 | Unchanged authenticated immutable old-version fixture can be reused. | Independent immutable input. |

## Validation

| ID | Depth / command or check | Target/locus | Proves | Expected evidence |
|---|---|---|---|---|
| VAL-01 | Tier 0/5; Validate JSON, exact guide/profile versions and unchanged other metadata | Repository checkout; local text/JSON tooling | AC-01 | Per-criterion audit findings and recorded proof |
| VAL-02 | Tier 0/5; Review AGENTS routing and permission semantics against ready milestone and ledger | Repository checkout; local text inspection | AC-02, AC-03 | Per-criterion audit findings and recorded proof |
| VAL-03 | Tier 0/5; Clause-by-clause engineering-policy audit of intent, milestone gates, candidate vs fixture evidence and environment authority | Repository checkout; local text inspection | AC-04, EC-05a, EC-05b, EC-07a, EC-07b, AC-09 | Per-criterion audit findings and recorded proof |
| VAL-04 | Tier 0/5; Read-only inspect M0020 stages and normal test/discovery entrypoints for accidental research simulation inclusion | Repository checkout; local source audit | AC-06, AC-08 | Per-criterion audit findings and recorded proof |
| VAL-05 | Tier 0/5; `git diff --check`; compare changed-file list and historical content to scope | Local Git checkout | AC-10, DOC-01 | Per-criterion audit findings and recorded proof |
| VAL-06 | Tier 0/5; Independent completion review of ledger evidence and change | Project owner/delegate review | REV-01 | Per-criterion audit findings and recorded proof |

## Human Review

Independent project-owner/delegate milestone-scoped review, canonical ID `REV-M0022-COMPLETION`. Must review exact ledger and evidence; no self-approval or automatic merger. Completion audit is the repository milestone/ledger reconciliation; do not assume a nonexistent `eng/review-check.sh` command.

## Documentation Policy

- **DOC-01** — Update only directly contradicted operative authority; no speculative `docs/PROJECT-GUIDANCE.md`, local-profile scaffolding or retrospective historical rewrites.

## Other Completion Obligations

- **REV-01** — Prepare independent reviewer packet at `.review/REV-M0022-COMPLETION.md`, leaving approval pending and PR unmerged.

## Completion Expectations

Reread milestone, verify exact obligation/case IDs and wording against ledger, trace each obligation to concrete repository change and validation evidence, verify all required gates, complete audit, write compact permanent proof below, and prepare pending independent review.

## Completion Evidence

Implementation evidence and gates below are reconciled against the planning-seeded ledger. VAL-01–VAL-05 have current evidence; VAL-06 remains pending independent review. The package defines four evidence cases, while the execution request said five; this discrepancy is preserved for reviewer resolution rather than creating a planner-owned case.

| Obligation/evidence case | Evidence | Gate | Result |
|---|---|---|---|
| AC-01 | `.guide-profile.json` JSON parses; guide and both existing profiles are 0.9.3; schema 2 and other fields preserved. | VAL-01 | done |
| AC-02 | `AGENTS.md` routes ready milestone, seeded ledger, referenced authority, then source/tests; local repository authority is sufficient. | VAL-02 | done |
| AC-03 | Removed canonical-only restriction while retaining declared gate commands, scope control and truthful focused-evidence reporting. | VAL-02 | done |
| AC-04 | Engineering authority distinguishes development feedback, investigative execution, milestone acceptance and full release qualification. | VAL-03 | done |
| AC-05 | Acceptance requires current evidence for declared gates; focused checks are scoped; named full-suite/consumer gates remain required. | VAL-03 | done |
| AC-06 | M0020 validator files and seven-stage model remain unchanged; stages, fingerprints, lock and verifier inspected. | VAL-04 | done |
| AC-07 | Changed candidate evidence is refreshed; authenticated immutable prior-version fixture and unaffected receipts can be reused; stale evidence fails closed. | VAL-03 | done |
| AC-08 | M0013/M0015 research entrypoints are outside ordinary test projects and canonical validator; historical research remains intact. | VAL-04 | done |
| AC-09 | Existing tools/restores/disposable paths are allowed; persistent host provisioning and CI/remote infrastructure changes are not authorized. | VAL-03 | done |
| AC-10 | No runtime, test, product contract, installed-consumer, M0020 validator or historical evidence changed; no full suite was required or run. | VAL-05 | done |
| EC-05a | Focused checks prove only named cases and no focused run is claimed as full-suite evidence. | VAL-03 | done |
| EC-05b | Milestones that declare full-suite/consumer validation still require it. | VAL-03 | done |
| EC-07a | Current-package and consumer-stage dependency/output provenance requires refresh when the candidate changes. | VAL-03 | done |
| EC-07b | Immutable old-version fixture reuse remains identity/hash checked with reconstruction for missing or changed cache. | VAL-03 | done |
| DOC-01 | Changes limited to directly contradicted authority, supplied M0022 planning artifacts and required pending review packet. | VAL-05 | done |
| REV-01 | Independent review packet prepared; approval pending and PR remains unmerged. | VAL-06 | pending |

## Escalation Boundary

Return to planning if 0.9.3 changes materially from the recorded contract, or if runtime/test/CI changes become necessary to deliver it.
