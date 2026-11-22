# M0028 Autoformat exploration: dotnet-nuget-content

**Status:** Investigative, non-authoritative; target Draft PR not opened because the raw output has a confirmed compile-breaking source-loss defect.

## Question and scope

Does the merged M0027 Autoformat implementation produce useful, readable, convergent C# normalization on a small real .NET CLI repository, and does its check-only path agree with the resulting source? The exercise selected all nine tracked C# files under `src/` and `tests/` in `carlrabbit/dotnet-nuget-content`. It did not run target tests, Native AOT publication, package qualification, or installed-tool qualification.

## Provenance and commands

- Target baseline: [`carlrabbit/dotnet-nuget-content` main at `25b28310c7de04426de02e4ac8037b32ed9dbd9e`](https://github.com/carlrabbit/dotnet-nuget-content/commit/25b28310c7de04426de02e4ac8037b32ed9dbd9e). A disposable clone used local branch `experiment/m0028-autoformat-canonicalization`; its raw commits remain local and were not pushed.
- Workbench implementation baseline: accepted M0028 commit `02604e340c26f20d25463afa20089d9c870534c7`; M0027 was merged in `3e38b45b33cc6695d5dd096fcad031cbaa17f866`, with the CLI/check-only correction in `c2f91984b2e787551424c5f841f0ecec1faa39dd`. The CLI was run from `src/DotNetAiWorkbench.Autoformat.Cli/DotNetAiWorkbench.Autoformat.Cli.csproj`, not from an installed global tool.
- Environment: Windows 11, .NET SDK `11.0.100-rc.3.26522.148`, Git `2.56.0.windows.4`.
- In the disposable target checkout, the relevant invocation sequence was:

  ```powershell
  dotnet run --project <workbench>/src/DotNetAiWorkbench.Autoformat.Cli/DotNetAiWorkbench.Autoformat.Cli.csproj -- --help
  dotnet run --project <workbench>/src/DotNetAiWorkbench.Autoformat.Cli/DotNetAiWorkbench.Autoformat.Cli.csproj -- normalize --help
  dotnet run --project <workbench>/src/DotNetAiWorkbench.Autoformat.Cli/DotNetAiWorkbench.Autoformat.Cli.csproj -- init --output json
  git add -- .editorconfig; git commit -m 'chore: initialize Autoformat policy for M0028 experiment'
  dotnet run --no-build --project <workbench>/src/DotNetAiWorkbench.Autoformat.Cli/DotNetAiWorkbench.Autoformat.Cli.csproj -- normalize --output json
  dotnet run --no-build --project <workbench>/src/DotNetAiWorkbench.Autoformat.Cli/DotNetAiWorkbench.Autoformat.Cli.csproj -- normalize --check --output json
  dotnet run --no-build --project <workbench>/src/DotNetAiWorkbench.Autoformat.Cli/DotNetAiWorkbench.Autoformat.Cli.csproj -- normalize --output json
  dotnet run --no-build --project <workbench>/src/DotNetAiWorkbench.Autoformat.Cli/DotNetAiWorkbench.Autoformat.Cli.csproj -- normalize --check --output json
  dotnet run --no-build --project <workbench>/src/DotNetAiWorkbench.Autoformat.Cli/DotNetAiWorkbench.Autoformat.Cli.csproj -- format --check --output json
  dotnet build src/DotnetNugetContent/DotnetNugetContent.csproj
  ```

  Autoformat help described `init`, mutating `normalize`, and whitespace-only `format`; `normalize --check` performs the non-mutating full normalization preview. Initialization created only Autoformat's root `.editorconfig` policy. Its commit supplied the required clean Git checkpoint.

## Directly observed

- The first mutating normalization selected 9 files and changed all 9: `ConsumerReferenceUpdater.cs`, `Program.cs`, `ProjectWorkspace.cs`, `VersionSelection.cs`, `M0006Tests.cs`, `ManagedContentTests.cs`, `PackageLifecycleTests.cs`, `ProjectWorkspaceTests.cs`, and `TestAssembly.cs`. The first raw source commit (`c20f395`) contained 3,295 insertions and 575 deletions. The initial command's JSON was not captured by the shell; a subsequent read-only preview reported 4 remaining changed files.
- The second mutating pass changed those 4 files only by adding 48 blank lines. Following it, `normalize --check` selected 9 files and reported `changedCount: 0`; `format --check` also selected 9 and reported `changedCount: 0`. This shows convergence after the second pass for this checkout. The second raw source commit is `741c795`; policy setup is isolated in `900f73e`.
- Useful examples include brace insertion for single-line `if` statements (`src/DotnetNugetContent/Program.cs`), explicit local types, and array/list collection expressions in tests. SDK style also removed `using` directives it considered unnecessary.
- The output is often visually excessive: repeated blank lines around statements, large vertical expansions of dense source, and awkward breaks around member-access/assertion chains appear in `ProjectWorkspace.cs`, `Program.cs`, and `PackageLifecycleTests.cs`. Many long lines remain; the report repeats notices for lines beyond the 120-character soft target. These are observed formatting characteristics, not by themselves compilation defects.
- The normalized application project failed to build with five unresolved NuGet symbols, including `NuGetVersion`, `NuGetFramework`, `IPackageFile`, and `FrameworkSpecificGroup`. The diff removed `using NuGet.Versioning`, `using NuGet.Frameworks`, and `using NuGet.Packaging` from files that still refer to those symbols. A separate detached worktree at the unmodified baseline built successfully with zero warnings/errors, confirming the failure is introduced by the generated diff.
- No manual repairs were made. The disposable experiment branch contains policy setup plus the two raw normalization commits; it was not pushed. Target `main` and other target branches were untouched.

## Assessment and limits

- **Useful transformations observed:** braces, explicit local types, collection-expression conversions, and removal of genuinely unused imports are plausible improvements. A successful build would still not establish semantic equivalence.
- **Intentional but potentially disruptive:** collection expressions can change the concrete collection type; the project has list/array patterns in tests. Treat each such conversion as requiring downstream API/behavior review.
- **Likely defect, confirmed by diagnostic build:** SDK `IDE0005` handling removed required imports in this repository context and broke compilation. The issue is associated with import selection, not fixed in this milestone.
- **Unsupported or unverified:** this was not a complete semantic review of every hunk. Comment/directive preservation, accessibility/metadata changes, all modernization eligibility boundaries, and downstream test behavior were not exhaustively audited. No target tests, AOT build, package operations, or semantic-equivalence checks were run. The formatter report's long-line notices are informational.
- **Inferences:** the removal appears to stem from the SDK style phase's unnecessary-using result; the experiment does not isolate which analyzer/project-loading condition caused that result.

## Safety stop and follow-up

No target Draft PR was opened. The raw formatter diff contains a confirmed source-loss/build defect, so publishing it would violate M0028's instruction not to push unsafe output merely to satisfy the PR deliverable. The exact raw output is preserved only in the disposable local clone at commits `900f73e`, `c20f395`, and `741c795`; it is not available as a remote review artifact. **No target PR URL exists.**

Recommended follow-up: isolate the `IDE0005` import-removal failure in a separately scoped Autoformat correction milestone, preserve a small project-aware regression for required cross-file NuGet symbols, then repeat this exploration against a newly recorded target revision before opening a raw-output Draft PR. Revalidate whenever the Autoformat engine/SDK style handling or target revision changes. No project authority was changed based on this experiment.
