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

The latest canonical `eng/validate.ps1` run completed on Windows 11 with .NET SDK `11.0.100-rc.1.26425.128`: Release build passed, 118/118 tests passed (94 Core, 24 CLI), the fresh isolated 0.10.0 installed consumer passed, the repository self-host fixture passed, and `git diff --check` passed. It also exercised a local 0.10.0-to-0.10.1 autonomous stable-feed update after parent exit, installed-shim restart and lifecycle revalidation; offline feed reporting; preservation of an unsupported profile marker; and rollback after a formatter failure on a later project.

The implementation and several path-specific evidence cases remain incomplete. The ledger records gaps for multiple-solution/orphan discovery, generated/unsupported-source exclusions, non-allowlisted analyzer rejection plus second-run no-op, rejected/materially changed baseline behavior, preservation of staged/unstaged/untracked work on successful bootstrap, grouped-work edge cases, the complete terminal-report matrix, and the combined installed-package formatter/provenance receipt. AC-09 and AC-19 remain partial for those cases. M0018 is currently BLOCKED on those evidence gaps. If they are resolved, the review decision remains yours or an explicitly delegated approver's; no approval is recorded here.

## Reviewer decision

Record the reviewer identity, authority/delegation basis, date, decision (`approved` or `rejected`), and any required follow-up here only after the reviewer has independently examined the final implementation and evidence. Until then, leave the decision pending.
