# Public Documentation

Install version `0.4.0` from a configured NuGet source with `dotnet tool install --global DotNetAiCodeHygiene.Tool --version 0.4.0`, then inspect `hygiene --version` and `hygiene help --agent`. This project supports Windows 11 with the .NET 11 SDK line; it does not claim public-feed publication.

Canonical workflow:

```text
implement/change code
-> run relevant tests
-> hygiene normalize
-> hygiene check
-> resolve findings/review work
-> rerun tests/check as appropriate
```

`format` is presentation-only. `normalize` applies a small fixed Roslyn semantic-simplification policy, requires clean project compilations before and after, and formats changed documents. Both support repository/default, explicit file/directory, and `--changed` targets, text/JSON, and non-mutating `--check`. Complete plans are validated before an all-or-nothing mutation; repeated runs are idempotent. `--check` exits 0 when pending changes are reported. JSON includes changed counts and paths. Analysis rules and transformations are separate. The canonical workflow is implement, test, normalize, check, resolve findings/review work, and retest/recheck as appropriate. Existing deterministic finding explain/ignore flows and explicit semantic review expansion/handoff remain available; no model call is made.

Do not claim Linux/macOS validation, broad modernization, automated remediation, MCP, IDE integration, or public package publication.
