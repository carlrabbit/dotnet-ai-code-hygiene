using DotNetAiCodeHygiene.Core;
using Microsoft.CodeAnalysis;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class M0025RelationshipAcceptanceTests
{
    [Test]
    public async Task AcceptedProductionTestRelationshipInvalidatesWhenLateAssertionChanges()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-m0025-real-test-fingerprint-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        Directory.CreateDirectory(Path.Combine(repository, "Production"));
        Directory.CreateDirectory(Path.Combine(repository, "Tests"));
        try
        {
            string production = "namespace Fixture; public sealed class Product { public int ChangedBehavior() => 42; }\n";
            string testSource = ContractTestSource("Assert.Equal(42, result); // MATERIAL_ASSERTION");
            await File.WriteAllTextAsync(Path.Combine(repository, "Production", "Production.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repository, "Production", "Product.cs"), production);
            await File.WriteAllTextAsync(Path.Combine(repository, "Tests", "Tests.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup><ItemGroup><ProjectReference Include=\"..\\Production\\Production.csproj\" /></ItemGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repository, "Tests", "ContractTests.cs"), testSource);

            ReviewSubject[] first = SelectCoverage(repository).Where(subject => subject.Item.Path == "Production/Product.cs" && subject.Item.Symbol.Contains("ChangedBehavior", StringComparison.Ordinal) &&
                subject.RelatedAnchors!.Any(anchor => anchor.Relation == "direct-exact-changed-symbol-reference")).ToArray();
            await Assert.That(first.Length).IsEqualTo(1);
            await Assert.That(first[0].RelatedAnchors!.Any(anchor => anchor.Relation == "direct-exact-changed-symbol-reference")).IsTrue();
            var initialStore = new GateLocalReviewStore(repository);
            await Assert.That(initialStore.AcceptTicket(first[0].GateTicket!)).IsTrue();
            initialStore.Commit();

            await File.WriteAllTextAsync(Path.Combine(repository, "Tests", "ContractTests.cs"), testSource + "public sealed class Unrelated { public void Change() { int value = 3; } }\n");
            await Assert.That(SelectCoverage(repository).Count(subject => subject.Item.Path == "Production/Product.cs" && subject.Item.Symbol.Contains("ChangedBehavior", StringComparison.Ordinal) &&
                subject.RelatedAnchors!.Any(anchor => anchor.Relation == "direct-exact-changed-symbol-reference"))).IsEqualTo(0);

            await File.WriteAllTextAsync(Path.Combine(repository, "Tests", "ContractTests.cs"), ContractTestSource("Assert.Equal(41, result); // MATERIAL_ASSERTION"));
            ReviewSubject[] afterMaterialEdit = SelectCoverage(repository).Where(subject => subject.Item.Path == "Production/Product.cs" && subject.Item.Symbol.Contains("ChangedBehavior", StringComparison.Ordinal) &&
                subject.RelatedAnchors!.Any(anchor => anchor.Relation == "direct-exact-changed-symbol-reference")).ToArray();
            await Assert.That(afterMaterialEdit.Length).IsEqualTo(1);
            await Assert.That(afterMaterialEdit[0].RelatedEvidenceFingerprint).IsNotEqualTo(first[0].RelatedEvidenceFingerprint);
            var secondStore = new GateLocalReviewStore(repository);
            await Assert.That(secondStore.AcceptTicket(afterMaterialEdit[0].GateTicket!)).IsTrue();
            secondStore.Commit();
            await File.WriteAllTextAsync(Path.Combine(repository, "Production", "Product.cs"), "namespace Fixture; public sealed class Product { public int ChangedBehavior() => 43; }\n");
            ReviewSubject[] afterPrimaryEdit = SelectCoverage(repository).Where(subject => subject.Item.Path == "Production/Product.cs" && subject.Item.Symbol.Contains("ChangedBehavior", StringComparison.Ordinal) &&
                subject.RelatedAnchors!.Any(anchor => anchor.Relation == "direct-exact-changed-symbol-reference")).ToArray();
            await Assert.That(afterPrimaryEdit.Length).IsEqualTo(1);
        }
        finally
        {
            foreach (string file in Directory.EnumerateFiles(repository, "*", SearchOption.AllDirectories)) { File.SetAttributes(file, FileAttributes.Normal); }
            Directory.Delete(repository, recursive: true);
        }
    }

    [Test]
    public async Task RelatedMethodMaterialChangesInvalidateAcceptanceButUnrelatedMethodChangesDoNot()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-m0025-fingerprint-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        string testPath = Path.Combine(repository, "Tests.cs");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repository, "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(testPath, TestSource("Assert.Equal(1, result); // MATERIAL_ASSERTION"));
            Rule rule = new("semantic.relationship", 1, "review-batch", "review-batch", "", true);
            ReviewAnchor related = new("Fixture.csproj", "Tests.cs", 2, 5, "Tests.Verify", "related-test");
            ReviewSubject original = Subject(related, "question:Q1 original");
            string before;
            using (var session = new RepositorySession(repository))
            {
                Document document = session.Projects.SelectMany(project => project.Documents).Single(candidate => candidate.Name == "Tests.cs");
                var context = new RuleContext(session, [document]);
                before = context.RelationshipFingerprint([related]);
                original = original with { RelatedEvidenceFingerprint = before };
            }

            var firstStore = new GateLocalReviewStore(repository);
            ReviewSubject selected = firstStore.Select("same-gate", rule, [original], new HashSet<string>(["Product.cs"], StringComparer.Ordinal), "changed").Single();
            await Assert.That(firstStore.AcceptTicket(selected.GateTicket!)).IsTrue();
            firstStore.Commit();

            await File.WriteAllTextAsync(testPath, TestSource("Assert.Equal(1, result); // MATERIAL_ASSERTION") + "public class Unrelated { public void Change() { int value = 2; } }\n");
            using (var session = new RepositorySession(repository))
            {
                Document document = session.Projects.SelectMany(project => project.Documents).Single(candidate => candidate.Name == "Tests.cs");
                var context = new RuleContext(session, [document]);
                string unchangedEvidence = context.RelationshipFingerprint([related]);
                await Assert.That(unchangedEvidence).IsEqualTo(before);
                ReviewSubject unchanged = original with { RelatedEvidenceFingerprint = unchangedEvidence };
                await Assert.That(new GateLocalReviewStore(repository).Select("same-gate", rule, [unchanged],
                    new HashSet<string>(["Product.cs"], StringComparer.Ordinal), "changed").Count).IsEqualTo(0);
            }

            await File.WriteAllTextAsync(testPath, TestSource("Assert.Equal(2, result); // MATERIAL_ASSERTION"));
            using (var session = new RepositorySession(repository))
            {
                Document document = session.Projects.SelectMany(project => project.Documents).Single(candidate => candidate.Name == "Tests.cs");
                var context = new RuleContext(session, [document]);
                string changedEvidence = context.RelationshipFingerprint([related]);
                await Assert.That(changedEvidence).IsNotEqualTo(before);
                ReviewSubject changed = original with { RelatedEvidenceFingerprint = changedEvidence };
                IReadOnlyList<ReviewSubject> reselected = new GateLocalReviewStore(repository).Select("same-gate", rule, [changed],
                    new HashSet<string>(["Product.cs"], StringComparer.Ordinal), "changed");
                await Assert.That(reselected.Count).IsEqualTo(1);
            }

            ReviewSubject changedQuestion = original with { Content = original.Content + "; question:Q1 revised" };
            await Assert.That(GateLocalReviewStore.CandidateFingerprint(changedQuestion)).IsNotEqualTo(GateLocalReviewStore.CandidateFingerprint(original));
            ReviewSubject changedRule = original with { RelatedEvidenceFingerprint = before };
            await Assert.That(new GateLocalReviewStore(repository).Select("new-question-gate", rule with { Version = 2 }, [changedRule],
                new HashSet<string>(["Product.cs"], StringComparer.Ordinal), "changed").Count).IsEqualTo(1);
        }
        finally
        {
            foreach (string file in Directory.EnumerateFiles(repository, "*", SearchOption.AllDirectories)) { File.SetAttributes(file, FileAttributes.Normal); }
            Directory.Delete(repository, recursive: true);
        }
    }

    private static ReviewSubject Subject(ReviewAnchor related, string content) => new("project\0Product\0Changed", content,
        new ReviewItem("", "Product.cs", 1, 1, "Product.Changed", "", ""), DirectChange: true,
        RelatedAnchors: [related], PrimaryAnchor: new ReviewAnchor("Fixture.csproj", "Product.cs", 1, 1, "Product.Changed", "changed-contract"));

    private static string TestSource(string assertion) => """
        public static class Assert { public static void Equal(int expected, int actual) { } }
        public sealed class Tests
        {
            public void Verify()
            {
        """ + string.Concat(Enumerable.Range(0, 20).Select(index => $"            int value{index} = {index};\n")) +
            "            int result = 1;\n            " + assertion + "\n        }\n    }\n";

    private static ReviewSubject[] SelectCoverage(string repository)
    {
        using var session = new RepositorySession(repository);
        Document[] documents = session.Projects.SelectMany(project => project.Documents).ToArray();
        const string path = "Production/Product.cs";
        string product = File.ReadAllText(Path.Combine(repository, "Production", "Product.cs"));
        int changedLine = Array.FindIndex(product.Split('\n'), line => line.Contains("ChangedBehavior", StringComparison.Ordinal)) + 1;
        var gate = new GateSelectionContext("same-production-change", "changed", new HashSet<string>([path], StringComparer.Ordinal),
            new Dictionary<string, HashSet<int>>(StringComparer.Ordinal) { [path] = [changedLine] });
        var context = new RuleContext(session, documents, gate);
        var module = new TestChangeContractCoverageReviewRuleModule();
        RuleModuleExecution execution = new(module.Descriptor, module.Evaluate(context));
        RuleModuleExecution selected = GeneralSemanticBudget.SelectExecutions([execution], context).Single();
        return selected.Result.SelectedReviewSubjects?.ToArray() ?? [];
    }

    private static string ContractTestSource(string assertion) => """
        namespace Xunit { public class FactAttribute : System.Attribute { } }
        public static class Assert { public static void Equal(int expected, int actual) { } }
        public sealed class ContractTests
        {
            [Xunit.Fact] public void VerifiesChangedBehavior()
            {
        """ + string.Concat(Enumerable.Range(0, 20).Select(index => $"            int unrelated{index} = {index};\n")) +
            "            int result = new Fixture.Product().ChangedBehavior();\n            " + assertion + "\n        }\n    }\n";
}
