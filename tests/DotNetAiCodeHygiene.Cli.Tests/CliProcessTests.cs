using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace DotNetAiCodeHygiene.Cli.Tests;

public sealed class CliProcessTests
{
    private static readonly string CliAssemblyPath = GetCliAssemblyPath();

    [Test]
    public async Task RootHelpListsAllReservedCommands()
    {
        ProcessResult result = await RunCliAsync("--help");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput).Contains("format");
        await Assert.That(result.StandardOutput).Contains("normalize");
        await Assert.That(result.StandardOutput).Contains("check");
        await Assert.That(result.StandardOutput).Contains("explain");
        await Assert.That(result.StandardOutput).Contains("ignore");
        await Assert.That(result.StandardOutput).Contains("unignore");
        await Assert.That(result.StandardOutput).Contains("ignores");
        await Assert.That(result.StandardOutput).Contains("rules");
        await Assert.That(result.StandardError).IsEmpty();
    }

    [Test]
    [Arguments("format")]
    [Arguments("normalize")]
    [Arguments("check")]
    [Arguments("explain")]
    [Arguments("ignore")]
    [Arguments("unignore")]
    [Arguments("ignores")]
    [Arguments("rules")]
    public async Task ReservedCommandHelpSucceeds(string command)
    {
        ProcessResult result = await RunCliAsync(command, "--help");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput).Contains(command);
        await Assert.That(result.StandardError).IsEmpty();
    }

    [Test]
    public async Task VersionUsesProductVersionOnStandardOutput()
    {
        ProcessResult result = await RunCliAsync("--version");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput.Trim()).IsEqualTo("0.1.0");
        await Assert.That(result.StandardError).IsEmpty();
    }

    [Test]
    public async Task UnknownCommandUsesInvalidInvocationContract()
    {
        ProcessResult result = await RunCliAsync("not-a-command");

        await Assert.That(result.ExitCode).IsEqualTo(2);
        await Assert.That(result.StandardError).Contains("not-a-command");
        await Assert.That(result.StandardOutput).IsEmpty();
    }

    [Test]
    public async Task MalformedOptionUsesInvalidInvocationContract()
    {
        ProcessResult result = await RunCliAsync("--not-a-real-option");

        await Assert.That(result.ExitCode).IsEqualTo(2);
        await Assert.That(result.StandardError).Contains("--not-a-real-option");
        await Assert.That(result.StandardOutput).IsEmpty();
    }

    [Test]
    public async Task IsolatedRepositorySupportsJsonExplainAndIgnoreLifecycle()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        try
        {
            await RunProcessAsync("git", repo, "init", "-q");
            Directory.CreateDirectory(Path.Combine(repo, "src"));
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.cs"), "public class Fixture { public string Long = \"" + new string('x', 205) + "\"; public void Run() { } }\n");
            ProcessResult first = await RunCliInAsync(repo, "check", "--output", "json");
            await Assert.That(first.ExitCode).IsEqualTo(0);
            using JsonDocument json = JsonDocument.Parse(first.StandardOutput);
            string finding = json.RootElement.GetProperty("findings")[0].GetProperty("id").GetString()!;
            string run = json.RootElement.GetProperty("runId").GetString()!;
            ProcessResult explanation = await RunCliInAsync(repo, "explain", run + "/" + finding, "--output", "json");
            await Assert.That(explanation.ExitCode).IsEqualTo(0);
            ProcessResult ignored = await RunCliInAsync(repo, "ignore", finding, "--reason", "Reviewed");
            await Assert.That(ignored.ExitCode).IsEqualTo(0);
            using (JsonDocument decisions = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(repo, ".hygiene", "decisions.json"))))
            {
                await Assert.That(decisions.RootElement.TryGetProperty("schemaVersion", out _)).IsTrue();
                await Assert.That(decisions.RootElement.GetProperty("decisions")[0].TryGetProperty("fingerprint", out _)).IsTrue();
            }
            ProcessResult second = await RunCliInAsync(repo, "check", "--output", "json");
            using JsonDocument after = JsonDocument.Parse(second.StandardOutput);
            await Assert.That(after.RootElement.GetProperty("ignoredCount").GetInt32()).IsEqualTo(1);
            ProcessResult list = await RunCliInAsync(repo, "ignores", "--output", "json");
            await Assert.That(list.StandardOutput).Contains("active");
            string ignoreId = JsonDocument.Parse(list.StandardOutput).RootElement.GetProperty("ignores")[0].GetProperty("id").GetString()!;
            ProcessResult fileFiltered = await RunCliInAsync(repo, "ignores", "src/Fixture.cs", "--output", "json");
            using JsonDocument filteredFile = JsonDocument.Parse(fileFiltered.StandardOutput);
            await Assert.That(filteredFile.RootElement.GetProperty("ignores").GetArrayLength()).IsEqualTo(1);
            ProcessResult directoryFiltered = await RunCliInAsync(repo, "ignores", "src", "--output", "json");
            using JsonDocument filteredDirectory = JsonDocument.Parse(directoryFiltered.StandardOutput);
            await Assert.That(filteredDirectory.RootElement.GetProperty("ignores").GetArrayLength()).IsEqualTo(1);
            await Assert.That((await RunCliInAsync(repo, "unignore", ignoreId)).ExitCode).IsEqualTo(0);
            ProcessResult staleHandle = await RunCliInAsync(repo, "explain", "R-OLD/F-1");
            await Assert.That(staleHandle.ExitCode).IsEqualTo(3);
            ProcessResult text = await RunCliInAsync(repo, "check", "--output", "text");
            await Assert.That(text.ExitCode).IsEqualTo(0);
            await Assert.That(text.StandardOutput).Contains("Run R-");
            await Assert.That(text.StandardOutput).Contains("finding(s)");
            await Assert.That(text.StandardOutput).Contains("review-candidate");
            await Assert.That((await RunCliInAsync(repo, "rules", "disable", "docs.summary.required")).ExitCode).IsEqualTo(0);
            await Assert.That((await RunCliInAsync(repo, "rules", "disable", "readability.long-line.review")).ExitCode).IsEqualTo(0);
            await Assert.That((await RunCliInAsync(repo, "rules", "disable", "readability.control-flow.visual-block")).ExitCode).IsEqualTo(0);
            ProcessResult empty = await RunCliInAsync(repo, "check", "--output", "json");
            using JsonDocument noFindings = JsonDocument.Parse(empty.StandardOutput);
            await Assert.That(noFindings.RootElement.GetProperty("findings").GetArrayLength()).IsEqualTo(0);
            await Assert.That((await RunCliInAsync(repo, "rules", "enable", "docs.summary.required")).ExitCode).IsEqualTo(0);
            await Assert.That((await RunCliInAsync(repo, "rules", "enable", "docs.summary.required")).ExitCode).IsEqualTo(0);
            ProcessResult enabledAgain = await RunCliInAsync(repo, "check", "--output", "json");
            using JsonDocument enabledResult = JsonDocument.Parse(enabledAgain.StandardOutput);
            await Assert.That(enabledResult.RootElement.GetProperty("findings").GetArrayLength()).IsGreaterThan(0);
            ProcessResult unknownRule = await RunCliInAsync(repo, "rules", "enable", "unknown.rule");
            await Assert.That(unknownRule.ExitCode).IsEqualTo(3);
            ProcessResult conflictingTargets = await RunCliInAsync(repo, "check", "src/Fixture.cs", "--changed");
            await Assert.That(conflictingTargets.ExitCode).IsEqualTo(2);
            ProcessResult invalidTarget = await RunCliInAsync(repo, "check", "missing.cs");
            await Assert.That(invalidTarget.ExitCode).IsEqualTo(3);
            ProcessResult noGit = await RunCliInAsync(repo, true, "check", "--changed");
            await Assert.That(noGit.ExitCode).IsEqualTo(4);
            string configPath = Path.Combine(repo, ".hygiene", "config.json");
            const string malformedConfig = "{\"schemaVersion\":99,\"disabledRules\":[]}";
            await File.WriteAllTextAsync(configPath, malformedConfig);
            ProcessResult badState = await RunCliInAsync(repo, "rules");
            await Assert.That(badState.ExitCode).IsEqualTo(3);
            await Assert.That(await File.ReadAllTextAsync(configPath)).IsEqualTo(malformedConfig);
        }
        finally { Directory.Delete(repo, true); }
    }

    private static async Task<ProcessResult> RunCliAsync(params string[] arguments)
        => await RunCliInAsync(Environment.CurrentDirectory, arguments);

    private static async Task<ProcessResult> RunCliInAsync(string workingDirectory, params string[] arguments)
        => await RunCliInAsync(workingDirectory, false, arguments);

    private static async Task<ProcessResult> RunCliInAsync(string workingDirectory, bool hideGit, params string[] arguments)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        if (hideGit) startInfo.Environment["PATH"] = Path.GetDirectoryName(CliAssemblyPath)!;
        startInfo.ArgumentList.Add(CliAssemblyPath);

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the CLI process.");

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new ProcessResult(process.ExitCode, await standardOutput, await standardError);
    }

    private static async Task RunProcessAsync(string executable, string workingDirectory, params string[] args)
    {
        ProcessStartInfo info = new(executable) { WorkingDirectory = workingDirectory, RedirectStandardError = true, UseShellExecute = false };
        foreach (string arg in args) info.ArgumentList.Add(arg);
        using Process process = Process.Start(info)!;
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException(error);
    }

    private static string GetCliAssemblyPath()
    {
        string configuredPath = typeof(CliProcessTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "CliAssemblyPath")
            .Value!;
        return Path.GetFullPath(configuredPath);
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
