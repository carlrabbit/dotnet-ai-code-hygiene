# REV-M0021-COMPLETION — Independent Review Request

## Decision

**Status: PENDING independent project-owner/delegate review.**

This packet does not record approval. Do not merge until the independent reviewer records a decision.

Pull request: [#21](https://github.com/carlrabbit/dotnet-ai-code-hygiene/pull/21).

## Review scope

Review the M0021 implementation against the exact obligations and evidence cases in `docs/milestones/M0021-remove-legacy-hazard-sampling.md` and `.execution/M0021-remove-legacy-hazard-sampling.md`.

Please verify:

- obsolete hazard/age/activity sampling sources, persistence, tickets, activity accounting, sampler transaction choreography, and historical-only tests are removed;
- current `GateLocalReviewStore`/`GateTicket` selection, acceptance, persistence, revalidation, bounded batches, and handoff semantics remain intact;
- mixed tests retain current latest-run publication atomicity, review acceptance/stale-ticket rejection, and BORINGness population assertions;
- operative specifications describe the current gate-local model and no longer claim legacy state preservation, while historical research and milestone records remain untouched;
- criterion-level evidence and focused-versus-suite receipt status are accurately reconciled in the execution ledger.

## Implementation summary

The change deletes `SamplingMechanics.cs`, `SamplingSession.cs`, `ProjectActivity.cs`, and `SamplingTests.cs`; removes the three identified historical lifecycle cases and `SamplingCursorRule`; removes the legacy sampler integration from `RuleContext`, `HygieneEngine`, and review ticket associations; and updates only active architecture/specification/engineering guidance. Current gate-local sampling is preserved.

## Evidence prepared

- `.execution/M0021-remove-legacy-hazard-sampling.md` — exact 13-obligation and 9-case reconciliation, work packages, named implementation/validation evidence, and completion audit.
- `eng/validate.ps1 -Shard solution` — Release solution build passed; current receipt was reused on the final check.
- Focused Core filters passed for latest-run publication failure atomicity, gate selection and merge-base behavior, current acceptance/stale-ticket rejection, gate-review concurrent-write atomicity, and BORINGness population.
- Focused CLI filter `ReviewAcceptReportsOnlyCurrentNormalSampleItems` passed.
- `eng/validate.ps1 -Shard pack-current` — freshly produced `DotNetAiCodeHygiene.Tool.0.10.0.nupkg`, SHA-256 `9fba2a2e42e6976719a583bcca0d86aeefe9ff994ce31d6390ce5a6643ede41a`.
- Installed the exact package into an isolated tool path. Shim: `C:\Users\carlt\AppData\Local\Temp\m0021-installed-consumer-8b00df70cf834e92ad3049cdf6193098\tools\dotnet-hygiene.exe`, SHA-256 `aaea6b325bfc45023b8891628d07499a919d8e2b5803b359d8c75984c4adc0a9`. Through `dotnet hygiene`, bootstrap succeeded; check selected one summary item; review expansion and explicit acceptance succeeded.
- Exact milestone/ledger reconciliation reported 13 obligations and 9 evidence cases with no ID, parent, or wording mismatches.

Focused Core/CLI runs are not complete `core-tests` or `cli-tests` receipts. The full historical installed-consumer matrix, complete canonical aggregate validation, and self-host stage were not run; M0021 does not require the historical matrix or claim aggregate validation. `git diff --check` is recorded after final documentation and ledger edits.

## Reviewer decision

| Reviewer | Authority basis | Date | Decision | Follow-up |
|---|---|---|---|---|
| Pending | Independent project owner or explicitly delegated reviewer | Pending | pending | Review this PR and the evidence above |
