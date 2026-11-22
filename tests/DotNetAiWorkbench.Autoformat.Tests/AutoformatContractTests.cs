using System.Diagnostics;
using DotNetAiWorkbench.Autoformat.Core;

namespace DotNetAiWorkbench.Autoformat.Tests;

public class AutoformatContractTests
{
    [Test]
    public async Task ApprovedDiagnosticSetRemainsFixed()
    {
        await Assert.That(RewriteEngine.ApprovedDiagnostics).IsEquivalentTo(new[] { "IDE0005", "IDE0011", "IDE0036", "IDE0040", "IDE0090", "IDE0034", "IDE0004", "IDE0037" });
    }

    [Test]
    public async Task InvalidModeIsRejectedBeforeWorkspaceCreation()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git"))) { directory = directory.Parent; }
        var engine = new RewriteEngine(directory!.FullName);
        await Assert.That(() => engine.Rewrite("analyzers", [], false, true)).Throws<ArgumentException>();
    }

    [Test]
    public async Task FailedSdkPhaseReportsPartialEditsWithoutRollingThemBack()
    {
        string root = Path.Combine(Path.GetTempPath(), "autoformat-failure-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        string project = Path.Combine(root, "Fixture.csproj");
        string source = Path.Combine(root, "Sample.cs");
        await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        await File.WriteAllTextAsync(source, "public class Sample { }\n");

        using var restore = Process.Start(new ProcessStartInfo("dotnet", ["restore", project, "--ignore-failed-sources"]) { WorkingDirectory = root, UseShellExecute = false })!;
        await restore.WaitForExitAsync();
        await Assert.That(restore.ExitCode).IsEqualTo(0);

        var engine = new RewriteEngine(root, (_, phase, files, _, _, _, _) =>
        {
            if (phase == "style")
            {
                File.WriteAllText(files[0], "public class Sample { public int Value = 1; }\n");
                return (17, "forced SDK failure");
            }

            return (0, string.Empty);
        });
        ProductException? observed = null;
        try { _ = engine.Rewrite("normalize", [], false, false); }
        catch (ProductException error) { observed = error; }

        await Assert.That(observed).IsNotNull();
        await Assert.That(observed!.Message).Contains("formatting is incomplete");
        await Assert.That(observed.Message).Contains("Sample.cs");
        await Assert.That(await File.ReadAllTextAsync(source)).Contains("Value = 1");
    }

    [Test]
    public async Task SdkRestoreKeepsRequiredNuGetImportAndRemovesUnusedImport()
    {
        string root = Path.Combine(Path.GetTempPath(), "autoformat-sdk-restore-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        string project = Path.Combine(root, "Fixture.csproj");
        string source = Path.Combine(root, "VersionProbe.cs");
        await File.WriteAllTextAsync(project, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup>
              <ItemGroup><PackageReference Include="NuGet.Packaging" Version="7.9.0" /></ItemGroup>
            </Project>
            """);
        AutoformatPolicy.EnsureInitialized(root);
        const string original = "using System.Text.Json;\nusing NuGet.Versioning;\npublic static class VersionProbe { public static string Parse(string value) => NuGetVersion.Parse(value).ToNormalizedString(); }\n";
        await File.WriteAllTextAsync(source, original);

        try
        {
            var engine = new RewriteEngine(root, requireCleanCheckpoint: false);
            RewriteResult normalized = engine.Rewrite("normalize", [source], changed: false, checkOnly: false);
            string result = await File.ReadAllTextAsync(source);
            await Assert.That(normalized.ChangedCount).IsEqualTo(1);
            await Assert.That(result).Contains("using NuGet.Versioning;");
            await Assert.That(result).DoesNotContain("using System.Text.Json;");

            byte[] canonicalBytes = await File.ReadAllBytesAsync(source);
            (int normalizeExit, string normalizeOutput) = await RunAutoformatCliAsync(root, "normalize", source, "--no-restore", "--check", "--output", "json");
            (int formatExit, string formatOutput) = await RunAutoformatCliAsync(root, "format", source, "--no-restore", "--check", "--output", "json");
            await Assert.That(normalizeExit).IsEqualTo(0);
            await Assert.That(formatExit).IsEqualTo(0);
            await Assert.That(System.Text.Json.JsonDocument.Parse(normalizeOutput).RootElement.GetProperty("changedCount").GetInt32()).IsEqualTo(0);
            await Assert.That(System.Text.Json.JsonDocument.Parse(formatOutput).RootElement.GetProperty("changedCount").GetInt32()).IsEqualTo(0);
            await Assert.That((await File.ReadAllBytesAsync(source)).AsSpan().SequenceEqual(canonicalBytes)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(root)) { Directory.Delete(root, recursive: true); }
        }
    }

    [Test]
    public async Task RestoreChoiceIsForwardedToSdkPhasesIncludingCheckOnlyPreview()
    {
        string root = Path.Combine(Path.GetTempPath(), "autoformat-restore-forwarding-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        string project = Path.Combine(root, "Fixture.csproj");
        string source = Path.Combine(root, "Sample.cs");
        await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        const string original = "class Sample { }\n";
        await File.WriteAllTextAsync(source, original);
        await RestoreProjectAsync(root, project);

        try
        {
            var calls = new List<(string Phase, bool CheckOnly, bool NoRestore)>();
            var engine = new RewriteEngine(root, (_, phase, _, _, checkOnly, noRestore, _) =>
            {
                calls.Add((phase, checkOnly, noRestore));
                return (0, string.Empty);
            });

            RewriteResult normalizeCheck = engine.Rewrite("normalize", [source], changed: false, checkOnly: true, noRestore: true);
            await Assert.That(normalizeCheck.CheckOnly).IsTrue();
            await Assert.That(calls.Select(call => call.Phase)).IsEquivalentTo(new[] { "style", "whitespace" });
            await Assert.That(calls.All(call => call.NoRestore)).IsTrue();
            await Assert.That((await File.ReadAllTextAsync(source)).AsSpan().SequenceEqual(original.AsSpan())).IsTrue();

            calls.Clear();
            _ = engine.Rewrite("format", [source], changed: false, checkOnly: true, noRestore: true);
            await Assert.That(calls).IsEquivalentTo(new[] { ("whitespace", true, true) });

            calls.Clear();
            _ = engine.Rewrite("format", [source], changed: false, checkOnly: true);
            await Assert.That(calls).IsEquivalentTo(new[] { ("whitespace", true, false) });
        }
        finally
        {
            if (Directory.Exists(root)) { Directory.Delete(root, recursive: true); }
        }
    }

    [Test]
    public async Task InitializationPreservesUnrelatedEditorConfigBytesAndDetectsApplicableLastOverride()
    {
        string root = Path.Combine(Path.GetTempPath(), "autoformat-policy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "src"));
        Directory.CreateDirectory(Path.Combine(root, "docs"));
        string config = Path.Combine(root, ".editorconfig");
        string original = "root = true\r\n\r\n[*.md]\r\ntrim_trailing_whitespace = true";
        await File.WriteAllTextAsync(config, original, new System.Text.UTF8Encoding(false));
        await File.WriteAllTextAsync(Path.Combine(root, "src", "Sample.cs"), "class Sample {}\n");
        string project = Path.Combine(root, "Fixture.csproj");
        await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        await File.WriteAllTextAsync(Path.Combine(root, "docs", ".editorconfig"), "[*.cs]\ncsharp_prefer_braces = false\n[*.md]\nindent_size = 2\n");
        await File.WriteAllTextAsync(Path.Combine(root, "docs", "guide.md"), "text\n");
        using (var restore = Process.Start(new ProcessStartInfo("dotnet", ["restore", project, "--ignore-failed-sources"]) { WorkingDirectory = root, UseShellExecute = false })!)
        {
            await restore.WaitForExitAsync();
            await Assert.That(restore.ExitCode).IsEqualTo(0);
        }
        AutoformatPolicy.EnsureInitialized(root);
        byte[] after = await File.ReadAllBytesAsync(config);
        await Assert.That(after.AsSpan(0, new System.Text.UTF8Encoding(false).GetByteCount(original)).SequenceEqual(System.Text.Encoding.UTF8.GetBytes(original))).IsTrue();
        await Assert.That(await File.ReadAllTextAsync(config)).Contains("[*.md]\r\ntrim_trailing_whitespace = true");

        await Git(root, "init", "-q", "-b", "main");
        await Git(root, "config", "user.email", "test@example.invalid");
        await Git(root, "config", "user.name", "Test");
        await Git(root, "add", ".editorconfig", "Fixture.csproj", "src/Sample.cs", "docs/.editorconfig", "docs/guide.md");
        await Git(root, "commit", "-m", "initialized policy");
        byte[] initialized = await File.ReadAllBytesAsync(config);
        await File.AppendAllTextAsync(config, "\r\ncsharp_prefer_braces = false\r\n");
        byte[] dirtyDuplicate = await File.ReadAllBytesAsync(config);
        ProductException? duplicate = null;
        try { AutoformatPolicy.EnsureInitialized(root); } catch (ProductException error) { duplicate = error; }
        await Assert.That(duplicate).IsNotNull();
        await Assert.That((await File.ReadAllBytesAsync(config)).AsSpan().SequenceEqual(dirtyDuplicate)).IsTrue();

        await File.WriteAllBytesAsync(config, initialized);
        await File.AppendAllTextAsync(config, "\r\n[*.cs]\r\ncsharp_prefer_braces = false\r\n");
        ProductException? conflict = null;
        try { AutoformatPolicy.EnsureInitialized(root); } catch (ProductException error) { conflict = error; }
        await Assert.That(conflict).IsNotNull();
        await Assert.That(conflict!.Message).Contains("Sample.cs");
    }

    [Test]
    public async Task InitializationRejectsConcurrentEditorConfigChangeWithoutReplacingIt()
    {
        string root = Path.Combine(Path.GetTempPath(), "autoformat-race-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string config = Path.Combine(root, ".editorconfig");
        await File.WriteAllTextAsync(config, "# authored\n");
        ProductException? observed = null;
        try
        {
            AutoformatPolicy.EnsureInitialized(root, () => File.AppendAllText(config, "# concurrent edit\n"));
        }
        catch (ProductException error) { observed = error; }
        await Assert.That(observed).IsNotNull();
        await Assert.That(await File.ReadAllTextAsync(config)).IsEqualTo("# authored\n# concurrent edit\n");
    }

    [Test]
    public async Task ConcurrentHeadOrIndexMutationIsReportedAsIncomplete()
    {
        foreach (string mutation in new[] { "head", "index" })
        {
            string root = Path.Combine(Path.GetTempPath(), "autoformat-git-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string source = Path.Combine(root, "Sample.cs");
            await File.WriteAllTextAsync(source, "public class Sample { }\n");
            await Git(root, "init", "-q", "-b", "main");
            await Git(root, "config", "user.email", "test@example.invalid");
            await Git(root, "config", "user.name", "Test");
            await Git(root, "add", "Sample.csproj", "Sample.cs");
            await Git(root, "commit", "-m", "baseline");
            using var restore = Process.Start(new ProcessStartInfo("dotnet", ["restore", "Sample.csproj", "--ignore-failed-sources"]) { WorkingDirectory = root, UseShellExecute = false })!;
            await restore.WaitForExitAsync();
            await Assert.That(restore.ExitCode).IsEqualTo(0);
            var engine = new RewriteEngine(root, (_, phase, _, _, _, _, _) =>
            {
                if (phase == "whitespace")
                {
                    if (mutation == "head")
                    {
                        RunGit(root, "commit", "--allow-empty", "-m", "concurrent");
                    }
                    else { File.WriteAllText(Path.Combine(root, "Other.cs"), "class Other {}\n"); RunGit(root, "add", "Other.cs"); }
                }
                return (0, string.Empty);
            });
            ProductException? error = null;
            try { engine.Rewrite("format", [], false, false); } catch (ProductException e) { error = e; }
            await Assert.That(error).IsNotNull();
            await Assert.That(error!.Message).Contains("HEAD or index changed");
        }
    }

    private static async Task Git(string root, params string[] args)
    {
        using Process process = Process.Start(new ProcessStartInfo("git", args) { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true })!;
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(await process.StandardError.ReadToEndAsync());
        }
    }

    private static async Task RestoreProjectAsync(string root, string project)
    {
        using Process process = Process.Start(new ProcessStartInfo("dotnet", ["restore", project, "--ignore-failed-sources"]) { WorkingDirectory = root, UseShellExecute = false })!;
        await process.WaitForExitAsync();
        await Assert.That(process.ExitCode).IsEqualTo(0);
    }

    private static async Task<(int ExitCode, string Output)> RunAutoformatCliAsync(string root, params string[] args)
    {
        string cli = Path.Combine(AppContext.BaseDirectory, "DotNetAiWorkbench.Autoformat.Cli.dll");
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(cli);
        foreach (string argument in args) { start.ArgumentList.Add(argument); }
        using Process process = Process.Start(start)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(stdout, stderr);
        return (process.ExitCode, stdout.Result + stderr.Result);
    }

    private static void RunGit(string root, params string[] args)
    {
        using Process process = Process.Start(new ProcessStartInfo("git", args) { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true })!;
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("Concurrent Git mutation failed.");
        }
    }
}
