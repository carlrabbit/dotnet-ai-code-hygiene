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
            await File.WriteAllTextAsync(Path.Combine(repository, "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup><ItemGroup><FrameworkReference Include=\"Microsoft.AspNetCore.App\" /></ItemGroup></Project>");
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
            await Assert.That(boundaries.Any(subject => subject.ObservedSignal!.Contains("RequiredAttribute", StringComparison.Ordinal))).IsTrue();
            ReviewSubject serializerBoundary = boundaries.Single(subject => subject.Content.Contains("JsonSerializer.Deserialize", StringComparison.Ordinal));
            await Assert.That(serializerBoundary.ObservedSignal).Contains("Compiler-resolved input/binder/serializer method");
            ReviewSubject enrichedSerializerBoundary = serializerBoundary.Enrich!(context, serializerBoundary);
            await Assert.That(enrichedSerializerBoundary.RelatedAnchors!.Any(anchor => anchor.Symbol.Contains("InputDto", StringComparison.Ordinal))).IsTrue();
            await Assert.That(enrichedSerializerBoundary.EvidenceLimitation).Contains("serializer options");
            await Assert.That(transitions.Any(subject => subject.ObservedSignal!.Contains("await count=", StringComparison.Ordinal))).IsTrue();
            await Assert.That(transitions.Single().ObservedSignal).Contains("async+await+state mutation");
            await Assert.That(transitions.Single().Content).Contains("requestGeneration");
            await Assert.That(resources.Any(subject => subject.ObservedSignal!.Contains("resource allocation", StringComparison.OrdinalIgnoreCase))).IsTrue();
            await Assert.That(failures.Any(subject => subject.Content.Contains("?? throw", StringComparison.Ordinal))).IsTrue();
            await Assert.That(failures.All(subject => !subject.Content.Contains("?? \"default\"", StringComparison.Ordinal))).IsTrue();
            await Assert.That(resources.All(subject => !subject.Item.Symbol.Contains("ArithmeticOnly", StringComparison.Ordinal))).IsTrue();
            await Assert.That(resources.Any(subject => subject.ObservedSignal!.Contains("event", StringComparison.OrdinalIgnoreCase))).IsTrue();
            await Assert.That(modules.Select(module => module.Descriptor.Id).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(4);
            await Assert.That(modules.All(module => module.Questions.Count == 1 && module.Questions[0].Id == "Q1")).IsTrue();
            await Assert.That(modules.All(module => module.EscalatedReviewerClass == "planner")).IsTrue();
            await Assert.That(failures.Concat(boundaries).Concat(transitions).Concat(resources).All(subject => subject.PrimaryAnchor?.Project.Length > 0)).IsTrue();
            await Assert.That(failures.Concat(boundaries).Concat(transitions).Concat(resources).All(subject => !string.IsNullOrWhiteSpace(subject.LegitimateCase))).IsTrue();
            await Assert.That(boundaries.Any(subject => subject.ObservedSignal!.Contains("RequiredAttribute", StringComparison.Ordinal))).IsTrue();
            ReviewSubject httpBoundary = boundaries.Single(subject => subject.ObservedSignal!.Contains("Microsoft.AspNetCore.Mvc.HttpPostAttribute", StringComparison.Ordinal));
            await Assert.That(httpBoundary.ObservedSignal).Contains("Microsoft.AspNetCore.Mvc.HttpPostAttribute");
            ReviewSubject enrichedHttpBoundary = httpBoundary.Enrich!(context, httpBoundary);
            await Assert.That(enrichedHttpBoundary.RelatedAnchors!.Any(anchor => anchor.Symbol.Contains("InputDto", StringComparison.Ordinal))).IsTrue();
            await Assert.That(enrichedHttpBoundary.EvidenceLimitation).Contains("Effective runtime binder");
            await Assert.That(boundaries.Length).IsGreaterThan(1);
            await Assert.That(transitions.Single().RelatedAnchors!.Any(anchor => anchor.Relation == "state-transition-write")).IsTrue();
            ReviewSubject resource = resources.First(subject => subject.Item.Symbol.Contains("OwnResource", StringComparison.Ordinal));
            await Assert.That(resource.RelatedAnchors!.Any(anchor => anchor.Relation == "related-acquisition-use-or-release-in-owner")).IsTrue();
            await Assert.That(resource.Content).Contains("finally");
            await Assert.That(resource.RelatedAnchors!.Any(anchor => anchor.Symbol.Contains("Dispose", StringComparison.Ordinal))).IsTrue();
            await Assert.That(resource.Content).Contains("CancellationTokenSource");
        }
        finally { Directory.Delete(repository, recursive: true); }
    }

    private const string Fixture = """
        using System.ComponentModel.DataAnnotations;
        using System.IO;
        using System.Text.Json;
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Mvc;
        public sealed class BehaviorFixture
        {
            private int _selected;
            private int _generation;
            public event System.EventHandler? Changed;
            public string Handle()
            {
                try { throw new System.IO.IOException(); }
                catch (System.IO.IOException) { return "fallback"; }
            }
            public void Bind([Required] string name) { _ = name; }
            public async Task SelectContextAsync(Task<int> pending, int requestGeneration)
            {
                int result = await pending;
                if (requestGeneration != _generation) { return; }
                _selected = result;
            }
            public void OwnResource()
            {
                var stream = new MemoryStream();
                try { stream.WriteByte(1); }
                catch (IOException) { throw; }
                finally { stream.Dispose(); }
                using var cancellation = new System.Threading.CancellationTokenSource();
                cancellation.Cancel();
            }
            public string OrdinaryCoalesce(string? value) => value ?? "default";
            public string FailureFallback(string? value) => value ?? throw new System.IO.IOException();
            public void Subscribe(System.EventHandler handler) { Changed += handler; Changed -= handler; }
            public void ArithmeticOnly() { _selected += 1; _selected -= 1; }
            public string NameBasedLookalikes(string value)
            {
                var helper = new LocalMethods();
                helper.Validate(value); helper.Bind(value); helper.Deserialize(value);
                return value;
            }
            private sealed class LocalMethods
            {
                public void Validate(string value) { }
                public void Bind(string value) { }
                public void Deserialize(string value) { }
            }
        }
        public sealed class InputDto { [Required] public string? Name { get; set; } }
        [ApiController]
        public sealed class HttpEndpointController : ControllerBase
        {
            [HttpPost]
            public IActionResult Post([FromBody] InputDto? input) => Ok(input?.Name);
            public InputDto? Decode(string payload) => JsonSerializer.Deserialize<InputDto>(payload);
        }
        """;
}
