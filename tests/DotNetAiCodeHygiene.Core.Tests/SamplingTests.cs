using DotNetAiCodeHygiene.Core.Sampling;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class SamplingTests
{
    private static byte[] Seed => Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

    [Test]
    public async Task DeterministicThresholdsAndExponentialDistributionArePinned()
    {
        double first = SamplingRandom.Uniform(Seed, ["test"], 7);
        await Assert.That(first).IsEqualTo(0.8317263474210779d);
        await Assert.That(SamplingRandom.Threshold(Seed, ["test"], 7)).IsEqualTo(0.18425180161313817d);
        await Assert.That(first).IsGreaterThan(0d);
        await Assert.That(first).IsLessThan(1d);
        await Assert.That(SamplingRandom.UniformFromBits(0)).IsGreaterThan(0d);
        await Assert.That(SamplingRandom.UniformFromBits((1UL << 53) - 1)).IsEqualTo(Math.BitDecrement(1d));
        await Assert.That(SamplingRandom.Uniform(Seed, ["test"], 7)).IsEqualTo(first);
        await Assert.That(SamplingRandom.Threshold(Seed, ["test"], 7)).IsEqualTo(-Math.Log(first));

        const int count = 100_000;
        foreach (double hazard in new[] { 0.1, 0.5, 1d, 2d })
        {
            int observed = Enumerable.Range(0, count).Count(i => SamplingRandom.Threshold(Seed, ["distribution", i.ToString()], 0) <= hazard);
            double actual = (double)observed / count;
            double expected = 1 - Math.Exp(-hazard);
            await Assert.That(Math.Abs(actual - expected)).IsLessThan(0.006d);
        }
    }

    [Test]
    public async Task AggregateEventCountsTrackHazardMassRatioAtLargeN()
    {
        const int count = 50_000;
        var sampler = new PopulationHazardSampler(Seed, "ratio.rule", 1, 1);
        int low = 0;
        int high = 0;
        for (int i = 0; i < count; i++)
        {
            string unit = i.ToString();
            sampler.AddHazard(unit, 0.01);
            low += sampler.DueEvents(unit).Count;
            string doubleUnit = "double-" + unit;
            sampler.AddHazard(doubleUnit, 0.02);
            high += sampler.DueEvents(doubleUnit).Count;
        }
        double ratio = (double)high / low;
        await Assert.That(Math.Abs(ratio - 2d)).IsLessThan(0.18d);
    }

    [Test]
    public async Task SubjectTicketsRemainDueUntilObservedAndSelectionUsesDebt()
    {
        var sampler = new SubjectHazardSampler(Seed, "test.rule", 1, 1);
        double aThreshold = SamplingRandom.Threshold(Seed, ["test.rule", "1", "1", "subject", "a"], 0);
        double bThreshold = SamplingRandom.Threshold(Seed, ["test.rule", "1", "1", "subject", "b"], 0);
        sampler.AddHazard("a", aThreshold + 0.1);
        sampler.AddHazard("b", bThreshold + 0.5);
        sampler.AddHazard("c", 0.1);
        DueSubject due = sampler.Due("a") ?? throw new InvalidOperationException("Expected subject a to be due.");
        DueSubject repeated = sampler.Due("a") ?? throw new InvalidOperationException("Expected the subject to remain due.");
        await Assert.That(repeated.Ticket.Generation).IsEqualTo(due.Ticket.Generation);
        var chosen = sampler.SelectDue(["a", "b", "c"], 1);
        await Assert.That(chosen.Count).IsEqualTo(1);
        await Assert.That(chosen[0].Ticket.SubjectId).IsEqualTo("b");
        await Assert.That(sampler.Observe(due.Ticket)).IsTrue();
        await Assert.That(sampler.Observe(due.Ticket)).IsFalse();
        await Assert.That(sampler.State("a").Generation).IsEqualTo(1);
        await Assert.That(sampler.State("b").Hazard).IsEqualTo(bThreshold + 0.5d);
        await Assert.That(sampler.Due("b") is not null).IsTrue();
    }

    [Test]
    public async Task AggregateEventsArePureUntilSequentialObservationsAndSubjectsAreTransient()
    {
        var sampler = new PopulationHazardSampler(Seed, "test.rule", 1, 1);
        sampler.AddHazard("scope/cohort", 12);
        var events = sampler.DueEvents("scope/cohort");
        await Assert.That(events.Count).IsGreaterThan(1);
        await Assert.That(sampler.State("scope/cohort").Generation).IsEqualTo(0);
        var selections = sampler.SelectSubjects("scope/cohort", ["x", "y", "z"], events.Count);
        var replay = sampler.SelectSubjects("scope/cohort", ["x", "y", "z"], events.Count);
        await Assert.That(selections.Select(x => x.Candidate).Distinct().Count()).IsEqualTo(Math.Min(3, selections.Count));
        await Assert.That(string.Join("|", replay.Select(x => x.Candidate))).IsEqualTo(string.Join("|", selections.Select(x => x.Candidate)));
        var repeatable = sampler.SelectSubjects("scope/cohort", ["only-candidate"], events.Count);
        await Assert.That(repeatable.All(x => x.Candidate == "only-candidate")).IsTrue();
        foreach (var item in events)
        {
            await Assert.That(sampler.Observe(item.Ticket, passed: true, retention: 0.5)).IsTrue();
        }
        await Assert.That(sampler.Observe(events[0].Ticket)).IsFalse();
        await Assert.That(sampler.State("scope/cohort").PassEvidence).IsGreaterThan(0d);
        await Assert.That(sampler.States.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DiscountedEvidenceAndBetaMeanAreOrdinaryArithmetic()
    {
        var evidence = new DiscountedEvidence(2, 3).Observe(true, 0.5);
        await Assert.That(evidence.Pass).IsEqualTo(2d);
        await Assert.That(evidence.Fail).IsEqualTo(1.5d);
        var reset = evidence.Observe(false, 0);
        await Assert.That(reset.Pass).IsEqualTo(0d);
        await Assert.That(reset.Fail).IsEqualTo(1d);
        await Assert.That(new DiscountedEvidence(1, 1).PosteriorMean(1, 1)).IsEqualTo(0.5d);
        var retained = new DiscountedEvidence(2, 3).Observe(true, 1);
        await Assert.That(retained.Pass).IsEqualTo(3d);
        await Assert.That(retained.Fail).IsEqualTo(3d);
        bool negativeRejected = false;
        try { _ = new DiscountedEvidence(-1, 0); } catch (ArgumentOutOfRangeException) { negativeRejected = true; }
        await Assert.That(negativeRejected).IsTrue();
    }

    [Test]
    public async Task SamplingStateIsLazyVersionedAtomicAndCorruptionIsNotReplaced()
    {
        string repo = Path.Combine(Path.GetTempPath(), "sampling-state-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        try
        {
            using (var repository = new RepositorySession(repo))
            {
                var context = new RuleContext(repository, []);
                await Assert.That(context.HasSampling).IsFalse();
                await Assert.That(repository.HasWorkspace).IsFalse();
                await Assert.That(File.Exists(Path.Combine(repo, ".hygiene", ".state", "sampling.json"))).IsFalse();
            }
            var session = new SamplingSession(repo);
            string path = Path.Combine(repo, ".hygiene", ".state", "sampling.json");
            await Assert.That(File.Exists(path)).IsFalse();
            session.PopulationSampler("rule", 1).AddHazard("scope", 2);
            session.Commit();
            string original = await File.ReadAllTextAsync(path);
            await Assert.That(original.Contains("scope", StringComparison.Ordinal)).IsTrue();
            var loaded = new SamplingSession(repo);
            await Assert.That(loaded.PopulationSampler("rule", 1).State("scope").ResidualHazard).IsEqualTo(2d);
            var partial = new SamplingSession(repo);
            partial.PopulationSampler("rule", 1).AddHazard("scope-two", 1);
            partial.Commit();
            var reread = new SamplingSession(repo).PopulationSampler("rule", 1);
            await Assert.That(reread.State("scope").ResidualHazard).IsEqualTo(2d);
            await Assert.That(reread.State("scope-two").ResidualHazard).IsEqualTo(1d);
            var invalidated = new SamplingSession(repo);
            var versionTwo = invalidated.PopulationSampler("rule", 2);
            await Assert.That(versionTwo.State("scope").ResidualHazard).IsEqualTo(0d);
            invalidated.Commit();
            await Assert.That(new SamplingSession(repo).PopulationSampler("rule", 2).State("scope").ResidualHazard).IsEqualTo(0d);
            string rollbackBaseline = await File.ReadAllTextAsync(path);
            var failing = new SamplingSession(repo, () => throw new IOException("injected"));
            failing.PopulationSampler("rule", 1).AddHazard("scope", 1);
            try { failing.Commit(); } catch (IOException) { }
            await Assert.That(await File.ReadAllTextAsync(path)).IsEqualTo(rollbackBaseline);
            await File.WriteAllTextAsync(path, "{");
            bool rejected = false;
            try { _ = new SamplingSession(repo); } catch (ProductException) { rejected = true; }
            await Assert.That(rejected).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(path)).IsEqualTo("{");
            File.Delete(path);
            var resetState = new SamplingSession(repo).PopulationSampler("rule", 1).State("scope");
            await Assert.That(resetState.PassEvidence).IsEqualTo(0d);
            await Assert.That(resetState.FailEvidence).IsEqualTo(0d);
        }
        finally { if (Directory.Exists(repo)) { Directory.Delete(repo, true); } }
    }

    [Test]
    public async Task IsolatedSdkGitFixtureLoadsSamplingOnlyWhenRequestedAndKeepsOtherPopulationUnits()
    {
        string repo = Path.Combine(Path.GetTempPath(), "sampling-git-fixture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        try
        {
            Directory.CreateDirectory(Path.Combine(repo, ".git"));
            await File.WriteAllTextAsync(Path.Combine(repo, "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string source = Path.Combine(repo, "Fixture.cs");
            await File.WriteAllTextAsync(source, "public sealed class Fixture { }");
            using var repository = new RepositorySession(repo);
            string[] targets = repository.ResolveTargets([source], false);
            var documents = repository.AssignTargets(targets, [source]).Values.Select(x => x.Document).ToArray();
            var context = new RuleContext(repository, documents);
            string statePath = Path.Combine(repo, ".hygiene", ".state", "sampling.json");
            await Assert.That(context.HasSampling).IsFalse();
            await Assert.That(File.Exists(statePath)).IsFalse();
            var sampler = context.Sampling.PopulationSampler("fixture.rule", 1);
            sampler.AddHazard("scope-one/cohort", 1);
            context.CommitSampling();
            var partial = new SamplingSession(repo);
            partial.PopulationSampler("fixture.rule", 1).AddHazard("scope-two/cohort", 2);
            partial.Commit();
            var persisted = new SamplingSession(repo).PopulationSampler("fixture.rule", 1);
            await Assert.That(persisted.State("scope-one/cohort").ResidualHazard).IsEqualTo(1d);
            await Assert.That(persisted.State("scope-two/cohort").ResidualHazard).IsEqualTo(2d);
            var staged = new SamplingSession(repo);
            staged.PopulationSampler("fixture.rule", 1).AddHazard("staged-only", 4);
            await Assert.That(File.ReadAllText(statePath).Contains("staged-only", StringComparison.Ordinal)).IsFalse();
        }
        finally { if (Directory.Exists(repo)) { Directory.Delete(repo, true); } }
    }
}
