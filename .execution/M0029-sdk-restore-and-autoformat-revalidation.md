# Execution Obligation Ledger — M0029

> Planner-seeded material-outcome inventory. This is not a test matrix, evidence registry, or additional source of policy.

**Milestone:** `docs/milestones/M0029-sdk-restore-and-autoformat-revalidation.md`  
**Planning base:** M0028 implementation head `64781d65997c712e47665f47e5c1d7d10732d00f`  
**Amended planning commit:** `a9b2846578171298bb459a17282160e919ece213`
**Implementation branch:** `implementation/m0029-sdk-restore-autoformat-revalidation`
**Status:** complete; corrected output appended to existing Draft PR #14

## Material obligations

| ID | Outcome | Status | Result or blocker |
|---|---|---|---|
| REST-01 | Default SDK-native restore and explicitly forwarded `--no-restore`; real SDK `IDE0005` retains needed imports, removes unused imports, and never requires an Autoformat-invoked build | done | Earlier focused real SDK-project integration retained a required NuGet import, removed unused `System.Text.Json`, and verified CLI forwarding/check-only behavior. The regenerated target also retains required NuGet imports. |
| LAY-01 | First successful normalize pass is byte-stable on the next pass; normalized check-only is consistent and `format` stays whitespace-only | done | Compact-flow and member-access canonicalizer regressions pass. The fresh target second normalize, `normalize --check`, and `format --check` each reported zero changes. The 176 blank-before-dot anomalies fell to zero. |
| COLL-01 | Amended: prevent collection-expression rewrites without a valid target type, including the three M0029 target `foreach` failures, while retaining eligible typed conversions; focused regression and diagnostic target recheck | done | Focused `CollectionExpressionsRequireARecognizedTargetTypeContext` passed. It preserves three `foreach` array creations, an untyped invocation, a nested `object` local-function return, and a block-bodied `Func<object>` lambda; it retains eligible typed locals/properties/method and local-function returns, List and interface conversions. Target build at the same baseline passed with 0 warnings/errors. |
| EXP-01 | Target Draft PR #14 preserves its policy/first-output commits and receives a new unmodified regenerated-output commit after the `COLL-01` fix; previous PR #14 and original PR #13 evidence, exact revisions, diagnostic build and convergence results are reported accurately | done | Existing [Draft PR #14](https://github.com/carlrabbit/dotnet-nuget-content/pull/14) now includes append-only raw delta commit `131de3a63dcba5b4eb9b9f27470f65bf79f9cb04`; all earlier commits preserved. It removes the 176 blank-before-dot patterns, matches fresh snapshot `f43f75c66151a745e388c4c9989b898f13322301`, passes the focused application build and converges. Original PR #13 remains unchanged; PR #14 stays DO NOT MERGE. |
| DOC-01 | Direct Autoformat docs/CLI plus the amended M0029 research, milestone index, completion note and PR descriptions accurately distinguish first and corrected M0029 experiment outputs and the result of `COLL-01` | done | Autoformat specs, research report, milestone index/completion note, ledger, and existing PR #14/PR #38 descriptions now record the member-access cause, exact commits and before/after counts. |

## Completion summary

- Amended planning base: `a9b2846578171298bb459a17282160e919ece213`; collection eligibility commit: `68f3d7de6a63f48f2d49f5cb178a0a4a80b3c9c0`; nested return-owner correction commit: `5d432577450dbd963c771341a794dd484d7a0ff2`.
- Member-access layout correction commit: `de87fce65e3e25750ef5b3316b061e5609f6f9b2`.
- Initial M0029 target output commit: `08781f4d3ae7e71bde39bc1e6af9271355ce6b12` (three `CS9176` errors); collection-eligibility delta: `2edddf2740e09c36bdb1fef879c3fec6ddbb9516`; latest member-access layout delta: `131de3a63dcba5b4eb9b9f27470f65bf79f9cb04` (focused build passes).
- Target Draft PR #14 remains open/unmerged and DO NOT MERGE; original PR #13 is unchanged. No target tests or release qualification were run.
- Detailed observations and exact commands are recorded in `docs/research/M0029-autoformat-nuget-content-revalidation.md`.
- The nested return-context correction was verified against PR #14 with read-only normalize check (`changedCount: 0`); target commits were not changed or regenerated.
- The subsequent fresh-baseline member-access rerun found 176 anomalies in prior output and zero after correction; its raw delta is appended as target commit `131de3a63dcba5b4eb9b9f27470f65bf79f9cb04`. Build and all stability checks passed.

## Completion accounting

All five amended material obligations are reconciled. The focused application build passes on the regenerated target snapshot, verifying the known collection-expression and formatting defects on this baseline; it is not a universal Autoformat runtime gate. Broader target compatibility remains unverified.
