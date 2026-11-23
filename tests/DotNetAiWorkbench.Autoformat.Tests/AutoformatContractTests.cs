using System.Diagnostics;
using System.Text.Json;
using DotNetAiWorkbench.Autoformat.Core;

namespace DotNetAiWorkbench.Autoformat.Tests;

public class AutoformatContractTests
{
    [Test]
    public async Task ApprovedDiagnosticSetRemainsFixed()
        => await Assert.That(RewriteEngine.ApprovedDiagnostics).IsEquivalentTo(new[] { "IDE0005", "IDE0011", "IDE0036", "IDE0040", "IDE0090", "IDE0034", "IDE0004", "IDE0037" });

    [Test]
    public async Task InvalidModeIsRejectedBeforeWorkspaceCreation()
    {
        string root = CreateRepo();
        try { await Assert.That(() => new RewriteEngine(root).Rewrite("analyzers", [])).Throws<ArgumentException>(); }
        finally { Directory.Delete(root, true); }
    }

    [Test]
    public async Task RemovedFormatterSwitchesAreRejectedBeforeMutation()
    {
        string root = CreateRepo();
        try
        {
            string source = Path.Combine(root, "Sample.cs");
            const string original = "public class Sample{ }\n";
            await File.WriteAllTextAsync(source, original);
            foreach (string[] args in new[] { new[] { "--check" }, new[] { "--changed" }, new[] { "format", "Sample.cs", "--check" }, new[] { "normalize", "--changed" } })
            {
                (int exitCode, _) = await RunAutoformatCliAsync(root, args);
                await Assert.That(exitCode).IsNotEqualTo(0);
                await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(original);
            }
        }
        finally { Directory.Delete(root, true); }
    }

    [Test]
    public async Task RewriteUsesDirectBatchedSdkPhasesAndForwardsRestoreChoice()
    {
        string root = CreateRepo();
        string project = Path.Combine(root, "Fixture.csproj"), source = Path.Combine(root, "Sample.cs");
        await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        await File.WriteAllTextAsync(source, "class Sample { }\n");
        await RestoreProjectAsync(root, project);
        try
        {
            var calls = new List<(string Phase, bool NoRestore)>();
            var engine = new RewriteEngine(root, (_, phase, files, _, noRestore, _) =>
            {
                calls.Add((phase, noRestore));
                if (phase == "style") { File.WriteAllText(files[0], "class Sample { int Value; }\n"); }
                return (0, string.Empty);
            });
            RewriteResult result = engine.Rewrite("normalize", [source], noRestore: true);
            await Assert.That(calls).IsEquivalentTo(new[] { ("style", true), ("whitespace", true) });
            await Assert.That(result.ChangedPaths).IsEquivalentTo(new[] { "Sample.cs" });
            await Assert.That(await File.ReadAllTextAsync(source)).Contains("Value");
        }
        finally { Directory.Delete(root, true); }
    }

    [Test]
    public async Task FailedSdkPhaseReportsPartialEditsWithoutRollingThemBack()
    {
        string root = CreateRepo();
        string project = Path.Combine(root, "Fixture.csproj"), source = Path.Combine(root, "Sample.cs");
        await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        await File.WriteAllTextAsync(source, "public class Sample { }\n");
        await RestoreProjectAsync(root, project);
        try
        {
            var engine = new RewriteEngine(root, (_, phase, files, _, _, _) =>
            {
                if (phase == "style") { File.WriteAllText(files[0], "public class Sample { public int Value = 1; }\n"); return (17, "forced SDK failure"); }
                return (0, string.Empty);
            });
            ProductException? observed = null;
            try { _ = engine.Rewrite("normalize", []); } catch (ProductException error) { observed = error; }
            await Assert.That(observed).IsNotNull();
            await Assert.That(observed!.Message).Contains("formatting is incomplete");
            await Assert.That(await File.ReadAllTextAsync(source)).Contains("Value = 1");
        }
        finally { Directory.Delete(root, true); }
    }

    [Test]
    public async Task HeadOrIndexChangesDuringMutationAreReportedIncomplete()
    {
        foreach (string mutation in new[] { "head", "index" })
        {
            string root = Path.Combine(Path.GetTempPath(), "autoformat-git-contract-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string project = Path.Combine(root, "Fixture.csproj"), source = Path.Combine(root, "Sample.cs");
            await File.WriteAllTextAsync(Path.Combine(root, ".gitignore"), "**/obj/\n**/bin/\n");
            await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(source, "class Sample { }\n");
            await GitAsync(root, "init", "-q", "-b", "main");
            await GitAsync(root, "config", "user.email", "test@example.invalid");
            await GitAsync(root, "config", "user.name", "Test");
            await GitAsync(root, "add", ".");
            await GitAsync(root, "commit", "-qm", "baseline");
            await RestoreProjectAsync(root, project);
            try
            {
                var engine = new RewriteEngine(root, (_, _, _, _, _, _) =>
                {
                    if (mutation == "head") { RunGit(root, "commit", "--allow-empty", "-qm", "concurrent"); }
                    else { File.WriteAllText(Path.Combine(root, "Other.cs"), "class Other {}\n"); RunGit(root, "add", "Other.cs"); }
                    return (0, string.Empty);
                });
                ProductException? error = null;
                try { _ = engine.Rewrite("format", []); } catch (ProductException observed) { error = observed; }
                await Assert.That(error).IsNotNull();
                await Assert.That(error!.Message).Contains("HEAD or index changed");
            }
            finally { DeleteTree(root); }
        }
    }

    private static string CreateRepo() { string root = Path.Combine(Path.GetTempPath(), "autoformat-contract-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(Path.Combine(root, ".git")); return root; }
    private static async Task RestoreProjectAsync(string root, string project)
    {
        using Process process = Process.Start(new ProcessStartInfo("dotnet", ["restore", project, "--ignore-failed-sources"]) { WorkingDirectory = root, UseShellExecute = false })!;
        await process.WaitForExitAsync(); await Assert.That(process.ExitCode).IsEqualTo(0);
    }
    private static async Task GitAsync(string root, params string[] args)
    {
        using Process process = Process.Start(new ProcessStartInfo("git", args) { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true })!;
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) { throw new InvalidOperationException(await process.StandardError.ReadToEndAsync()); }
    }
    private static void RunGit(string root, params string[] args)
    {
        using Process process = Process.Start(new ProcessStartInfo("git", args) { WorkingDirectory = root, UseShellExecute = false })!;
        process.WaitForExit();
        if (process.ExitCode != 0) { throw new InvalidOperationException("Concurrent Git mutation failed."); }
    }
    private static void DeleteTree(string root)
    {
        if (!Directory.Exists(root)) { return; }
        foreach (string path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) { File.SetAttributes(path, FileAttributes.Normal); }
        Directory.Delete(root, true);
    }
    private static async Task<(int ExitCode, string Output)> RunAutoformatCliAsync(string root, string[] args)
    {
        string cli = Path.Combine(AppContext.BaseDirectory, "DotNetAiWorkbench.Autoformat.Cli.dll");
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(cli);
        foreach (string argument in args) { start.ArgumentList.Add(argument); }
        using Process process = Process.Start(start)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(), stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(); await Task.WhenAll(stdout, stderr); return (process.ExitCode, stdout.Result + stderr.Result);
    }
}
