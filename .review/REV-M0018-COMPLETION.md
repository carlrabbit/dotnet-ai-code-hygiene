# M0018 Completion Review Request

**Decision:** pending — no approval is recorded.  
**Requested reviewer:** project owner or explicitly delegated approver.  
**Implementation branch:** `implementation/m0018-autonomous-hygiene-lifecycle`.

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

The latest canonical `eng/validate.ps1` run completed on Windows 11 with .NET SDK `11.0.100-rc.1.26425.128`: Release build passed, all 119 tests passed (95 Core, 24 CLI), the freshly packed and hash-verified 0.10.0 installed consumer passed, the repository self-host fixture passed, and `git diff --check` passed. The installed consumer covered autonomous local stable-feed update from 0.10.0 to a 0.10.1 fixture after parent exit, installed-shim restart and lifecycle revalidation; offline and incompatible-profile reporting; exact preservation and rollback; multiple solutions/orphan projects and source exclusions; formatter allowlist boundaries and idempotence; grouped agent work including zero-work batches; and distinct complete, approval-required, escalation-required and incomplete outcomes. The package SHA-256 is `AF89D87D5FD1A891FBC780053C48C788AAB6A8DA6FFF9BC85B47216ADD70C035`. The 0.10.1 package is validation fixture data, not a release.

The final ledger reconciliation records AC-01–AC-20 and all 26 software evidence cases as passed; DOC-01/02 are complete. The only remaining milestone gate is this independent human review. M0018 is **AWAITING HUMAN REVIEW**. No approval is recorded here.

## Reviewer decision

Record the reviewer identity, authority/delegation basis, date, decision (`approved` or `rejected`), and any required follow-up here only after the reviewer has independently examined the final implementation and evidence. Until then, leave the decision pending.
