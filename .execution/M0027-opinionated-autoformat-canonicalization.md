# M0027 — Optional Execution Progress

> Operational resume notes only. The authoritative product contract is [M0027](../docs/milestones/M0027-opinionated-autoformat-canonicalization.md).

**Milestone:** `docs/milestones/M0027-opinionated-autoformat-canonicalization.md`
**Implementation branch:** `codex/m0027-opinionated-autoformat-canonicalization`
**Last updated:** 2026-11-22 (implementation PR opened)

## Completed work

- Implemented an Autoformat-owned Roslyn canonicalizer covering all nine modernization families, retain annotations, visual-block spacing, vertical parameters, syntax-aware line wrapping and redundant blank-line cleanup.
- Removed Hygiene-aware Autoformat policy hooks, duplicate formatter catalog modules and unused copied repository session state while retaining live semantic-review modules.
- Simplified normal Autoformat validation and updated operative policy, architecture, engineering and readability docs. Added the M0027 completion note.
- Aggressive collection-expression conversion applies to eligible array/list initializers without protecting downstream concrete `List<T>` assumptions. Runtime target compilation and semantic equivalence are not gates.

## Checks and remaining work

- `dotnet test tests/DotNetAiWorkbench.Autoformat.Tests/DotNetAiWorkbench.Autoformat.Tests.csproj -c Release --no-restore` — passed, 12/12.
- Autoformat CLI/Core and Hygiene CLI affected-project builds passed during implementation (warnings only).
- Fixed a focused long-collection idempotence regression by preserving generated continuation indentation; the Autoformat suite passed afterward.
- Installed-tool/package qualification, aggregate/self-host checks, real-repository processing and full Hygiene suites remain deferred release checks.
- Known syntax-shape coverage limits are listed in the milestone completion note. Consumer projects may need agent-owned repair after canonicalization.

## Handoff

Commit `4c31408` was pushed to `codex/m0027-opinionated-autoformat-canonicalization`; implementation PR [#33](https://github.com/carlrabbit/dotnet-ai-code-hygiene/pull/33) targets `planning/m0027-opinionated-autoformat-canonicalization` while planning PR #32 remains open.
