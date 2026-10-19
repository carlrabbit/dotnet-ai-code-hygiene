# Execution Ledger — M0022 Guide 0.9.3 Validation Policy Migration

Primary milestone: `docs/milestones/M0022-guide-093-validation-policy-migration.md`

Planning-seeded. Milestone is authority; ID, parent and obligation texts are immutable to implementation.

## Milestone Obligation Registry

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | Update `.guide-profile.json` to guide system and existing `base`/`cli-tool` profile versions 0.9.3; preserve schema version 2, scopes, role and other metadata. | WP-1 | Both guide version fields updated from 0.9.1 to 0.9.3; schemaVersion 2 and all other metadata preserved. | VAL-01: JSON parse and baseline metadata comparison passed. | done |
| AC-02 | acceptance | Route `AGENTS.md` milestone-first: ready milestone, planning-seeded execution ledger, referenced operative authority, then relevant source/tests; no ordinary implementer dependency on external guides. | WP-1 | AGENTS.md now routes ready milestone, seeded ledger, referenced authority, then source/tests; local authority retained. | VAL-02: routing order and external dependency inspected. | done |
| AC-03 | acceptance | Remove contradictory `AGENTS.md` reading order and overly rigid canonical-command restriction while preserving useful project-local instructions and truthful evidence reporting. | WP-1 | Removed canonical-only command restriction; retained repository gate commands, no invented commands and honest focused evidence safeguards. | VAL-02: command and safeguard semantics inspected. | done |
| AC-04 | acceptance | In `docs/ENGINEERING.md`, distinguish cheap affected development feedback, investigative simulation/profiling, and authoritative milestone/release acceptance without new tiers/profiles. | WP-2 | Engineering policy separately describes development feedback, investigative execution, milestone acceptance and full release qualification. | VAL-03: policy clause audit passed; no new tier/profile. | done |
| AC-05 | acceptance | Make milestone completion depend on fresh/current evidence for its *declared* acceptance gates, not universal complete-suite or historical consumer runs; focused checks do not masquerade as full-suite receipts. | WP-2 | Completion requires current evidence for declared gates; full-suite/consumer gates stay mandatory when declared; focused checks remain scoped. | VAL-03: milestone and focused-receipt semantics audited. | done |
| AC-06 | acceptance | Preserve M0020 canonical comprehensive release validation, independent stages, fingerprints, locks, fail-closed aggregate verifier and current prerequisite checks without rewriting or decomposing scripts. | WP-2/WP-3 | M0020 validation implementation was not edited; seven stages, fingerprints, lock and fail-closed verification inspected. | VAL-04: read-only source comparison against origin/main. | done |
| AC-07 | acceptance | State current release-candidate evidence must be refreshed when relevant inputs change, but trustworthy immutable prior-version fixtures and other unaffected evidence may be reused; stale/missing evidence fails closed. | WP-2 | Policy requires refreshing affected current candidate package/consumer evidence while allowing authenticated immutable fixture and unaffected receipt reuse. | VAL-03: current-candidate and immutable-fixture policy audited against M0020. | done |
| AC-08 | acceptance | Audit ordinary tests, canonical validator and legacy research script entrypoints: exploratory simulations must not accidentally be default regression/CI; do not delete independent historical research just to simplify the migration. | WP-3 | Research M0013/M0015 launchers remain standalone; ordinary Core/CLI projects and canonical stage list exclude historical simulations. | VAL-04: test project and entrypoint audit passed. | done |
| AC-09 | acceptance | Clarify execution versus provisioning versus infrastructure authority: existing Windows 11/.NET 11, Git, PowerShell and disposable isolated tools allowed; no new persistent host installs or CI/remote infrastructure authorized. | WP-2 | Engineering authority allows existing tools, restores and disposable paths; disallows persistent provisioning and CI/remote infrastructure changes. | VAL-03: permission boundary audited. | done |
| AC-10 | acceptance | Keep C# production code, current tests, CLI/product contracts, installed-consumer scripts, M0020 validator and completed milestone/research evidence unchanged; no full build or release validation for documentation-only migration. | WP-3 | No runtime/tests/CLI contract/consumer/M0020 implementation or historical evidence edited; no full suite run. | VAL-05: scope audit and git diff --check. | done |
| DOC-01 | documentation | Update only directly contradicted operative authority; no speculative `docs/PROJECT-GUIDANCE.md`, local-profile scaffolding or retrospective historical rewrites. | WP-1/WP-2 | Only directly contradicted profile/routing/engineering authority plus supplied M0022 evidence and review packet changed. | VAL-05: bounded file scope verified. | done |
| REV-01 | human review | Prepare independent reviewer packet at `.review/REV-M0022-COMPLETION.md`, leaving approval pending and PR unmerged. | WP-3 | Independent completion packet created with decision pending; PR will remain unmerged. | VAL-06: packet prepared; independent reviewer action pending. | pending |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-05a | AC-05 | Focused tests may prove specific milestone acceptance only when its required gate names those cases. | VAL-03 | Engineering policy limits focused proof to named gate cases; no focused test is claimed for this migration. | done |
| EC-05b | AC-05 | Full suite/consumer validation remains mandatory whenever a milestone specifically declares it. | VAL-03 | Engineering policy retains milestone-declared full-suite/consumer requirements. | done |
| EC-07a | AC-07 | Changed current-candidate output invalidates dependent current-package validation. | VAL-03 | M0020 current package and consumer stages are dependency/output coupled; policy requires refresh when relevant inputs change. | done |
| EC-07b | AC-07 | Unchanged authenticated immutable old-version fixture can be reused. | VAL-03 | Policy permits reuse only after pinned source/toolchain/package identity and content hash verification; stale/missing cache reconstructs. | done |

