# M0027 — Optional Execution Progress

> Operational resume notes only. The authoritative product contract is [M0027](../docs/milestones/M0027-opinionated-autoformat-canonicalization.md).

**Milestone:** `docs/milestones/M0027-opinionated-autoformat-canonicalization.md`
**Implementation branch:** `codex/m0027-opinionated-autoformat-canonicalization`
**Last updated:** 2026-11-22 (review corrections pushed to implementation PR)

## Completed work

- Implemented an Autoformat-owned Roslyn canonicalizer covering all nine modernization families, retain annotations, visual-block spacing, vertical parameters, syntax-aware line wrapping and redundant blank-line cleanup.
- Corrected expression-body comment preservation, owner-resolved exceptions across modernization/layout/retained comment indentation, DTO versus collection detection, full vertical parameter delimiters, reusable `out` locals, and the current Hygiene capability specification.
- Fresh review added same-target assignment switch conversion and a post-SDK layout stabilization pass; the disposable SDK engine test now exercises retained comments and two-pass stability.
- Removed Hygiene-aware Autoformat policy hooks, duplicate formatter catalog modules and unused copied repository session state while retaining live semantic-review modules.
- Simplified normal Autoformat validation and updated operative policy, architecture, engineering and readability docs. Added the M0027 completion note.
- Aggressive collection-expression conversion applies to eligible array/list initializers without protecting downstream concrete `List<T>` assumptions. Runtime target compilation and semantic equivalence are not gates.

## Checks and remaining work

- `dotnet test tests/DotNetAiWorkbench.Autoformat.Tests/DotNetAiWorkbench.Autoformat.Tests.csproj -c Release --no-restore` — passed after corrections, 20/20.
- `dotnet build src/DotNetAiWorkbench.Autoformat.Core/DotNetAiWorkbench.Autoformat.Core.csproj -c Release --no-restore`, `dotnet build tests/DotNetAiWorkbench.Autoformat.Tests/DotNetAiWorkbench.Autoformat.Tests.csproj -c Release --no-restore`, and `dotnet build src/DotNetAiWorkbench.Autoformat.Cli/DotNetAiWorkbench.Autoformat.Cli.csproj -c Release --no-restore` — passed with existing SDK/style warnings only.
- The corrected engine integration test exercises a disposable SDK project, retained comment indentation, vertical parameters and byte-stable second normalization.
- Installed-tool/package qualification, aggregate/self-host checks, real-repository processing and full Hygiene suites remain deferred release checks.
- Known syntax-shape coverage limits are listed in the milestone completion note. Consumer projects may need agent-owned repair after canonicalization.

## Handoff

Update the existing PR #33 from this branch after committing the review corrections. Keep the target `planning/m0027-opinionated-autoformat-canonicalization` while planning PR #32 remains open.
