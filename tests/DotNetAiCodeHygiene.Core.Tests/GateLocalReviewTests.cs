using System.Diagnostics;
using System.Text.Json;
using DotNetAiCodeHygiene.Core;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class GateLocalReviewTests
{
    [Test]
    public async Task GateSelectionIsStableBoundedAndDoesNotRefillAfterAcceptance()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-gate-select-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            string path = Path.Combine(repository, "Api.cs");
            ReviewSubject[] subjects = Enumerable.Range(0, 12).Select(i => new ReviewSubject(
                "project\0Api.cs\0T:Api" + i, "content-" + i,
                new ReviewItem("", "Api.cs", i + 1, 1, "Api" + i, "Summary", "Api"),
                DirectChange: i < 7)).ToArray();
            Rule rule = new(SummaryQualityReviewRuleModule.RuleId, 6, "review-batch", "review-batch", "", true);
            var store = new GateLocalReviewStore(repository);
            IReadOnlySet<string> touched = new HashSet<string>(["Api.cs"], StringComparer.Ordinal);
            ReviewSubject[] first = store.Select("gate", rule, subjects, touched, "changed").ToArray();
            ReviewSubject[] repeat = store.Select("gate", rule, subjects.Reverse().ToArray(), touched, "changed").ToArray();
            await Assert.That(first.Select(x => x.Identity).ToArray()).IsEquivalentTo(repeat.Select(x => x.Identity).ToArray());
            await Assert.That(first.Count).IsEqualTo(5);
            await Assert.That(first.Count(x => x.DirectChange)).IsEqualTo(4);
            await Assert.That(first.Count(x => !x.DirectChange)).IsEqualTo(1);
            ReviewSubject accepted = first[0];
            await Assert.That(store.Accept("ignored", rule, accepted)).IsFalse();
            await Assert.That(store.AcceptTicket(accepted.GateTicket!)).IsTrue();
            store.Commit();

            ReviewSubject[] afterAcceptance = new GateLocalReviewStore(repository).Select("gate", rule, subjects, touched, "changed").ToArray();
            await Assert.That(afterAcceptance.Select(x => x.Identity).ToArray()).IsEquivalentTo(first.Skip(1).Select(x => x.Identity).ToArray());
            await Assert.That(afterAcceptance.Count).IsEqualTo(4);
            ReviewSubject[] newFingerprint = new GateLocalReviewStore(repository).Select("new-gate", rule,
                subjects.Select(x => x.Identity == accepted.Identity ? x with { Content = "changed-content" } : x).ToArray(), touched, "changed").ToArray();
            await Assert.That(newFingerprint.Count).IsEqualTo(5);
        }
        finally { RemoveFixture(repository); }
    }

    [Test]
    public async Task RealGitBaseGateAdmitsCurrentSourceChangesAndPersistsAcceptanceAcrossRestart()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-gate-git-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            await Git(repository, "init", "-q", "-b", "main");
            await Git(repository, "config", "user.email", "fixture@example.invalid");
            await Git(repository, "config", "user.name", "Gate Fixture");
            await File.WriteAllTextAsync(Path.Combine(repository, "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string sourcePath = Path.Combine(repository, "Api.cs");
            string source = "/// <summary>Provides useful behavior for API 0.</summary>\npublic sealed class Api0\n{\n    public int Value => 0;\n}";
            await File.WriteAllTextAsync(sourcePath, source);
            string unrelated = string.Join("\n", Enumerable.Range(1, 7).Select(i => $"/// <summary>Provides useful behavior for API {i}.</summary>\npublic sealed class Api{i} {{ public int Value => {i}; }}"));
            await File.WriteAllTextAsync(Path.Combine(repository, "Other.cs"), unrelated);
            _ = new ProfileManager(repository).Bootstrap();
            Directory.CreateDirectory(Path.Combine(repository, ".hygiene", ".state"));
            string legacyPath = Path.Combine(repository, ".hygiene", ".state", "sampling.json");
            await File.WriteAllTextAsync(legacyPath, "legacy hazard data retained verbatim");
            await Git(repository, "add", "-A");
            await Git(repository, "commit", "-m", "baseline");
            string baseCommit = (await Git(repository, "rev-parse", "HEAD")).Trim();

            string changed = source.Replace("Value => 0", "Value => 42", StringComparison.Ordinal);
            await File.WriteAllTextAsync(sourcePath, changed);
            await Git(repository, "add", "Api.cs");
            await Git(repository, "commit", "-m", "feature source change");
            var engine = new HygieneEngine(repository);
            CheckResult bare = engine.Check([], false);
            await Assert.That(bare.ReviewBatches.All(x => x.SampleCount == 0)).IsTrue();
            CheckResult pr = engine.Check([], false, baseRef: baseCommit);
            ReviewBatch quality = pr.ReviewBatches.Single(x => x.RuleId == SummaryQualityReviewRuleModule.RuleId);
            await Assert.That(quality.PopulationCount).IsEqualTo(1);
            await Assert.That(quality.SampleCount).IsEqualTo(1);
            await Assert.That(quality.Items.Any(item => item.Symbol == "Api0")).IsTrue();
            await Assert.That(quality.PopulationItems!.Any(item => item.Symbol == quality.Items.Single().Symbol)).IsTrue();
            CheckResult repeated = new HygieneEngine(repository).Check([], false, baseRef: baseCommit);
            ReviewBatch repeatedQuality = repeated.ReviewBatches.Single(x => x.RuleId == quality.RuleId);
            await Assert.That(repeatedQuality.Items.Select(x => x.Id).ToArray()).IsEquivalentTo(quality.Items.Select(x => x.Id).ToArray());

            string acceptedId = repeatedQuality.Items.Single().Id;
            ReviewAcceptance accepted = new HygieneEngine(repository).AcceptReview(repeatedQuality.Handle, [acceptedId]);
            await Assert.That(accepted.AcceptedCount).IsEqualTo(1);
            CheckResult afterRestart = new HygieneEngine(repository).Check([], false, baseRef: baseCommit);
            await Assert.That(afterRestart.ReviewBatches.Single(x => x.RuleId == quality.RuleId).SampleCount).IsEqualTo(0);
            await Assert.That(File.Exists(Path.Combine(repository, ".hygiene", ".state", "gate-reviews.json"))).IsTrue();

            string docsBase = (await Git(repository, "rev-parse", "HEAD")).Trim();
            await File.WriteAllTextAsync(Path.Combine(repository, "README.md"), "documentation-only change");
            CheckResult docsOnly = new HygieneEngine(repository).Check([], false, baseRef: docsBase);
            await Assert.That(docsOnly.ReviewBatches.All(x => x.SampleCount == 0)).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(legacyPath)).IsEqualTo("legacy hazard data retained verbatim");
        }
        finally { RemoveFixture(repository); }
    }

    [Test]
    public async Task InvalidGateReviewStateFailsWithoutReplacingIt()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-gate-invalid-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repository, ".hygiene", ".state"));
        try
        {
            string path = Path.Combine(repository, ".hygiene", ".state", "gate-reviews.json");
            await File.WriteAllTextAsync(path, "{broken");
            bool rejected = false;
            try { _ = new GateLocalReviewStore(repository); }
            catch (ProductException) { rejected = true; }
            await Assert.That(rejected).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(path)).IsEqualTo("{broken");
        }
        finally { RemoveFixture(repository); }
    }

    [Test]
    public async Task AcceptedObservationWriteIsAtomicAndRejectsConcurrentStateChanges()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-gate-atomic-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            ReviewSubject subject = new("project\0Api.cs\0T:Api", "summary", new ReviewItem("", "Api.cs", 1, 1, "Api", "Summary", "Api"));
            Rule rule = new(SummaryQualityReviewRuleModule.RuleId, 6, "review-batch", "review-batch", "", true);
            GateTicket ticket = new GateLocalReviewStore(repository).Select("gate", rule, [subject], new HashSet<string>(), "all").Single().GateTicket!;
            var interrupted = new GateLocalReviewStore(repository, () => throw new IOException("injected before replacement"));
            await Assert.That(interrupted.AcceptTicket(ticket)).IsTrue();
            bool failed = false;
            try { interrupted.Commit(); } catch (IOException) { failed = true; }
            await Assert.That(failed).IsTrue();
            string statePath = Path.Combine(repository, ".hygiene", ".state", "gate-reviews.json");
            await Assert.That(File.Exists(statePath)).IsFalse();

            var winner = new GateLocalReviewStore(repository);
            var concurrent = new GateLocalReviewStore(repository);
            await Assert.That(winner.AcceptTicket(ticket)).IsTrue();
            winner.Commit();
            bool conflict = false;
            try { concurrent.Commit(); } catch (ProductException) { conflict = true; }
            await Assert.That(conflict).IsTrue();
            using JsonDocument saved = JsonDocument.Parse(await File.ReadAllTextAsync(statePath));
            await Assert.That(saved.RootElement.GetProperty("observations").GetArrayLength()).IsEqualTo(1);
        }
        finally { RemoveFixture(repository); }
    }

    private static async Task<string> Git(string cwd, params string[] args)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = cwd, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string arg in args) { start.ArgumentList.Add(arg); }
        using Process process = Process.Start(start)!;
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) { throw new InvalidOperationException(error); }
        return output;
    }

    private static void RemoveFixture(string repository)
    {
        if (!Directory.Exists(repository)) { return; }
        foreach (string file in Directory.EnumerateFiles(repository, "*", SearchOption.AllDirectories)) { File.SetAttributes(file, FileAttributes.Normal); }
        Directory.Delete(repository, true);
    }
}
