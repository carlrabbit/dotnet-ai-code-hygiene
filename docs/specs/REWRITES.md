# SDK Formatting and Rewrite Behavior

## Commands and scope

`dotnet hygiene format` runs the supported SDK `dotnet format whitespace` pass. `dotnet hygiene normalize` runs the fixed approved SDK style diagnostics and then whitespace formatting. Both support explicit paths, `--changed`, and `--check`; the autonomous gate normalizes the complete eligible project source population regardless of the changed-file semantic review gate.

`--check` uses `--verify-no-changes` and reports the exact eligible paths the SDK says would change without writing source. Normal mutations require a clean Git worktree/index and a known `HEAD`; the tool records and verifies `HEAD` and the index around the SDK run. It never creates commits, pushes, resets, cleans, or promises rollback. A failed or interrupted pass leaves inspectable edits and reports that recovery is agent-owned.

Results identify SDK phases and the fixed diagnostic inventory. Changed paths are measured from the SDK report for check-only operations and source snapshots for mutating operations; the tool does not claim per-character or exact diagnostic causality.

## Approved style diagnostics

The `dotnet-11 v3` profile permits only these `dotnet format style` diagnostics:

| Diagnostic | Capability |
|---|---|
| IDE0005 | Remove unused imports |
| IDE0011 | Require braces |
| IDE0036 | Order modifiers |
| IDE0040 | Add explicit accessibility |
| IDE0090 | Use target-typed creation when the type is apparent |
| IDE0034 | Prefer default literals |
| IDE0004 | Remove unnecessary casts |
| IDE0071 | Simplify interpolated strings |
| IDE0037 | Use inferred tuple/member names |

Whitespace formatting and import organization are handled by the SDK whitespace phase. No generic analyzer-fix pass runs. The actual supported SDK must expose each listed fixer; unapproved structural modernizations remain semantic review suggestions.

## Rule disposition

The stable catalog retains historical M0023 IDs. `docs/specs/RULE-CAPABILITIES.md` records each ID's SDK or review-only disposition. Removed Roslyn source rewriters are not executed. `dotnet format whitespace` is used for formatting, the allowlist above handles standard fixes, and unsupported narrow cases stay review-only. Parity claims are limited to behavior observed in SDK fixtures and the installed-consumer gate.

## Concurrency and failure

Source snapshots are used only to report SDK edits. They are not a rollback transaction. If `HEAD` or the index changes during mutation, the operation stops and reports incomplete state. Conflicting external edits are not overwritten by an application-level restore. Recovery is explicit from Git after inspecting all remaining worktree changes.

## Exhaustive audit

`dotnet hygiene --yolo-all` remains nonmutating. It uses check-only SDK formatter phases, enumerates all enabled semantic modernization populations including valid source-owned exceptions, and reports retained exceptions with their reason rather than counting them as unexamined or clean.
