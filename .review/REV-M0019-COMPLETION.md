# Human review request — REV-M0019-COMPLETION

**Milestone:** `docs/milestones/M0019-exhaustive-read-only-hygiene-audit.md`

**Execution evidence:** `.execution/M0019-exhaustive-read-only-hygiene-audit.md`

**Status:** Human approval recorded 2026-10-20.

## Requested review

Please independently review the implementation and its evidence for:

- the hard read-only boundary, including source files, Git/index, profile/configuration, generated projections, ignore decisions, normal review acceptance, latest-run/gate-review state, formatter/normalizer output, and installed tool state;
- complete deterministic and semantic population coverage, including ignored findings, previously accepted observations, disabled/zero-population rules, generated-source exclusions, and independent rule/subject obligations;
- explicit caller attestation, supported-profile/baseline prerequisites, bounded judgment validation, atomic report-local checkpoints, corruption/concurrency behavior, and interruption/resumption;
- JSON/Markdown report counts, findings, problem/uncertainty rationale, incomplete work, source HEAD/time/dirty state, and snapshot/change-tracking limitations;
- the exact 0.11.0 package and generated-shim provenance and the isolated installed consumer’s source/state byte-preservation result;
- the ledger’s exact obligation/evidence reconciliation and the scope of focused M0020/M0022 validation.

The execution tested mechanical audit behavior only; it did not assign semantic gold labels or estimate code quality. Core/CLI test evidence is focused and does not claim complete-suite validation. The installed consumer used a disposable local feed and isolated repository; its report embeds source context in a local directory. No package publication, merge, or approval is part of this request.

## Correction pass for reviewed commit `fcbebe9`

The source identity is now captured as a canonical root in the schema-2 audit manifest and hashed inventory. Resume, record and finish validate that identity and use the stored inventory; JSON and Markdown report the captured source root even when the installed CLI runs from another repository. Missing, malformed, or manifest/inventory-mismatched roots fail closed. No source-change tracking, rescanning or dependency invalidation was added.

Coverage now separates exact semantic rule/subject assessments from deterministic rule results. Deterministic rules expose their applicable source-document or SDK-project population as an explicitly approximate count, and findings are reported separately. Inapplicable semantic counters are null for deterministic and disabled rules; enabled semantic zero populations remain exact zero. Mandatory, optional-enabled and disabled participation remain explicit. No synthetic judgments or subject-level evaluations are generated.

Fresh focused validation on the correction includes Core `AuditDeterministicCoverageSeparatesFindingsApproximationAndDisabledZeroPopulation`, `ExhaustiveAuditBypassesNormalSampleAndAcceptedObservationWithoutPublishingNormalState`, `ExhaustiveReviewMaterializationKeepsMoreThanFiftyThousandObligations`, and `ExhaustivePopulationIncludesMultipleProjectsAndExcludesGeneratedSubjects`; CLI `ExhaustiveAuditRequiresAttestationResumesJudgmentsAndPreservesNormalState`, `ExhaustiveAuditResumeUsesRecordedRepositoryIdentityAcrossRepositories`, and `ExhaustiveAuditStopsWithoutAcceptedBaselineAndLeavesDestinationAbsent`. All passed under the current M0020 solution build. These are focused checks, not complete Core/CLI stage receipts or full release qualification.

The corrected package was freshly packed and installed into a disposable local-feed consumer. Package SHA-256: `154658a8046a328f030d20da12ebe25be20f38cc87e5adc9dfaa936b1852a89b`. Installed `dotnet-hygiene.exe` shim SHA-256: `8b167e9c21e80ee74bf25045ec46ccac298dc22c5ce0240b16ba228ac44ac81e`. Through the installed 0.11.0 `dotnet hygiene` command, repo C created the audit and repo D ran `next`, `record`, `finish`, resumed `next`, recorded a concrete uncertainty rationale and finished the report. Schema-2 JSON and Markdown retained repo C identity and inventory; the final report reconciled eligible=2, assessed=2, unassessed=0, uncertain=1, and deterministic findings=2. The per-rule deterministic total matched the report total; `docs.summary.required` showed approximate=1, measured=null, eligible=null, findings=2. Eight protected source/project/Git-index/profile/projection/approval file hashes stayed unchanged, three absent configuration/latest-run/gate-review files remained absent, and repo C's Git status remained clean. Consumer evidence is retained at `C:\Users\carlt\AppData\Local\Temp\M0019-correction-installed-9c204c47f87f40c49932ef21d17ace38\audit-report-schema2`.

Remaining limitations: deterministic applicability counts are estimates of supported population size, not measured per-rule invocation totals; audit inventory is finite and source changes during collection are not tracked; semantic judgments remain external; full Core/CLI suites and the historical cold release matrix were not run because they are not declared M0019 correction gates.

## Consumer provenance

- Package: `DotNetAiCodeHygiene.Tool.0.11.0`
- Package SHA-256 (original review): `6D2DB12994458F9D3BFDF7FF2C96E22CB3B6941E6DAAC4D957B260E666B10477`
- Package SHA-256 (corrected build): `154658a8046a328f030d20da12ebe25be20f38cc87e5adc9dfaa936b1852a89b`
- Installed shim: `dotnet-hygiene.exe`
- Shim SHA-256 (corrected build): `8b167e9c21e80ee74bf25045ec46ccac298dc22c5ce0240b16ba228ac44ac81e`
- Corrected result: freshly packed and installed report schema 2 preserved repo C identity while resuming from repo D; judgment collection finished at 2/2 with a recorded uncertainty rationale, deterministic findings reconciled at 2, and protected source/configuration state remained unchanged.

## Reviewer and decision

- Reviewer role: human approver responding to the M0019 review request in this conversation.
- Reviewer identity: not separately supplied or independently verified in this record.
- Decision: **approved by the user on 2026-10-20**, following the correction pass for reviewed commit `fcbebe9`.
- Corrections: the two review findings are addressed and their fresh criterion-specific evidence is recorded in the execution ledger and above.
- Scope note: this records the user's explicit approval; it does not claim an externally authenticated identity or independent verification beyond that approval.

This approval is a direct human decision, not inferred from implementation or validation. No merge or self-approval occurred. **Milestone status: complete, with reviewer identity not independently verified.**
