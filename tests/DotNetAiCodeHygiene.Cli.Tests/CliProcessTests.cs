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
        await Assert.That(result.StandardOutput.Trim()).IsEqualTo("0.11.0");
        await Assert.That(result.StandardError).IsEmpty();
    }

    [Test]
    public async Task LocalToolScopeSearchesAncestorsAndNeverUsesCompetingPathInstall()
    {
        string root = Path.Combine(Path.GetTempPath(), "hygiene-tool-scope-" + Guid.NewGuid().ToString("N"));
        string nested = Path.Combine(root, "repo", "src");
        string cliHome = Path.Combine(root, "cli-home");
        string resolverEntry = Path.Combine(cliHome, ".dotnet", "toolResolverCache", "1", "dotnetaicodehygiene.tool");
        string assembly = Path.Combine(root, "packages", "DotNetAiCodeHygiene.Cli.dll");
        string dotnet = Path.Combine(root, "dotnet.exe");
        string competingShim = Path.Combine(root, "other-tools", "dotnet-hygiene.exe");
        Directory.CreateDirectory(nested);
        Directory.CreateDirectory(Path.GetDirectoryName(resolverEntry)!);
        Directory.CreateDirectory(Path.GetDirectoryName(assembly)!);
        Directory.CreateDirectory(Path.GetDirectoryName(competingShim)!);
        string manifest = Path.Combine(root, "repo", ".config", "dotnet-tools.json");
        Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
        await File.WriteAllTextAsync(manifest, "{\"version\":1,\"isRoot\":true,\"tools\":{\"dotnetaicodehygiene.tool\":{\"version\":\"0.11.0\",\"commands\":[\"dotnet-hygiene\"]}}}");
        string nestedManifest = Path.Combine(nested, ".config", "dotnet-tools.json");
        Directory.CreateDirectory(Path.GetDirectoryName(nestedManifest)!);
        await File.WriteAllTextAsync(nestedManifest, "{\"version\":1,\"isRoot\":true,\"tools\":{\"DOTNETAICODEHYGIENE.TOOL\":{\"version\":\"0.11.0\",\"commands\":[\"dotnet-hygiene\"]}}}");
        await File.WriteAllTextAsync(resolverEntry, "[{\"Version\":\"0.11.0\",\"PathToExecutable\":\"" + assembly.Replace("\\", "\\\\") + "\"}]");
        try
        {
            ToolInstallationScope local = ToolInstallationScope.Resolve(nested, "DotNetAiCodeHygiene.Tool", "0.11.0", dotnet, assembly, cliHome);
            await Assert.That(local.ManifestPath).IsEqualTo(nestedManifest);
            await Assert.That(local.ToolPath).IsNull();

            ToolInstallationScope ancestor = ToolInstallationScope.Resolve(Path.Combine(root, "repo"), "DotNetAiCodeHygiene.Tool", "0.11.0", dotnet, assembly, cliHome);
            await Assert.That(ancestor.ManifestPath).IsEqualTo(manifest);

            ToolInstallationScope other = ToolInstallationScope.Resolve(nested, "DotNetAiCodeHygiene.Tool", "0.11.0", competingShim, assembly, cliHome);
            await Assert.That(other.ManifestPath).IsNull();
            await Assert.That(other.ToolPath).IsEqualTo(Path.GetDirectoryName(competingShim));
            await Assert.That(other.ShimPath).IsEqualTo(competingShim);

            ToolInstallationScope unproven = ToolInstallationScope.Resolve(nested, "DotNetAiCodeHygiene.Tool", "0.11.0", dotnet, assembly, Path.Combine(root, "other-cli-home"));
            await Assert.That(unproven.IsResolved).IsFalse();
        }
        finally { DeleteTree(root); }
    }

    [Test]
    public async Task AutonomousGateUsesCommittedBaseOrFailsClosedBeforeDirtyWorktreeFallback()
    {
        string root = Path.Combine(Path.GetTempPath(), "hygiene-autonomous-git-gates-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "src"));
        try
        {
            await InitGitAsync(root, "main");
            string source = Path.Combine(root, "src", "Fixture.cs");
            await File.WriteAllTextAsync(source, "/// <summary>A useful fixture.</summary>\npublic class Fixture { public int Value => 1; }\n");
            await PrepareAcceptedBaselineAsync(root);
            await CommitAllAsync(root, "accepted baseline");
            string initialMainCommit = await GitTextAsync(root, "rev-parse", "HEAD");
            await RunGitRawAsync(root, "update-ref", "refs/remotes/origin/main", initialMainCommit);

            await File.WriteAllTextAsync(source, "/// <summary>A useful fixture.</summary>\npublic class Fixture { public int Value => 2; }\n");
            ProcessResult ordinaryWorktree = await RunCliInAsync(root, "--output", "json");
            using (JsonDocument report = JsonDocument.Parse(ordinaryWorktree.StandardOutput))
            {
                await Assert.That(report.RootElement.GetProperty("gate").GetString()).IsEqualTo("worktree");
            }
            await CommitAllAsync(root, "main worktree fixture edit");

            await RunGitRawAsync(root, "branch", "feature/committed");
            await RunGitRawAsync(root, "checkout", "feature/committed");
            await File.WriteAllTextAsync(source, "/// <summary>A useful fixture.</summary>\npublic class Fixture { public int Value => 3; }\n");
            await CommitAllAsync(root, "committed feature change");
            string mainCommit = await GitTextAsync(root, "rev-parse", "main");
            await RunGitRawAsync(root, "update-ref", "refs/remotes/origin/main", mainCommit);
            await File.WriteAllTextAsync(source, "/// <summary>A useful fixture.</summary>\npublic class Fixture { public int Value => 4; }\n");
            ProcessResult committedFeature = await RunCliInAsync(root, "--output", "json");
            using (JsonDocument report = JsonDocument.Parse(committedFeature.StandardOutput))
            {
                await Assert.That(report.RootElement.GetProperty("gate").GetString()).IsEqualTo("base:origin/main");
            }

            await RunGitRawAsync(root, "update-ref", "-d", "refs/remotes/origin/main");
            await RunGitRawAsync(root, "branch", "-D", "main");
            ProcessResult combinedUnknownBase = await RunCliInAsync(root, "--output", "json");
            using (JsonDocument report = JsonDocument.Parse(combinedUnknownBase.StandardOutput))
            {
                await Assert.That(report.RootElement.GetProperty("status").GetString()).IsEqualTo("incomplete");
                await Assert.That(report.RootElement.GetProperty("gate").ValueKind).IsEqualTo(JsonValueKind.Null);
                await Assert.That(report.RootElement.GetProperty("action").GetString()).Contains("worktree-only inspection is unsafe");
            }

            await RunGitRawAsync(root, "update-ref", "refs/remotes/origin/main", mainCommit);
            await RunGitRawAsync(root, "update-ref", "refs/remotes/origin/master", await GitTextAsync(root, "rev-parse", "HEAD"));
            ProcessResult ambiguousBase = await RunCliInAsync(root, "--output", "json");
            using (JsonDocument report = JsonDocument.Parse(ambiguousBase.StandardOutput))
            {
                await Assert.That(report.RootElement.GetProperty("status").GetString()).IsEqualTo("incomplete");
                await Assert.That(report.RootElement.GetProperty("gate").ValueKind).IsEqualTo(JsonValueKind.Null);
            }
        }
        finally { DeleteTree(root); }
    }

    [Test]
    public async Task AutonomousGateTreatsDivergedMasterAsFeatureWhenOriginMainIsConfiguredDefault()
    {
        string root = Path.Combine(Path.GetTempPath(), "hygiene-master-vs-origin-main-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "src"));
        try
        {
            await InitGitAsync(root, "main");
            string source = Path.Combine(root, "src", "Fixture.cs");
            await File.WriteAllTextAsync(source, "/// <summary>A useful fixture.</summary>\npublic class Fixture { public int Value => 1; }\n");
            await PrepareAcceptedBaselineAsync(root);
            await CommitAllAsync(root, "accepted baseline");
            string defaultCommit = await GitTextAsync(root, "rev-parse", "HEAD");
            await RunGitRawAsync(root, "update-ref", "refs/remotes/origin/main", defaultCommit);
            await RunGitRawAsync(root, "symbolic-ref", "refs/remotes/origin/HEAD", "refs/remotes/origin/main");
            await RunGitRawAsync(root, "checkout", "-b", "master");
            await File.WriteAllTextAsync(source, "/// <summary>A useful fixture.</summary>\npublic class Fixture { public int Value => 2; }\n");
            string committedFeatureSource = Path.Combine(root, "src", "CommittedFeature.cs");
            await File.WriteAllTextAsync(committedFeatureSource, "public class CommittedFeature { }\n");
            await CommitAllAsync(root, "committed master feature change");

            ProcessResult cleanWorktree = await RunCliInAsync(root, "--output", "json");
            using (JsonDocument report = JsonDocument.Parse(cleanWorktree.StandardOutput))
            {
                await Assert.That(report.RootElement.GetProperty("gate").GetString()).IsEqualTo("base:origin/main");
            }
            using (JsonDocument snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, ".hygiene", ".state", "latest-run.json"))))
            { await Assert.That(snapshot.RootElement.GetProperty("targetPaths").EnumerateArray().Any(item => item.GetString() == "src/CommittedFeature.cs")).IsTrue(); }

            await File.WriteAllTextAsync(source, "/// <summary>A useful fixture.</summary>\npublic class Fixture { public int Value => 3; }\n");
            ProcessResult dirtyWorktree = await RunCliInAsync(root, "--output", "json");
            using (JsonDocument report = JsonDocument.Parse(dirtyWorktree.StandardOutput))
            {
                await Assert.That(report.RootElement.GetProperty("gate").GetString()).IsEqualTo("base:origin/main");
            }
            using (JsonDocument snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, ".hygiene", ".state", "latest-run.json"))))
            { await Assert.That(snapshot.RootElement.GetProperty("targetPaths").EnumerateArray().Any(item => item.GetString() == "src/CommittedFeature.cs")).IsTrue(); }
        }
        finally { DeleteTree(root); }
    }

    [Test]
    public async Task BaselineApprovalBindsReportAndSnapshotWithoutFreezingOrdinarySources()
    {
        string root = Path.Combine(Path.GetTempPath(), "hygiene-baseline-approval-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "src"));
        try
        {
            await InitGitAsync(root, "main");
            string source = Path.Combine(root, "src", "Fixture.cs");
            await File.WriteAllTextAsync(source, "/// <summary>A useful fixture.</summary>\npublic class Fixture { public int Value => 1; }\n");

            ProcessResult prepared = await RunCliInAsync(root, "bootstrap", "--output", "json");
            using JsonDocument firstReport = JsonDocument.Parse(prepared.StandardOutput);
            string staleHash = firstReport.RootElement.GetProperty("reportSha256").GetString()!;
            await File.WriteAllTextAsync(source, "/// <summary>A useful fixture.</summary>\npublic class Fixture { public int Value => 2; }\n");
            ProcessResult staleAccept = await RunCliInAsync(root, "baseline", "accept", "--approver", "fixture approver", "--report-sha256", staleHash);
            await Assert.That(staleAccept.ExitCode).IsEqualTo(3);
            await Assert.That(staleAccept.StandardError).Contains("stale");

            ProcessResult refreshed = await RunCliInAsync(root, "bootstrap", "--output", "json");
            using JsonDocument refreshedReport = JsonDocument.Parse(refreshed.StandardOutput);
            string currentHash = refreshedReport.RootElement.GetProperty("reportSha256").GetString()!;
            await File.AppendAllTextAsync(Path.Combine(root, ".editorconfig"), "\n# pending configuration change\n");
            ProcessResult staleConfiguration = await RunCliInAsync(root, "baseline", "accept", "--approver", "fixture approver", "--report-sha256", currentHash);
            await Assert.That(staleConfiguration.ExitCode).IsEqualTo(3);
            refreshed = await RunCliInAsync(root, "bootstrap", "--output", "json");
            using JsonDocument reportChanged = JsonDocument.Parse(refreshed.StandardOutput);
            currentHash = reportChanged.RootElement.GetProperty("reportSha256").GetString()!;
            string reportPath = Path.Combine(root, reportChanged.RootElement.GetProperty("reportPath").GetString()!.Replace('/', Path.DirectorySeparatorChar));
            await File.AppendAllTextAsync(reportPath, " ");
            ProcessResult changedReport = await RunCliInAsync(root, "baseline", "accept", "--approver", "fixture approver", "--report-sha256", currentHash);
            await Assert.That(changedReport.ExitCode).IsEqualTo(3);
            refreshed = await RunCliInAsync(root, "bootstrap", "--output", "json");
            using JsonDocument reportMissing = JsonDocument.Parse(refreshed.StandardOutput);
            currentHash = reportMissing.RootElement.GetProperty("reportSha256").GetString()!;
            reportPath = Path.Combine(root, reportMissing.RootElement.GetProperty("reportPath").GetString()!.Replace('/', Path.DirectorySeparatorChar));
            File.Delete(reportPath);
            ProcessResult missingReport = await RunCliInAsync(root, "baseline", "accept", "--approver", "fixture approver", "--report-sha256", currentHash);
            await Assert.That(missingReport.ExitCode).IsEqualTo(3);
            refreshed = await RunCliInAsync(root, "bootstrap", "--output", "json");
            using JsonDocument currentReport = JsonDocument.Parse(refreshed.StandardOutput);
            currentHash = currentReport.RootElement.GetProperty("reportSha256").GetString()!;
            ProcessResult accepted = await RunCliInAsync(root, "baseline", "accept", "--approver", "fixture approver", "--report-sha256", currentHash, "--output", "json");
            await Assert.That(accepted.ExitCode).IsEqualTo(0);

            await File.WriteAllTextAsync(source, "/// <summary>A useful fixture.</summary>\npublic class Fixture { public int Value => 3; }\n");
            ProcessResult ordinaryEdit = await RunCliInAsync(root, "--output", "json");
            using (JsonDocument ordinaryReport = JsonDocument.Parse(ordinaryEdit.StandardOutput))
            {
                await Assert.That(ordinaryReport.RootElement.GetProperty("lifecycle").GetString()).IsEqualTo("gate");
            }

            await File.AppendAllTextAsync(Path.Combine(root, ".editorconfig"), "\n# material pending policy edit\n");
            ProcessResult policyEdit = await RunCliInAsync(root, "--output", "json");
            using (JsonDocument policyReport = JsonDocument.Parse(policyEdit.StandardOutput))
            {
                await Assert.That(policyReport.RootElement.GetProperty("status").GetString()).IsEqualTo("approval-required");
                await Assert.That(policyReport.RootElement.GetProperty("lifecycle").GetString()).IsEqualTo("profile-update");
            }
        }
        finally { DeleteTree(root); }
    }

    [Test]
    public async Task BaselineVersionContractChangesWhenToolOrRuleVersionChanges()
    {
        (string RuleId, int Version)[] rules = [("format.csharp.roslyn", 1), ("docs.summary.quality.review", 6)];
        string current = Program.VersionContractSha256("0.11.0", rules);
        string changedTool = Program.VersionContractSha256("0.10.1", rules);
        string changedRule = Program.VersionContractSha256("0.11.0", [("format.csharp.roslyn", 2), ("docs.summary.quality.review", 6)]);

        await Assert.That(changedTool).IsNotEqualTo(current);
        await Assert.That(changedRule).IsNotEqualTo(current);
    }

    [Test]
    public async Task ProfileMarkerWithoutAcceptedBaselineCannotAuthorizeNormalGate()
    {
        string root = Path.Combine(Path.GetTempPath(), "hygiene-marker-only-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "src"));
        try
        {
            await InitGitAsync(root, "main");
            await File.WriteAllTextAsync(Path.Combine(root, "src", "Fixture.cs"), "public class Fixture { }\n");
            ProcessResult manualBootstrap = await RunCliInAsync(root, "bootstrap", "--output", "json");
            await Assert.That(manualBootstrap.ExitCode).IsEqualTo(0);
            File.Delete(Path.Combine(root, ".hygiene", ".state", "baseline-approval.json"));
            ProcessResult markerOnly = await RunCliInAsync(root, "--output", "json");
            using JsonDocument report = JsonDocument.Parse(markerOnly.StandardOutput);
            await Assert.That(report.RootElement.GetProperty("status").GetString()).IsEqualTo("incomplete");
            await Assert.That(report.RootElement.GetProperty("lifecycle").GetString()).IsEqualTo("baseline");
            await Assert.That(report.RootElement.GetProperty("action").GetString()).Contains("without a valid accepted baseline record");
        }
        finally { DeleteTree(root); }
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
            await Assert.That(noFindings.RootElement.GetProperty("reviewBatches").GetArrayLength()).IsEqualTo(6);
            await Assert.That((await RunCliInAsync(repo, "rules", "disable", "docs.summary.quality.review")).ExitCode).IsEqualTo(0);
            ProcessResult reviewDisabled = await RunCliInAsync(repo, "check", "--output", "json");
            using JsonDocument reviewDisabledJson = JsonDocument.Parse(reviewDisabled.StandardOutput);
            await Assert.That(reviewDisabledJson.RootElement.GetProperty("reviewBatches").GetArrayLength()).IsEqualTo(5);
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
            string apiPath = Path.Combine(repo, "src", "Api.cs");
            const string apiSource = "/// <summary>Ein gültiger Typ.</summary>\n/// <param name=\"Name\">Der Anzeigename.</param>\npublic record Person(string Name);\n";
            await File.WriteAllTextAsync(apiPath, apiSource);

            ProcessResult check = await RunCliInAsync(repo, "check", "src/Api.cs", "--output", "json");
            await Assert.That(check.ExitCode).IsEqualTo(0);
            using JsonDocument checkJson = JsonDocument.Parse(check.StandardOutput);
            JsonElement checkBatch = checkJson.RootElement.GetProperty("reviewBatches").EnumerateArray()
                .Single(batch => batch.GetProperty("ruleId").GetString() == "docs.summary.quality.review");
            JsonElement checkItem = checkBatch.GetProperty("items").EnumerateArray()
                .FirstOrDefault(item => item.GetProperty("symbol").GetString() == "T:Person.Name");
            if (checkItem.ValueKind == JsonValueKind.Undefined) { throw new InvalidOperationException("Explicit-path gate did not select the positional-record subject."); }
            AssertPositionalReviewItem(checkItem);

            ProcessResult expanded = await RunCliInAsync(repo, "review", "expand", "B-1", "--output", "json");
            await Assert.That(expanded.ExitCode).IsEqualTo(0);
            using JsonDocument expandedJson = JsonDocument.Parse(expanded.StandardOutput);
            JsonElement expandedItem = expandedJson.RootElement.GetProperty("reviewBatch").GetProperty("items").EnumerateArray()
                .Single(item => item.GetProperty("symbol").GetString() == "T:Person.Name");
            AssertPositionalReviewItem(expandedItem);

            ProcessResult handoff = await RunCliInAsync(repo, true, "review", "handoff", "B-1");
            if (handoff.ExitCode != 0) { throw new InvalidOperationException(handoff.StandardError); }
            await Assert.That(handoff.ExitCode).IsEqualTo(0);
            string handoffLine = handoff.StandardOutput.Split('\n').Single(line => line.TrimStart().StartsWith("Review handoff written: ", StringComparison.Ordinal));
            string handoffPath = handoffLine[(handoffLine.IndexOf("Review handoff written: ", StringComparison.Ordinal) + "Review handoff written: ".Length)..].Trim();
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
            ProcessResult check = await RunCliInAsync(repo, "check", "--review-all", "--output", "json");
            await Assert.That(check.ExitCode).IsEqualTo(0);
            using JsonDocument checkedOutput = JsonDocument.Parse(check.StandardOutput);
            JsonElement quality = checkedOutput.RootElement.GetProperty("reviewBatches").EnumerateArray().Single(x => x.GetProperty("ruleId").GetString() == "docs.summary.quality.review");
            await Assert.That(quality.GetProperty("populationCount").GetInt32()).IsEqualTo(32);
            await Assert.That(quality.GetProperty("sampleCount").GetInt32()).IsEqualTo(5);
            string handle = quality.GetProperty("handle").GetString()!;
            string itemId = quality.GetProperty("items")[0].GetProperty("id").GetString()!;
            ProcessResult mixed = await RunCliInAsync(repo, "review", "accept", handle, itemId, "--all");
            await Assert.That(mixed.ExitCode).IsEqualTo(2);
            await Assert.That(mixed.StandardError).Contains("Use either item IDs or --all");

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
            }
            ProcessResult duplicate = await RunCliInAsync(repo, "review", "accept", handle, itemId);
            await Assert.That(duplicate.ExitCode).IsEqualTo(3);
            await Assert.That(duplicate.StandardError).Contains("rerun hygiene check");

            ProcessResult nextCheck = await RunCliInAsync(repo, "check", "--review-all", "--output", "json");
            await Assert.That(nextCheck.ExitCode).IsEqualTo(0);
            using JsonDocument next = JsonDocument.Parse(nextCheck.StandardOutput);
            JsonElement nextQuality = next.RootElement.GetProperty("reviewBatches").EnumerateArray().Single(x => x.GetProperty("ruleId").GetString() == "docs.summary.quality.review");
            ProcessResult acceptAll = await RunCliInAsync(repo, "review", "accept", nextQuality.GetProperty("handle").GetString()!, "--all", "--output", "json");
            await Assert.That(acceptAll.ExitCode).IsEqualTo(0);
            using JsonDocument allResult = JsonDocument.Parse(acceptAll.StandardOutput);
            await Assert.That(allResult.RootElement.GetProperty("acceptedCount").GetInt32()).IsEqualTo(nextQuality.GetProperty("sampleCount").GetInt32());
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task CheckBaseAndReviewAllAreExplicitGateModesWithProcessErrors()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-cli-gate-modes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await RunProcessAsync("git", repo, "init", "-q", "-b", "main");
            await RunProcessAsync("git", repo, "config", "user.email", "fixture@example.invalid");
            await RunProcessAsync("git", repo, "config", "user.name", "Gate Fixture");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), "/// <summary>A useful API description.</summary>\npublic class Api { public int Value => 1; }");
            ProcessResult bootstrap = await RunCliInAsync(repo, "bootstrap");
            await Assert.That(bootstrap.ExitCode).IsEqualTo(0);
            await RunProcessAsync("git", repo, "add", "-A");
            await RunProcessAsync("git", repo, "commit", "-m", "gate baseline");

            string sourcePath = Path.Combine(repo, "src", "Api.cs");
            await File.WriteAllTextAsync(sourcePath, "/// <summary>A useful API description.</summary>\npublic class Api { public int Value => 2; }\n/// <summary>A second useful API description.</summary>\npublic class ApiTwo { }");
            ProcessResult bare = await RunCliInAsync(repo, "check", "--output", "json");
            using (JsonDocument output = JsonDocument.Parse(bare.StandardOutput))
            {
                await Assert.That(output.RootElement.GetProperty("reviewBatches").EnumerateArray().All(batch => batch.GetProperty("sampleCount").GetInt32() == 0)).IsTrue();
            }
            ProcessResult pr = await RunCliInAsync(repo, "check", "--base", "main", "--output", "json");
            await Assert.That(pr.ExitCode).IsEqualTo(0);
            using (JsonDocument output = JsonDocument.Parse(pr.StandardOutput))
            {
                JsonElement quality = output.RootElement.GetProperty("reviewBatches").EnumerateArray().Single(batch => batch.GetProperty("ruleId").GetString() == "docs.summary.quality.review");
                await Assert.That(quality.GetProperty("populationCount").GetInt32()).IsEqualTo(2);
                await Assert.That(quality.GetProperty("sampleCount").GetInt32()).IsGreaterThan(0);
                await Assert.That(quality.GetProperty("sampleCount").GetInt32()).IsLessThanOrEqualTo(5);
            }
            ProcessResult all = await RunCliInAsync(repo, "check", "--review-all", "--output", "json");
            await Assert.That(all.ExitCode).IsEqualTo(0);
            using (JsonDocument output = JsonDocument.Parse(all.StandardOutput))
            {
                await Assert.That(output.RootElement.GetProperty("reviewBatches")[0].GetProperty("sampleCount").GetInt32()).IsGreaterThan(0);
            }
            ProcessResult badRef = await RunCliInAsync(repo, "check", "--base", "no-such-ref");
            await Assert.That(badRef.ExitCode).IsEqualTo(3);
            await Assert.That(badRef.StandardError).Contains("invalid or has no merge base");
            await Assert.That((await RunCliInAsync(repo, "check", "src/Api.cs", "--base", "main")).ExitCode).IsEqualTo(2);
            await Assert.That((await RunCliInAsync(repo, "check", "--changed", "--review-all")).ExitCode).IsEqualTo(2);
            await Assert.That((await RunCliInAsync(repo, "check", "src/Api.cs", "--review-all")).ExitCode).IsEqualTo(2);
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
            ProcessResult check = await RunCliInAsync(repo, "check", "--review-all", "--output", "json");
            await Assert.That(check.ExitCode).IsEqualTo(0);
            using JsonDocument json = JsonDocument.Parse(check.StandardOutput);
            string handle = json.RootElement.GetProperty("reviewBatches")[0].GetProperty("handle").GetString()!;
            ProcessResult defaultHandoff = await RunCliInAsync(repo, true, "review", "handoff", "B-1");
            if (defaultHandoff.ExitCode != 0) { throw new InvalidOperationException(defaultHandoff.StandardError); }
            await Assert.That(defaultHandoff.ExitCode).IsEqualTo(0);
            await Assert.That(defaultHandoff.StandardOutput).Contains(".hygiene");
            await Assert.That(defaultHandoff.StandardOutput).Contains("Status: escalation-required");
            string handoffLine = defaultHandoff.StandardOutput.Split('\n').Single(line => line.TrimStart().StartsWith("Review handoff written: ", StringComparison.Ordinal));
            string defaultPath = handoffLine[(handoffLine.IndexOf("Review handoff written: ", StringComparison.Ordinal) + "Review handoff written: ".Length)..].Trim();
            await Assert.That(File.Exists(defaultPath)).IsTrue();

            string externalPath = Path.Combine(external, "nested", "request.json");
            ProcessResult externalHandoff = await RunCliInAsync(repo, true, "review", "handoff", handle, "--file", externalPath, "--output", "json");
            await Assert.That(externalHandoff.ExitCode).IsEqualTo(0);
            await Assert.That(File.Exists(externalPath)).IsTrue();
            using JsonDocument handoffReport = JsonDocument.Parse(externalHandoff.StandardOutput);
            await Assert.That(handoffReport.RootElement.GetProperty("status").GetString()).IsEqualTo("escalation-required");
            await Assert.That(handoffReport.RootElement.GetProperty("reviewerClass").GetString()).IsNotNull();
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
        await Assert.That(rules.GetArrayLength()).IsEqualTo(19);
        await Assert.That(rules.EnumerateArray().Select(x => x.GetProperty("id").GetString()!).TakeLast(4).ToArray())
            .IsEquivalentTo(new[] { "architecture.boringness.review", "readability.type-responsibility.review", "tests.quality.review", "comments.intent.review" });
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
        foreach (string required in new[] { "Targets:", "--base", "--review-all", "Exit 0", "--output text|json", "presentation-only", "non-mutating", "findings", "review accept", "0/N", "review expand", "review handoff", "planner", ".hygiene", "no model/provider", "dotnet hygiene" })
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
            await Assert.That(string.Join(",", normalizeCheckJson.RootElement.GetProperty("selectedRuleIds").EnumerateArray().Select(item => item.GetString()))).IsEqualTo("style.braces.required,style.qualification.this.unnecessary,style.qualification.redundant,style.object-creation.target-typed,format.csharp.roslyn");
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

    [Test]
    public async Task ExhaustiveAuditRequiresAttestationResumesJudgmentsAndPreservesNormalState()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-yolo-cli-" + Guid.NewGuid().ToString("N"));
        string report = Path.Combine(Path.GetTempPath(), "hygiene-yolo-report-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await InitGitAsync(repo, "main");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string sourcePath = Path.Combine(repo, "src", "Fixture.cs");
            await File.WriteAllTextAsync(sourcePath, "/// <summary>Returns a stable value for callers.</summary>\n// Explain the public value contract.\npublic class Fixture { public int ReadValue() => 1; }\n");
            Directory.CreateDirectory(Path.Combine(repo, ".hygiene"));
            await File.WriteAllTextAsync(Path.Combine(repo, ".hygiene", "profile.json"), "{\"schemaVersion\":1,\"profile\":\"dotnet-11\",\"version\":2}");
            await File.WriteAllTextAsync(Path.Combine(repo, ".hygiene", "config.json"), "{\"schemaVersion\":1,\"disabledRules\":[\"docs.summary.language.german.review\",\"docs.text.sentence\"]}");
            await PrepareAcceptedBaselineAsync(repo);
            await File.AppendAllTextAsync(sourcePath, "\npublic sealed class UndocumentedApi{public int Value=>3;}\n");
            await CommitAllAsync(repo, "accepted audit fixture baseline");
            await File.AppendAllTextAsync(sourcePath, "\n");
            ProcessResult normalCheck = await RunCliInAsync(repo, "check", "--output", "json");
            using JsonDocument normalJson = JsonDocument.Parse(normalCheck.StandardOutput);
            string ignoredHandle = normalJson.RootElement.GetProperty("findings").EnumerateArray()
                .Single(finding => finding.GetProperty("ruleId").GetString() == "docs.summary.required" && finding.GetProperty("path").GetString() == "src/Fixture.cs" && finding.GetProperty("symbol").GetString() == "UndocumentedApi")
                .GetProperty("handle").GetString()!;
            ProcessResult ignore = await RunCliInAsync(repo, "ignore", ignoredHandle, "--reason", "Explicit audit fixture ignore.");
            await Assert.That(ignore.ExitCode).IsEqualTo(0);
            Directory.CreateDirectory(Path.Combine(repo, ".hygiene", ".state"));
            string latestPath = Path.Combine(repo, ".hygiene", ".state", "latest-run.json");
            string reviewsPath = Path.Combine(repo, ".hygiene", ".state", "gate-reviews.json");
            await File.WriteAllTextAsync(latestPath, "latest sentinel");
            await File.WriteAllTextAsync(reviewsPath, "accepted-observation sentinel");
            string[] protectedPaths = [sourcePath, Path.Combine(repo, "src", "Fixture.csproj"), Path.Combine(repo, ".hygiene", "profile.json"), Path.Combine(repo, ".hygiene", "config.json"), Path.Combine(repo, ".hygiene", "decisions.json"), Path.Combine(repo, ".hygiene", ".state", "baseline-approval.json"), latestPath, reviewsPath, Path.Combine(repo, ".hygiene", "profile", "Hygiene.props"), Path.Combine(repo, ".editorconfig"), Path.Combine(repo, "Directory.Build.props"), Path.Combine(repo, ".git", "index")];
            string[] before = await Task.WhenAll(protectedPaths.Select(async path => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(path)))));

            ProcessResult prompt = await RunCliInAsync(repo, "--yolo-all", "--output", "json");
            await Assert.That(prompt.ExitCode).IsEqualTo(3);
            using JsonDocument promptJson = JsonDocument.Parse(prompt.StandardOutput);
            await Assert.That(promptJson.RootElement.GetProperty("status").GetString()).IsEqualTo("authorization-required");
            await Assert.That(promptJson.RootElement.GetProperty("scanStarted").GetBoolean()).IsFalse();
            await Assert.That(Directory.Exists(report)).IsFalse();
            ProcessResult denied = await RunCliInAsync(repo, "--yolo-all", "--deny-explicit-task", "--report-dir", report, "--output", "json");
            await Assert.That(denied.ExitCode).IsEqualTo(3);
            await Assert.That(Directory.Exists(report)).IsFalse();
            string occupied = Path.Combine(repo, "occupied-audit-dir");
            Directory.CreateDirectory(occupied);
            string unrelated = Path.Combine(occupied, "keep.txt");
            await File.WriteAllTextAsync(unrelated, "unrelated output sentinel");
            ProcessResult collision = await RunCliInAsync(repo, "--yolo-all", "--attest-explicit-task", "--report-dir", occupied);
            await Assert.That(collision.ExitCode).IsEqualTo(3);
            await Assert.That(await File.ReadAllTextAsync(unrelated)).IsEqualTo("unrelated output sentinel");

            ProcessResult start = await RunCliInAsync(repo, "--yolo-all", "--attest-explicit-task", "--report-dir", report, "--output", "json");
            await Assert.That(start.ExitCode).IsEqualTo(0);
            using JsonDocument startJson = JsonDocument.Parse(start.StandardOutput);
            await Assert.That(startJson.RootElement.GetProperty("status").GetString()).IsEqualTo("collecting");
            ProcessResult firstWork = await RunCliInAsync(repo, "audit", "next", "--run", report);
            await Assert.That(firstWork.ExitCode).IsEqualTo(0);
            using JsonDocument firstJson = JsonDocument.Parse(firstWork.StandardOutput);
            JsonElement firstItems = firstJson.RootElement.GetProperty("items");
            await Assert.That(firstItems.GetArrayLength()).IsGreaterThan(1);

            string auditId = startJson.RootElement.GetProperty("auditDirectory").GetString() is not null
                ? JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(report, "manifest.json"))).RootElement.GetProperty("auditId").GetString()!
                : throw new InvalidOperationException("Audit ID was absent.");
            JsonElement firstItem = firstItems[0];
            string firstObligation = firstItem.GetProperty("obligationId").GetString()!;
            string judgmentsPath = Path.Combine(repo, "judgments.json");
            var invalidSubmissions = new object[]
            {
                new { auditId, judgments = new[] { new { obligationId = firstObligation, outcome = "problem", rationale = (string?)null } } },
                new { auditId, judgments = new[] { new { obligationId = "unknown/rule-item", outcome = "acceptable", rationale = (string?)null } } },
                new { auditId = "A-OTHER-RUN", judgments = new[] { new { obligationId = firstObligation, outcome = "acceptable", rationale = (string?)null } } },
                new { auditId, judgments = new[] { new { obligationId = firstObligation, outcome = "acceptable", rationale = (string?)null }, new { obligationId = firstObligation, outcome = "uncertain", rationale = (string?)"duplicate" } } }
            };
            foreach (object invalidSubmission in invalidSubmissions)
            {
                await File.WriteAllTextAsync(judgmentsPath, JsonSerializer.Serialize(invalidSubmission));
                ProcessResult rejected = await RunCliInAsync(repo, "audit", "record", "--run", report, "--file", judgmentsPath);
                await Assert.That(rejected.ExitCode).IsEqualTo(3);
            }
            await File.WriteAllTextAsync(judgmentsPath, "{");
            ProcessResult malformed = await RunCliInAsync(repo, "audit", "record", "--run", report, "--file", judgmentsPath);
            await Assert.That(malformed.ExitCode).IsEqualTo(3);
            ProcessResult stillPending = await RunCliInAsync(repo, "audit", "next", "--run", report);
            using (JsonDocument pendingJson = JsonDocument.Parse(stillPending.StandardOutput))
            {
                await Assert.That(pendingJson.RootElement.GetProperty("items")[0].GetProperty("obligationId").GetString()).IsEqualTo(firstObligation);
            }
            var firstJudgments = new { auditId, judgments = new[] { new { obligationId = firstObligation, outcome = "acceptable", rationale = (string?)null } } };
            await File.WriteAllTextAsync(judgmentsPath, JsonSerializer.Serialize(firstJudgments));
            ProcessResult recordOne = await RunCliInAsync(repo, "audit", "record", "--run", report, "--file", judgmentsPath);
            await Assert.That(recordOne.ExitCode).IsEqualTo(0);
            ProcessResult resumed = await RunCliInAsync(repo, "audit", "next", "--run", report);
            using JsonDocument resumedJson = JsonDocument.Parse(resumed.StandardOutput);
            await Assert.That(resumedJson.RootElement.GetProperty("remaining").GetInt32()).IsGreaterThan(0);
            await Assert.That(resumedJson.RootElement.GetProperty("items").GetArrayLength()).IsLessThanOrEqualTo(20);
            await Assert.That(resumedJson.RootElement.GetProperty("items").EnumerateArray().Any(item => item.GetProperty("obligationId").GetString() == firstObligation)).IsFalse();
            ProcessResult partialFinish = await RunCliInAsync(repo, "audit", "finish", "--run", report, "--output", "json");
            await Assert.That(partialFinish.ExitCode).IsEqualTo(5);
            using JsonDocument partialFinishJson = JsonDocument.Parse(partialFinish.StandardOutput);
            await Assert.That(partialFinishJson.RootElement.GetProperty("status").GetString()).IsEqualTo("incomplete");
            using (JsonDocument partialReport = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(report, "report.json"))))
            {
                await Assert.That(partialReport.RootElement.GetProperty("coverage").GetProperty("unassessed").GetInt32()).IsGreaterThan(0);
            }

            var remaining = resumedJson.RootElement.GetProperty("items").EnumerateArray().Select((item, index) => new
            {
                obligationId = item.GetProperty("obligationId").GetString(),
                outcome = index % 2 == 0 ? "problem" : "uncertain",
                rationale = index % 2 == 0 ? "Fixture evidence indicates a concrete issue." : "The available context does not establish this contract."
            }).ToArray();
            await File.WriteAllTextAsync(judgmentsPath, JsonSerializer.Serialize(new { auditId, judgments = remaining }));
            ProcessResult recordRest = await RunCliInAsync(repo, "audit", "record", "--run", report, "--file", judgmentsPath);
            await Assert.That(recordRest.ExitCode).IsEqualTo(0);
            ProcessResult finish = await RunCliInAsync(repo, "audit", "finish", "--run", report, "--output", "json");
            await Assert.That(finish.ExitCode).IsEqualTo(0);
            using JsonDocument finishJson = JsonDocument.Parse(finish.StandardOutput);
            await Assert.That(finishJson.RootElement.GetProperty("status").GetString()).IsEqualTo("report-complete");
            using JsonDocument reportJson = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(report, "report.json")));
            await Assert.That(reportJson.RootElement.GetProperty("schemaVersion").GetInt32()).IsEqualTo(2);
            JsonElement coverage = reportJson.RootElement.GetProperty("coverage");
            await Assert.That(coverage.GetProperty("eligible").GetInt32()).IsEqualTo(coverage.GetProperty("assessed").GetInt32());
            await Assert.That(coverage.GetProperty("problem").GetInt32() + coverage.GetProperty("uncertain").GetInt32()).IsGreaterThan(0);
            await Assert.That(reportJson.RootElement.GetProperty("source").GetProperty("dirtyAtStart").GetString()).IsEqualTo("dirty");
            await Assert.That(reportJson.RootElement.GetProperty("findings").EnumerateArray().Any(finding => finding.GetProperty("wasIgnored").GetBoolean() && finding.GetProperty("finding").GetProperty("ruleId").GetString() == "docs.summary.required")).IsTrue();
            await Assert.That(reportJson.RootElement.GetProperty("rewriteProposals").EnumerateArray().Single(proposal => proposal.GetProperty("command").GetString() == "format").GetProperty("changedPaths").EnumerateArray().Any(path => path.GetString() == "src/Fixture.cs")).IsTrue();
            await Assert.That(reportJson.RootElement.GetProperty("rewriteProposals").EnumerateArray().Any(proposal => proposal.GetProperty("command").GetString() == "normalize")).IsTrue();
            JsonElement reportRules = reportJson.RootElement.GetProperty("rules");
            int ruleFindingTotal = reportRules.EnumerateArray().Where(rule => rule.GetProperty("evaluationKind").GetString() == "deterministic-evaluation").Sum(rule => rule.GetProperty("deterministicFindings").GetInt32());
            await Assert.That(ruleFindingTotal).IsEqualTo(coverage.GetProperty("deterministicFindings").GetInt32());
            JsonElement disabled = reportRules.EnumerateArray().Single(rule => rule.GetProperty("id").GetString() == "docs.summary.language.german.review");
            await Assert.That(disabled.GetProperty("enabled").GetBoolean()).IsFalse();
            await Assert.That(disabled.GetProperty("evaluationKind").GetString()).IsEqualTo("disabled");
            await Assert.That(disabled.GetProperty("approximateApplicableCount").ValueKind).IsEqualTo(JsonValueKind.Null);
            JsonElement disabledDeterministic = reportRules.EnumerateArray().Single(rule => rule.GetProperty("id").GetString() == "docs.text.sentence");
            await Assert.That(disabledDeterministic.GetProperty("enabled").GetBoolean()).IsFalse();
            await Assert.That(disabledDeterministic.GetProperty("evaluationKind").GetString()).IsEqualTo("disabled");
            await Assert.That(disabledDeterministic.GetProperty("deterministicFindings").ValueKind).IsEqualTo(JsonValueKind.Null);
            await Assert.That(disabledDeterministic.GetProperty("approximateApplicableCount").ValueKind).IsEqualTo(JsonValueKind.Null);
            JsonElement zeroSemantic = reportRules.EnumerateArray().Single(rule => rule.GetProperty("id").GetString() == "tests.quality.review");
            await Assert.That(zeroSemantic.GetProperty("eligible").GetInt32()).IsEqualTo(0);
            await Assert.That(zeroSemantic.GetProperty("measuredEvaluations").GetInt32()).IsEqualTo(0);
            JsonElement mandatory = reportRules.EnumerateArray().Single(rule => rule.GetProperty("id").GetString() == "profile.dotnet.analysis.required");
            await Assert.That(mandatory.GetProperty("participation").GetString()).IsEqualTo("mandatory");
            JsonElement deterministicFindingRule = reportRules.EnumerateArray().Single(rule => rule.GetProperty("id").GetString() == "docs.summary.required");
            await Assert.That(deterministicFindingRule.GetProperty("deterministicFindings").GetInt32()).IsGreaterThan(0);
            await Assert.That(deterministicFindingRule.GetProperty("eligible").ValueKind).IsEqualTo(JsonValueKind.Null);
            await Assert.That(deterministicFindingRule.GetProperty("measuredEvaluations").ValueKind).IsEqualTo(JsonValueKind.Null);
            await Assert.That(deterministicFindingRule.GetProperty("approximateApplicableCount").GetInt32()).IsGreaterThan(0);
            JsonElement deterministicNoFindings = reportRules.EnumerateArray().Single(rule => rule.GetProperty("id").GetString() == "docs.xml.consistent");
            await Assert.That(deterministicNoFindings.GetProperty("deterministicFindings").GetInt32()).IsEqualTo(0);
            await Assert.That(deterministicNoFindings.GetProperty("evaluationKind").GetString()).IsEqualTo("deterministic-evaluation");
            foreach (JsonElement ruleFact in reportRules.EnumerateArray().Where(rule => rule.GetProperty("evaluationKind").GetString() == "semantic-assessment"))
            {
                int outcomeCount = ruleFact.GetProperty("acceptable").GetInt32() + ruleFact.GetProperty("problem").GetInt32() + ruleFact.GetProperty("uncertain").GetInt32() + ruleFact.GetProperty("unassessed").GetInt32();
                await Assert.That(ruleFact.GetProperty("eligible").GetInt32()).IsEqualTo(outcomeCount);
            }
            await Assert.That(await File.ReadAllTextAsync(Path.Combine(report, "report.md"))).Contains("approximate population sizes");
            await Assert.That(await File.ReadAllTextAsync(Path.Combine(report, "report.md"))).Contains("Limitations");
            string firstStoredBatch = Directory.GetFiles(Path.Combine(report, "judgments"), "*.json").Order(StringComparer.Ordinal).First();
            using (JsonDocument storedBatch = JsonDocument.Parse(await File.ReadAllTextAsync(firstStoredBatch)))
            {
                int removedCount = storedBatch.RootElement.GetProperty("judgments").GetArrayLength();
                File.Delete(firstStoredBatch);
                ProcessResult missingJudgment = await RunCliInAsync(repo, "audit", "finish", "--run", report, "--output", "json");
                await Assert.That(missingJudgment.ExitCode).IsEqualTo(5);
                using JsonDocument missingReport = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(report, "report.json")));
                await Assert.That(missingReport.RootElement.GetProperty("status").GetString()).IsEqualTo("incomplete");
                await Assert.That(missingReport.RootElement.GetProperty("coverage").GetProperty("assessed").GetInt32()).IsEqualTo(coverage.GetProperty("assessed").GetInt32() - removedCount);
            }
            string inventoryPath = Path.Combine(report, "inventory.json");
            await File.AppendAllTextAsync(inventoryPath, " ");
            ProcessResult corruptFinish = await RunCliInAsync(repo, "audit", "finish", "--run", report, "--output", "json");
            await Assert.That(corruptFinish.ExitCode).IsEqualTo(3);
            string[] after = await Task.WhenAll(protectedPaths.Select(async path => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(path)))));
            await Assert.That(string.Join(",", after)).IsEqualTo(string.Join(",", before));
            ProcessResult defaultStart = await RunCliInAsync(repo, "--yolo-all", "--attest-explicit-task", "--output", "json");
            await Assert.That(defaultStart.ExitCode).IsEqualTo(0);
            using JsonDocument defaultStartJson = JsonDocument.Parse(defaultStart.StandardOutput);
            string defaultDirectory = defaultStartJson.RootElement.GetProperty("auditDirectory").GetString()!;
            await Assert.That(Path.GetFullPath(defaultDirectory).StartsWith(Path.GetFullPath(Path.Combine(repo, ".hygiene", "audits")), StringComparison.OrdinalIgnoreCase)).IsTrue();
            await Assert.That(File.Exists(Path.Combine(defaultDirectory, "inventory.json"))).IsTrue();
        }
        finally
        {
            DeleteTree(repo);
            DeleteTree(report);
        }
    }

    [Test]
    public async Task ExhaustiveAuditStopsWithoutAcceptedBaselineAndLeavesDestinationAbsent()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-yolo-prereq-" + Guid.NewGuid().ToString("N"));
        string report = Path.Combine(Path.GetTempPath(), "hygiene-yolo-prereq-report-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        try
        {
            await InitGitAsync(repo, "main");
            ProcessResult result = await RunCliInAsync(repo, "--yolo-all", "--attest-explicit-task", "--report-dir", report, "--output", "json");
            await Assert.That(result.ExitCode).IsEqualTo(3);
            using JsonDocument json = JsonDocument.Parse(result.StandardOutput);
            await Assert.That(json.RootElement.GetProperty("status").GetString()).IsEqualTo("prerequisite-required");
            await Assert.That(json.RootElement.GetProperty("scanStarted").GetBoolean()).IsFalse();
            await Assert.That(Directory.Exists(report)).IsFalse();
            await Assert.That(Directory.Exists(Path.Combine(repo, ".hygiene"))).IsFalse();
        }
        finally { DeleteTree(repo); DeleteTree(report); }
    }

    [Test]
    public async Task ExhaustiveAuditResumeUsesRecordedRepositoryIdentityAcrossRepositories()
    {
        string repoA = Path.Combine(Path.GetTempPath(), "hygiene-yolo-source-" + Guid.NewGuid().ToString("N"));
        string repoB = Path.Combine(Path.GetTempPath(), "hygiene-yolo-resume-" + Guid.NewGuid().ToString("N"));
        string report = Path.Combine(Path.GetTempPath(), "hygiene-yolo-cross-repo-report-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repoA, "src"));
        Directory.CreateDirectory(repoB);
        try
        {
            await InitGitAsync(repoA, "main");
            await File.WriteAllTextAsync(Path.Combine(repoA, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repoA, "src", "Fixture.cs"), "/// <summary>Provides a stable caller-facing fixture.</summary>\npublic sealed class Fixture { public int Value => 1; }\n");
            Directory.CreateDirectory(Path.Combine(repoA, ".hygiene"));
            await File.WriteAllTextAsync(Path.Combine(repoA, ".hygiene", "profile.json"), "{\"schemaVersion\":1,\"profile\":\"dotnet-11\",\"version\":2}");
            await File.WriteAllTextAsync(Path.Combine(repoA, ".hygiene", "config.json"), "{\"schemaVersion\":1,\"disabledRules\":[\"docs.summary.language.german.review\"]}");
            await PrepareAcceptedBaselineAsync(repoA);
            await CommitAllAsync(repoA, "cross repository audit fixture");
            await InitGitAsync(repoB, "main");

            ProcessResult start = await RunCliInAsync(repoA, "--yolo-all", "--attest-explicit-task", "--report-dir", report, "--output", "json");
            await Assert.That(start.ExitCode).IsEqualTo(0);
            using (JsonDocument manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(report, "manifest.json"))))
            {
                await Assert.That(manifest.RootElement.GetProperty("sourceRoot").GetString()).IsEqualTo(Path.GetFullPath(repoA).TrimEnd(Path.DirectorySeparatorChar));
            }
            using (JsonDocument inventory = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(report, "inventory.json"))))
            {
                await Assert.That(inventory.RootElement.GetProperty("sourceRoot").GetString()).IsEqualTo(Path.GetFullPath(repoA).TrimEnd(Path.DirectorySeparatorChar));
                await Assert.That(inventory.RootElement.GetProperty("sources").EnumerateArray().All(source => source.GetProperty("path").GetString() == "src/Fixture.cs")).IsTrue();
            }

            ProcessResult next = await RunCliInAsync(repoB, "audit", "next", "--run", report);
            await Assert.That(next.ExitCode).IsEqualTo(0);
            using JsonDocument nextJson = JsonDocument.Parse(next.StandardOutput);
            JsonElement item = nextJson.RootElement.GetProperty("items")[0];
            await Assert.That(item.GetProperty("sourcePath").GetString()).IsEqualTo("src/Fixture.cs");
            string judgmentPath = Path.Combine(repoB, "judgments.json");
            await File.WriteAllTextAsync(judgmentPath, JsonSerializer.Serialize(new { auditId = nextJson.RootElement.GetProperty("auditId").GetString(), judgments = new[] { new { obligationId = item.GetProperty("obligationId").GetString(), outcome = "acceptable", rationale = (string?)null } } }));
            ProcessResult record = await RunCliInAsync(repoB, "audit", "record", "--run", report, "--file", judgmentPath);
            await Assert.That(record.ExitCode).IsEqualTo(0);
            ProcessResult finish = await RunCliInAsync(repoB, "audit", "finish", "--run", report, "--output", "json");
            await Assert.That(finish.ExitCode).IsEqualTo(5);
            using (JsonDocument reportJson = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(report, "report.json"))))
            {
                await Assert.That(reportJson.RootElement.GetProperty("source").GetProperty("root").GetString()).IsEqualTo(Path.GetFullPath(repoA).TrimEnd(Path.DirectorySeparatorChar));
                await Assert.That(reportJson.RootElement.GetProperty("source").GetProperty("root").GetString()).IsNotEqualTo(Path.GetFullPath(repoB).TrimEnd(Path.DirectorySeparatorChar));
                JsonElement deterministic = reportJson.RootElement.GetProperty("rules").EnumerateArray().Single(rule => rule.GetProperty("id").GetString() == "docs.xml.consistent");
                await Assert.That(deterministic.GetProperty("evaluationKind").GetString()).IsEqualTo("deterministic-evaluation");
                await Assert.That(deterministic.GetProperty("deterministicFindings").GetInt32()).IsEqualTo(0);
                await Assert.That(deterministic.GetProperty("approximateApplicableCount").GetInt32()).IsEqualTo(1);
            }
            string markdown = await File.ReadAllTextAsync(Path.Combine(report, "report.md"));
            await Assert.That(markdown).Contains($"Source repository: `{Path.GetFullPath(repoA).TrimEnd(Path.DirectorySeparatorChar)}`");
            await Assert.That(markdown).DoesNotContain(Path.GetFullPath(repoB).TrimEnd(Path.DirectorySeparatorChar));

            string manifestPath = Path.Combine(report, "manifest.json");
            string validManifest = await File.ReadAllTextAsync(manifestPath);
            Dictionary<string, object?> malformedRoot = JsonSerializer.Deserialize<Dictionary<string, object?>>(validManifest)!;
            malformedRoot["sourceRoot"] = "relative/repository";
            await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(malformedRoot));
            ProcessResult malformedIdentity = await RunCliInAsync(repoB, "audit", "finish", "--run", report, "--output", "json");
            await Assert.That(malformedIdentity.ExitCode).IsEqualTo(3);
            Dictionary<string, object?> incompatibleRoot = JsonSerializer.Deserialize<Dictionary<string, object?>>(validManifest)!;
            incompatibleRoot["sourceRoot"] = Path.GetFullPath(repoB).TrimEnd(Path.DirectorySeparatorChar);
            await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(incompatibleRoot));
            ProcessResult incompatibleIdentity = await RunCliInAsync(repoB, "audit", "finish", "--run", report, "--output", "json");
            await Assert.That(incompatibleIdentity.ExitCode).IsEqualTo(3);
            await File.WriteAllTextAsync(manifestPath, validManifest);
            Dictionary<string, object?> missingRoot = JsonSerializer.Deserialize<Dictionary<string, object?>>(validManifest)!;
            missingRoot.Remove("sourceRoot");
            await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(missingRoot));
            ProcessResult missingIdentity = await RunCliInAsync(repoB, "audit", "finish", "--run", report, "--output", "json");
            await Assert.That(missingIdentity.ExitCode).IsEqualTo(3);
        }
        finally
        {
            DeleteTree(repoA);
            DeleteTree(repoB);
            DeleteTree(report);
        }
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
        startInfo.Environment["DOTNET_HYGIENE_SKIP_UPDATE"] = "1";

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

    private static async Task InitGitAsync(string root, string branch)
    {
        await RunGitRawAsync(root, "init", "-q", "-b", branch);
        await RunGitRawAsync(root, "config", "user.email", "fixture@example.invalid");
        await RunGitRawAsync(root, "config", "user.name", "M0018 Fixture");
    }

    private static async Task PrepareAcceptedBaselineAsync(string root)
    {
        ProcessResult bootstrap = await RunCliInAsync(root, "bootstrap", "--output", "json");
        if (bootstrap.ExitCode != 0) { throw new InvalidOperationException(bootstrap.StandardError); }
        using JsonDocument report = JsonDocument.Parse(bootstrap.StandardOutput);
        string hash = report.RootElement.GetProperty("reportSha256").GetString()!;
        ProcessResult accepted = await RunCliInAsync(root, "baseline", "accept", "--approver", "fixture approver", "--report-sha256", hash, "--output", "json");
        if (accepted.ExitCode != 0) { throw new InvalidOperationException(accepted.StandardError); }
    }

    private static async Task CommitAllAsync(string root, string message)
    {
        await RunGitRawAsync(root, "add", "-A");
        await RunGitRawAsync(root, "commit", "-m", message);
    }

    private static async Task<string> GitTextAsync(string root, params string[] args)
    {
        ProcessStartInfo info = new("git") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string argument in args) { info.ArgumentList.Add(argument); }
        using Process process = Process.Start(info)!;
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) { throw new InvalidOperationException(error); }
        return output.Trim();
    }

    private static async Task RunGitRawAsync(string root, params string[] args) => _ = await GitTextAsync(root, args);

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
