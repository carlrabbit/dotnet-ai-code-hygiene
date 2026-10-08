using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using DotNetAiCodeHygiene.Cli;

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
        await Assert.That(result.StandardOutput).Contains("review");
        await Assert.That(result.StandardOutput).Contains("help");
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
    [Arguments("review")]
    [Arguments("help")]
    public async Task ReservedCommandHelpSucceeds(string command)
    {
        ProcessResult result = await RunCliAsync(command, "--help");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput).Contains(command);
        if (command is "format" or "normalize")
        {
            await Assert.That(result.StandardOutput).DoesNotContain("future");
            await Assert.That(result.StandardOutput).DoesNotContain("not implemented");
        }
        if (command == "help")
        {
            await Assert.That(result.StandardOutput).Contains("--agent");
        }

        await Assert.That(result.StandardError).IsEmpty();
    }

    [Test]
    public async Task VersionUsesProductVersionOnStandardOutput()
    {
        ProcessResult result = await RunCliAsync("--version");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput.Trim()).IsEqualTo("0.7.0");
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
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.cs"), "public class Fixture { public string Long = \"" + new string('x', 205) + "\"; public void Run() { } }\n/// <summary>Die Fixture beschreibt eine ausgewählte Testressource.</summary>\npublic class ReviewedFixture { }\n");
            ProcessResult first = await RunCliInAsync(repo, "check", "--output", "json");
            await Assert.That(first.ExitCode).IsEqualTo(0);
            using JsonDocument json = JsonDocument.Parse(first.StandardOutput);
            JsonElement firstFinding = json.RootElement.GetProperty("findings")[0];
            string finding = firstFinding.GetProperty("id").GetString()!;
            string run = json.RootElement.GetProperty("runId").GetString()!;
            await Assert.That(firstFinding.TryGetProperty("path", out _)).IsTrue();
            await Assert.That(firstFinding.TryGetProperty("line", out _)).IsTrue();
            await Assert.That(firstFinding.TryGetProperty("column", out _)).IsTrue();
            await Assert.That(firstFinding.TryGetProperty("fingerprint", out _)).IsFalse();
            JsonElement batchJson = json.RootElement.GetProperty("reviewBatches")[0];
            await Assert.That(batchJson.GetProperty("reviewerClass").GetString()).IsEqualTo("implementer");
            await Assert.That(batchJson.GetProperty("mode").GetString()).IsEqualTo("sample");
            await Assert.That(batchJson.GetProperty("populationCount").GetInt32()).IsEqualTo(1);
            await Assert.That(batchJson.GetProperty("sampleCount").GetInt32()).IsLessThanOrEqualTo(5);
            if (batchJson.GetProperty("sampleCount").GetInt32() > 0)
            {
                await Assert.That(batchJson.GetProperty("items")[0].TryGetProperty("summary", out _)).IsTrue();
            }
            await Assert.That(batchJson.TryGetProperty("populationFingerprint", out _)).IsFalse();
            if (batchJson.GetProperty("sampleCount").GetInt32() > 0)
            {
                await Assert.That(batchJson.GetProperty("items")[0].TryGetProperty("rankingHash", out _)).IsFalse();
            }
            await Assert.That(first.StandardError).IsEmpty();
            string latestPath = Path.Combine(repo, ".hygiene", ".state", "latest-run.json");
            string latestBeforeExplain = await File.ReadAllTextAsync(latestPath);
            string batchHandle = batchJson.GetProperty("handle").GetString()!;
            ProcessResult expandedBare = await RunCliInAsync(repo, "review", "expand", "B-1", "--output", "json");
            await Assert.That(expandedBare.ExitCode).IsEqualTo(0);
            using (JsonDocument expandedJson = JsonDocument.Parse(expandedBare.StandardOutput))
            {
                JsonElement expandedBatch = expandedJson.RootElement.GetProperty("reviewBatch");
                await Assert.That(expandedBatch.GetProperty("mode").GetString()).IsEqualTo("expanded");
                await Assert.That(expandedBatch.GetProperty("reviewerClass").GetString()).IsEqualTo("frontier");
                await Assert.That(expandedBatch.GetProperty("sampleCount").GetInt32()).IsEqualTo(expandedBatch.GetProperty("populationCount").GetInt32());
                await Assert.That(expandedBatch.GetProperty("items")[0].TryGetProperty("summary", out _)).IsTrue();
            }
            await Assert.That(await File.ReadAllTextAsync(latestPath)).IsEqualTo(latestBeforeExplain);
            ProcessResult expandedQualified = await RunCliInAsync(repo, "review", "expand", batchHandle, "--output", "json");
            await Assert.That(expandedQualified.ExitCode).IsEqualTo(0);
            await Assert.That((await RunCliInAsync(repo, "review", "expand", "R-OLD/B-1")).ExitCode).IsEqualTo(3);
            await Assert.That((await RunCliInAsync(repo, "ignore", "B-1")).ExitCode).IsEqualTo(3);
            await Assert.That(Directory.GetFiles(Path.Combine(repo, ".hygiene"), "*review*", SearchOption.AllDirectories).Length).IsEqualTo(0);
            ProcessResult bareExplanation = await RunCliInAsync(repo, "explain", finding, "--output", "json");
            await Assert.That(bareExplanation.ExitCode).IsEqualTo(0);
            ProcessResult explanation = await RunCliInAsync(repo, "explain", run + "/" + finding, "--output", "json");
            await Assert.That(explanation.ExitCode).IsEqualTo(0);
            using (JsonDocument explanationJson = JsonDocument.Parse(explanation.StandardOutput))
            {
                JsonElement value = explanationJson.RootElement;
                foreach (string field in new[] { "schemaVersion", "ruleId", "ruleVersion", "classification", "path", "line", "column", "observation", "reason", "suggestion", "constraint" })
                {
                    await Assert.That(value.TryGetProperty(field, out _)).IsTrue();
                }
            }
            ProcessResult textExplanation = await RunCliInAsync(repo, "explain", finding);
            await Assert.That(textExplanation.ExitCode).IsEqualTo(0);
            await Assert.That(textExplanation.StandardOutput).Contains("Observation:");
            await Assert.That(textExplanation.StandardOutput).Contains("Why:");
            await Assert.That(textExplanation.StandardOutput).Contains("Suggestion:");
            await Assert.That(textExplanation.StandardOutput).Contains("Constraint:");
            await Assert.That(await File.ReadAllTextAsync(latestPath)).IsEqualTo(latestBeforeExplain);
            ProcessResult ignored = await RunCliInAsync(repo, "ignore", finding, "--reason", "Reviewed");
            await Assert.That(ignored.ExitCode).IsEqualTo(0);
            using (JsonDocument decisions = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(repo, ".hygiene", "decisions.json"))))
            {
                await Assert.That(decisions.RootElement.TryGetProperty("schemaVersion", out _)).IsTrue();
                JsonElement persistedDecision = decisions.RootElement.GetProperty("decisions")[0];
                await Assert.That(persistedDecision.GetProperty("id").GetString()!.StartsWith("I-", StringComparison.Ordinal)).IsTrue();
                await Assert.That(persistedDecision.GetProperty("ruleId").GetString()).IsEqualTo(firstFinding.GetProperty("ruleId").GetString());
                await Assert.That(persistedDecision.GetProperty("ruleVersion").GetInt32()).IsEqualTo(2);
                await Assert.That(persistedDecision.GetProperty("path").GetString()).IsEqualTo("src/Fixture.cs");
                await Assert.That(persistedDecision.GetProperty("anchor").GetString()).IsNotEmpty();
                await Assert.That(persistedDecision.GetProperty("fingerprint").GetString()!.Length).IsEqualTo(64);
                await Assert.That(persistedDecision.GetProperty("reason").GetString()).IsEqualTo("Reviewed");
                await Assert.That(DateTimeOffset.TryParse(persistedDecision.GetProperty("createdAt").GetString(), out _)).IsTrue();
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
            await Assert.That(text.StandardError).IsEmpty();
            await Assert.That(text.StandardOutput).Contains("Run R-");
            await Assert.That(text.StandardOutput).Contains("finding(s)");
            await Assert.That(text.StandardOutput).Contains("review-candidate");
            await Assert.That(text.StandardOutput).Contains("F-");
            await Assert.That(text.StandardOutput).Contains("readability.long-line.review");
            await Assert.That(text.StandardOutput).Contains("src/Fixture.cs:");
            await Assert.That(text.StandardOutput).Contains("Physical line exceeds 200 characters.");
            await Assert.That((await RunCliInAsync(repo, "rules", "disable", "docs.summary.required")).ExitCode).IsEqualTo(0);
            await Assert.That((await RunCliInAsync(repo, "rules", "disable", "readability.long-line.review")).ExitCode).IsEqualTo(0);
            await Assert.That((await RunCliInAsync(repo, "rules", "disable", "readability.control-flow.visual-block")).ExitCode).IsEqualTo(0);
            ProcessResult empty = await RunCliInAsync(repo, "check", "--output", "json");
            using JsonDocument noFindings = JsonDocument.Parse(empty.StandardOutput);
            await Assert.That(noFindings.RootElement.GetProperty("schemaVersion").GetInt32()).IsEqualTo(1);
            await Assert.That(noFindings.RootElement.GetProperty("runId").GetString()!.StartsWith("R-", StringComparison.Ordinal)).IsTrue();
            await Assert.That(noFindings.RootElement.GetProperty("findings").GetArrayLength()).IsEqualTo(0);
            await Assert.That(noFindings.RootElement.GetProperty("ignoredCount").GetInt32()).IsEqualTo(0);
            await Assert.That(noFindings.RootElement.GetProperty("reviewBatches").GetArrayLength()).IsEqualTo(3);
            await Assert.That((await RunCliInAsync(repo, "rules", "disable", "docs.summary.quality.review")).ExitCode).IsEqualTo(0);
            ProcessResult reviewDisabled = await RunCliInAsync(repo, "check", "--output", "json");
            using JsonDocument reviewDisabledJson = JsonDocument.Parse(reviewDisabled.StandardOutput);
            await Assert.That(reviewDisabledJson.RootElement.GetProperty("reviewBatches").GetArrayLength()).IsEqualTo(2);
            await Assert.That(reviewDisabledJson.RootElement.GetProperty("reviewBatches")[0].GetProperty("ruleId").GetString()).IsEqualTo("docs.summary.language.german.review");
            await Assert.That(reviewDisabledJson.RootElement.GetProperty("findings").GetArrayLength()).IsEqualTo(noFindings.RootElement.GetProperty("findings").GetArrayLength());
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
            File.Delete(configPath);
            string decisionsPath = Path.Combine(repo, ".hygiene", "decisions.json");
            const string malformedDecisions = "{\"schemaVersion\":99,\"decisions\":[]}";
            await File.WriteAllTextAsync(decisionsPath, malformedDecisions);
            badState = await RunCliInAsync(repo, "check", "--output", "json");
            await Assert.That(badState.ExitCode).IsEqualTo(3);
            await Assert.That(await File.ReadAllTextAsync(decisionsPath)).IsEqualTo(malformedDecisions);
        }
        finally { Directory.Delete(repo, true); }
    }

    [Test]
    public async Task PositionalRecordReviewItemFieldsPersistAcrossCheckExpandAndHandoff()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-record-review-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await RunProcessAsync("git", repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), "/// <summary>Ein gültiger Typ.</summary>\n/// <param name=\"Name\">Der Anzeigename.</param>\npublic record Person(string Name);\n");

            ProcessResult check = await RunCliInAsync(repo, "check", "--output", "json");
            await Assert.That(check.ExitCode).IsEqualTo(0);
            using JsonDocument checkJson = JsonDocument.Parse(check.StandardOutput);
            JsonElement checkBatch = checkJson.RootElement.GetProperty("reviewBatches").EnumerateArray()
                .Single(batch => batch.GetProperty("ruleId").GetString() == "docs.summary.quality.review");
            JsonElement checkItem = checkBatch.GetProperty("items").EnumerateArray()
                .Single(item => item.GetProperty("symbol").GetString() == "T:Person.Name");
            AssertPositionalReviewItem(checkItem);

            ProcessResult expanded = await RunCliInAsync(repo, "review", "expand", "B-1", "--output", "json");
            await Assert.That(expanded.ExitCode).IsEqualTo(0);
            using JsonDocument expandedJson = JsonDocument.Parse(expanded.StandardOutput);
            JsonElement expandedItem = expandedJson.RootElement.GetProperty("reviewBatch").GetProperty("items").EnumerateArray()
                .Single(item => item.GetProperty("symbol").GetString() == "T:Person.Name");
            AssertPositionalReviewItem(expandedItem);

            ProcessResult handoff = await RunCliInAsync(repo, true, "review", "handoff", "B-1");
            await Assert.That(handoff.ExitCode).IsEqualTo(0);
            string handoffPath = handoff.StandardOutput["Review handoff written: ".Length..].Trim();
            using JsonDocument handoffJson = JsonDocument.Parse(await File.ReadAllTextAsync(handoffPath));
            JsonElement handoffItem = handoffJson.RootElement.GetProperty("items").EnumerateArray()
                .Single(item => item.GetProperty("symbol").GetString() == "T:Person.Name");
            AssertPositionalReviewItem(handoffItem);
        }
        finally
        {
            Directory.Delete(repo, true);
        }
    }

    private static void AssertPositionalReviewItem(JsonElement item)
    {
        if (item.GetProperty("id").GetString() != "RI-2"
            || item.GetProperty("path").GetString() != "src/Api.cs"
            || item.GetProperty("line").GetInt32() != 3
            || item.GetProperty("column").GetInt32() != 22
            || item.GetProperty("symbol").GetString() != "T:Person.Name"
            || item.GetProperty("summary").GetString() != "Der Anzeigename."
            || item.GetProperty("declaration").GetString() != "Person")
        {
            throw new InvalidOperationException("Positional-record review item differs from the accepted M0006 public representation.");
        }
    }

    [Test]
    public async Task EmptyReviewBatchIsPresentInJsonAndText()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-empty-review-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await RunProcessAsync("git", repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.cs"), "public class NoSummary { }");
            ProcessResult jsonResult = await RunCliInAsync(repo, "check", "--output", "json");
            await Assert.That(jsonResult.ExitCode).IsEqualTo(0);
            using JsonDocument json = JsonDocument.Parse(jsonResult.StandardOutput);
            JsonElement batch = json.RootElement.GetProperty("reviewBatches")[0];
            await Assert.That(batch.GetProperty("populationCount").GetInt32()).IsEqualTo(0);
            await Assert.That(batch.GetProperty("sampleCount").GetInt32()).IsEqualTo(0);
            await Assert.That(batch.GetProperty("items").GetArrayLength()).IsEqualTo(0);
            ProcessResult text = await RunCliInAsync(repo, "check", "--output", "text");
            await Assert.That(text.StandardOutput).Contains("docs.summary.quality.review — sample 0/0 — reviewer: implementer");
        }
        finally { Directory.Delete(repo, true); }
    }

    [Test]
    public async Task ReviewAcceptReportsOnlyCurrentNormalSampleItems()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-cli-review-accept-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await RunProcessAsync("git", repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string source = string.Join("\n", Enumerable.Range(0, 32).Select(i => $"/// <summary>A useful API description for item {i}.</summary>\npublic class Subject{i} {{ }}"));
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.cs"), source);
            ProcessResult check = await RunCliInAsync(repo, "check", "--output", "json");
            await Assert.That(check.ExitCode).IsEqualTo(0);
            using JsonDocument checkedOutput = JsonDocument.Parse(check.StandardOutput);
            JsonElement quality = checkedOutput.RootElement.GetProperty("reviewBatches").EnumerateArray().Single(x => x.GetProperty("ruleId").GetString() == "docs.summary.quality.review");
            await Assert.That(quality.GetProperty("populationCount").GetInt32()).IsEqualTo(32);
            await Assert.That(quality.GetProperty("sampleCount").GetInt32()).IsEqualTo(5);
            string handle = quality.GetProperty("handle").GetString()!;
            string itemId = quality.GetProperty("items")[0].GetProperty("id").GetString()!;
            ProcessResult accepted = await RunCliInAsync(repo, "review", "accept", handle, itemId, "--output", "json");
            await Assert.That(accepted.ExitCode).IsEqualTo(0);
            using (JsonDocument json = JsonDocument.Parse(accepted.StandardOutput))
            {
                JsonElement result = json.RootElement;
                await Assert.That(result.GetProperty("schemaVersion").GetInt32()).IsEqualTo(1);
                await Assert.That(result.GetProperty("batchHandle").GetString()).IsEqualTo(handle);
                await Assert.That(result.GetProperty("ruleId").GetString()).IsEqualTo("docs.summary.quality.review");
                await Assert.That(result.GetProperty("acceptedCount").GetInt32()).IsEqualTo(1);
                await Assert.That(result.GetProperty("acceptedItemIds")[0].GetString()).IsEqualTo(itemId);
                await Assert.That(accepted.StandardOutput.Contains("ticket", StringComparison.OrdinalIgnoreCase)).IsFalse();
                await Assert.That(accepted.StandardOutput.Contains("hazard", StringComparison.OrdinalIgnoreCase)).IsFalse();
            }
            ProcessResult duplicate = await RunCliInAsync(repo, "review", "accept", handle, itemId);
            await Assert.That(duplicate.ExitCode).IsEqualTo(3);
            await Assert.That(duplicate.StandardError).Contains("rerun hygiene check");
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task HandoffCommandWritesDefaultAndExternalRequestsWithoutGit()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-cli-handoff-" + Guid.NewGuid().ToString("N"));
        string external = Path.Combine(Path.GetTempPath(), "hygiene-cli-handoff-outside-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await RunProcessAsync("git", repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.cs"), "/// <summary>Eine verständliche Fixturebeschreibung.</summary>\npublic class Fixture { }\n");
            ProcessResult check = await RunCliInAsync(repo, "check", "--output", "json");
            await Assert.That(check.ExitCode).IsEqualTo(0);
            using JsonDocument json = JsonDocument.Parse(check.StandardOutput);
            string handle = json.RootElement.GetProperty("reviewBatches")[0].GetProperty("handle").GetString()!;
            ProcessResult defaultHandoff = await RunCliInAsync(repo, true, "review", "handoff", "B-1");
            await Assert.That(defaultHandoff.ExitCode).IsEqualTo(0);
            await Assert.That(defaultHandoff.StandardOutput).Contains(".hygiene");
            string defaultPath = defaultHandoff.StandardOutput["Review handoff written: ".Length..].Trim();
            await Assert.That(File.Exists(defaultPath)).IsTrue();

            string externalPath = Path.Combine(external, "nested", "request.json");
            ProcessResult externalHandoff = await RunCliInAsync(repo, true, "review", "handoff", handle, "--file", externalPath);
            await Assert.That(externalHandoff.ExitCode).IsEqualTo(0);
            await Assert.That(File.Exists(externalPath)).IsTrue();
            using JsonDocument externalJson = JsonDocument.Parse(await File.ReadAllTextAsync(externalPath));
            await Assert.That(externalJson.RootElement.GetProperty("mode").GetString()).IsEqualTo("expanded");

            string stalePath = Path.Combine(external, "stale", "request.json");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.cs"), "/// <summary>Eine geänderte verständliche Fixturebeschreibung.</summary>\npublic class Fixture { }\n");
            ProcessResult stale = await RunCliInAsync(repo, true, "review", "handoff", "B-1", "--file", stalePath);
            await Assert.That(stale.ExitCode).IsEqualTo(3);
            await Assert.That(stale.StandardError).Contains("rerun hygiene check");
            await Assert.That(File.Exists(stalePath)).IsFalse();
        }
        finally
        {
            Directory.Delete(repo, true);
            if (Directory.Exists(external))
            {
                Directory.Delete(external, true);
            }
        }
    }

    [Test]
    public async Task RulesJsonDeclaresFindingAndBatchOutputKinds()
    {
        ProcessResult result = await RunCliAsync("rules", "--output", "json");
        await Assert.That(result.ExitCode).IsEqualTo(0);
        using JsonDocument json = JsonDocument.Parse(result.StandardOutput);
        JsonElement rules = json.RootElement.GetProperty("rules");
        await Assert.That(rules.GetArrayLength()).IsEqualTo(15);
        JsonElement formatter = rules.EnumerateArray().Single(x => x.GetProperty("id").GetString() == "format.csharp.roslyn");
        await Assert.That(formatter.GetProperty("configurable").GetBoolean()).IsTrue();
        await Assert.That(formatter.GetProperty("capabilities").GetProperty("format").GetBoolean()).IsTrue();
        JsonElement rule = json.RootElement.GetProperty("rules").EnumerateArray().Single(x => x.GetProperty("id").GetString() == "docs.summary.quality.review");
        await Assert.That(rule.GetProperty("outputKind").GetString()).IsEqualTo("review-batch");
        await Assert.That(rule.GetProperty("classification").GetString()).IsEqualTo("review-batch");
    }

    [Test]
    public async Task UnexpectedHandlerExceptionUsesInternalFailureExitCode()
    {
        int exitCode = Program.Run(() => throw new InvalidOperationException("synthetic internal failure"));
        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task FormatAndNormalizeExposeFunctionalHelpAndOutput()
    {
        ProcessResult help = await RunCliAsync("help", "--agent");
        await Assert.That(help.ExitCode).IsEqualTo(0);
        await Assert.That(help.StandardOutput).Contains("Workflow:");
        foreach (string required in new[] { "Targets:", "Exit 0", "--output text|json", "presentation-only", "non-mutating --check", "findings", "review accept", "0/N", "review expand", "review handoff", "planner", ".hygiene", "no model/provider" })
        {
            await Assert.That(help.StandardOutput).Contains(required);
        }
    }

    [Test]
    public async Task RewriteCliJsonTextCheckModesAndPersistentStateBoundaries()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-rewrite-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        Directory.CreateDirectory(Path.Combine(repo, "broken"));
        try
        {
            await RunProcessAsync("git", repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string file = Path.Combine(repo, "src", "Fixture.cs");
            const string original = "using System; public class Fixture{private int value; public String Empty(){return System.String.Empty;} public int Read(){return this.value;}}";
            await File.WriteAllTextAsync(file, original);
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Other.cs"), "public class Other { }\n");
            await File.WriteAllTextAsync(Path.Combine(repo, "broken", "Broken.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string broken = Path.Combine(repo, "broken", "Broken.cs");
            await File.WriteAllTextAsync(broken, "public class Broken{void Run(){MissingType value=null;}}\n");

            ProcessResult formatWithCompilerError = await RunCliInAsync(repo, "format", broken, "--check", "--output", "json");
            await Assert.That(formatWithCompilerError.ExitCode).IsEqualTo(0);
            await Assert.That(formatWithCompilerError.StandardError).IsEmpty();
            using (JsonDocument.Parse(formatWithCompilerError.StandardOutput)) { }

            string hygiene = Path.Combine(repo, ".hygiene");
            Directory.CreateDirectory(Path.Combine(hygiene, ".state"));
            string config = Path.Combine(hygiene, "config.json");
            string decisions = Path.Combine(hygiene, "decisions.json");
            string latest = Path.Combine(hygiene, ".state", "latest-run.json");
            await File.WriteAllTextAsync(config, "{\"schemaVersion\":1,\"disabledRules\":[\"docs.summary.required\"]}");
            await File.WriteAllTextAsync(decisions, "{\"schemaVersion\":1,\"decisions\":[]}");
            await File.WriteAllTextAsync(latest, "rewrite-state-sentinel");
            string configBefore = await File.ReadAllTextAsync(config);
            string decisionsBefore = await File.ReadAllTextAsync(decisions);
            string latestBefore = await File.ReadAllTextAsync(latest);

            string sourceBeforeFormat = await File.ReadAllTextAsync(file);
            ProcessResult formatCheck = await RunCliInAsync(repo, "format", "src/Fixture.cs", "--check", "--output", "json");
            await Assert.That(formatCheck.ExitCode).IsEqualTo(0);
            await Assert.That(formatCheck.StandardError).IsEmpty();
            using JsonDocument json = JsonDocument.Parse(formatCheck.StandardOutput);
            JsonElement format = json.RootElement;
            await Assert.That(format.GetProperty("schemaVersion").GetInt32()).IsEqualTo(1);
            await Assert.That(format.GetProperty("checkOnly").GetBoolean()).IsTrue();
            await Assert.That(format.GetProperty("targetCount").GetInt32()).IsEqualTo(1);
            await Assert.That(format.GetProperty("changedPaths").GetArrayLength()).IsEqualTo(format.GetProperty("changedCount").GetInt32());
            await Assert.That(format.GetProperty("selectedRuleIds")[0].GetString()).IsEqualTo("format.csharp.roslyn");
            await Assert.That(await File.ReadAllTextAsync(file)).IsEqualTo(sourceBeforeFormat);
            ProcessResult invalidOutput = await RunCliInAsync(repo, "format", "src/Fixture.cs", "--output", "yaml");
            await Assert.That(invalidOutput.ExitCode).IsEqualTo(2);
            await Assert.That(await File.ReadAllTextAsync(file)).IsEqualTo(sourceBeforeFormat);
            ProcessResult formatMutation = await RunCliInAsync(repo, "format", "src/Fixture.cs", "--output", "json");
            await Assert.That(formatMutation.ExitCode).IsEqualTo(0);
            using JsonDocument formatMutationJson = JsonDocument.Parse(formatMutation.StandardOutput);
            await Assert.That(formatMutationJson.RootElement.GetProperty("changedPaths").ToString()).IsEqualTo(format.GetProperty("changedPaths").ToString());
            ProcessResult cleanJson = await RunCliInAsync(repo, "format", "src/Fixture.cs", "--check", "--output", "json");
            using JsonDocument cleanFormat = JsonDocument.Parse(cleanJson.StandardOutput);
            await Assert.That(cleanFormat.RootElement.GetProperty("changedCount").GetInt32()).IsEqualTo(0);
            ProcessResult cleanText = await RunCliInAsync(repo, "format", "src/Fixture.cs", "--output", "text");
            await Assert.That(cleanText.ExitCode).IsEqualTo(0);
            await Assert.That(cleanText.StandardOutput).Contains("0 changed");

            byte[] sourceBeforeNormalize = await File.ReadAllBytesAsync(file);
            ProcessResult normalizeCheck = await RunCliInAsync(repo, "normalize", "src/Fixture.cs", "--check", "--output", "json");
            await Assert.That(normalizeCheck.ExitCode).IsEqualTo(0);
            using JsonDocument normalizeCheckJson = JsonDocument.Parse(normalizeCheck.StandardOutput);
            string paths = normalizeCheckJson.RootElement.GetProperty("changedPaths").ToString();
            await Assert.That(string.Join(",", normalizeCheckJson.RootElement.GetProperty("selectedRuleIds").EnumerateArray().Select(item => item.GetString()))).IsEqualTo("style.qualification.this.unnecessary,style.qualification.redundant,format.csharp.roslyn");
            await Assert.That(normalizeCheckJson.RootElement.GetProperty("changedCount").GetInt32()).IsGreaterThan(0);
            await Assert.That((await File.ReadAllBytesAsync(file)).AsSpan().SequenceEqual(sourceBeforeNormalize)).IsTrue();
            ProcessResult normalizeMutation = await RunCliInAsync(repo, "normalize", "src/Fixture.cs", "--output", "json");
            await Assert.That(normalizeMutation.ExitCode).IsEqualTo(0);
            using JsonDocument normalizeMutationJson = JsonDocument.Parse(normalizeMutation.StandardOutput);
            await Assert.That(normalizeMutationJson.RootElement.GetProperty("changedPaths").ToString()).IsEqualTo(paths);
            string normalized = await File.ReadAllTextAsync(file);
            await Assert.That(normalized).Contains("string Empty()");
            await Assert.That(normalized).DoesNotContain("System.String.Empty");
            await Assert.That(await File.ReadAllTextAsync(config)).IsEqualTo(configBefore);
            await Assert.That(await File.ReadAllTextAsync(decisions)).IsEqualTo(decisionsBefore);
            await Assert.That(await File.ReadAllTextAsync(latest)).IsEqualTo(latestBefore);

            await File.WriteAllTextAsync(file, "public class Fixture{public int Value=>1;}");
            byte[] sourceBeforeDisabledNormalize = await File.ReadAllBytesAsync(file);
            ProcessResult disableThis = await RunCliInAsync(repo, "rules", "disable", "style.qualification.this.unnecessary");
            await Assert.That(disableThis.ExitCode).IsEqualTo(0);
            ProcessResult disableRedundant = await RunCliInAsync(repo, "rules", "disable", "style.qualification.redundant");
            await Assert.That(disableRedundant.ExitCode).IsEqualTo(0);
            ProcessResult disabledNormalizeCheck = await RunCliInAsync(repo, "normalize", "src/Fixture.cs", "--check", "--output", "json");
            await Assert.That(disabledNormalizeCheck.ExitCode).IsEqualTo(0);
            using JsonDocument disabledNormalizeJson = JsonDocument.Parse(disabledNormalizeCheck.StandardOutput);
            await Assert.That(disabledNormalizeJson.RootElement.GetProperty("changedCount").GetInt32()).IsEqualTo(0);
            await Assert.That((await File.ReadAllBytesAsync(file)).AsSpan().SequenceEqual(sourceBeforeDisabledNormalize)).IsTrue();

            ProcessResult conflictingTargets = await RunCliInAsync(repo, "format", "src/Fixture.cs", "--changed");
            await Assert.That(conflictingTargets.ExitCode).IsEqualTo(2);
            ProcessResult unsupported = await RunCliInAsync(repo, "format", "src/Fixture.csproj");
            await Assert.That(unsupported.ExitCode).IsEqualTo(3);
            ProcessResult outside = await RunCliInAsync(repo, "format", Path.GetTempPath());
            await Assert.That(outside.ExitCode).IsEqualTo(3);
        }
        finally { DeleteTree(repo); }
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
        if (hideGit)
        {
            startInfo.Environment["PATH"] = Path.GetDirectoryName(CliAssemblyPath)!;
        }

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
        foreach (string arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        using Process process = Process.Start(info)!;
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(error);
        }

        if (executable == "git" && args.Length > 0 && args[0] == "init")
        {
            ProcessResult bootstrap = await RunCliInAsync(workingDirectory, "bootstrap", "--output", "json");
            if (bootstrap.ExitCode != 0)
            {
                throw new InvalidOperationException(bootstrap.StandardError);
            }
        }
    }

    private static string GetCliAssemblyPath()
    {
        string configuredPath = typeof(CliProcessTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "CliAssemblyPath")
            .Value!;
        return Path.GetFullPath(configuredPath);
    }

    private static void DeleteTree(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(path, true);
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
