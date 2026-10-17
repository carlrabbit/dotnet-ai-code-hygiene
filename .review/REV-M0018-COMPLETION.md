# M0018 Completion Review Request

**Decision:** pending — no approval is recorded.  
**Requested reviewer:** project owner or explicitly delegated approver.  
**Implementation branch:** `implementation/m0018-autonomous-hygiene-lifecycle`.

This file is a review request, not evidence of reviewer consent. Do not mark M0018 complete until the implementation is finished, the evidence registry is reconciled, and an authorized reviewer records a decision.

## Review scope

Please review the final pull request and verify:

- autonomous `dotnet hygiene` bootstrap, profile reconciliation, gate inference, grouped selected-only work orders, terminal reports, and safe failure behavior;
- explicit external approval for initial/material policy baselines, including the absence of silent approval;
- deterministic formatting scope, original-policy assessment, allowlisted post-profile fixes, and preservation of dirty/staged/untracked user work;
- exact freshly packed 0.10.0 package provenance and installed `dotnet hygiene` behavior, including update/restart behavior from trusted sources;
- preservation of the independent M0016/M0017 semantic-review rule contracts, selection limits, questions, tickets, and explicit per-rule acceptance;
- exact obligation/evidence reconciliation and scope fidelity, including deferred `--yolo-all`.

## Evidence state at preparation

The branch contains an implementation in progress. A Release build succeeded on Windows 11 with .NET SDK `11.0.100-rc.1.26425.128`. An isolated 0.10.0 package installation resolved `dotnet hygiene --version`, and a consumer bootstrap emitted `approval-required`. A project consumer also exercised the actual SDK `dotnet-format.dll`, profile approval state, gate check, and grouped selected review output.

The final `eng/validate.ps1` run completed successfully on Windows 11 with .NET SDK `11.0.100-rc.1.26425.128`: build passed, 118/118 tests passed (94 Core, 24 CLI), the freshly packed 0.10.0 isolated consumer passed, and the repository self-host fixture passed. The script also installed 0.9.0 and updated to 0.10.0 from a controlled local feed, then verified the `dotnet-hygiene` shim and version.

This does not close every review item. Repository-wide source formatting is not rolled back atomically if a later external formatter/project invocation fails, and autonomous no-argument execution does not detect an available trusted-feed release. The update test proves the package manager path, not autonomous update discovery/restart. Offline/incompatible update states and full dirty-worktree preservation during a failed formatting run also lack evidence. See the execution ledger for criterion-specific status. The reviewer must decide whether these are required corrections before approval.

## Reviewer decision

Record the reviewer identity, authority/delegation basis, date, decision (`approved` or `rejected`), and any required follow-up here only after the reviewer has independently examined the final implementation and evidence. Until then, leave the decision pending.
