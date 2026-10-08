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
    public async Task ElapsedHazardCursorsAccrueEachEffectiveIntervalOnce()
    {
        var subjects = new SubjectHazardSampler(Seed, "clock.rule", 1, 1, "clock-epoch");
        subjects.AccrueElapsed("subject", 1_000, 2);
        await Assert.That(subjects.State("subject").Hazard).IsEqualTo(0d);
        subjects.AccrueElapsed("subject", 4_000, 2);
        await Assert.That(subjects.State("subject").Hazard).IsEqualTo(6d);
        subjects.AccrueElapsed("subject", 4_000, 2);
        await Assert.That(subjects.State("subject").Hazard).IsEqualTo(6d);
        subjects.AccrueElapsed("subject", 5_000, 3);
        await Assert.That(subjects.State("subject").Hazard).IsEqualTo(9d);
        await Assert.That(subjects.State("subject").LastEvaluationCursorUnixMilliseconds).IsEqualTo(5_000L);

        var populations = new PopulationHazardSampler(Seed, "clock.rule", 1, 1, "clock-epoch");
        populations.AccrueElapsed("scope", 1_000, 2);
        populations.AccrueElapsed("scope", 4_000, 2);
        populations.AccrueElapsed("scope", 4_000, 2);
        await Assert.That(populations.State("scope").ResidualHazard).IsEqualTo(6d);
        await Assert.That(populations.State("scope").LastEvaluationCursorUnixMilliseconds).IsEqualTo(4_000L);
        bool backwardsRejected = false;
        try { populations.AccrueElapsed("scope", 3_999, 2); } catch (ArgumentOutOfRangeException) { backwardsRejected = true; }
        await Assert.That(backwardsRejected).IsTrue();
    }

    [Test]
    public async Task AcceptedSubjectObservationStartsElapsedAgingAtObservationTime()
    {
        var sampler = new SubjectHazardSampler(Seed, "observation-clock.rule", 1, 1, "observation-clock-epoch");
        sampler.AddHazard("subject", 100d);
        sampler.AccrueElapsed("subject", 1_000, 1d);
        SubjectTicket ticket = sampler.Due("subject")!.Ticket;

        await Assert.That(sampler.Observe(ticket, at: DateTimeOffset.FromUnixTimeMilliseconds(11_000))).IsTrue();
        await Assert.That(sampler.State("subject").LastEvaluationCursorUnixMilliseconds).IsEqualTo(11_000L);
        await Assert.That(sampler.State("subject").Hazard).IsEqualTo(0d);

        sampler.AccrueElapsed("subject", 16_000, 1d);
        await Assert.That(sampler.State("subject").Hazard).IsEqualTo(5d);
    }

    [Test]
    public async Task EvaluationMetadataDistinguishesFirstUnchangedAndChangedSubjectAndAggregateEvaluations()
    {
        var subject = new SubjectHazardSampler(Seed, "metadata.subject", 1, 1);
        var first = subject.State("subject");
        await Assert.That(first.LastEvaluationFingerprint).IsNull();
        subject.AddHazard("subject", 1);
        subject.AccrueElapsed("subject", 1_000, 1d / (365 * 24 * 60 * 60));
        subject.SetEvaluationFingerprint("subject", new string('A', 64));
        double initialHazard = subject.State("subject").Hazard;
        await Assert.That(initialHazard).IsEqualTo(1d);
        subject.AccrueElapsed("subject", 1_000, 1d / (365 * 24 * 60 * 60));
        await Assert.That(subject.State("subject").Hazard).IsEqualTo(initialHazard);
        subject.AddHazard("subject", 1);
        subject.SetEvaluationFingerprint("subject", new string('B', 64));
        await Assert.That(subject.State("subject").Hazard).IsEqualTo(2d);

        var population = new PopulationHazardSampler(Seed, "metadata.population", 1, 1);
        population.AddHazard("file.cs", 1);
        population.AccrueElapsed("file.cs", 1_000, 0);
        population.SetEvaluationMetadata("file.cs", new string('A', 64), 4);
        await Assert.That(population.State("file.cs").LastCandidateCount).IsEqualTo(4);
        double beforeElapsed = population.State("file.cs").ResidualHazard;
        population.AccrueElapsed("file.cs", 31_536_001_000, 4d / (365 * 24 * 60 * 60));
        population.SetEvaluationMetadata("file.cs", new string('A', 64), 6);
        await Assert.That(population.State("file.cs").ResidualHazard - beforeElapsed).IsEqualTo(4d);
        await Assert.That(population.State("file.cs").LastCandidateCount).IsEqualTo(6);
        population.AccrueElapsed("file.cs", 31_536_001_000, 6d / (365 * 24 * 60 * 60));
        await Assert.That(population.State("file.cs").ResidualHazard - beforeElapsed).IsEqualTo(4d);
        population.AddHazard("file.cs", 1);
        population.SetEvaluationMetadata("file.cs", new string('B', 64), 3);
        await Assert.That(population.State("file.cs").ResidualHazard - beforeElapsed).IsEqualTo(5d);
    }

    [Test]
    public async Task ObservationTicketsCannotCrossSamplingEpochOrSeedReset()
    {
        var oldSeed = Seed;
        var oldSubjectSampler = new SubjectHazardSampler(oldSeed, "epoch.rule", 1, 1, "old-epoch");
        oldSubjectSampler.AddHazard("subject", 100);
        SubjectTicket oldSubjectTicket = oldSubjectSampler.Due("subject")!.Ticket;
        var newSeed = Seed;
        newSeed[0] ^= 0x80;
        var resetSubjectSampler = new SubjectHazardSampler(newSeed, "epoch.rule", 1, 1, "new-epoch");
        resetSubjectSampler.AddHazard("subject", 100);
        SubjectTicket resetSubjectTicket = resetSubjectSampler.Due("subject")!.Ticket;
        await Assert.That(resetSubjectTicket.Generation).IsEqualTo(oldSubjectTicket.Generation);
        await Assert.That(resetSubjectSampler.Observe(oldSubjectTicket)).IsFalse();
        var sameEpochSubjectSampler = new SubjectHazardSampler(oldSeed, "epoch.rule", 1, 1, "old-epoch");
        sameEpochSubjectSampler.AddHazard("subject", 100);
        await Assert.That(sameEpochSubjectSampler.Observe(oldSubjectTicket)).IsTrue();

        var oldPopulationSampler = new PopulationHazardSampler(oldSeed, "epoch.rule", 1, 1, "old-epoch");
        oldPopulationSampler.AddHazard("scope", 100);
        PopulationTicket oldPopulationTicket = oldPopulationSampler.DueEvents("scope")[0].Ticket;
        var resetPopulationSampler = new PopulationHazardSampler(newSeed, "epoch.rule", 1, 1, "new-epoch");
        resetPopulationSampler.AddHazard("scope", 100);
        PopulationTicket resetPopulationTicket = resetPopulationSampler.DueEvents("scope")[0].Ticket;
        await Assert.That(resetPopulationTicket.Generation).IsEqualTo(oldPopulationTicket.Generation);
        await Assert.That(resetPopulationSampler.Observe(oldPopulationTicket)).IsFalse();
        var sameEpochPopulationSampler = new PopulationHazardSampler(oldSeed, "epoch.rule", 1, 1, "old-epoch");
        sameEpochPopulationSampler.AddHazard("scope", 100);
        await Assert.That(sameEpochPopulationSampler.Observe(oldPopulationTicket)).IsTrue();
    }

    [Test]
    public async Task ObservationTicketsSurvivePersistenceAcrossSessionsAndAdvanceOnce()
    {
        string repo = Path.Combine(Path.GetTempPath(), "sampling-ticket-session-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        try
        {
            var sessionA = new SamplingSession(repo);
            var subjectsA = sessionA.SubjectSampler("ticket.rule", 1);
            subjectsA.AddHazard("subject", 100);
            SubjectTicket subjectTicket = subjectsA.Due("subject")!.Ticket;
            var populationsA = sessionA.PopulationSampler("ticket.rule", 1);
            populationsA.AddHazard("scope", 100);
            PopulationTicket populationTicket = populationsA.DueEvents("scope")[0].Ticket;
            sessionA.Commit();

            var sessionB = new SamplingSession(repo);
            var subjectsB = sessionB.SubjectSampler("ticket.rule", 1);
            var populationsB = sessionB.PopulationSampler("ticket.rule", 1);
            await Assert.That(subjectsB.Observe(subjectTicket)).IsTrue();
            await Assert.That(subjectsB.State("subject").Generation).IsEqualTo(subjectTicket.Generation + 1);
            await Assert.That(subjectsB.Observe(subjectTicket)).IsFalse();
            await Assert.That(populationsB.Observe(populationTicket)).IsTrue();
            await Assert.That(populationsB.State("scope").Generation).IsEqualTo(populationTicket.Generation + 1);
            await Assert.That(populationsB.Observe(populationTicket)).IsFalse();
            sessionB.Commit();

            var sessionC = new SamplingSession(repo);
            await Assert.That(sessionC.SubjectSampler("ticket.rule", 1).Observe(subjectTicket)).IsFalse();
            await Assert.That(sessionC.PopulationSampler("ticket.rule", 1).Observe(populationTicket)).IsFalse();

            await Assert.That(typeof(SubjectTicket).GetProperties().Any(p => p.PropertyType == typeof(object) || p.Name.Contains("Identity", StringComparison.Ordinal))).IsFalse();
            await Assert.That(typeof(PopulationTicket).GetProperties().Any(p => p.PropertyType == typeof(object) || p.Name.Contains("Identity", StringComparison.Ordinal))).IsFalse();
        }
        finally { if (Directory.Exists(repo)) { Directory.Delete(repo, true); } }
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
            var versionOne = new SamplingSession(repo).PopulationSampler("rule", 1);
            versionOne.AddHazard("version-ticket", 100);
            PopulationTicket oldVersionTicket = versionOne.DueEvents("version-ticket")[0].Ticket;
            var sameSession = new SamplingSession(repo);
            _ = sameSession.PopulationSampler("conflict.rule", 1);
            bool multiVersionRejected = false;
            try { _ = sameSession.PopulationSampler("conflict.rule", 2); } catch (ProductException) { multiVersionRejected = true; }
            await Assert.That(multiVersionRejected).IsTrue();
            sameSession.Commit();
            _ = new SamplingSession(repo).PopulationSampler("conflict.rule", 1);
            var subjectVersionOneSession = new SamplingSession(repo);
            var subjectVersionOne = subjectVersionOneSession.SubjectSampler("subject-version.rule", 1);
            subjectVersionOne.AddHazard("subject", 100);
            SubjectTicket oldSubjectVersionTicket = subjectVersionOne.Due("subject")!.Ticket;
            subjectVersionOneSession.Commit();
            var subjectInvalidated = new SamplingSession(repo);
            var subjectVersionTwo = subjectInvalidated.SubjectSampler("subject-version.rule", 2);
            await Assert.That(subjectVersionTwo.State("subject").Hazard).IsEqualTo(0d);
            await Assert.That(subjectVersionTwo.Observe(oldSubjectVersionTicket)).IsFalse();
            subjectInvalidated.Commit();
            var invalidated = new SamplingSession(repo);
            var versionTwo = invalidated.PopulationSampler("rule", 2);
            await Assert.That(versionTwo.State("scope").ResidualHazard).IsEqualTo(0d);
            await Assert.That(versionTwo.StateEpoch).IsNotEqualTo(oldVersionTicket.StateEpoch);
            versionTwo.AddHazard("version-ticket", 100);
            PopulationTicket newVersionTicket = versionTwo.DueEvents("version-ticket")[0].Ticket;
            await Assert.That(newVersionTicket.Generation).IsEqualTo(oldVersionTicket.Generation);
            await Assert.That(versionTwo.Observe(oldVersionTicket)).IsFalse();
            invalidated.Commit();
            await Assert.That(new SamplingSession(repo).PopulationSampler("rule", 2).State("scope").ResidualHazard).IsEqualTo(0d);
            var beforeDelete = new SamplingSession(repo).PopulationSampler("rule", 2);
            PopulationTicket beforeDeleteTicket = beforeDelete.DueEvents("version-ticket")[0].Ticket;
            string beforeDeleteEpoch = beforeDelete.StateEpoch;
            File.Delete(path);
            var afterDeleteSession = new SamplingSession(repo);
            var afterDelete = afterDeleteSession.PopulationSampler("rule", 2);
            await Assert.That(afterDelete.StateEpoch).IsNotEqualTo(beforeDeleteEpoch);
            await Assert.That(afterDelete.State("version-ticket").ResidualHazard).IsEqualTo(0d);
            afterDelete.AddHazard("version-ticket", 100);
            PopulationTicket afterDeleteTicket = afterDelete.DueEvents("version-ticket")[0].Ticket;
            await Assert.That(afterDeleteTicket.Generation).IsEqualTo(beforeDeleteTicket.Generation);
            await Assert.That(afterDelete.Observe(beforeDeleteTicket)).IsFalse();
            afterDelete.AccrueElapsed("scope", 10_000, 2);
            afterDeleteSession.Commit();
            var persistedCursor = new SamplingSession(repo).PopulationSampler("rule", 2);
            await Assert.That(persistedCursor.State("scope").LastEvaluationCursorUnixMilliseconds).IsEqualTo(10_000L);
            persistedCursor.AccrueElapsed("scope", 10_000, 2);
            await Assert.That(persistedCursor.State("scope").ResidualHazard).IsEqualTo(0d);
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
            var resetSampler = new SamplingSession(repo).PopulationSampler("rule", 1);
            var resetState = resetSampler.State("scope");
            await Assert.That(resetState.PassEvidence).IsEqualTo(0d);
            await Assert.That(resetState.FailEvidence).IsEqualTo(0d);
            await Assert.That(resetState.ResidualHazard).IsEqualTo(0d);
            await Assert.That(resetState.LastEvaluationCursorUnixMilliseconds).IsNull();
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
            context.CompleteSampling();
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
