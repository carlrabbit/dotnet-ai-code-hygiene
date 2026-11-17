using System.Text;
using DotNetAiCodeHygiene.Core;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class M0025SharedSemanticBudgetTests
{
    [Test]
    public async Task SharedBudgetCountsEachRuleSubjectAndDoesNotRefillAfterPartialAcceptance()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-m0025-budget-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            (Rule Rule, IReadOnlyList<ReviewSubject> Population)[] populations = Enumerable.Range(1, 17).Select(ruleNumber =>
            {
                var rule = new Rule("semantic.rule." + ruleNumber.ToString("D2"), 1, "review-batch", "review-batch", "", true);
                ReviewSubject subject = new(rule.Id + "\0project\0Shared.cs\0Decision", "body-" + ruleNumber,
                    new ReviewItem("", "Shared.cs", 1, 1, "Decision", "", "Decision"), DirectChange: true,
                    PrimaryAnchor: new ReviewAnchor("Project.csproj", "Shared.cs", 1, 1, "Decision", "primary"));
                return (rule, (IReadOnlyList<ReviewSubject>)[subject]);
            }).ToArray();

            var store = new GateLocalReviewStore(repository);
            IReadOnlyDictionary<string, IReadOnlyList<ReviewSubject>> first = store.SelectShared("gate", populations,
                new HashSet<string>(["Shared.cs"], StringComparer.Ordinal), "changed");
            await Assert.That(first.Values.Sum(items => items.Count)).IsEqualTo(10);
            await Assert.That(first.Count(pair => pair.Value.Count == 1)).IsEqualTo(10);
            await Assert.That(first.Values.SelectMany(items => items).Select(item => item.Item.Path).Distinct().Count()).IsEqualTo(1);

            IReadOnlyDictionary<string, IReadOnlyList<ReviewSubject>> reordered = new GateLocalReviewStore(repository).SelectShared("gate", populations.Reverse().ToArray(),
                new HashSet<string>(["Shared.cs"], StringComparer.Ordinal), "changed");
            await Assert.That(SelectedIdentities(first)).IsEquivalentTo(SelectedIdentities(reordered));

            ReviewSubject accepted = first.Values.SelectMany(items => items).First();
            var acceptance = new GateLocalReviewStore(repository);
            await Assert.That(acceptance.AcceptTicket(accepted.GateTicket!)).IsTrue();
            acceptance.Commit();
            IReadOnlyDictionary<string, IReadOnlyList<ReviewSubject>> afterPartial = new GateLocalReviewStore(repository).SelectShared("gate", populations,
                new HashSet<string>(["Shared.cs"], StringComparer.Ordinal), "changed");
            await Assert.That(afterPartial.Values.Sum(items => items.Count)).IsEqualTo(9);
        }
        finally { Directory.Delete(repository, recursive: true); }
    }

    [Test]
    public async Task RelatedContextIsLimitedToTwoAndComesAfterChangedSubjects()
    {
        var directRule = new Rule("semantic.direct", 1, "review-batch", "review-batch", "", true);
        var contextRule = new Rule("semantic.context", 1, "review-batch", "review-batch", "", true);
        ReviewSubject[] direct = Enumerable.Range(1, 9).Select(i => new ReviewSubject("direct\0" + i, "direct-" + i,
            new ReviewItem("", "Changed.cs", i, 1, "Direct" + i, "", ""), DirectChange: true)).ToArray();
        ReviewSubject[] context = Enumerable.Range(1, 4).Select(i => new ReviewSubject("context\0" + i, "context-" + i,
            new ReviewItem("", "Unchanged.cs", i, 1, "Related" + i, "", ""),
            RelatedAnchors: [new ReviewAnchor("Project", "Changed.cs", i, 1, "ChangedOwner" + i, "changed related owner")])).ToArray();
        var populations = new (Rule, IReadOnlyList<ReviewSubject>)[] { (directRule, direct), (contextRule, context) };
        IReadOnlyDictionary<string, IReadOnlyList<ReviewSubject>> selected = new GateLocalReviewStore(Path.GetTempPath()).SelectShared("related-gate", populations,
            new HashSet<string>(["Changed.cs"], StringComparer.Ordinal), "base");
        await Assert.That(selected.Values.Sum(items => items.Count)).IsEqualTo(10);
        await Assert.That(selected[directRule.Id].Count).IsEqualTo(9);
        await Assert.That(selected[contextRule.Id].Count).IsEqualTo(1);
        await Assert.That(new GateLocalReviewStore(Path.GetTempPath()).SelectShared("related-gate", populations,
            new HashSet<string>(["Changed.cs"], StringComparer.Ordinal), "closed").Values.Sum(items => items.Count)).IsEqualTo(0);
    }

    [Test]
    public async Task SelectedEvidenceIsBoundedAndReportsMissingOrTruncatedAnchors()
    {
        string text = "public class Api { }\n" + new string('Ä', 30_000);
        var sources = new Dictionary<string, string>(StringComparer.Ordinal) { ["Api.cs"] = text };
        var primary = new ReviewAnchor("Api.csproj", "Api.cs", 2, 1, "Api", "primary");
        ReviewAnchor missing = new("Other.csproj", "Missing.cs", 40, 1, "Provider", "related provider");
        ReviewItem item = new("RI-1", "Api.cs", 2, 1, "Api", "", "public API declaration");
        (ReviewItem truncated, ReviewSource[] sourcesResult) = ReviewEvidenceBuilder.Build(item, primary, [missing], sources,
            "Does the contract hold?", "A legitimate adapter may be justified.", "A compiler-resolved candidate signal was observed.");
        await Assert.That(truncated.ContextStatus).IsEqualTo("partial");
        await Assert.That(truncated.Evidence!.Count).IsEqualTo(1);
        await Assert.That(truncated.MissingEvidence!.Any(reason => reason.Contains("truncated", StringComparison.Ordinal))).IsTrue();
        await Assert.That(truncated.MissingEvidence!.Any(reason => reason.Contains("source unavailable", StringComparison.Ordinal))).IsTrue();
        int utf8Bytes = Encoding.UTF8.GetByteCount(truncated.Question!) + Encoding.UTF8.GetByteCount(truncated.LegitimateCase!) +
            Encoding.UTF8.GetByteCount(truncated.ObservedSignal!) + Encoding.UTF8.GetByteCount(truncated.Declaration) +
            Encoding.UTF8.GetByteCount(truncated.Symbol) + Encoding.UTF8.GetByteCount(truncated.Path) +
            truncated.Evidence.Sum(excerpt => Encoding.UTF8.GetByteCount(excerpt.Project + excerpt.Path + excerpt.Symbol + excerpt.Text));
        await Assert.That(utf8Bytes).IsLessThanOrEqualTo(ReviewEvidenceBuilder.MaximumUtf8Bytes);
        await Assert.That(sourcesResult.Single().Content).IsNotEqualTo(text);
    }

    private static string[] SelectedIdentities(IReadOnlyDictionary<string, IReadOnlyList<ReviewSubject>> selection) =>
        selection.Values.SelectMany(items => items).Select(item => item.Identity).Order(StringComparer.Ordinal).ToArray();
}
