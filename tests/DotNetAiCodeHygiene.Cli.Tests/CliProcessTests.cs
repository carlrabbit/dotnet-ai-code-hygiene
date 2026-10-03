using System.Diagnostics;
using System.Reflection;

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

    private static async Task<ProcessResult> RunCliAsync(params string[] arguments)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
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
