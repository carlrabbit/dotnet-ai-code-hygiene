# M0029 — Autoformat NuGet-content revalidation

**Result:** The SDK-native restore path retained the required NuGet imports and the corrected formatter converged in one pass. The raw output still has unresolved compile failures; the target PR is for inspection only and must not be merged.

## Question and scope

Does Autoformat's default SDK restore preserve required imports while removing genuinely unused imports, and does one normalization reach a fixed point on the same NuGet-content revision used by M0028? This was a disposable source exploration, not release qualification. No target tests, package publication, Native AOT, or installed-tool qualification were run.

## Provenance and commands

- Workbench base: accepted M0029 planning commit `1b722047fd06487c542fbd5332a44f22e9201559`.
- Corrected formatter source: workbench commit [`0fadcdef0516bfd6a2ffa631079adc4115744fe7`](https://github.com/carlrabbit/dotnet-ai-code-hygiene/commit/0fadcdef0516bfd6a2ffa631079adc4115744fe7).
- Target base: `main` at `25b28310c7de04426de02e4ac8037b32ed9dbd9e`, checked out in a fresh disposable clone.
- Target experiment: policy commit `ac183618012c474821b6f6ac2cc9f2b55a5d325f`, followed by raw formatter output commit `08781f4d3ae7e71bde39bc1e6af9271355ce6b12`.
- Toolchain: .NET SDK `11.0.100-rc.3.26522.148`; Git `2.56.0.windows.4`.
- New [Draft PR #14](https://github.com/carlrabbit/dotnet-nuget-content/pull/14) targets `main`, is marked **DO NOT MERGE**, and contains only those two commits. The original [Draft PR #13](https://github.com/carlrabbit/dotnet-nuget-content/pull/13) was left untouched.

The corrected source was run directly from the workbench checkout:

```text
dotnet run -c Release --no-build --project C:\src\dotnet-ai-code-hygiene\src\DotNetAiWorkbench.Autoformat.Cli\DotNetAiWorkbench.Autoformat.Cli.csproj -- init
dotnet run -c Release --no-build --project C:\src\dotnet-ai-code-hygiene\src\DotNetAiWorkbench.Autoformat.Cli\DotNetAiWorkbench.Autoformat.Cli.csproj -- normalize --output json
dotnet run -c Release --no-build --project C:\src\dotnet-ai-code-hygiene\src\DotNetAiWorkbench.Autoformat.Cli\DotNetAiWorkbench.Autoformat.Cli.csproj -- normalize --output json
dotnet run -c Release --no-build --project C:\src\dotnet-ai-code-hygiene\src\DotNetAiWorkbench.Autoformat.Cli\DotNetAiWorkbench.Autoformat.Cli.csproj -- normalize --check --output json
dotnet run -c Release --no-build --project C:\src\dotnet-ai-code-hygiene\src\DotNetAiWorkbench.Autoformat.Cli\DotNetAiWorkbench.Autoformat.Cli.csproj -- format --check --output json
dotnet build src\DotNetNugetContent\DotNetNugetContent.csproj
```

Autoformat invoked SDK phases as `dotnet format <workspace> style|whitespace --report <report-dir> --include <selected-paths>` (and `--diagnostics IDE0005` for the style phase); the default omitted `--no-restore`. The one mutating normalization selected all 9 C# files under target `src` and `tests`.

## Observations

- Policy initialization was committed before formatting, establishing a clean checkpoint. One normalization changed all 9 selected files (2,555 insertions, 549 deletions). The second mutating pass had no diff.
- Both `normalize --check` and `format --check` reported 0 changed files on the canonical result.
- `NuGet.Versioning`, `NuGet.Frameworks`, and `NuGet.Packaging` imports required by the application remained in the output. A focused real SDK-project test also showed implicit restore preserving a required NuGet import and removing an unused `System.Text.Json` import.
- **Observed defect:** the rewritten target diagnostic build failed with three `CS9176` errors, at two collection expressions in `Program.cs` and one in `ProjectWorkspace.cs`, because they had no target type. The same focused build at the recorded baseline succeeded. These failures are in formatter-generated modernizations, not missing-import diagnostics, and were not repaired by hand.
- M0028 PR #13's output removed required NuGet imports and then accumulated 48 blank lines on another normalization. M0029 retained the imports and reached a one-pass fixed point, but exposed the collection-expression compile failures. The generated source is unmodified in PR #14.

## Interpretation and follow-up

The observed import result supports the inference that allowing SDK-native restore supplies the dependency context that was missing in M0028; this does not establish that all downstream project shapes are safe. The compile errors show that one-pass stability and import preservation do not certify semantic validity. Review the specific collection-expression modernization behavior and its syntax eligibility before considering any production use. Re-run this comparison if the SDK version, IDE0005 integration, or relevant modernization eligibility changes. No conclusion about semantic equivalence or broad consumer compatibility is claimed.
