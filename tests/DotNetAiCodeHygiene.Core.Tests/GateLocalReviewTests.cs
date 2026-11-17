using System.Diagnostics;
using System.Text.Json;
using DotNetAiCodeHygiene.Core;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class GateLocalReviewTests
{
    [Test]
    public async Task ChangedMergeBaseRejectsOldExpansionHandoffAndAcceptanceWithSamePopulation()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-gate-base-stale-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repository, "src"));
        try
        {
            await Git(repository, "init", "-q", "-b", "main");
            await Git(repository, "config", "user.email", "fixture@example.invalid");
            await Git(repository, "config", "user.name", "Gate Fixture");
            await File.WriteAllTextAsync(Path.Combine(repository, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string sourcePath = Path.Combine(repository, "src", "Api.cs");
            await File.WriteAllTextAsync(sourcePath, "/// <summary>Describes a stable API.</summary>\npublic class Api { public int Run() => 1; }");
            _ = new ProfileManager(repository).Bootstrap();
            await Git(repository, "add", "-A");
            await Git(repository, "commit", "-m", "initial source baseline");
            string initialBase = (await Git(repository, "rev-parse", "HEAD")).Trim();
            await File.WriteAllTextAsync(Path.Combine(repository, "README.md"), "second commit changes docs only");
            await Git(repository, "add", "README.md");
            await Git(repository, "commit", "-m", "advance main without changing C#");
            await Git(repository, "checkout", "-q", "-b", "feature");
            await File.WriteAllTextAsync(sourcePath, "/// <summary>Describes a stable API.</summary>\npublic class Api { public int Run() => 2; }");
            await Git(repository, "add", "src/Api.cs");
            await Git(repository, "commit", "-m", "feature source change");

            var engine = new HygieneEngine(repository);
            string firstMergeBase = (await Git(repository, "merge-base", "main", "HEAD")).Trim();
            CheckResult checkedRun = engine.Check([], false, baseRef: "main");
            ReviewBatch batch = checkedRun.ReviewBatches.Single(item => item.RuleId == DocumentationSummaryQualityReviewRuleModule.RuleId);
            await Assert.That(batch.SampleCount).IsEqualTo(1);
            string itemId = batch.Items.Single().Id;

            await Git(repository, "branch", "-f", "main", initialBase);
            string secondMergeBase = (await Git(repository, "merge-base", "main", "HEAD")).Trim();
            await Assert.That(secondMergeBase).IsNotEqualTo(firstMergeBase);
            CheckResult changedBase = engine.Check([], false, publishLatest: false, baseRef: "main");
            ReviewBatch samePopulation = changedBase.ReviewBatches.Single(item => item.RuleId == batch.RuleId);
            await Assert.That(samePopulation.PopulationFingerprint).IsEqualTo(batch.PopulationFingerprint);

            bool expandRejected = false;
            try { _ = engine.ExpandReview(batch.Handle); } catch (ProductException) { expandRejected = true; }
            await Assert.That(expandRejected).IsTrue();
            string handoffPath = Path.Combine(repository, "stale-handoff.json");
            bool handoffRejected = false;
            try { _ = engine.CreateReviewHandoff(batch.Handle, handoffPath); } catch (ProductException) { handoffRejected = true; }
            await Assert.That(handoffRejected).IsTrue();
            await Assert.That(File.Exists(handoffPath)).IsFalse();
            bool acceptanceRejected = false;
            try { _ = engine.AcceptReview(batch.Handle, [itemId]); } catch (ProductException) { acceptanceRejected = true; }
            await Assert.That(acceptanceRejected).IsTrue();
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
            await Git(repository, "add", "-A");
            await Git(repository, "commit", "-m", "baseline");
            string baseCommit = (await Git(repository, "rev-parse", "HEAD")).Trim();

            string changed = source.Replace("Provides useful behavior for API 0", "Provides updated useful behavior for API 0", StringComparison.Ordinal)
                .Replace("Value => 0", "Value => 42", StringComparison.Ordinal);
            await File.WriteAllTextAsync(sourcePath, changed);
            await Git(repository, "add", "Api.cs");
            await Git(repository, "commit", "-m", "feature source change");
            var engine = new HygieneEngine(repository);
            CheckResult bare = engine.Check([], false);
            await Assert.That(bare.ReviewBatches.All(x => x.SampleCount == 0)).IsTrue();
            ReviewBatch fullReview = engine.Check([], false, reviewAll: true).ReviewBatches.Single(x => x.RuleId == DocumentationSummaryQualityReviewRuleModule.RuleId);
            await Assert.That(fullReview.PopulationItems!.Any(item => item.Path == "Other.cs")).IsTrue();
            await Assert.That(fullReview.SampleCount).IsLessThanOrEqualTo(10);
            CheckResult pr = engine.Check([], false, baseRef: baseCommit);
            ReviewBatch quality = pr.ReviewBatches.Single(x => x.RuleId == DocumentationSummaryQualityReviewRuleModule.RuleId);
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
            Rule rule = new(DocumentationSummaryQualityReviewRuleModule.RuleId, 7, "review-batch", "review-batch", "", true);
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

    [Test]
    public async Task ExhaustiveAuditBypassesNormalSampleAndAcceptedObservationWithoutPublishingNormalState()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-audit-population-" + Guid.NewGuid().ToString("N"));
        string sourcePath = Path.Combine(repository, "Api.cs");
        string source = string.Join("\n", Enumerable.Range(0, 8).Select(i => $"/// <summary>Provides a stable caller-facing purpose for API {i}.</summary>\npublic sealed class Api{i} {{ // rationale for API {i}\n    public int Value => {i};\n}}"));
        await InitializeGitReviewRepository(repository, sourcePath, source);
        try
        {
            string sourceBase = (await Git(repository, "rev-parse", "HEAD")).Trim();
            await File.WriteAllTextAsync(sourcePath, source.Replace("Provides a stable", "Provides an updated", StringComparison.Ordinal)
                .Replace("Value => 0", "Value => 42", StringComparison.Ordinal));
            await Git(repository, "add", sourcePath);
            await Git(repository, "commit", "-m", "change source contract");
            HygieneEngine engine = new(repository);
            ReviewBatch[] normal = engine.Check([], false, baseRef: sourceBase).ReviewBatches.ToArray();
            await Assert.That(normal.Single(batch => batch.RuleId == DocumentationSummaryQualityReviewRuleModule.RuleId).PopulationCount).IsEqualTo(8);
            await Assert.That(normal.Sum(batch => batch.SampleCount)).IsLessThanOrEqualTo(10);
            ReviewBatch acceptedBatch = normal.First(batch => batch.SampleCount > 0);
            string acceptedItemId = acceptedBatch.Items[0].Id;
            _ = engine.AcceptReview(acceptedBatch.Handle, [acceptedItemId]);
            string latestPath = Path.Combine(repository, ".hygiene", ".state", "latest-run.json");
            string gatePath = Path.Combine(repository, ".hygiene", ".state", "gate-reviews.json");
            byte[] latestBefore = await File.ReadAllBytesAsync(latestPath);
            byte[] gateBefore = await File.ReadAllBytesAsync(gatePath);
            byte[] sourceBefore = await File.ReadAllBytesAsync(sourcePath);

            (CheckResult audit, _, _) = engine.CheckExhaustiveAuditSnapshot();
            ReviewBatch auditQuality = audit.ReviewBatches.Single(batch => batch.RuleId == DocumentationSummaryQualityReviewRuleModule.RuleId);
            await Assert.That(auditQuality.PopulationCount).IsEqualTo(8);
            await Assert.That(auditQuality.SampleCount).IsEqualTo(8);
            await Assert.That(auditQuality.Items.Count(item => item.Symbol.StartsWith("Api", StringComparison.Ordinal))).IsEqualTo(8);
            await Assert.That(audit.ReviewBatches.Single(batch => batch.RuleId == TestAssertionEffectivenessReviewRuleModule.RuleId).PopulationCount).IsEqualTo(0);
            await Assert.That(audit.ReviewBatches.Count).IsEqualTo(26);
            await Assert.That((await File.ReadAllBytesAsync(latestPath)).AsSpan().SequenceEqual(latestBefore)).IsTrue();
            await Assert.That((await File.ReadAllBytesAsync(gatePath)).AsSpan().SequenceEqual(gateBefore)).IsTrue();
            await Assert.That((await File.ReadAllBytesAsync(sourcePath)).AsSpan().SequenceEqual(sourceBefore)).IsTrue();
        }
        finally { RemoveFixture(repository); }
    }

    [Test]
    public async Task ExhaustiveReviewMaterializationKeepsMoreThanFiftyThousandObligations()
    {
        const int populationSize = 50_001;
        ReviewSubject[] subjects = Enumerable.Range(0, populationSize).Select(index => new ReviewSubject(
            "project\0Api.cs\0T:Api" + index,
            "content-" + index,
            new ReviewItem("", "Api.cs", index + 1, 1, "Api" + index, "Summary", "Api"))).ToArray();
        Rule rule = new(DocumentationSummaryQualityReviewRuleModule.RuleId, 7, "review-batch", "review-batch", "", true);
        ReviewBatchMaterialization materialized = HygieneEngine.BuildReviewBatch("A-SYNTHETIC", rule, 1, subjects, subjects,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["Api.cs"] = "public sealed class Api { }" },
            [new ReviewQuestion("Q1", "Inspect the contract.")], "No automatic escalation.", "frontier", "implementer", createTickets: false);

        await Assert.That(materialized.Batch.PopulationCount).IsEqualTo(populationSize);
        await Assert.That(materialized.Batch.SampleCount).IsEqualTo(populationSize);
        await Assert.That(materialized.Batch.Items.Count).IsEqualTo(populationSize);
        await Assert.That(materialized.Tickets.Count).IsEqualTo(0);
        await Assert.That(materialized.Batch.Items[populationSize - 1].Id).IsEqualTo("RI-" + populationSize);

        AuditSemanticObligation[] obligations = materialized.Batch.Items.Select(item => new AuditSemanticObligation(
            rule.Id + "/" + item.Id, rule.Id, rule.Version, item.Id, [], item, item.Path)).ToArray();
        var recorded = new HashSet<string>(StringComparer.Ordinal);
        int workOrders = 0;
        while (true)
        {
            IReadOnlyList<AuditSemanticObligation> page = ReadOnlyAuditEngine.SelectNextObligations(obligations, recorded);
            if (page.Count == 0) { break; }
            if (page.Count > ReadOnlyAuditEngine.WorkBatchSize) { throw new InvalidOperationException("Synthetic audit work exceeded its batch bound."); }
            foreach (AuditSemanticObligation obligation in page)
            {
                if (!recorded.Add(obligation.ObligationId)) { throw new InvalidOperationException("Synthetic audit work duplicated an obligation."); }
            }
            workOrders++;
        }

        await Assert.That(recorded.Count).IsEqualTo(populationSize);
        await Assert.That(workOrders).IsEqualTo((populationSize + ReadOnlyAuditEngine.WorkBatchSize - 1) / ReadOnlyAuditEngine.WorkBatchSize);
    }

    [Test]
    public async Task ExhaustivePopulationIncludesMultipleProjectsAndExcludesGeneratedSubjects()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-audit-project-population-" + Guid.NewGuid().ToString("N"));
        string primarySource = Path.Combine(repository, "Api.cs");
        await InitializeGitReviewRepository(repository, primarySource, "/// <summary>Provides the primary public contract.</summary>\npublic sealed class PrimaryApi { public int Value => 1; }");
        try
        {
            string secondProject = Path.Combine(repository, "Other");
            Directory.CreateDirectory(secondProject);
            await File.WriteAllTextAsync(Path.Combine(secondProject, "Other.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(secondProject, "Other.cs"), "/// <summary>Provides a second project contract.</summary>\npublic sealed class OtherApi { public int Value => 2; }");
            await File.WriteAllTextAsync(Path.Combine(secondProject, "Auto.g.cs"), "// <auto-generated/>\npublic sealed class GeneratedApi { }");

            (CheckResult audit, int sourceDocuments, int projects) = new HygieneEngine(repository).CheckExhaustiveAuditSnapshot();
            ReviewItem[] allSubjects = audit.ReviewBatches.SelectMany(batch => batch.PopulationItems ?? batch.Items).ToArray();
            bool independentRubrics = audit.ReviewBatches
                .SelectMany(batch => (batch.PopulationItems ?? batch.Items).Select(item => (batch.RuleId, item.Path, item.Symbol)))
                .GroupBy(subject => subject.Path + "\0" + subject.Symbol, StringComparer.Ordinal)
                .Any(group => group.Select(subject => subject.RuleId).Distinct(StringComparer.Ordinal).Count() > 1);
            await Assert.That(projects).IsEqualTo(2);
            await Assert.That(sourceDocuments).IsEqualTo(2);
            await Assert.That(allSubjects.Any(item => item.Path.EndsWith("Other.cs", StringComparison.Ordinal))).IsTrue();
            await Assert.That(allSubjects.Any(item => item.Symbol.Contains("GeneratedApi", StringComparison.Ordinal))).IsFalse();
            await Assert.That(independentRubrics).IsTrue();
        }
        finally { RemoveFixture(repository); }
    }

    [Test]
    public async Task AuditDeterministicCoverageSeparatesFindingsApproximationAndDisabledZeroPopulation()
    {
        Rule configurable = new("sample.deterministic", 1, "finding", "finding", "fixture rule", true);
        AuditRuleFact withFindings = ReadOnlyAuditEngine.CreateRuleFact(configurable, enabled: true, supportedSourceDocuments: 3, projects: 2, deterministicFindings: 2);
        AuditRuleFact withoutFindings = ReadOnlyAuditEngine.CreateRuleFact(configurable, enabled: true, supportedSourceDocuments: 3, projects: 2, deterministicFindings: 0);
        AuditRuleFact zeroPopulation = ReadOnlyAuditEngine.CreateRuleFact(configurable, enabled: true, supportedSourceDocuments: 0, projects: 0, deterministicFindings: 0);
        AuditRuleFact disabled = ReadOnlyAuditEngine.CreateRuleFact(configurable, enabled: false, supportedSourceDocuments: 3, projects: 2, deterministicFindings: 0);
        AuditRuleFact mandatory = ReadOnlyAuditEngine.CreateRuleFact(new Rule("sample.mandatory", 1, "finding", "finding", "fixture mandatory rule", false), enabled: true, supportedSourceDocuments: 3, projects: 2, deterministicFindings: 1);
        AuditRuleFact projectScoped = ReadOnlyAuditEngine.CreateRuleFact(new Rule("profile.sample", 1, "finding", "finding", "fixture project rule", false), enabled: true, supportedSourceDocuments: 9, projects: 2, deterministicFindings: 0);

        await Assert.That(withFindings.EvaluationKind).IsEqualTo("deterministic-evaluation");
        await Assert.That(withFindings.ApproximateApplicableCount).IsEqualTo(3);
        await Assert.That(withFindings.MeasuredEvaluations).IsNull();
        await Assert.That(withFindings.Eligible).IsNull();
        await Assert.That(withFindings.DeterministicFindings).IsEqualTo(2);
        await Assert.That(withoutFindings.DeterministicFindings).IsEqualTo(0);
        await Assert.That(zeroPopulation.ApproximateApplicableCount).IsEqualTo(0);
        await Assert.That(zeroPopulation.DeterministicFindings).IsEqualTo(0);
        await Assert.That(disabled.Enabled).IsFalse();
        await Assert.That(disabled.Participation).IsEqualTo("disabled");
        await Assert.That(disabled.ApproximateApplicableCount).IsNull();
        await Assert.That(disabled.DeterministicFindings).IsNull();
        await Assert.That(mandatory.Enabled).IsTrue();
        await Assert.That(mandatory.Participation).IsEqualTo("mandatory");
        await Assert.That(projectScoped.CheckUnit).IsEqualTo("supported SDK projects");
        await Assert.That(projectScoped.ApproximateApplicableCount).IsEqualTo(2);
        await Assert.That(projectScoped.Participation).IsEqualTo("mandatory");
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

    private static async Task InitializeGitReviewRepository(string repository, string sourcePath, string source)
    {
        Directory.CreateDirectory(repository);
        await Git(repository, "init", "-q", "-b", "main");
        await Git(repository, "config", "user.email", "fixture@example.invalid");
        await Git(repository, "config", "user.name", "Gate Fixture");
        await File.WriteAllTextAsync(Path.Combine(repository, "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        await File.WriteAllTextAsync(sourcePath, source);
        _ = new ProfileManager(repository).Bootstrap();
        await Git(repository, "add", "-A");
        await Git(repository, "commit", "-m", "deletion attribution baseline");
    }

    private static void RemoveFixture(string repository)
    {
        if (!Directory.Exists(repository)) { return; }
        foreach (string file in Directory.EnumerateFiles(repository, "*", SearchOption.AllDirectories)) { File.SetAttributes(file, FileAttributes.Normal); }
        Directory.Delete(repository, true);
    }
}
