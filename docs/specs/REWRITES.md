# SDK Formatting and Rewrite Behavior

## Commands and scope

`dotnet hygiene format` runs the supported SDK `dotnet format whitespace` pass. `dotnet hygiene normalize` runs the fixed approved SDK style diagnostics and then whitespace formatting. Both support explicit paths, `--changed`, and `--check`; `--changed` always implies check-only selection because it selects dirty staged, unstaged, and untracked paths while mutation requires a clean Git checkpoint. The autonomous gate normalizes the complete eligible project source population regardless of the changed-file semantic review gate.

`--check` and `--changed` use `--verify-no-changes` and report the union of paths from every SDK phase and project batch without writing source. Each formatter invocation receives its own report directory so a later report cannot overwrite earlier findings. Normal mutations require a clean Git worktree/index and a known `HEAD`; the tool records and verifies `HEAD` and the index around the SDK run. It never creates commits, pushes, resets, cleans, or promises rollback. A failed or interrupted pass leaves inspectable edits and reports that recovery is agent-owned.

Results identify SDK phases and the fixed diagnostic inventory. Changed paths are measured from the SDK report for check-only operations and source snapshots for mutating operations; the tool does not claim per-character or exact diagnostic causality.

## Approved style diagnostics

The `dotnet-11 v3` profile permits only these `dotnet format style` diagnostics:

| Diagnostic | Purpose and configured policy | SDK fix disposition |
|---|---|---|
| IDE0005 | Remove unnecessary imports; severity `warning`; no style option | SDK fixer is approved; actual normalize fixtures exercise the style phase and installed package. |
| IDE0011 | Require braces; `csharp_prefer_braces=true`; severity `error` | SDK fixer is approved; nested severity conflicts are rejected. |
| IDE0036 | Order declaration modifiers; `csharp_preferred_modifier_order=public,private,protected,internal,file,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,required,volatile,async`; severity `warning` | SDK fixer is approved; public/static, file/sealed, and required/public declarations are formatted and compiled in the SDK fixture. |
| IDE0040 | Add explicit accessibility; `dotnet_style_require_accessibility_modifiers=always`; severity `error` | SDK fixer is approved; profile migration/effective-setting validation owns the policy. |
| IDE0090 | Use implicit object creation when type is apparent; `csharp_style_implicit_object_creation_when_type_is_apparent=true`; severity `warning` | SDK fixer is approved; nested `false` prevents the fix, while effective `true` transforms field/local/return contexts and the installed package preserves pragmatically suppressed cases. |
| IDE0034 | Prefer default literals; `csharp_prefer_simple_default_expression=true`; severity `warning` | SDK fixer is approved and is verified to run alongside a valid IDE0090 suppression. |
| IDE0004 | Remove unnecessary casts; severity `warning`; no style option | SDK fixer is approved; no separate option is required. |
| IDE0037 | Prefer inferred tuple/member names; `dotnet_style_prefer_inferred_tuple_names=true` and `dotnet_style_prefer_inferred_anonymous_type_member_names=true`; severity `warning` | SDK fixer is approved for both tuple and anonymous type naming preferences. |

Whitespace formatting and import organization are handled by the SDK whitespace phase. No generic analyzer-fix pass runs. The actual supported SDK must expose each listed fixer; unapproved structural modernizations remain semantic review suggestions. The profile's options are IDE0011 `csharp_prefer_braces=true`, IDE0036 `csharp_preferred_modifier_order` using the SDK order `public,private,protected,internal,file,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,required,volatile,async`, IDE0040 `dotnet_style_require_accessibility_modifiers=always`, IDE0090 `csharp_style_implicit_object_creation_when_type_is_apparent=true`, IDE0034 `csharp_prefer_simple_default_expression=true`, and IDE0037 `dotnet_style_prefer_inferred_tuple_names=true` plus `dotnet_style_prefer_inferred_anonymous_type_member_names=true`. IDE0005 and IDE0004 need no style option. Their severities are explicit in the profile.

IDE0071 is excluded from the approved inventory: the SDK rule simplifies explicit `ToString()` calls inside an interpolation, which does not match the historical hygiene cleanup that converted interpolated strings without holes (for example `$"hello"`) to ordinary string literals. That historical rule remains review-only; M0024 does not add another automatic transformation to preserve its old status.

## Rule disposition

The stable catalog retains historical M0023 IDs. `docs/specs/RULE-CAPABILITIES.md` records each ID's SDK or review-only disposition. Removed Roslyn source rewriters are not executed. `dotnet format whitespace` is used for formatting, the allowlist above handles standard fixes, and unsupported narrow cases stay review-only. Parity claims are limited to behavior observed in SDK fixtures and the installed-consumer gate.

## Concurrency and failure

Source snapshots are used only to report SDK edits. They are not a rollback transaction. If `HEAD` or the index changes during mutation, the operation stops and reports incomplete state. Conflicting external edits are not overwritten by an application-level restore. Recovery is explicit from Git after inspecting all remaining worktree changes.

## Exhaustive audit

`dotnet hygiene --yolo-all` remains nonmutating. It uses check-only SDK formatter phases, enumerates all enabled semantic modernization populations including valid source-owned exceptions, and reports retained exceptions with their reason rather than counting them as unexamined or clean.
