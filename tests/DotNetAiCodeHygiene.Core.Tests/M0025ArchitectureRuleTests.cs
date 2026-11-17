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
            await Assert.That(authority.Any(x => x.Item.Symbol.Contains("Refresh", StringComparison.Ordinal))).IsTrue();
            await Assert.That(lifetimes.Any(x => x.Item.Symbol.Contains("Coordinator", StringComparison.Ordinal))).IsTrue();
            await Assert.That(mechanisms.Any(x => x.Content.Contains("dynamic", StringComparison.Ordinal))).IsTrue();

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