## Work Packages

- **WP-1 — Guide metadata and entrypoint routing:** AC-01–AC-03. Update the existing profile versions and revise stable AGENTS.md routing while preserving repository safeguards.
- **WP-2 — Validation authority policy:** AC-04–AC-07 and AC-09. Clarify development, investigation, milestone and release evidence; preserve M0020 as implemented.
- **WP-3 — Read-only audit and completion evidence:** AC-08, AC-10, DOC-01, REV-01. Audit test/research participation and scope, then record criterion evidence and prepare the pending review packet.

## Validation Gates

| ID | Required validation | Target/locus | Proves evidence units | Status | Evidence |
|---|---|---|---|---|---|
| VAL-01 | Validate JSON, exact guide/profile versions and unchanged other metadata | Repository checkout; local text/JSON tooling | AC-01 | done | ConvertFrom-Json and field-by-field comparison passed. |
| VAL-02 | Review AGENTS routing and permission semantics against ready milestone and ledger | Repository checkout; local text inspection | AC-02, AC-03 | done | Entry order, source authority, command flexibility and retained safeguards inspected. |
| VAL-03 | Clause-by-clause engineering-policy audit of intent, milestone gates, candidate vs fixture evidence and environment authority | Repository checkout; local text inspection | AC-04, EC-05a, EC-05b, EC-07a, EC-07b, AC-09 | done | Policy clauses match the registered cases; candidate dependencies and immutable fixture identity inspected. |
| VAL-04 | Read-only inspect M0020 stages and normal test/discovery entrypoints for accidental research simulation inclusion | Repository checkout; local source audit | AC-06, AC-08 | done | Seven stage definitions and normal test project source lists inspected; research entrypoints excluded. |
| VAL-05 | `git diff --check`; compare changed-file list and historical content to scope | Local Git checkout | AC-10, DOC-01 | done | git diff --check passed; changed paths confined to requested scope. |
| VAL-06 | Independent completion review of ledger evidence and change | Project owner/delegate review | REV-01 | pending | .review/REV-M0022-COMPLETION.md prepared with approval pending. |

## Resume Point

1. Read the milestone and seeded ledger, then inspect AGENTS.md, docs/ENGINEERING.md, docs/ARCHITECTURE.md, eng/validate.ps1, eng/ValidationStages.ps1, test project files and research launchers.
2. Reconcile registry: 12 obligations (AC-01–AC-10, DOC-01, REV-01); the package milestone, execution instructions and seeded ledger each declare four evidence cases (EC-05a, EC-05b, EC-07a, EC-07b), despite the user request count of five. No fifth planner-owned case or wording exists in the supplied package; do not invent or silently rewrite one. This count discrepancy is recorded as an unresolved reconciliation item for review.
3. Changed files: .guide-profile.json, AGENTS.md, docs/ENGINEERING.md, the supplied M0022 milestone and ledger, plus .review/REV-M0022-COMPLETION.md. No runtime, test, M0020 validation implementation, consumer script or historical evidence file changed.
4. VAL-01 through VAL-05 have current static/read-only/diff evidence recorded above. No .NET build, complete test suite, installed-tool lifecycle test or full M0020 qualification was run, consistent with metadata-only scope.
5. VAL-06/REV-01 remain pending independent reviewer action. Milestone status: **awaiting independent review**, with the requested five-versus-four evidence-case count discrepancy also disclosed.

## Final Reconciliation

- [x] Reread primary milestone and verify exact obligation-ID set and planner-owned wording (12 obligations).
- [x] Verify exact evidence case IDs and parentage: four seeded cases; user-requested count of five is inconsistent with all supplied planning artifacts and is disclosed, not fabricated.
- [x] Attach criterion-specific implementation and validation proof, rather than a general suite claim.
- [ ] Confirm each gate has applicable current evidence; VAL-06 is awaiting independent review.
- [x] Audit diff scope; run `git diff --check`.
- [x] Persist completion evidence in the milestone and leave independent review pending.
