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

        var engine = new RewriteEngine(root, (_, phase, files, _, _, _) =>
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
            var engine = new RewriteEngine(root, (_, phase, _, _, _, _) =>
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
