# REV-M0020-COMPLETION — Independent Review

## Decision

**Status: APPROVED**
Reviewer: Project owner (approval conveyed in this conversation)
Decision date: 2026-10-18

The project owner approved the milestone after reviewing the recorded implementation, validation, and registry evidence. Automated validation and a green receipt set alone are not approval.

## Review scope

Review the M0020 implementation against the exact acceptance and evidence-case registries in `docs/milestones/M0020-efficient-resumable-validation.md` and `.execution/M0020-efficient-resumable-validation.md`.

Please verify:

- all M0018 solution, Core, CLI, README/policy, package, installed-consumer, update/migration, local-manifest, rollback and repository self-host checks remain represented and were executed at the required Windows/.NET locus;
- stage fingerprints cover the relevant source, scripts, toolchain, prerequisites and output identities, and stale current-package evidence cannot pass verification;
- failed, interrupted, changed-input and overlapping runs cannot fabricate a valid receipt;
- the prior-version package cache is immutable, identity/content checked, reconstructible when absent, and isolated from current-package and consumer feed provenance;
- cold/warm, invalidation, focused-test and late-stage recovery results support the reported cost improvement without treating reused evidence as fresh execution;
- each milestone obligation and independent evidence case has concrete implementation and validation evidence.

## Evidence to review

The implementation author has prepared the following evidence for review:

- `.execution/validation/*.json` (ignored local runtime evidence) — current successful stage receipts and output hashes; seven stages passed the final verifier;
- `.execution/validation/consumer-provenance.json` (ignored local runtime evidence) — current package SHA `4f3b2e9bb9a38f075975baf5ec830220d83d4f681b988454a4f5db2473ee400b`, installed shim SHA `aaea6b325bfc45023b8891628d07499a919d8e2b5803b359d8c75984c4adc0a9`, and pinned 0.9.0 fixture commit/tree/SDK/package SHA;
- `.execution/M0020-efficient-resumable-validation.md` — exact 15-obligation/14-case reconciliation and criterion/case-specific cold/warm/failure-recovery evidence;
- `eng/validate.ps1` and `eng/ValidationStages.ps1` — canonical orchestration and receipt/fingerprint contract;
- `eng/validate-installed-consumers.ps1` — preserved M0018 installed-consumer boundaries and fixture reuse;
- `docs/ENGINEERING.md` — contributor commands, reuse rules, timings and completion semantics.

Correction evidence for the three review findings:

1. The self-host fingerprint now covers repository source/configuration/documentation, the Git change set from the merge base with `origin/main`, and non-ignored untracked input content; volatile `.hygiene/.state` outputs are excluded. A trailing-whitespace line in `docs/ENGINEERING.md` made `-Verify` reject only `self-host`, and `git diff --check` identified that line. Restoring the file restored the receipt. An ignored `.execution/validation` probe left self-host reusable.
2. The solution receipt now hashes 519 compiled test runtime artifacts, including the Core/CLI test assemblies and dependency/runtime manifests. `eng/test-validation-test-outputs.ps1` modified one byte in the actual Core test DLL and then removed it; both probes invalidated solution, Core, CLI, pack, consumer and self-host evidence. Restoring the original DLL restored aggregate verification.
3. Aggregate mode now calls the same `Assert-CompleteValidation` used by `-Verify` before printing success. `eng/test-validation-final-verification.ps1` altered the Core receipt and confirmed the final-boundary verifier rejected it, then confirmed the restored full receipt set passed.

The corrected cold `-Fresh` run executed all seven stages in 1,028.93 seconds of stage execution: solution 16.06s; Core 295.317s (95 passed); CLI 158.124s (30 passed); README/policy 0.002s; current package 2.071s; installed consumers 524.325s; self-host 33.031s. The no-argument warm aggregate reused all seven and verified all seven in about 38.2s wall time; `-Verify` accepted all seven in about 24.6s. These wall times include fingerprint and artifact hashing; reused stage elapsed fields are historical execution costs, not current-run time. No historical M0018 stopwatch baseline is available.

The exact registry audit found 15 obligations and 14 independent evidence cases, with all implementation-owned evidence rows complete. The project owner recorded approval for VAL-05 / REV-01 on 2026-10-18.

## Reviewer decision

**APPROVED** by the project owner on 2026-10-18 after review of the evidence documented above and in the execution ledger. This records the milestone decision; PR #20 remains open and unmerged.
