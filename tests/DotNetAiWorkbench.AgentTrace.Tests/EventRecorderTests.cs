using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DotNetAiWorkbench.AgentTrace;

namespace DotNetAiWorkbench.AgentTrace.Tests;

public sealed class EventRecorderTests
{
    [Test]
    public async Task RecordsExactCommandsAndReportedFailuresWithoutExecutingThem()
    {
        using var repo = new TemporaryRepository();
        await Assert.That((await repo.Cli("--help")).Stdout).Contains("command");
        await Assert.That((await repo.Cli("--help")).Stdout).DoesNotContain("run --");
        await Assert.That((await repo.Cli("--version")).Stdout.Trim()).IsEqualTo("0.1.0");
        await Assert.That((await repo.Cli("command", "start", "--session", "missing-command")).ExitCode).IsNotEqualTo(0);

        const string command = "pwsh -NoProfile -Command \"Set-Content -Path should-not-exist.txt -Value 'hello' | Write-Output\"";
        await Assert.That((await repo.Cli("start", "--session", "PR-45", "--actor", "agent-a")).ExitCode).IsEqualTo(0);
        ProcessResult start = await repo.Cli("command", "start", "--session", "PR-45", "--actor", "agent-a", "--command", command);
        await Assert.That(start.ExitCode).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(repo.Path, "should-not-exist.txt"))).IsFalse();
        ProcessResult end = await repo.Cli("command", "end", "--session", "PR-45", "--actor", "agent-a", "--command", command, "--exit-code", "7", "--status", "failed");
        await Assert.That(end.ExitCode).IsEqualTo(0);

        using JsonDocument export = JsonDocument.Parse((await repo.Cli("export", "--session", "PR-45")).Stdout);
        JsonElement events = export.RootElement.GetProperty("events");
        await Assert.That(events.GetArrayLength()).IsEqualTo(3);
        await Assert.That(events[1].GetProperty("commandText").GetString()).IsEqualTo(command);
        await Assert.That(events[2].TryGetProperty("commandText", out _)).IsFalse();
        await Assert.That(events[2].GetProperty("exitCode").GetInt32()).IsEqualTo(7);
        await Assert.That(events[2].TryGetProperty("eventId", out _)).IsFalse();
        JsonElement pair = export.RootElement.GetProperty("derived").GetProperty("commands")[0];
        await Assert.That(pair.GetProperty("durationMilliseconds").GetDouble() >= 0).IsTrue();
        await Assert.That(pair.GetProperty("ambiguous").GetBoolean()).IsFalse();

        string jsonl = (await repo.Cli("export", "--session", "PR-45", "--format", "jsonl")).Stdout;
        await Assert.That(jsonl.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length).IsEqualTo(5);
    }

    [Test]
    public async Task CallerKeysAndLifecycleAreAppendOnlyAndNeverGateRecording()
    {
        using var repo = new TemporaryRepository();
        await repo.Cli("start", "--session", "shared");
        await repo.Cli("start", "--session", "shared");
        await repo.Cli("pause", "--session", "shared", "--actor", "a");
        await repo.Cli("command", "start", "--session", "shared", "--actor", "b", "--command", "dotnet test");
        await repo.Cli("stop", "--session", "shared");
        await repo.Cli("stop", "--session", "shared");
        await repo.Cli("resume", "--session", "shared", "--actor", "a");
        await repo.Cli("command", "end", "--session", "shared", "--actor", "b", "--command", "dotnet test", "--exit-code", "0");
        ProcessResult[] concurrentMarks = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => repo.Cli("mark", "--session", "shared", "--actor", "agent-" + (i % 2), "--note", "parallel-" + i)));
        await Assert.That(concurrentMarks.All(result => result.ExitCode == 0)).IsTrue();

        using JsonDocument export = JsonDocument.Parse((await repo.Cli("export", "--session", "shared")).Stdout);
        await Assert.That(export.RootElement.GetProperty("events").GetArrayLength()).IsEqualTo(14);
        await Assert.That(export.RootElement.GetProperty("derived").GetProperty("commands").GetArrayLength()).IsEqualTo(1);
        string report = (await repo.Cli("report", "--session", "shared")).Stdout;
        await Assert.That(report).Contains("actor=a");
        await Assert.That(report).Contains("actor=b");
        string markdown = (await repo.Cli("report", "--session", "shared", "--format", "markdown")).Stdout;
        await Assert.That(markdown).Contains("# AgentTrace — `shared`");
    }

    [Test]
    public async Task IdenticalOverlappingCommandsUseFifoAndDiscloseAmbiguity()
    {
        using var repo = new TemporaryRepository();
        const string command = "pwsh -NoProfile -Command 'Start-Sleep 1'";
        await repo.Cli("command", "start", "--session", "fifo", "--actor", "agent", "--command", command);
        await repo.Cli("command", "start", "--session", "fifo", "--actor", "agent", "--command", command);
        await repo.Cli("command", "end", "--session", "fifo", "--actor", "agent", "--command", command, "--exit-code", "0");
        await repo.Cli("command", "end", "--session", "fifo", "--actor", "agent", "--command", command, "--exit-code", "0");
        using JsonDocument export = JsonDocument.Parse((await repo.Cli("export", "--session", "fifo")).Stdout);
        JsonElement pairs = export.RootElement.GetProperty("derived").GetProperty("commands");
        await Assert.That(pairs.GetArrayLength()).IsEqualTo(2);
        await Assert.That(pairs[0].GetProperty("startIndex").GetInt32()).IsEqualTo(0);
        await Assert.That(pairs[0].GetProperty("endIndex").GetInt32()).IsEqualTo(2);
        await Assert.That(pairs[1].GetProperty("startIndex").GetInt32()).IsEqualTo(1);
        await Assert.That(pairs[1].GetProperty("endIndex").GetInt32()).IsEqualTo(3);
        await Assert.That(pairs.EnumerateArray().All(pair => pair.GetProperty("ambiguous").GetBoolean())).IsTrue();

        await repo.Cli("command", "start", "--session", "fifo", "--command", command, "--label", "left");
        await repo.Cli("command", "start", "--session", "fifo", "--command", command, "--label", "right");
        await repo.Cli("command", "end", "--session", "fifo", "--command", command, "--label", "right", "--status", "complete");
        await repo.Cli("command", "end", "--session", "fifo", "--command", command, "--label", "left", "--status", "complete");
        using JsonDocument withLabels = JsonDocument.Parse((await repo.Cli("export", "--session", "fifo")).Stdout);
        JsonElement labeledPairs = withLabels.RootElement.GetProperty("derived").GetProperty("commands");
        await Assert.That(labeledPairs.EnumerateArray().Skip(2).All(pair => !pair.GetProperty("ambiguous").GetBoolean())).IsTrue();
        string repeatedReport = (await repo.Cli("report", "--session", "fifo")).Stdout;
        await Assert.That(repeatedReport).Contains("2 recorded executions");

        await repo.Cli("operation", "start", "--session", "fifo", "--actor", "agent", "--kind", "mcp", "--name", "tool.call");
        await repo.Cli("operation", "start", "--session", "fifo", "--actor", "agent", "--kind", "mcp", "--name", "tool.call");
        await repo.Cli("operation", "end", "--session", "fifo", "--actor", "agent", "--kind", "mcp", "--name", "tool.call", "--status", "complete");
        await repo.Cli("operation", "end", "--session", "fifo", "--actor", "agent", "--kind", "mcp", "--name", "tool.call", "--status", "complete");
        using JsonDocument operationExport = JsonDocument.Parse((await repo.Cli("export", "--session", "fifo")).Stdout);
        await Assert.That(operationExport.RootElement.GetProperty("derived").GetProperty("operations").EnumerateArray().All(pair => pair.GetProperty("ambiguous").GetBoolean())).IsTrue();
    }

    [Test]
    public async Task DistinctCommandContentCorrelatesAcrossActorsWithOutOfOrderCompletion()
    {
        using var repo = new TemporaryRepository();
        await repo.Cli("command", "start", "--session", "parallel", "--actor", "a", "--command", "dotnet build ProjectA.sln");
        await repo.Cli("command", "start", "--session", "parallel", "--actor", "b", "--command", "dotnet test ProjectB.sln");
        await repo.Cli("command", "end", "--session", "parallel", "--actor", "b", "--command", "dotnet test ProjectB.sln", "--exit-code", "0");
        await repo.Cli("command", "end", "--session", "parallel", "--actor", "a", "--command", "dotnet build ProjectA.sln", "--exit-code", "0");

        using JsonDocument export = JsonDocument.Parse((await repo.Cli("export", "--session", "parallel")).Stdout);
        JsonElement pairs = export.RootElement.GetProperty("derived").GetProperty("commands");
        await Assert.That(pairs.GetArrayLength()).IsEqualTo(2);
        await Assert.That(pairs.EnumerateArray().All(pair => !pair.GetProperty("ambiguous").GetBoolean())).IsTrue();
        await Assert.That(pairs[0].GetProperty("startIndex").GetInt32()).IsEqualTo(0);
        await Assert.That(pairs[0].GetProperty("endIndex").GetInt32()).IsEqualTo(3);
        await Assert.That(pairs[1].GetProperty("startIndex").GetInt32()).IsEqualTo(1);
        await Assert.That(pairs[1].GetProperty("endIndex").GetInt32()).IsEqualTo(2);
        await Assert.That(export.RootElement.GetProperty("derived").GetProperty("crossActorCommandOverlaps").GetArrayLength() > 0).IsTrue();
    }

    [Test]
    public async Task SensitiveCommandsWithAndWithoutLabelsStoreOnlyHashFlagAndOptionalLabelAcrossStores()
    {
        const string labeledSecret = "dotnet nuget push package.nupkg --api-key labeled-secret-xyz";
        const string unlabeledSecret = "dotnet nuget push private.nupkg --api-key unlabeled-secret-xyz";
        string labeledHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(labeledSecret))).ToLowerInvariant();
        string unlabeledHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(unlabeledSecret))).ToLowerInvariant();
        using var jsonl = new TemporaryRepository();
        using var sqlite = new TemporaryRepository();
        string externalDbRoot = Path.Combine(Path.GetTempPath(), "agenttrace-external-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(externalDbRoot);
        sqlite.ExternalDirectory = externalDbRoot;
        await sqlite.SetConfigAsync("sqlite", Path.Combine(externalDbRoot, "shared.db"));

        foreach (TemporaryRepository repo in new[] { jsonl, sqlite })
        {
            await repo.Cli("command", "start", "--session", "PR-45", "--actor", "publisher", "--command", labeledSecret, "--sensitive", "--label", "release");
            await repo.Cli("command", "end", "--session", "PR-45", "--actor", "publisher", "--command", labeledSecret, "--sensitive", "--label", "release", "--exit-code", "0");
            await repo.Cli("command", "start", "--session", "PR-45", "--actor", "publisher", "--command", unlabeledSecret, "--sensitive");
            await repo.Cli("command", "end", "--session", "PR-45", "--actor", "publisher", "--command", unlabeledSecret, "--sensitive", "--status", "complete");
        }

        ProcessResult[] exportResults = await Task.WhenAll(jsonl.Cli("export", "--session", "PR-45"), sqlite.Cli("export", "--session", "PR-45"));
        string[] exports = exportResults.Select(result => result.Stdout).ToArray();
        ProcessResult[] jsonlExportResults = await Task.WhenAll(jsonl.Cli("export", "--session", "PR-45", "--format", "jsonl"), sqlite.Cli("export", "--session", "PR-45", "--format", "jsonl"));
        await Assert.That(string.Join(" | ", jsonlExportResults.Where(result => result.ExitCode != 0).Select(result => result.Stderr))).IsEqualTo("");
        await Assert.That(jsonlExportResults.Select(result => result.ExitCode)).IsEquivalentTo(new[] { 0, 0 });
        string[] jsonlExports = jsonlExportResults.Select(result => result.Stdout).ToArray();
        foreach (string raw in exports)
        {
            await Assert.That(raw).DoesNotContain("labeled-secret-xyz");
            await Assert.That(raw).DoesNotContain("unlabeled-secret-xyz");
            await Assert.That(raw).DoesNotContain("package.nupkg");
            await Assert.That(raw).DoesNotContain("private.nupkg");
            await Assert.That(raw).Contains(labeledHash);
            await Assert.That(raw).Contains(unlabeledHash);
            using JsonDocument export = JsonDocument.Parse(raw);
            JsonElement events = export.RootElement.GetProperty("events");
            await Assert.That(events.GetArrayLength()).IsEqualTo(4);
            foreach (JsonElement item in events.EnumerateArray())
            {
                await Assert.That(item.GetProperty("sensitive").GetBoolean()).IsTrue();
                await Assert.That(item.TryGetProperty("commandText", out _)).IsFalse();
                await Assert.That(item.TryGetProperty("commandName", out _)).IsFalse();
            }

            await Assert.That(events[0].GetProperty("label").GetString()).IsEqualTo("release");
            await Assert.That(events[1].GetProperty("label").GetString()).IsEqualTo("release");
            await Assert.That(events[2].TryGetProperty("label", out _)).IsFalse();
            await Assert.That(events[3].TryGetProperty("label", out _)).IsFalse();
            JsonElement pairs = export.RootElement.GetProperty("derived").GetProperty("commands");
            await Assert.That(pairs.GetArrayLength()).IsEqualTo(2);
            await Assert.That(pairs[0].GetProperty("startIndex").GetInt32()).IsEqualTo(0);
            await Assert.That(pairs[0].GetProperty("endIndex").GetInt32()).IsEqualTo(1);
            await Assert.That(pairs[1].GetProperty("startIndex").GetInt32()).IsEqualTo(2);
            await Assert.That(pairs[1].GetProperty("endIndex").GetInt32()).IsEqualTo(3);
            await Assert.That(pairs.EnumerateArray().All(pair => pair.GetProperty("sensitive").GetBoolean() && !pair.GetProperty("ambiguous").GetBoolean())).IsTrue();
            await Assert.That(export.RootElement.GetProperty("derived").GetProperty("unmatchedCommandStarts").GetArrayLength()).IsEqualTo(0);
            await Assert.That(export.RootElement.GetProperty("derived").GetProperty("unmatchedCommandEnds").GetArrayLength()).IsEqualTo(0);
        }

        foreach (string raw in jsonlExports)
        {
            await Assert.That(raw).DoesNotContain("labeled-secret-xyz");
            await Assert.That(raw).DoesNotContain("unlabeled-secret-xyz");
            await Assert.That(raw).DoesNotContain("package.nupkg");
            await Assert.That(raw).DoesNotContain("private.nupkg");
            await Assert.That(raw).Contains(labeledHash);
            await Assert.That(raw).Contains(unlabeledHash);
        }

        using JsonDocument one = JsonDocument.Parse(exports[0]);
        using JsonDocument two = JsonDocument.Parse(exports[1]);
        string[] rawFields(JsonDocument doc) => doc.RootElement.GetProperty("events").EnumerateArray().Select(item => string.Join("|", item.GetProperty("type").GetString(), item.GetProperty("actor").GetString(), item.GetProperty("commandFingerprint").GetString(), item.TryGetProperty("label", out JsonElement label) ? label.GetString() : "", item.GetProperty("sensitive").GetBoolean(), item.TryGetProperty("exitCode", out JsonElement code) ? code.ToString() : "", item.TryGetProperty("status", out JsonElement status) ? status.GetString() : "")).ToArray();
        await Assert.That(rawFields(one)).IsEquivalentTo(rawFields(two));
        await Assert.That(File.Exists(Path.Combine(externalDbRoot, "shared.db"))).IsTrue();
        string sensitiveReport = (await jsonl.Cli("report", "--session", "PR-45")).Stdout;
        string sqliteReport = (await sqlite.Cli("report", "--session", "PR-45")).Stdout;
        await Assert.That(sensitiveReport).DoesNotContain("labeled-secret-xyz");
        await Assert.That(sensitiveReport).DoesNotContain("unlabeled-secret-xyz");
        await Assert.That(sensitiveReport).Contains("label=release");
        await Assert.That(sensitiveReport).Contains(unlabeledHash);
        await Assert.That(sqliteReport).DoesNotContain("labeled-secret-xyz");
        await Assert.That(sqliteReport).DoesNotContain("unlabeled-secret-xyz");
        await Assert.That(sqliteReport).Contains("2 paired commands");
        ProcessResult rejectedName = await jsonl.Cli("command", "start", "--session", "reject-name", "--command", labeledSecret, "--sensitive", "--name", "publish");
        await Assert.That(rejectedName.ExitCode).IsNotEqualTo(0);
        ProcessResult rejectedEndName = await jsonl.Cli("command", "end", "--session", "reject-name", "--command", labeledSecret, "--exit-code", "0", "--name", "publish");
        await Assert.That(rejectedEndName.ExitCode).IsNotEqualTo(0);
        ProcessResult commandHelp = await jsonl.Cli("command", "start", "--help");
        await Assert.That(commandHelp.Stdout).DoesNotContain("--name");
        ProcessResult commandEndHelp = await jsonl.Cli("command", "end", "--help");
        await Assert.That(commandEndHelp.Stdout).DoesNotContain("--name");
    }

    [Test]
    public async Task ConcurrentWritersShareExternalSqliteAndRetainAllEvents()
    {
        string externalDbRoot = Path.Combine(Path.GetTempPath(), "agenttrace-shared-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(externalDbRoot);
        using var first = new TemporaryRepository();
        using var second = new TemporaryRepository();
        first.ExternalDirectory = externalDbRoot;
        second.ExternalDirectory = externalDbRoot;
        string databasePath = Path.Combine(externalDbRoot, "shared.db");
        await first.SetConfigAsync("sqlite", databasePath);
        await second.SetConfigAsync("sqlite", databasePath);
        ProcessResult[] writes = await Task.WhenAll(Enumerable.Range(0, 10).Select(i => (i % 2 == 0 ? first : second).Cli("mark", "--session", "shared-session", "--actor", "agent-" + (i % 2), "--note", "mark-" + i)));
        await Assert.That(string.Join(" | ", writes.Where(write => write.ExitCode != 0).Select(write => write.Stderr))).IsEqualTo("");
        await Assert.That(writes.Select(write => write.ExitCode)).IsEquivalentTo(Enumerable.Repeat(0, writes.Length).ToArray());
        using JsonDocument export = JsonDocument.Parse((await first.Cli("export", "--session", "shared-session")).Stdout);
        await Assert.That(export.RootElement.GetProperty("events").GetArrayLength()).IsEqualTo(10);
        await Assert.That(export.RootElement.GetProperty("events").EnumerateArray().Select(item => item.GetProperty("note").GetString()).Distinct().Count()).IsEqualTo(10);
    }

    [Test]
    public async Task ConcurrentWritersShareConfiguredJsonlAcrossRepositories()
    {
        string externalJsonl = Path.Combine(Path.GetTempPath(), "agenttrace-jsonl-shared-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(externalJsonl);
        using var first = new TemporaryRepository { ExternalDirectory = externalJsonl };
        using var second = new TemporaryRepository { ExternalDirectory = externalJsonl };
        await first.SetConfigAsync("jsonl", externalJsonl);
        await second.SetConfigAsync("jsonl", externalJsonl);
        ProcessResult[] writes = await Task.WhenAll(Enumerable.Range(0, 10).Select(i => (i % 2 == 0 ? first : second).Cli("mark", "--session", "shared-session", "--actor", "agent-" + (i % 2), "--note", "mark-" + i)));
        await Assert.That(writes.All(write => write.ExitCode == 0)).IsTrue();
        using JsonDocument export = JsonDocument.Parse((await first.Cli("export", "--session", "shared-session")).Stdout);
        await Assert.That(export.RootElement.GetProperty("events").GetArrayLength()).IsEqualTo(10);
        await Assert.That(export.RootElement.GetProperty("events").EnumerateArray().Select(item => item.GetProperty("note").GetString()).Distinct().Count()).IsEqualTo(10);
    }

    [Test]
    public async Task PauseAnnotationsArePerActorAndUnmatchedEventsRemainVisible()
    {
        using var repo = new TemporaryRepository();
        await repo.Cli("pause", "--session", "actors", "--actor", "a");
        await repo.Cli("pause", "--session", "actors", "--actor", "a");
        await repo.Cli("command", "start", "--session", "actors", "--actor", "b", "--command", "build");
        await repo.Cli("command", "end", "--session", "actors", "--actor", "b", "--command", "build", "--exit-code", "0");
        await repo.Cli("resume", "--session", "actors", "--actor", "a");
        await repo.Cli("operation", "end", "--session", "actors", "--kind", "mcp", "--name", "tool.call", "--status", "complete");

        string report = (await repo.Cli("report", "--session", "actors")).Stdout;
        await Assert.That(report).Contains("- b: 1 paired commands");
        await Assert.That(report).DoesNotContain("- b: 1 paired commands; command coverage 00:00:00.000");
        await Assert.That(report).Contains("AMBIGUOUS pause/resume FIFO interpretation");
        await Assert.That(report).Contains("unmatched declared mcp end");
        await Assert.That(report).Contains("Unattributed gaps are not model thinking time");
    }

    [Test]
    public async Task JsonlReadAppendExportRecoversTornTailAndPreservesValidUnterminatedRecord()
    {
        using var tornRepo = new TemporaryRepository();
        await tornRepo.Cli("mark", "--session", "tail", "--note", "complete-record");
        string tornFile = Directory.EnumerateFiles(Path.Combine(tornRepo.Path, ".agenttrace", "sessions"), "*.jsonl").Single();
        await File.AppendAllTextAsync(tornFile, "{\"partial\":");
        ProcessResult sessions = await tornRepo.Cli("sessions", "--format", "json");
        await Assert.That(sessions.ExitCode).IsEqualTo(0);
        await Assert.That(sessions.Stdout).Contains("tail");
        ProcessResult appendAfterRecovery = await tornRepo.Cli("mark", "--session", "tail", "--note", "after-recovery");
        await Assert.That(appendAfterRecovery.ExitCode).IsEqualTo(0);
        await Assert.That(appendAfterRecovery.Stdout).IsEqualTo("");
        await Assert.That(appendAfterRecovery.Stderr).Contains("discarded an incomplete malformed JSONL tail");
        using JsonDocument recovered = JsonDocument.Parse((await tornRepo.Cli("export", "--session", "tail")).Stdout);
        await Assert.That(recovered.RootElement.GetProperty("events").GetArrayLength()).IsEqualTo(2);
        await Assert.That(recovered.RootElement.GetProperty("events")[0].GetProperty("note").GetString()).IsEqualTo("complete-record");
        await Assert.That(recovered.RootElement.GetProperty("events")[1].GetProperty("note").GetString()).IsEqualTo("after-recovery");

        using var unterminatedRepo = new TemporaryRepository();
        await unterminatedRepo.Cli("mark", "--session", "unterminated", "--note", "valid-json");
        string unterminatedFile = Directory.EnumerateFiles(Path.Combine(unterminatedRepo.Path, ".agenttrace", "sessions"), "*.jsonl").Single();
        byte[] completeLine = await File.ReadAllBytesAsync(unterminatedFile);
        await Assert.That(completeLine[^1]).IsEqualTo((byte)'\n');
        await File.WriteAllBytesAsync(unterminatedFile, completeLine[..^1]);
        ProcessResult validSessions = await unterminatedRepo.Cli("sessions", "--format", "json");
        await Assert.That(validSessions.ExitCode).IsEqualTo(0);
        await Assert.That(validSessions.Stdout).Contains("unterminated");
        ProcessResult appendAfterValid = await unterminatedRepo.Cli("mark", "--session", "unterminated", "--note", "next-record");
        await Assert.That(appendAfterValid.ExitCode).IsEqualTo(0);
        await Assert.That(appendAfterValid.Stderr).IsEqualTo("");
        using JsonDocument validRecovered = JsonDocument.Parse((await unterminatedRepo.Cli("export", "--session", "unterminated")).Stdout);
        await Assert.That(validRecovered.RootElement.GetProperty("events").GetArrayLength()).IsEqualTo(2);
        await Assert.That(validRecovered.RootElement.GetProperty("events")[0].GetProperty("note").GetString()).IsEqualTo("valid-json");
        await Assert.That(validRecovered.RootElement.GetProperty("events")[1].GetProperty("note").GetString()).IsEqualTo("next-record");
    }

    [Test]
    public async Task BadConfigurationFailsClearly()
    {
        using var repo = new TemporaryRepository();
        await File.WriteAllTextAsync(Path.Combine(repo.Path, ".agenttrace.json"), "{bad json");
        ProcessResult rejected = await repo.Cli("sessions");
        await Assert.That(rejected.ExitCode).IsNotEqualTo(0);
        await Assert.That(rejected.Stderr).Contains("Invalid .agenttrace.json");
    }

    [Test]
    public async Task ExistingUnrelatedDatabaseDestinationIsRejectedWithoutOverwrite()
    {
        using var repo = new TemporaryRepository();
        string databasePath = Path.Combine(repo.Path, "unrelated.db");
        byte[] sentinel = Encoding.UTF8.GetBytes("unrelated local data must remain intact");
        await File.WriteAllBytesAsync(databasePath, sentinel);
        await repo.SetConfigAsync("sqlite", databasePath);
        ProcessResult result = await repo.Cli("mark", "--session", "safe", "--note", "must-not-write");
        await Assert.That(result.ExitCode).IsNotEqualTo(0);
        await Assert.That(await File.ReadAllBytesAsync(databasePath)).IsEquivalentTo(sentinel);
    }

    [Test]
    public async Task ModelProjectionUsesExactFingerprintAndKeepsUnmatchedEnd()
    {
        DateTimeOffset time = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        TraceEvent Event(string type, string text, int offset) => new() { Session = "s", Actor = "a", Type = type, TimestampUtc = time.AddSeconds(offset), CommandFingerprint = CommandFingerprint.Compute(text), CommandText = type == "command-start" ? text : null };
        string exact = "pwsh -NoProfile -Command 'Get-Date'";
        var rows = new[] { new StoredEvent(0, Event("command-end", exact, 1)), new StoredEvent(1, Event("command-start", exact + " ", 2)), new StoredEvent(2, Event("command-start", exact, 3)), new StoredEvent(3, Event("command-end", exact, 4)) };
        EventProjection projection = EventProjectionBuilder.Build(rows, "s");
        await Assert.That(projection.Commands.Count).IsEqualTo(1);
        await Assert.That(projection.Commands[0].StartIndex).IsEqualTo(2);
        await Assert.That(projection.Commands[0].EndIndex).IsEqualTo(3);
        await Assert.That(projection.UnmatchedCommandStarts).IsEquivalentTo(new[] { 1 });
        await Assert.That(projection.UnmatchedCommandEnds).IsEquivalentTo(new[] { 0 });
    }
}

internal sealed class TemporaryRepository : IDisposable
{
    private readonly string _cli;
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "agenttrace-tests-" + Guid.NewGuid().ToString("N"));
    public string? ExternalDirectory { get; set; }

    public TemporaryRepository()
    {
        Directory.CreateDirectory(Path);
        Directory.CreateDirectory(System.IO.Path.Combine(Path, ".git"));
        File.WriteAllText(System.IO.Path.Combine(Path, ".gitignore"), "**/.agenttrace/\n");
        string root = FindRoot();
        _cli = System.IO.Path.Combine(root, "src", "DotNetAiWorkbench.AgentTrace.Cli", "bin", "Release", "net11.0", "DotNetAiWorkbench.AgentTrace.Cli.dll");
        if (!File.Exists(_cli))
        {
            throw new FileNotFoundException("Build AgentTrace before running its CLI tests.", _cli);
        }
    }

    public async Task SetConfigAsync(string provider, string path)
    {
        object config = new { schemaVersion = 1, storage = new { provider, path } };
        await File.WriteAllTextAsync(System.IO.Path.Combine(Path, ".agenttrace.json"), JsonSerializer.Serialize(config));
    }

    public Task<ProcessResult> Cli(params string[] args) => RunAsync("dotnet", [_cli, .. args, "--repo", Path]);

    private async Task<ProcessResult> RunAsync(string executable, string[] args)
    {
        var info = new ProcessStartInfo(executable) { WorkingDirectory = Path, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in args)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start AgentTrace CLI fixture.");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(); Task<string> stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new ProcessResult(process.ExitCode, await stdout, await stderr);
    }

    private static string FindRoot()
    {
        DirectoryInfo? current = new(Environment.CurrentDirectory);
        while (current is not null && !File.Exists(System.IO.Path.Combine(current.FullName, "DotNetAiWorkbench.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException("Could not find workbench root.");
    }

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch { }
        if (ExternalDirectory is not null) { try { Directory.Delete(ExternalDirectory, recursive: true); } catch { } }
    }
}

internal sealed record ProcessResult(int ExitCode, string Stdout, string Stderr);
