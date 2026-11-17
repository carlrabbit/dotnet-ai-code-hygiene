using DotNetAiCodeHygiene.Core;
using Microsoft.CodeAnalysis;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class M0025BehaviorRuleTests
{
    [Test]
    public async Task BehaviorRulesExposeMechanicalChangedSourceSubjectsAndSingleQuestions()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-m0025-behavior-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repository, "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repository, "Fixture.cs"), Fixture);
            ISemanticReviewRuleModule[] modules =
            [new BehaviorErrorContractReviewRuleModule(), new BehaviorValidationBoundaryReviewRuleModule(), new BehaviorStateConsistencyReviewRuleModule(), new BehaviorResourceOwnershipReviewRuleModule()];
            using var session = new RepositorySession(repository);
            Document[] documents = session.Projects.SelectMany(project => project.Documents).ToArray();
            var context = new RuleContext(session, documents);

            ReviewSubject[] failures = new BehaviorErrorContractReviewRuleModule().Evaluate(context).ReviewSubjects.ToArray();
            ReviewSubject[] boundaries = new BehaviorValidationBoundaryReviewRuleModule().Evaluate(context).ReviewSubjects.ToArray();
            ReviewSubject[] transitions = new BehaviorStateConsistencyReviewRuleModule().Evaluate(context).ReviewSubjects.ToArray();
            ReviewSubject[] resources = new BehaviorResourceOwnershipReviewRuleModule().Evaluate(context).ReviewSubjects.ToArray();
            await Assert.That(failures.Any(subject => subject.ObservedSignal!.Contains("failure/outcome", StringComparison.Ordinal))).IsTrue();
            await Assert.That(boundaries.Any(subject => subject.ObservedSignal!.Contains("input/validation", StringComparison.Ordinal))).IsTrue();
            await Assert.That(transitions.Any(subject => subject.ObservedSignal!.Contains("asynchronous/state", StringComparison.Ordinal))).IsTrue();
            await Assert.That(resources.Any(subject => subject.ObservedSignal!.Contains("resource lifecycle", StringComparison.Ordinal))).IsTrue();
            await Assert.That(modules.Select(module => module.Descriptor.Id).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(4);
            await Assert.That(modules.All(module => module.Questions.Count == 1 && module.Questions[0].Id == "Q1")).IsTrue();
            await Assert.That(modules.All(module => module.EscalatedReviewerClass == "planner")).IsTrue();
            await Assert.That(failures.Concat(boundaries).Concat(transitions).Concat(resources).All(subject => subject.PrimaryAnchor?.Project.Length > 0)).IsTrue();
            await Assert.That(failures.Concat(boundaries).Concat(transitions).Concat(resources).All(subject => !string.IsNullOrWhiteSpace(subject.LegitimateCase))).IsTrue();
        }
        finally { Directory.Delete(repository, recursive: true); }
    }

    private const string Fixture = """
        using System.ComponentModel.DataAnnotations;
        using System.IO;
        using System.Threading.Tasks;
        public sealed class BehaviorFixture
        {
            private int _selected;
            public string Handle()
            {
                try { throw new System.IO.IOException(); }
                catch (System.IO.IOException) { return "fallback"; }
            }
            public void Bind([Required] string name) { _ = name; }
            public async Task SelectContextAsync(Task<int> pending)
            {
                int result = await pending;
                _selected = result;
            }
            public void OwnResource()
            {
                using var stream = new MemoryStream();
                stream.WriteByte(1);
            }
        }
        """;
}
