using DotNetAiCodeHygiene.Core;
using Microsoft.CodeAnalysis;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class M0025ArchitectureRuleTests
{
    [Test]
    public async Task ArchitectureRulesDiscoverTheirMechanicalSignalsIndependently()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-m0025-architecture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            string consumer = Path.Combine(repository, "Consumer");
            string provider = Path.Combine(repository, "Provider");
            Directory.CreateDirectory(consumer); Directory.CreateDirectory(provider);
            await File.WriteAllTextAsync(Path.Combine(provider, "Provider.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(provider, "Service.cs"), "namespace Provider; public interface IService { string Get(); } public sealed class Service : IService { public string Get() => \"ok\"; }");
            await File.WriteAllTextAsync(Path.Combine(consumer, "Consumer.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup><ItemGroup><ProjectReference Include=\"..\\Provider\\Provider.csproj\" /></ItemGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(consumer, "Fixture.cs"), Fixture);

            IReadOnlyList<ReviewSubject> abstractions = GetPopulation(repository, new AbstractionNecessityReviewRuleModule());
            IReadOnlyList<ReviewSubject> forwarding = GetPopulation(repository, new IndirectionValueReviewRuleModule());
            IReadOnlyList<ReviewSubject> cohesion = GetPopulation(repository, new ResponsibilityCohesionReviewRuleModule());
            IReadOnlyList<ReviewSubject> boundaries = GetPopulation(repository, new BoundaryOwnershipReviewRuleModule());
            IReadOnlyList<ReviewSubject> authority = GetPopulation(repository, new AuthorityUniquenessReviewRuleModule());
            IReadOnlyList<ReviewSubject> lifetimes = GetPopulation(repository, new LifecycleBindingReviewRuleModule());
            IReadOnlyList<ReviewSubject> mechanisms = GetPopulation(repository, new MechanismExplicitnessReviewRuleModule());

            await Assert.That(abstractions.Any(x => x.Item.Symbol.Contains("IStore", StringComparison.Ordinal))).IsTrue();
            await Assert.That(abstractions.Any(x => x.Item.Symbol.Contains("CreateStore", StringComparison.Ordinal))).IsTrue();
            await Assert.That(forwarding.Any(x => x.Item.Symbol.Contains("Read", StringComparison.Ordinal))).IsTrue();
            await Assert.That(forwarding.Any(x => x.Item.Symbol.Contains("ReadNested", StringComparison.Ordinal))).IsTrue();
            await Assert.That(cohesion.Any(x => x.Item.Symbol.Contains("Coordinator", StringComparison.Ordinal))).IsTrue();
            await Assert.That(boundaries.Any(x => x.Content.Contains("Provider.IService.Get", StringComparison.Ordinal) || x.Item.Summary.Contains("Cross-assembly target", StringComparison.Ordinal))).IsTrue();
            await Assert.That(boundaries.Any(x => x.RelatedAnchors!.Any(anchor => anchor.Project.StartsWith("Provider", StringComparison.Ordinal) && anchor.Relation == "compiler-resolved-provider-contract-or-symbol"))).IsTrue();
            await Assert.That(authority.Any(x => x.Item.Symbol.Contains("Refresh", StringComparison.Ordinal))).IsTrue();
            await Assert.That(lifetimes.Any(x => x.Item.Symbol.Contains("Coordinator", StringComparison.Ordinal))).IsTrue();
            await Assert.That(mechanisms.Any(x => x.Content.Contains("dynamic", StringComparison.Ordinal))).IsTrue();

            using (var session = new RepositorySession(repository))
            {
                Document[] documents = session.Projects.SelectMany(project => project.Documents).ToArray();
                var context = new RuleContext(session, documents);
                ReviewSubject abstraction = new AbstractionNecessityReviewRuleModule().Evaluate(context).ReviewSubjects.Single(subject => subject.Item.Symbol.Contains("IStore", StringComparison.Ordinal));
                ReviewSubject enrichedAbstraction = abstraction.Enrich!(context, abstraction);
                await Assert.That(enrichedAbstraction.RelatedAnchors!.Any(anchor => anchor.Symbol.Contains("Store", StringComparison.Ordinal) && anchor.Relation == "compiler-resolved-abstraction-implementation")).IsTrue();
                await Assert.That(enrichedAbstraction.RelatedAnchors!.Any(anchor => anchor.Relation == "compiler-resolved-abstraction-consumer")).IsTrue();
                ReviewSubject factory = new AbstractionNecessityReviewRuleModule().Evaluate(context).ReviewSubjects.Single(subject => subject.Item.Symbol.Contains("CreateStore", StringComparison.Ordinal));
                ReviewSubject enrichedFactory = factory.Enrich!(context, factory);
                await Assert.That(enrichedFactory.RelatedAnchors!.Any(anchor => anchor.Relation == "factory-returned-abstraction-contract")).IsTrue();
                await Assert.That(enrichedFactory.RelatedAnchors!.Any(anchor => anchor.Relation == "compiler-resolved-abstraction-implementation")).IsTrue();
                ReviewSubject forwardingSubject = new IndirectionValueReviewRuleModule().Evaluate(context).ReviewSubjects.First(subject => subject.Content.Contains("inner.Read", StringComparison.Ordinal));
                ReviewSubject enrichedForwarding = forwardingSubject.Enrich!(context, forwardingSubject);
                await Assert.That(enrichedForwarding.RelatedAnchors!.Any(anchor => anchor.Relation == "compiler-resolved-callee-of-intermediary")).IsTrue();
                await Assert.That(enrichedForwarding.RelatedAnchors!.Any(anchor => anchor.Relation == "compiler-resolved-caller-of-intermediary")).IsTrue();
                ReviewSubject authoritySubject = new AuthorityUniquenessReviewRuleModule().Evaluate(context).ReviewSubjects.Single(subject => subject.Item.Symbol.Contains("Refresh", StringComparison.Ordinal));
                await Assert.That(authoritySubject.RelatedAnchors!.Any(anchor => anchor.Relation == "direct-right-hand-side-provenance" && anchor.Symbol.Contains("configuration", StringComparison.OrdinalIgnoreCase))).IsTrue();
                ReviewSubject lifecycle = new LifecycleBindingReviewRuleModule().Evaluate(context).ReviewSubjects.Single(subject => subject.Item.Symbol.Contains("Coordinator", StringComparison.Ordinal));
                ReviewSubject enrichedLifecycle = lifecycle.Enrich!(context, lifecycle);
                await Assert.That(enrichedLifecycle.RelatedAnchors!.Any(anchor => anchor.Symbol.Contains("RequestContext", StringComparison.Ordinal))).IsTrue();
            }

            ISemanticReviewRuleModule[] modules =
            [new AbstractionNecessityReviewRuleModule(), new IndirectionValueReviewRuleModule(), new ResponsibilityCohesionReviewRuleModule(), new BoundaryOwnershipReviewRuleModule(), new AuthorityUniquenessReviewRuleModule(), new LifecycleBindingReviewRuleModule(), new MechanismExplicitnessReviewRuleModule()];
            await Assert.That(modules.Select(x => x.Descriptor.Id).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(7);
            await Assert.That(modules.All(x => x.Questions.Count == 1 && x.Questions[0].Id == "Q1")).IsTrue();
            await Assert.That(modules.All(x => x.EscalatedReviewerClass == "planner")).IsTrue();
        }
        finally
        {
            foreach (string file in Directory.EnumerateFiles(repository, "*", SearchOption.AllDirectories)) { File.SetAttributes(file, FileAttributes.Normal); }
            Directory.Delete(repository, recursive: true);
        }
    }

    [Test]
    public async Task PartialAbstractionKeepsTheChangedDeclarationAndOneSymbolDecision()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-m0025-partials-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repository, "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repository, "PartA.cs"), "public partial interface IPart<T> { void First(); }\npublic partial class Outer<T> { public interface IInner<U> { } }\n");
            string later = "public partial interface IPart<T> { void Later(); }\npublic partial interface ISame { void First(); }\npublic partial interface ISame { void Later(); }\npublic sealed class Implementation : IPart<int>, ISame { public void First() { } public void Later() { } }\npublic sealed class NestedImplementation : Outer<int>.IInner<string> { }\n";
            await File.WriteAllTextAsync(Path.Combine(repository, "PartB.cs"), later);
            using var session = new RepositorySession(repository);
            Document[] documents = session.Projects.SelectMany(project => project.Documents).ToArray();
            int laterLine = Array.FindIndex(later.Split('\n'), line => line.Contains("void Later", StringComparison.Ordinal)) + 1;
            int sameLaterLine = Array.FindIndex(later.Split('\n'), line => line.Contains("ISame { void Later", StringComparison.Ordinal)) + 1;
            var gate = new GateSelectionContext("partial-gate", "changed", new HashSet<string>(["PartB.cs"], StringComparer.Ordinal),
                new Dictionary<string, HashSet<int>>(StringComparer.Ordinal) { ["PartB.cs"] = [laterLine, sameLaterLine] });
            var context = new RuleContext(session, documents, gate);
            ReviewSubject[] partials = new AbstractionNecessityReviewRuleModule().Evaluate(context).ReviewSubjects
                .Where(subject => subject.Item.Symbol.Contains("IPart", StringComparison.Ordinal)).ToArray();
            await Assert.That(partials.Length).IsEqualTo(1);
            await Assert.That(partials.Single().Item.Path).IsEqualTo("PartB.cs");
            await Assert.That(partials.Single().DirectChange).IsTrue();
            ReviewSubject enriched = partials.Single().Enrich!(context, partials.Single());
            await Assert.That(enriched.RelatedAnchors!.Any(anchor => anchor.Path == "PartA.cs" && anchor.Relation == "same-symbol-partial-or-secondary-declaration")).IsTrue();
            await Assert.That(enriched.RelatedAnchors!.Any(anchor => anchor.Symbol.Contains("Implementation", StringComparison.Ordinal))).IsTrue();
            ReviewSubject sameFile = new AbstractionNecessityReviewRuleModule().Evaluate(context).ReviewSubjects.Single(subject => subject.Item.Symbol.Contains("ISame", StringComparison.Ordinal));
            await Assert.That(sameFile.Item.Path).IsEqualTo("PartB.cs");
            await Assert.That(sameFile.RelatedAnchors!.Any(anchor => anchor.Relation == "same-symbol-partial-or-secondary-declaration")).IsTrue();
            ReviewSubject nested = new AbstractionNecessityReviewRuleModule().Evaluate(context).ReviewSubjects.Single(subject => subject.Item.Symbol.Contains("IInner", StringComparison.Ordinal));
            ReviewSubject nestedEvidence = nested.Enrich!(context, nested);
            await Assert.That(nestedEvidence.RelatedAnchors!.Any(anchor => anchor.Symbol.Contains("NestedImplementation", StringComparison.Ordinal))).IsTrue();
        }
        finally { Directory.Delete(repository, recursive: true); }
    }

    [Test]
    public async Task PartialResponsibilitiesAggregatePerProjectAndKeepTheChangedReportingPart()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-m0025-partial-responsibility-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repository, "ProjectA"));
        Directory.CreateDirectory(Path.Combine(repository, "ProjectB"));
        try
        {
            const string project = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>";
            await File.WriteAllTextAsync(Path.Combine(repository, "ProjectA", "ProjectA.csproj"), project);
            await File.WriteAllTextAsync(Path.Combine(repository, "ProjectB", "ProjectB.csproj"), project);
            await File.WriteAllTextAsync(Path.Combine(repository, "ProjectA", "PartA.cs"), "namespace Shared; public partial class Worker<T> { private int _state; }\n");
            string changedPart = "namespace Shared; public partial class Worker<T> { public void Run() { } }\n";
            await File.WriteAllTextAsync(Path.Combine(repository, "ProjectA", "PartB.cs"), changedPart);
            await File.WriteAllTextAsync(Path.Combine(repository, "ProjectB", "Worker.cs"), "namespace Shared; public partial class Worker<T> { private int _state; public void Run() { } }\n");
            using var session = new RepositorySession(repository);
            Document[] documents = session.Projects.SelectMany(projectContext => projectContext.Documents).ToArray();
            const string changedPath = "ProjectA/PartB.cs";
            var gate = new GateSelectionContext("partial-responsibility-gate", "changed", new HashSet<string>([changedPath], StringComparer.Ordinal),
                new Dictionary<string, HashSet<int>>(StringComparer.Ordinal) { [changedPath] = [1] });
            var context = new RuleContext(session, documents, gate);

            ReviewSubject[] workers = new ResponsibilityCohesionReviewRuleModule().Evaluate(context).ReviewSubjects
                .Where(subject => subject.Item.Symbol.Contains("Shared.Worker<T>", StringComparison.Ordinal)).ToArray();
            await Assert.That(workers.Length).IsEqualTo(2);
            ReviewSubject changedProject = workers.Single(subject => subject.Item.Path == changedPath);
            await Assert.That(changedProject.DirectChange).IsTrue();
            await Assert.That(changedProject.RelatedAnchors!.Any(anchor => anchor.Path == "ProjectA/PartA.cs" && anchor.Relation == "same-symbol-partial-type-declaration")).IsTrue();
            await Assert.That(workers.Select(subject => subject.Identity.Split('\0')[0]).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(2);
            await Assert.That(workers.Any(subject => subject.Item.Path == "ProjectB/Worker.cs" && !subject.DirectChange)).IsTrue();
        }
        finally { Directory.Delete(repository, recursive: true); }
    }

    [Test]
    public async Task ClosedAndZeroWorkGatesSkipRelationshipEnrichmentUntilWorkIsSelected()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-m0025-lazy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repository, "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repository, "Fixture.cs"), "public interface IWork { void Run(); } public sealed class Work : IWork { public void Run() { } }\n");
            using var session = new RepositorySession(repository);
            Document[] documents = session.Projects.SelectMany(project => project.Documents).ToArray();
            var closed = new RuleContext(session, documents);
            IReadOnlyList<RuleModuleExecution> closedRun = new RuleModuleRunner([new AbstractionNecessityReviewRuleModule()]).Run(closed, new HashSet<string>());
            await Assert.That(closedRun.Single().Result.ReviewSubjects.Count).IsGreaterThan(0);
            await Assert.That(closed.RelationshipAnalysisCount).IsEqualTo(0);

            var zeroWork = new RuleContext(session, documents, new GateSelectionContext("zero", "changed", new HashSet<string>(["Fixture.cs"], StringComparer.Ordinal), new Dictionary<string, HashSet<int>>()));
            RuleModuleExecution zeroExecution = new RuleModuleRunner([new AbstractionNecessityReviewRuleModule()]).Run(zeroWork, new HashSet<string>()).Single();
            _ = GeneralSemanticBudget.SelectExecutions([zeroExecution], zeroWork);
            await Assert.That(zeroWork.RelationshipAnalysisCount).IsEqualTo(0);

            var selectedContext = new RuleContext(session, documents, new GateSelectionContext("changed", "changed", new HashSet<string>(["Fixture.cs"], StringComparer.Ordinal),
                new Dictionary<string, HashSet<int>>(StringComparer.Ordinal) { ["Fixture.cs"] = [1] }));
            RuleModuleExecution candidate = new RuleModuleRunner([new AbstractionNecessityReviewRuleModule()]).Run(selectedContext, new HashSet<string>()).Single();
            IReadOnlyList<RuleModuleExecution> selected = GeneralSemanticBudget.SelectExecutions([candidate], selectedContext);
            await Assert.That(selected.Single().Result.SelectedReviewSubjects!.Count).IsGreaterThan(0);
            await Assert.That(selectedContext.RelationshipAnalysisCount).IsGreaterThan(0);

            foreach (string mode in new[] { "paths", "all" })
            {
                var broadContext = new RuleContext(session, documents, new GateSelectionContext(mode, mode, new HashSet<string>(StringComparer.Ordinal), new Dictionary<string, HashSet<int>>()));
                RuleModuleExecution broadCandidate = new RuleModuleRunner([new AbstractionNecessityReviewRuleModule()]).Run(broadContext, new HashSet<string>()).Single();
                RuleModuleExecution broadSelection = GeneralSemanticBudget.SelectExecutions([broadCandidate], broadContext).Single();
                await Assert.That(broadSelection.Result.SelectedReviewSubjects!.Count).IsGreaterThan(0);
                await Assert.That(broadContext.RelationshipAnalysisCount).IsGreaterThan(0);
            }

            var expansionContext = new RuleContext(session, documents, new GateSelectionContext("expand", "changed", new HashSet<string>(StringComparer.Ordinal), new Dictionary<string, HashSet<int>>(), AbstractionNecessityReviewRuleModule.RuleId));
            RuleModuleExecution expansionCandidate = new RuleModuleRunner([new AbstractionNecessityReviewRuleModule()]).Run(expansionContext, new HashSet<string>()).Single();
            RuleModuleExecution expanded = GeneralSemanticBudget.SelectExecutions([expansionCandidate], expansionContext).Single();
            await Assert.That(expanded.Result.SelectedReviewSubjects!.Count).IsGreaterThan(0);
            await Assert.That(expansionContext.RelationshipAnalysisCount).IsGreaterThan(0);

            var auditContext = new RuleContext(session, documents, exhaustiveAudit: true);
            RuleModuleExecution auditCandidate = new RuleModuleRunner([new AbstractionNecessityReviewRuleModule()]).Run(auditContext, new HashSet<string>()).Single();
            RuleModuleExecution audited = GeneralSemanticBudget.SelectExecutions([auditCandidate], auditContext).Single();
            await Assert.That(audited.Result.SelectedReviewSubjects!.Count).IsGreaterThan(0);
            await Assert.That(auditContext.RelationshipAnalysisCount).IsGreaterThan(0);
        }
        finally { Directory.Delete(repository, recursive: true); }
    }

    private static IReadOnlyList<ReviewSubject> GetPopulation(string repository, ISemanticReviewRuleModule module)
    {
        using var session = new RepositorySession(repository);
        Document[] documents = session.Projects.SelectMany(project => project.Documents).ToArray();
        return module.Evaluate(new RuleContext(session, documents)).ReviewSubjects;
    }

    private const string Fixture = """
        using Provider;
        public interface IStore { string Read(); }
        public sealed class Store : IStore { public string Read() => "ok"; }
        public static class StoreFactory { public static IStore CreateStore() => new Store(); }
        public sealed class Forwarder(IStore inner) : IStore { public string Read() => inner.Read(); }
        public sealed class ForwarderCaller { public string Use(Forwarder forwarding) => forwarding.Read(); }
        public sealed class ProviderCaller(Provider.IService service)
        {
            public string Read() => service.Get();
            public string ReadNested() => service.Get().Trim();
        }
        public sealed class Coordinator(RequestContext context)
        {
            private RequestContext _context = context;
            private string _configuration = "";
            private int _configVersion;
            public int Count { get; private set; }
            public void Refresh(string configuration, int version)
            {
                _configuration = configuration;
                _configVersion = version;
                Count++;
            }
        }
        public sealed class RequestContext { }
        public sealed class DynamicReader
        {
            public object? Read(string typeName, dynamic value)
            {
                Type type = value.GetType();
                return Activator.CreateInstance(type);
            }
        }
        """;
}
