# dotnet-ai-code-hygiene

`hygiene` is a Windows-first CLI for deterministic C# code hygiene checks. M0002 provides three parameterless rules: `docs.summary.required`, `readability.long-line.review`, and `readability.control-flow.visual-block`. Checks are read-only and findings do not cause a non-zero exit code.

Run from a Git repository containing SDK-style .NET projects:

```powershell
hygiene check
hygiene check src/Example/Service.cs
hygiene check --changed
hygiene check --output json
hygiene rules
hygiene explain R-7K2M9P/F-1
hygiene ignore F-1 --reason "Reviewed and intentionally retained"
hygiene ignores
hygiene unignore I-1
```

Use `--output json` for automation. JSON results use schema version 1; diagnostics are written to stderr. Exit codes are `0` for successful commands (including checks with findings), `1` for unexpected failures, `2` for malformed invocation, `3` for invalid repository/input/state, and `4` when a required dependency such as Git is unavailable.

The CLI owns `.hygiene/config.json` and `.hygiene/decisions.json`. The latest local run is stored under `.hygiene/.state/` and is Git-ignored. Use `hygiene rules enable|disable` to select rules and the ignore commands to manage reviewed exceptions.

M0002 is validated on Windows 11 with the .NET 11 SDK line. `format` and `normalize` are not implemented until M0003. Installed-tool usage guidance is deferred until the packaged consumer surface is validated.

Run repository validation with:

```powershell
./eng/validate.ps1
```
