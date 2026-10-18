# M0018 Completion Review Request

**Decision:** pending — no approval is recorded.  
**Requested reviewer:** project owner or explicitly delegated approver.  
**Implementation branch:** implementation/m0018-autonomous-hygiene-lifecycle.
**Pull request:** https://github.com/carlrabbit/dotnet-ai-code-hygiene/pull/19

This file is a review request, not evidence of reviewer consent. Do not mark M0018 complete until all implementation/evidence gaps are closed, the registry is reconciled, and an authorized reviewer records a decision.

## Review scope

Please review the final pull request and verify:

- autonomous `dotnet hygiene` bootstrap, profile reconciliation, gate inference, grouped selected-only work orders, terminal reports, and safe failure behavior;
- explicit external approval for initial/material policy baselines, including the absence of silent approval;
- deterministic formatting scope, original-policy assessment, allowlisted post-profile fixes, and preservation of dirty/staged/untracked user work;
- exact freshly packed 0.10.0 package provenance and installed `dotnet hygiene` behavior, including update/restart behavior from trusted sources;
- preservation of the independent M0016/M0017 semantic-review rule contracts, selection limits, questions, tickets, and explicit per-rule acceptance;
- exact obligation/evidence reconciliation and scope fidelity, including deferred `--yolo-all`.

## Evidence state at preparation

The final canonical ./eng/validate.ps1 run passed on Windows 11 with PowerShell, Git and .NET SDK 11.0.100-rc.1.26425.128 (RC). Release build succeeded with 0 warnings/errors; 124 tests passed (95 Core, 29 CLI), 0 failed/skipped. The exact freshly built and packed 0.10.0 package was installed from an isolated local feed and exercised through the installed dotnet hygiene entrypoint. The consumer passed bootstrap/report-bound approval, stale source/configuration/report rejection, ordinary source edits after acceptance, material-policy reapproval, trusted 0.10.0-to-0.10.1 fixture update/restart, offline/incompatible handling, ancestor local-manifest resolution, protection of a competing tool-path install, formatter restrictions/idempotence, Git gate outcomes, grouped selected-only agent work, explicit review acceptance and mutation rollback. Repository M0006 self-host validation, README/package provenance checks and git diff --check passed.

Final package SHA-256: 8EAD5CB61A479234F0A3CBE2BD4C5F7C566617D85D6D16ED28ED5FF57E47B1E6. The canonical validator built and version-verified the prior 0.9.0 fixture from pinned source commit 2e17b293be78480dcad9817f43c9bcc079a81ea8 into a fresh run-local feed; it did not consume an ignored previous-version nupkg. The 0.10.1 package was only a local update fixture.

The amended registry preserves exact wording and parent relationships for all 23 obligation IDs and 31 evidence-case IDs. Every row has work-package, implementation and validation evidence, with each case tied to a named scenario. New focused tests cover marker-only baseline rejection, stale report/source/configuration, tool/rule version fingerprints, real Git committed/worktree/missing/ambiguous/combined gate paths, local-manifest ancestor discovery and fail-closed competing-install handling.

The implementation and software evidence are ready for independent review. M0018 is **AWAITING HUMAN REVIEW**. The only remaining milestone gate is the project-owner or explicitly delegated approver decision; no approval is recorded here.
## Reviewer decision

Record the reviewer identity, authority/delegation basis, date, decision (`approved` or `rejected`), and any required follow-up here only after the reviewer has independently examined the final implementation and evidence. Until then, leave the decision pending.
