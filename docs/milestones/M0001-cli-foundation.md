# Milestone — M0001 CLI Foundation

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | done |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Execution ledger | `.execution/M0001-cli-foundation.md` |
| Validation locus/platform | local Windows 11 with .NET 11 SDK and PowerShell |
| Human review | accepted |

## Goal

Establish the Windows-first .NET CLI foundation required by later hygiene milestones.

## Completion Summary

M0001 was implemented in PR #1 and merged to `main`.

Established:

- .NET 11 solution/project skeleton;
- `System.CommandLine` CLI surface;
- TUnit process tests;
- stable M0001 help/version/invalid-invocation behavior;
- `eng/validate.ps1`;
- packable `DotNetAiCodeHygiene.Tool` with command `hygiene`;
- Windows-local validation;
- no GitHub Actions/workflows;
- no premature hygiene engine/formatter/normalizer implementation.

`REV-M0001-COMPLETION` was accepted by the project owner after review of PR #1.

## Durable Completion Evidence

The merged PR and retained execution ledger contain criterion-specific evidence for all M0001 obligations and evidence cases.

Final result:

```text
AC-01..AC-12   Pass
DOC-01..DOC-02 Pass
EC-04a..EC-10b Pass
REV-01         Accepted
```

M0001 is historical authority only. M0002 is the active implementation milestone.
