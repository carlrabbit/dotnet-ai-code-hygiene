using DotNetAiWorkbench.Autoformat.Core;
using System.Diagnostics;

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
}
