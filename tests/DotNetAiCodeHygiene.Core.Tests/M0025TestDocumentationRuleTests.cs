using DotNetAiCodeHygiene.Core;
using Microsoft.CodeAnalysis;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class M0025TestDocumentationRuleTests
{
    [Test]
    public async Task TestAndDocumentationRulesExposeTheirMechanicalPopulations()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-m0025-testdocs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repository, "Production"));
        Directory.CreateDirectory(Path.Combine(repository, "Tests"));
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repository, "Production", "Production.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repository, "Tests", "Tests.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup><ItemGroup><ProjectReference Include=\"..\\Production\\Production.csproj\" /></ItemGroup></Project>");
            string production = ProductionSource;
            string tests = TestSource;
            string productionPath = Path.Combine(repository, "Production", "Product.cs");
            await File.WriteAllTextAsync(productionPath, production);
            await File.WriteAllTextAsync(Path.Combine(repository, "Tests", "ProductTests.cs"), tests);

            using var session = new RepositorySession(repository);
            Document[] documents = session.Projects.SelectMany(project => project.Documents).ToArray();
            string relativeProduction = "Production/Product.cs";
            int methodLine = Array.FindIndex(production.Split('\n'), line => line.Contains("ChangedBehavior", StringComparison.Ordinal)) + 1;
            int uncoveredLine = Array.FindIndex(production.Split('\n'), line => line.Contains("UncoveredBehavior", StringComparison.Ordinal)) + 1;
            var gate = new GateSelectionContext("m0025-fixture-gate", "changed", new HashSet<string>(StringComparer.Ordinal) { relativeProduction },
                new Dictionary<string, HashSet<int>>(StringComparer.Ordinal) { [relativeProduction] = new() { methodLine, uncoveredLine } });
            var context = new RuleContext(session, documents, gate);

            ReviewSubject[] assertions = new TestAssertionEffectivenessReviewRuleModule().Evaluate(context).ReviewSubjects.ToArray();
            ReviewSubject[] coverage = new TestChangeContractCoverageReviewRuleModule().Evaluate(context).ReviewSubjects.ToArray();
            ReviewSubject[] isolation = new TestIsolationCouplingReviewRuleModule().Evaluate(context).ReviewSubjects.ToArray();
            ReviewSubject[] quality = new DocumentationSummaryQualityReviewRuleModule().Evaluate(context).ReviewSubjects.ToArray();
            ReviewSubject[] german = new DocumentationSummaryGermanReviewRuleModule().Evaluate(context).ReviewSubjects.ToArray();
            ReviewSubject[] comments = new CommentIntentQualityReviewRuleModule().Evaluate(context).ReviewSubjects.ToArray();

            await Assert.That(assertions.Length).IsEqualTo(3);
            await Assert.That(assertions.Any(subject => subject.Item.Symbol.Contains("VerifiesChangedBehavior", StringComparison.Ordinal))).IsTrue();
            await Assert.That(assertions.Any(subject => subject.Item.Symbol.Contains("VerifiesOther", StringComparison.Ordinal))).IsTrue();
            await Assert.That(coverage.Length).IsEqualTo(2);
            await Assert.That(coverage.All(subject => subject.Item.Path == relativeProduction)).IsTrue();
            using (var relationshipSession = new RepositorySession(repository))
            {
                Document[] relationshipDocuments = relationshipSession.Projects.SelectMany(project => project.Documents).ToArray();
                int changedLine = Array.FindIndex(production.Split('\n'), line => line.Contains("ChangedBehavior", StringComparison.Ordinal)) + 1;
                int uncoveredChangedLine = Array.FindIndex(production.Split('\n'), line => line.Contains("UncoveredBehavior", StringComparison.Ordinal)) + 1;
                var relationshipGate = new GateSelectionContext("test-relationship-gate", "changed", new HashSet<string>([relativeProduction], StringComparer.Ordinal),
                    new Dictionary<string, HashSet<int>>(StringComparer.Ordinal) { [relativeProduction] = [changedLine, uncoveredChangedLine] });
                var relationshipContext = new RuleContext(relationshipSession, relationshipDocuments, relationshipGate);
                ReviewSubject[] contracts = new TestChangeContractCoverageReviewRuleModule().Evaluate(relationshipContext).ReviewSubjects.ToArray();
                await Assert.That(contracts.Any(subject => subject.Item.Symbol.Contains("ChangedBehavior", StringComparison.Ordinal))).IsTrue();
                await Assert.That(contracts.Any(subject => subject.Item.Symbol.Contains("UncoveredBehavior", StringComparison.Ordinal))).IsTrue();
                ReviewSubject productionCoverage = contracts.First(subject => subject.Item.Symbol.Contains("ChangedBehavior", StringComparison.Ordinal));
                ReviewSubject enrichedCoverage = productionCoverage.Enrich!(relationshipContext, productionCoverage);
                await Assert.That(enrichedCoverage.RelatedAnchors!.Any(anchor => anchor.Relation == "direct-exact-changed-symbol-reference" && anchor.Symbol.Contains("VerifiesChangedBehavior", StringComparison.Ordinal))).IsTrue();
                await Assert.That(enrichedCoverage.RelatedAnchors!.Any(anchor => anchor.Relation == "same-containing-type-other-member-reference-not-exact" && anchor.Symbol.Contains("VerifiesOther", StringComparison.Ordinal))).IsTrue();
                await Assert.That(enrichedCoverage.RelatedAnchors!.Any(anchor => anchor.Relation == "containing-type-reference-only-indirect" && anchor.Symbol.Contains("ReferencesTypeOnly", StringComparison.Ordinal))).IsTrue();
                ReviewSubject uncovered = contracts.First(subject => subject.Item.Symbol.Contains("UncoveredBehavior", StringComparison.Ordinal));
                ReviewSubject uncoveredEvidence = uncovered.Enrich!(relationshipContext, uncovered);
                await Assert.That(uncoveredEvidence.RelatedAnchors!.Any(anchor => anchor.Relation == "direct-exact-changed-symbol-reference")).IsFalse();
                await Assert.That(uncoveredEvidence.EvidenceLimitation).Contains("integration/contract coverage");
                var allContext = new RuleContext(relationshipSession, relationshipDocuments,
                    new GateSelectionContext("test-double-gate", "all", new HashSet<string>(StringComparer.Ordinal), new Dictionary<string, HashSet<int>>(StringComparer.Ordinal)));
                ReviewSubject testDouble = new TestIsolationCouplingReviewRuleModule().Evaluate(allContext).ReviewSubjects.Single();
                ReviewSubject enrichedDouble = testDouble.Enrich!(allContext, testDouble);
                await Assert.That(enrichedDouble.RelatedAnchors!.Any(anchor => anchor.Relation == "compiler-resolved-production-seam-for-test-double" && anchor.Symbol.Contains("IStore", StringComparison.Ordinal))).IsTrue();
            }
            await Assert.That(isolation.Length).IsEqualTo(1);
            await Assert.That(isolation.Single().Item.Summary).Contains("FakeStore");
            await Assert.That(quality.Length).IsEqualTo(2);
            await Assert.That(german.Select(subject => subject.Identity).ToArray()).IsEquivalentTo(quality.Select(subject => subject.Identity).ToArray());
            await Assert.That(quality.Any(subject => subject.Item.Symbol.Contains("Entry", StringComparison.Ordinal))).IsTrue();
            await Assert.That(comments.Length).IsEqualTo(2);
            await Assert.That(comments.All(subject => !subject.Content.Contains("disabled comment", StringComparison.Ordinal)
                && !subject.Content.Contains("summary docs", StringComparison.Ordinal)
                && !subject.Content.Contains("comment in text", StringComparison.Ordinal))).IsTrue();
            await Assert.That(new TestAssertionEffectivenessReviewRuleModule().Questions.Count).IsEqualTo(1);
            await Assert.That(new TestChangeContractCoverageReviewRuleModule().Questions.Count).IsEqualTo(1);
            await Assert.That(new TestIsolationCouplingReviewRuleModule().Questions.Count).IsEqualTo(1);
            await Assert.That(new DocumentationSummaryQualityReviewRuleModule().Questions.Count).IsEqualTo(1);
            await Assert.That(new DocumentationSummaryGermanReviewRuleModule().Questions.Count).IsEqualTo(1);
            await Assert.That(new CommentIntentQualityReviewRuleModule().Questions.Count).IsEqualTo(1);
        }
        finally
        {
            foreach (string file in Directory.EnumerateFiles(repository, "*", SearchOption.AllDirectories)) { File.SetAttributes(file, FileAttributes.Normal); }
            Directory.Delete(repository, recursive: true);
        }
    }

    private const string ProductionSource = """
        namespace Example;
        /// <summary>Loads the requested value from the current source of truth.</summary>
        public sealed class Product
        {
            public int ChangedBehavior() => 42;
            public int OtherBehavior() => 17;
            public int UncoveredBehavior() => 8;
        }
        /// <param name="name">A German fixture value.</param>
        public record Entry(string name);
        public interface IStore { string Read(); }
        """;

    private const string TestSource = """
        using Example;
        namespace Xunit { public class FactAttribute : System.Attribute { } }
        public static class Assert { public static void Equal(int expected, int actual) { } }
        public sealed class ProductTests
        {
            // Keep this fixture isolated from file I/O.
            [Xunit.Fact] public void VerifiesChangedBehavior() { Assert.Equal(42, new Product().ChangedBehavior()); } // protects the public outcome
            [Xunit.Fact] public void VerifiesOther() { Assert.Equal(17, new Product().OtherBehavior()); }
            [Xunit.Fact] public void ReferencesTypeOnly() { Product product = new(); Assert.Equal(1, product.GetHashCode()); }
            [Xunit.Fact] public void UsesFake() { FakeStore store = new(); _ = store; }
            public void TestNamedMethodWithoutAttribute() { }
        }
        public sealed class FakeStore : IStore { public string Read() => "fake"; }
        #if false
        // disabled comment is excluded
        #endif
        """;
}
