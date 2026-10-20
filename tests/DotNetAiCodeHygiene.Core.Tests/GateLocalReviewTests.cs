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
            ReviewSubject[] fiveChanged = new GateLocalReviewStore(repository).Select("five-changed", rule,
                subjects.Take(5).Select(subject => subject with { DirectChange = true }).ToArray(), touched, "changed").ToArray();
            await Assert.That(fiveChanged.Count).IsEqualTo(5);
            await Assert.That(fiveChanged.All(subject => subject.DirectChange)).IsTrue();
            ReviewSubject[] sparse = new GateLocalReviewStore(repository).Select("sparse", rule,
                [subjects[0], subjects[1] with { DirectChange = true }, .. subjects.Skip(2).Select(subject => subject with { Item = subject.Item with { Path = "Other.cs" }, DirectChange = true })],
                touched, "changed").ToArray();
            await Assert.That(sparse.Select(subject => subject.Identity).ToArray()).IsEquivalentTo(new[] { subjects[0].Identity, subjects[1].Identity });
            ReviewSubject accepted = first[0];
            await Assert.That(store.Accept("ignored", rule, accepted)).IsFalse();
            await Assert.That(store.AcceptTicket(accepted.GateTicket!)).IsTrue();
            store.Commit();

            ReviewSubject[] afterAcceptance = new GateLocalReviewStore(repository).Select("gate", rule, subjects, touched, "changed").ToArray();
            await Assert.That(afterAcceptance.Select(x => x.Identity).ToArray()).IsEquivalentTo(first.Skip(1).Select(x => x.Identity).ToArray());
            await Assert.That(afterAcceptance.Count).IsEqualTo(4);
            ReviewSubject changedFingerprint = accepted with { Content = "changed-content" };
            ReviewSubject[] newFingerprint = new GateLocalReviewStore(repository).Select("new-gate", rule,
                [changedFingerprint], touched, "all").ToArray();
            await Assert.That(newFingerprint.Select(subject => subject.Identity).ToArray()).IsEquivalentTo(new[] { accepted.Identity });
            await Assert.That(newFingerprint.Single().GateTicket!.Fingerprint).IsNotEqualTo(accepted.GateTicket!.Fingerprint);
            Rule newVersion = rule with { Version = rule.Version + 1 };
            ReviewSubject[] versioned = new GateLocalReviewStore(repository).Select("gate", newVersion, subjects, touched, "changed").ToArray();
            await Assert.That(versioned[0].GateTicket!.TicketId).IsNotEqualTo(first[0].GateTicket!.TicketId);
        }
        finally { RemoveFixture(repository); }
    }

    [Test]
    public async Task EarlierAcceptedSubjectsDoNotSpendNewGateCapacityButSameGateDoesNotRefill()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-gate-capacity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            ReviewSubject[] original = Enumerable.Range(0, 5).Select(i => new ReviewSubject(
                "project\0Api.cs\0T:Api" + i, "content-" + i,
                new ReviewItem("", "Api.cs", i + 1, 1, "Api" + i, "Summary", "Api"))).ToArray();
            Rule rule = new(SummaryQualityReviewRuleModule.RuleId, 6, "review-batch", "review-batch", "", true);
            IReadOnlySet<string> touched = new HashSet<string>(StringComparer.Ordinal);
            ReviewSubject[] firstGate = new GateLocalReviewStore(repository).Select("gate-a", rule, original, touched, "all").ToArray();
            await Assert.That(firstGate.Count).IsEqualTo(5);

            var accept = new GateLocalReviewStore(repository);
            foreach (ReviewSubject subject in firstGate.Take(3)) { await Assert.That(accept.AcceptTicket(subject.GateTicket!)).IsTrue(); }
            accept.Commit();

            ReviewSubject[] repeatedGate = new GateLocalReviewStore(repository).Select("gate-a", rule, original, touched, "all").ToArray();
            await Assert.That(repeatedGate.Select(subject => subject.Identity).ToArray())
                .IsEquivalentTo(firstGate.Skip(3).Select(subject => subject.Identity).ToArray());
            await Assert.That(repeatedGate.Count).IsEqualTo(2);

            ReviewSubject newlyEligible = new("project\0Api.cs\0T:New", "new content",
                new ReviewItem("", "Api.cs", 6, 1, "New", "Summary", "Api"));
            ReviewSubject[] newGate = new GateLocalReviewStore(repository).Select("gate-b", rule,
                firstGate.Select(subject => original.Single(item => item.Identity == subject.Identity)).Append(newlyEligible).ToArray(), touched, "all").ToArray();
            await Assert.That(newGate.Count).IsEqualTo(3);
            await Assert.That(newGate.Any(subject => subject.Identity == newlyEligible.Identity)).IsTrue();
            await Assert.That(newGate.Count).IsLessThanOrEqualTo(5);
        }
        finally { RemoveFixture(repository); }
    }

    [Test]
    public async Task DeletionOnlyChangesMarkSurvivingClassesDirectForChangedAndBaseGates()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-gate-delete-direct-" + Guid.NewGuid().ToString("N"));
        const string source = "public class Alpha\n{\n    public void Removed() { }\n}\npublic class Beta\n{\n    public void Removed() { }\n}\npublic class Gamma\n{\n    public void Removed() { }\n}\n";
        try
        {
            string sourcePath = Path.Combine(repository, "Api.cs");
            await InitializeGitReviewRepository(repository, sourcePath, source);
            string changed = source.Replace("    public void Removed() { }\n", "", StringComparison.Ordinal);
            await File.WriteAllTextAsync(sourcePath, changed);

            ReviewBatch changedBatch = new HygieneEngine(repository).Check([], true).ReviewBatches
                .Single(batch => batch.RuleId == BoringnessReviewRuleModule.RuleId);
            await Assert.That(changedBatch.SampleCount).IsEqualTo(3);
            await Assert.That(changedBatch.Items.Select(item => item.Symbol).ToArray())
                .IsEquivalentTo(new[] { "global::Alpha", "global::Beta", "global::Gamma" });

            await Git(repository, "add", "Api.cs");
            ReviewBatch stagedBatch = new HygieneEngine(repository).Check([], true).ReviewBatches
                .Single(batch => batch.RuleId == BoringnessReviewRuleModule.RuleId);
            await Assert.That(stagedBatch.SampleCount).IsEqualTo(3);

            ReviewBatch baseBatch = new HygieneEngine(repository).Check([], false, baseRef: "main").ReviewBatches
                .Single(batch => batch.RuleId == BoringnessReviewRuleModule.RuleId);
            await Assert.That(baseBatch.SampleCount).IsEqualTo(3);
            await Assert.That(baseBatch.Items.Select(item => item.Symbol).ToArray())
                .IsEquivalentTo(new[] { "global::Alpha", "global::Beta", "global::Gamma" });
        }
        finally { RemoveFixture(repository); }
    }

    [Test]
    public async Task TopLevelAndEndOfFileDeletionsDoNotMarkNeighboringClassesDirect()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-gate-delete-boundary-" + Guid.NewGuid().ToString("N"));
        const string middleType = "public class RemovedMiddle\n{\n    public void Gone() { }\n}\n";
        const string finalType = "public class RemovedAtEnd\n{\n    public void Gone() { }\n}\n";
        const string source = "public class First\n{\n    public void Keep() { }\n}\n" + middleType +
            "public class Last\n{\n    public void Keep() { }\n}\n" + finalType;
        try
        {
            string sourcePath = Path.Combine(repository, "Api.cs");
            await InitializeGitReviewRepository(repository, sourcePath, source);
            await File.WriteAllTextAsync(sourcePath, source.Replace(middleType, "", StringComparison.Ordinal)
                .Replace(finalType, "", StringComparison.Ordinal));

            ReviewBatch batch = new HygieneEngine(repository).Check([], true).ReviewBatches
                .Single(item => item.RuleId == BoringnessReviewRuleModule.RuleId);
            await Assert.That(batch.PopulationCount).IsEqualTo(2);
            await Assert.That(batch.SampleCount).IsEqualTo(1);
            await Assert.That(new[] { "global::First", "global::Last" }.Contains(batch.Items.Single().Symbol)).IsTrue();
        }
        finally { RemoveFixture(repository); }
    }

    [Test]
    public async Task AddedAndReplacedLinesRemainDirectWithOneContextCandidate()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-gate-add-replace-" + Guid.NewGuid().ToString("N"));
        const string source = "public class First\n{\n    public int Value() => 1;\n}\npublic class Second\n{\n    public int Value() => 2;\n}\npublic class Third\n{\n    public int Value() => 3;\n}\n";
        try
        {
            string sourcePath = Path.Combine(repository, "Api.cs");
            await InitializeGitReviewRepository(repository, sourcePath, source);
            string changed = source.Replace("Value() => 1", "Value() => 10", StringComparison.Ordinal)
                + "public class Added\n{\n    public int Value() => 4;\n}\n";
            await File.WriteAllTextAsync(sourcePath, changed);

            ReviewBatch batch = new HygieneEngine(repository).Check([], true).ReviewBatches
                .Single(item => item.RuleId == BoringnessReviewRuleModule.RuleId);
            await Assert.That(batch.SampleCount).IsEqualTo(3);
            await Assert.That(batch.Items.Any(item => item.Symbol == "global::First")).IsTrue();
            await Assert.That(batch.Items.Any(item => item.Symbol == "global::Added")).IsTrue();
            await Assert.That(batch.Items.Count(item => item.Symbol is "global::Second" or "global::Third")).IsEqualTo(1);
        }
        finally { RemoveFixture(repository); }
    }

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
            ReviewBatch batch = checkedRun.ReviewBatches.Single(item => item.RuleId == SummaryQualityReviewRuleModule.RuleId);
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
    public async Task AcceptedBoringnessSubjectSurvivesUnrelatedPrecedingDeclarationInsertion()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-boringness-stable-id-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repository, "src"));
        try
        {
            await Git(repository, "init", "-q", "-b", "main");
            await File.WriteAllTextAsync(Path.Combine(repository, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string sourcePath = Path.Combine(repository, "src", "Types.cs");
            const string acceptedDeclaration = "public class Accepted { public void Run() { } }";
            await File.WriteAllTextAsync(sourcePath, acceptedDeclaration);
            _ = new ProfileManager(repository).Bootstrap();
            var engine = new HygieneEngine(repository);
            CheckResult first = engine.Check([], false, reviewAll: true);
            ReviewBatch boringness = first.ReviewBatches.Single(item => item.RuleId == BoringnessReviewRuleModule.RuleId);
            ReviewItem acceptedItem = boringness.Items.Single();
            ReviewAcceptance accepted = engine.AcceptReview(boringness.Handle, [acceptedItem.Id]);
            await Assert.That(accepted.AcceptedCount).IsEqualTo(1);

            await File.WriteAllTextAsync(sourcePath, "public class Inserted { }\n" + acceptedDeclaration);
            CheckResult afterInsertion = new HygieneEngine(repository).Check([], false, reviewAll: true);
            ReviewBatch updated = afterInsertion.ReviewBatches.Single(item => item.RuleId == BoringnessReviewRuleModule.RuleId);
            await Assert.That(updated.PopulationCount).IsEqualTo(2);
            await Assert.That(updated.SampleCount).IsEqualTo(1);
            await Assert.That(updated.Items.Single().Symbol).Contains("Inserted");
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

            string changed = source.Replace("Value => 0", "Value => 42", StringComparison.Ordinal);
            await File.WriteAllTextAsync(sourcePath, changed);
            await Git(repository, "add", "Api.cs");
            await Git(repository, "commit", "-m", "feature source change");
            var engine = new HygieneEngine(repository);
            CheckResult bare = engine.Check([], false);
            await Assert.That(bare.ReviewBatches.All(x => x.SampleCount == 0)).IsTrue();
            ReviewBatch fullReview = engine.Check([], false, reviewAll: true).ReviewBatches.Single(x => x.RuleId == SummaryQualityReviewRuleModule.RuleId);
            await Assert.That(fullReview.PopulationItems!.Any(item => item.Path == "Other.cs")).IsTrue();
            await Assert.That(fullReview.SampleCount).IsLessThanOrEqualTo(5);
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

    [Test]
    public async Task ExhaustiveAuditBypassesNormalSampleAndAcceptedObservationWithoutPublishingNormalState()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-audit-population-" + Guid.NewGuid().ToString("N"));
        string sourcePath = Path.Combine(repository, "Api.cs");
        string source = string.Join("\n", Enumerable.Range(0, 8).Select(i => $"/// <summary>Provides a stable caller-facing purpose for API {i}.</summary>\npublic sealed class Api{i} {{ // rationale for API {i}\n    public int Value => {i};\n}}"));
        await InitializeGitReviewRepository(repository, sourcePath, source);
        try
        {
            HygieneEngine engine = new(repository);
            ReviewBatch normal = engine.Check([], false, reviewAll: true).ReviewBatches.Single(batch => batch.RuleId == SummaryQualityReviewRuleModule.RuleId);
            await Assert.That(normal.PopulationCount).IsEqualTo(8);
            await Assert.That(normal.SampleCount).IsLessThanOrEqualTo(5);
            _ = engine.AcceptReview(normal.Handle, [normal.Items[0].Id]);
            string latestPath = Path.Combine(repository, ".hygiene", ".state", "latest-run.json");
            string gatePath = Path.Combine(repository, ".hygiene", ".state", "gate-reviews.json");
            byte[] latestBefore = await File.ReadAllBytesAsync(latestPath);
            byte[] gateBefore = await File.ReadAllBytesAsync(gatePath);
            byte[] sourceBefore = await File.ReadAllBytesAsync(sourcePath);

            (CheckResult audit, _, _) = engine.CheckExhaustiveAuditSnapshot();
            ReviewBatch auditQuality = audit.ReviewBatches.Single(batch => batch.RuleId == SummaryQualityReviewRuleModule.RuleId);
            await Assert.That(auditQuality.PopulationCount).IsEqualTo(8);
            await Assert.That(auditQuality.SampleCount).IsEqualTo(8);
            await Assert.That(auditQuality.Items.Count(item => item.Id == normal.Items[0].Id)).IsEqualTo(1);
            await Assert.That(audit.ReviewBatches.Single(batch => batch.RuleId == TestQualityReviewRuleModule.RuleId).PopulationCount).IsEqualTo(0);
            await Assert.That(audit.ReviewBatches.Count).IsEqualTo(6);
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
        Rule rule = new(SummaryQualityReviewRuleModule.RuleId, 6, "review-batch", "review-batch", "", true);
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
