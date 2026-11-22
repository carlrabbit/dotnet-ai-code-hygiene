# Migration — Guide System v0.3.0 to v0.4.0

## Required

None for repositories that do not use Tier 5 review or complex cross-platform command logic.

## Conditional

- add `.review/` when human review is active;
- add canonical review commands;
- move complex shell logic into tested .NET code;
- add `.ps1` launchers only when Windows is actively supported and tested.

## Deprecated

- unrecorded chat-only approval;
- complex domain or workspace semantics in shell scripts;
- assuming a PowerShell launcher alone proves Windows support.
