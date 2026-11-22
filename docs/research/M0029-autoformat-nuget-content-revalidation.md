# M0029 — Autoformat NuGet-content revalidation

**Amended result:** The collection-expression eligibility correction retained the three previously invalid array creation forms. The regenerated target output builds successfully, converges after one pass, and remains published as an unaccepted, DO NOT MERGE Draft PR for inspection.

## Scope and provenance

The experiment compares formatter output on the same target baseline, `carlrabbit/dotnet-nuget-content` `main` at `25b28310c7de04426de02e4ac8037b32ed9dbd9e`. Workbench implementation began at amended planning commit `a9b2846578171298bb459a17282160e919ece213`. The formatter source for the corrected rerun is workbench commit [`68f3d7de6a63f48f2d49f5cb178a0a4a80b3c9c0`](https://github.com/carlrabbit/dotnet-ai-code-hygiene/commit/68f3d7de6a63f48f2d49f5cb178a0a4a80b3c9c0). Toolchain: .NET SDK `11.0.100-rc.3.26522.148`, Git `2.56.0.windows.4`.

The existing [target Draft PR #14](https://github.com/carlrabbit/dotnet-nuget-content/pull/14) remains open and marked **DO NOT MERGE**. It retains policy commit `ac183618012c474821b6f6ac2cc9f2b55a5d325f` and the initial output commit `08781f4d3ae7e71bde39bc1e6af9271355ce6b12`, followed by corrected raw-output delta commit [`2edddf2740e09c36bdb1fef879c3fec6ddbb9516`](https://github.com/carlrabbit/dotnet-nuget-content/commit/2edddf2740e09c36bdb1fef879c3fec6ddbb9516). The regenerated complete output snapshot in the fresh disposable checkout is commit `84ab6a63d89f46b932f2c83b9cfe6c7b8610de57`; comparing it with the previous PR #14 snapshot changed only five files (17 insertions, 14 deletions). The original [M0028 Draft PR #13](https://github.com/carlrabbit/dotnet-nuget-content/pull/13) was not modified.

## Experiment history

### Initial M0029 output

The first M0029 run used formatter source `0fadcdef0516bfd6a2ffa631079adc4115744fe7` and created target output commit `08781f4d3ae7e71bde39bc1e6af9271355ce6b12`. It retained required NuGet imports and reached one-pass convergence, but a focused build failed at three generated collection expressions with `CS9176`: two `foreach` sources in `Program.cs` and one in `ProjectWorkspace.cs` had no target type. This initial output is preserved as the parent snapshot in PR #14, not overwritten.

### Amended collection-expression rerun

A fresh disposable worktree was checked out at the same target baseline and given the existing Autoformat policy from commit `ac183618012c474821b6f6ac2cc9f2b55a5d325f` (local setup commit in that scratch worktree: `40eea5ff28274f0d072597f27d4142e3634a64bd`). The corrected CLI was built from the workbench source commit above and run directly:

```text
dotnet C:\src\dotnet-ai-code-hygiene\src\DotNetAiWorkbench.Autoformat.Cli\bin\Release\net11.0\DotNetAiWorkbench.Autoformat.Cli.dll normalize --output json
dotnet build src\DotnetNugetContent\DotNetNugetContent.csproj
dotnet C:\src\dotnet-ai-code-hygiene\src\DotNetAiWorkbench.Autoformat.Cli\bin\Release\net11.0\DotNetAiWorkbench.Autoformat.Cli.dll normalize --output json
dotnet C:\src\dotnet-ai-code-hygiene\src\DotNetAiWorkbench.Autoformat.Cli\bin\Release\net11.0\DotNetAiWorkbench.Autoformat.Cli.dll normalize --check --output json
dotnet C:\src\dotnet-ai-code-hygiene\src\DotNetAiWorkbench.Autoformat.Cli\bin\Release\net11.0\DotNetAiWorkbench.Autoformat.Cli.dll format --check --output json
```

The first pass changed the 9 selected C# files; its unmodified generated output was snapshot commit `84ab6a63d89f46b932f2c83b9cfe6c7b8610de57`. The actual second mutating pass reported `changedCount: 0`. `normalize --check` and `format --check` each reported `changedCount: 0`. All three observed `foreach` sources remain `new[] { ... }`; required `NuGet.Versioning`, `NuGet.Frameworks`, and `NuGet.Packaging` imports remain.

**Observed diagnostic result:** `dotnet build src\DotnetNugetContent\DotNetNugetContent.csproj` succeeded with 0 warnings and 0 errors (restore reported all projects up-to-date). The three `CS9176` failures from the initial M0029 result no longer occur. No other build failure was observed. This focused build only checks the known defect; target tests, full Hygiene suites, package/release qualification, Native AOT, installed-tool qualification, and broad compatibility were not evaluated.

## Interpretation and limitations

The direct-target syntax allowlist prevented collection-expression conversion for `foreach` sources and comparable untyped uses, while typed variables, properties, returns, concrete `List<T>`, and eligible collection-interface targets still modernized in the deterministic canonicalizer regression. This establishes the requested syntax-level eligibility behavior for covered forms; it does not claim semantic equivalence or compatibility for every consumer shape. PR #14 remains experimental and should receive separate review before any production use.

## Nested return-context follow-up

Source review found that a return inside a nested local function could find an outer method before its own return type. Return target resolution now selects the nearest return-owner boundary: local functions and methods supply their own declared type, while a nearer anonymous function is a conservative barrier with no inferred target. Corrected follow-up commit: `5d432577450dbd963c771341a794dd484d7a0ff2`.

The single focused `CollectionExpressionsRequireARecognizedTargetTypeContext` regression now covers an eligible outer return, an `object` local function retaining `new[]`, a collection-returning local function converting to `[...]`, and a block-bodied `Func<object>` lambda retaining `new[]`. It passed. `dotnet build src\DotNetAiWorkbench.Autoformat.Cli\DotNetAiWorkbench.Autoformat.Cli.csproj -c Release --no-restore` passed with 0 warnings/errors. A read-only `normalize --check --output json` against current PR #14 head `2edddf2740e09c36bdb1fef879c3fec6ddbb9516` reported `changedCount: 0` for all 9 selected files. Therefore this follow-up does not affect the previously regenerated NuGet-content output; its existing experiment commits remain unchanged and no target regeneration was needed. The review's illustrative snippet was not independently compiled; the focused canonicalizer test validates the rewriting behavior, and the prior target diagnostic build remains evidence only for that target revision.
