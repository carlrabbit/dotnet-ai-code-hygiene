using DotNetAiCodeHygiene.Core;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class SharedModernizationBudgetTests
{
    [Test]
    public async Task SharedBudgetIsFiveChangedFirstFairAcrossRulesAndInputOrderIndependent()
    {
        Rule[] rules = Enumerable.Range(0, 4)
            .Select(index => new Rule($"modernization.rule{index}", 1, "review", "review", "", true))
            .ToArray();
        (Rule Rule, IReadOnlyList<ReviewSubject> Population)[] populations = rules.Select((rule, ruleIndex) =>
        {
            ReviewSubject[] subjects = Enumerable.Range(0, 4).Select(subjectIndex => new ReviewSubject(
                $"rule{ruleIndex}-subject{subjectIndex}", $"content-{ruleIndex}-{subjectIndex}",
                new ReviewItem("", "Api.cs", subjectIndex + 1, 1, "Candidate", "Review", "Api"),
                DirectChange: subjectIndex == 0)).ToArray();
            return (rule, (IReadOnlyList<ReviewSubject>)subjects);
        }).ToArray();
        IReadOnlySet<string> changedPaths = new HashSet<string>(["Api.cs"], StringComparer.Ordinal);

        IReadOnlyList<(Rule Rule, ReviewSubject Subject)> selected = SharedModernizationBudget.Select(
            "gate-1", "changed", changedPaths, populations);
        IReadOnlyList<(Rule Rule, ReviewSubject Subject)> repeated = SharedModernizationBudget.Select(
            "gate-1", "changed", changedPaths, populations.Reverse());

        await Assert.That(selected.Count).IsEqualTo(SharedModernizationBudget.MaximumPackages);
        await Assert.That(selected.Count(candidate => candidate.Subject.DirectChange)).IsEqualTo(4);
        await Assert.That(selected.Take(4).All(candidate => candidate.Subject.DirectChange)).IsTrue();
        await Assert.That(selected.Skip(4).All(candidate => !candidate.Subject.DirectChange)).IsTrue();
        await Assert.That(selected.Select(candidate => candidate.Subject.Identity).ToArray())
            .IsEquivalentTo(repeated.Select(candidate => candidate.Subject.Identity).ToArray());
        await Assert.That(selected.Select(candidate => candidate.Rule.Id).Distinct().Count()).IsEqualTo(4);

        IReadOnlyList<(Rule Rule, ReviewSubject Subject)> contextFill = SharedModernizationBudget.Select(
            "gate-context", "changed", changedPaths,
            populations.Select(population => (population.Rule,
                (IReadOnlyList<ReviewSubject>)population.Population.Skip(1).ToArray())));
        await Assert.That(contextFill.Count).IsEqualTo(SharedModernizationBudget.MaximumPackages);
        await Assert.That(contextFill.All(candidate => !candidate.Subject.DirectChange)).IsTrue();
        await Assert.That(contextFill.Select(candidate => candidate.Rule.Id).Distinct().Count()).IsGreaterThan(1);
    }

    [Test]
    public async Task ClosedGateSelectsNothingAndBroadInspectionIncludesUnchangedSubjects()
    {
        Rule rule = new("modernization.single", 1, "review", "review", "", true);
        ReviewSubject[] subjects =
        [
            new("changed", "changed", new ReviewItem("", "Api.cs", 1, 1, "Changed", "Review", "Api"), DirectChange: true),
            new("unchanged", "unchanged", new ReviewItem("", "Api.cs", 2, 1, "Unchanged", "Review", "Api"))
        ];
        (Rule Rule, IReadOnlyList<ReviewSubject> Population)[] populations = [(rule, subjects)];

        await Assert.That(SharedModernizationBudget.Select("gate", "closed", new HashSet<string>(), populations).Count).IsEqualTo(0);
        IReadOnlyList<(Rule Rule, ReviewSubject Subject)> broad = SharedModernizationBudget.Select(
            "audit", "all", new HashSet<string>(), populations);
        await Assert.That(broad.Count).IsEqualTo(2);
        await Assert.That(broad.Select(candidate => candidate.Subject.Identity).ToArray())
            .IsEquivalentTo(subjects.Select(subject => subject.Identity).ToArray());
    }

    [Test]
    public async Task GateIdentityRotationEventuallySelectsEveryEligibleRule()
    {
        (Rule Rule, IReadOnlyList<ReviewSubject> Population)[] populations = Enumerable.Range(0, 8)
            .Select(index =>
            {
                Rule rule = new($"modernization.rotation{index}", 1, "review", "review", "", true);
                ReviewSubject subject = new($"subject-{index}", "content",
                    new ReviewItem("", "Api.cs", index + 1, 1, "Candidate", "Review", "Api"), DirectChange: true);
                return (rule, (IReadOnlyList<ReviewSubject>)[subject]);
            }).ToArray();
        IReadOnlySet<string> changedPaths = new HashSet<string>(["Api.cs"], StringComparer.Ordinal);
        string[] firstRules = Enumerable.Range(0, 32)
            .Select(index => SharedModernizationBudget.Select($"gate-{index}", "changed", changedPaths, populations)[0].Rule.Id)
            .ToArray();

        await Assert.That(firstRules.Distinct().Count()).IsEqualTo(populations.Length);
    }
}
