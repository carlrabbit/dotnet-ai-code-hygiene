using System.Collections.Immutable;
using DotNetAiCodeHygiene.Core;
using Microsoft.CodeAnalysis;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class RoslynModernizationAnalyzerTests
{
    [Test]
    public async Task PinnedRoslynPackageExposesTheAgreedModernizationDiagnosticFamilies()
    {
        string[][] families =
        [
            ["IDE0007", "IDE0008"],
            ["IDE0021", "IDE0022", "IDE0023", "IDE0024", "IDE0025", "IDE0026", "IDE0027"],
            ["IDE0300", "IDE0301", "IDE0302", "IDE0303", "IDE0304", "IDE0305"],
            ["IDE0290"],
            ["IDE0066"],
            ["IDE0045", "IDE0046"],
            ["IDE0017"],
            ["IDE0044"],
            ["IDE0018"]
        ];

        foreach (string[] family in families)
        {
            string[] exposed = RoslynCodeStyleAnalyzers.ForDiagnostics(family)
                .SelectMany(analyzer => analyzer.SupportedDiagnostics)
                .Select(diagnostic => diagnostic.Id)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            foreach (string diagnosticId in family)
            {
                await Assert.That(exposed).Contains(diagnosticId);
            }
        }
    }

    [Test]
    public async Task VarStyleDiagnosticsFollowTheProjectAnalyzerConfiguration()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-var-analyzer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            string projectPath = Path.Combine(repository, "Sample.csproj");
            string sourcePath = Path.Combine(repository, "Sample.cs");
            await File.WriteAllTextAsync(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(sourcePath, "public class Sample { public void Run() { var inferred = 1; int explicitValue = 2; } }\n");

            await File.WriteAllTextAsync(Path.Combine(repository, ".editorconfig"),
                "root = true\n[*.cs]\ncsharp_style_var_for_built_in_types = false:warning\ncsharp_style_var_when_type_is_apparent = false:warning\ncsharp_style_var_elsewhere = false:warning\n");
            using (var session = new RepositorySession(repository))
            {
                Project project = session.Projects.Single();
                Document document = project.Documents.Single(item => item.Name == "Sample.cs");
                SyntaxTree tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!;
                var options = project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);
                options.TryGetValue("csharp_style_var_for_built_in_types", out string? preference);
                await Assert.That(preference).IsEqualTo("false:warning");
                ImmutableArray<Diagnostic> diagnostics = RoslynCodeStyleAnalyzers.Analyze(
                    session.GetCompilation(project), project.AnalyzerOptions, "IDE0008");
                await Assert.That(diagnostics.Any(diagnostic => diagnostic.Id == "IDE0008" && diagnostic.Location.SourceTree == tree)).IsTrue();
                Diagnostic candidate = diagnostics.Single(diagnostic => diagnostic.Id == "IDE0008" && diagnostic.Location.SourceTree == tree);
                RoslynCodeFixPreview? preview = await RoslynCodeStyleAnalyzers.PreviewCodeFixAsync(document, candidate, CancellationToken.None);
                await Assert.That(preview?.Text).Contains("int inferred = 1;");
            }

            await File.WriteAllTextAsync(Path.Combine(repository, ".editorconfig"),
                "root = true\n[*.cs]\ncsharp_style_var_for_built_in_types = true:warning\ncsharp_style_var_when_type_is_apparent = true:warning\ncsharp_style_var_elsewhere = true:warning\n");
            using (var session = new RepositorySession(repository))
            {
                Project project = session.Projects.Single();
                Document document = project.Documents.Single(item => item.Name == "Sample.cs");
                SyntaxTree tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!;
                ImmutableArray<Diagnostic> diagnostics = RoslynCodeStyleAnalyzers.Analyze(
                    session.GetCompilation(project), project.AnalyzerOptions, "IDE0007");
                await Assert.That(diagnostics.Any(diagnostic => diagnostic.Id == "IDE0007" && diagnostic.Location.SourceTree == tree)).IsTrue();
                Diagnostic candidate = diagnostics.Single(diagnostic => diagnostic.Id == "IDE0007" && diagnostic.Location.SourceTree == tree);
                RoslynCodeFixPreview? preview = await RoslynCodeStyleAnalyzers.PreviewCodeFixAsync(document, candidate, CancellationToken.None);
                await Assert.That(preview?.Text).Contains("var explicitValue = 2;");
            }
        }
        finally { if (Directory.Exists(repository)) { Directory.Delete(repository, recursive: true); } }
    }

    [Test]
    public async Task VarStyleCandidatesGroupByFileAndKeepOccurrencePreviewsIndependent()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-var-candidates-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repository, "Sample.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repository, "Sample.cs"),
                "public class Sample { public void Run() { var first = 1; var second = 2; } }\n");
            await File.WriteAllTextAsync(Path.Combine(repository, ".editorconfig"),
                "root = true\n[*.cs]\ncsharp_style_var_for_built_in_types = false:warning\ncsharp_style_var_when_type_is_apparent = false:warning\ncsharp_style_var_elsewhere = false:warning\n");

            using var session = new RepositorySession(repository);
            Project project = session.Projects.Single();
            Compilation compilation = session.GetCompilation(project);
            IReadOnlyList<VarStyleFileGroup> first = VarStyleCandidatePopulation.Build(project, compilation, CancellationToken.None);
            IReadOnlyList<VarStyleFileGroup> second = VarStyleCandidatePopulation.Build(project, compilation, CancellationToken.None);

            await Assert.That(first).Count().IsEqualTo(1);
            VarStyleFileGroup group = first.Single();
            await Assert.That(group.TotalEligibleOccurrences).IsEqualTo(2);
            await Assert.That(group.Occurrences).Count().IsEqualTo(2);
            await Assert.That(group.RemainingOccurrences).IsEqualTo(0);
            await Assert.That(group.Occurrences.Select(candidate => candidate.Id).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(2);
            await Assert.That(group.Occurrences.All(candidate => candidate.PreviewDocument.Contains("int ", StringComparison.Ordinal))).IsTrue();
            await Assert.That(second.Single().Occurrences.Select(candidate => candidate.Id))
                .IsEquivalentTo(group.Occurrences.Select(candidate => candidate.Id));
            await Assert.That(group.Occurrences.All(candidate => candidate.Question == VarStyleCandidatePopulation.Question)).IsTrue();
        }
        finally { if (Directory.Exists(repository)) { Directory.Delete(repository, recursive: true); } }
    }

    [Test]
    public async Task ExpressionBodyCandidatesGroupByTypeAndIncludeTypeContext()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-expression-body-candidates-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repository, "Sample.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repository, "Sample.cs"),
                "public class Sample { public int First() { return 1; } public int Second() { return 2; } }\n");
            await File.WriteAllTextAsync(Path.Combine(repository, ".editorconfig"),
                "root = true\n[*.cs]\ncsharp_style_expression_bodied_methods = when_on_single_line:warning\n");

            using var session = new RepositorySession(repository);
            Project project = session.Projects.Single();
            IReadOnlyList<ExpressionBodyTypeGroup> groups = ExpressionBodyCandidatePopulation.Build(
                project, session.GetCompilation(project), CancellationToken.None);

            await Assert.That(groups).Count().IsEqualTo(1);
            ExpressionBodyTypeGroup group = groups.Single();
            await Assert.That(group.TypeName).IsEqualTo("Sample");
            await Assert.That(group.TotalEligibleOccurrences).IsEqualTo(2);
            await Assert.That(group.Occurrences).Count().IsEqualTo(2);
            await Assert.That(group.Occurrences.All(candidate => candidate.Context.Contains("class Sample", StringComparison.Ordinal))).IsTrue();
            await Assert.That(group.Occurrences.All(candidate => candidate.PreviewDocument.Contains("=>", StringComparison.Ordinal))).IsTrue();
            await Assert.That(group.Occurrences.Select(candidate => candidate.Id).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(2);
        }
        finally { if (Directory.Exists(repository)) { Directory.Delete(repository, recursive: true); } }
    }

    [Test]
    public async Task CollectionExpressionCandidatesGroupByFileAndIncludeConcretePreviews()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-collection-candidates-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repository, "Sample.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repository, "Sample.cs"),
                "public class Sample { public int[] Run() { int[] values = new int[] { 1, 2 }; return values; } }\n");
            await File.WriteAllTextAsync(Path.Combine(repository, ".editorconfig"),
                "root = true\n[*.cs]\ndotnet_style_prefer_collection_expression = true:warning\n");

            using var session = new RepositorySession(repository);
            Project project = session.Projects.Single();
            IReadOnlyList<CollectionExpressionFileGroup> groups = CollectionExpressionCandidatePopulation.Build(
                project, session.GetCompilation(project), CancellationToken.None);

            await Assert.That(groups).Count().IsEqualTo(1);
            CollectionExpressionFileGroup group = groups.Single();
            await Assert.That(group.TotalEligibleOccurrences).IsEqualTo(1);
            await Assert.That(group.Occurrences.Single().OriginalSyntax).Contains("new int[]");
            await Assert.That(group.Occurrences.Single().Context).Contains("int[] Run()");
            await Assert.That(group.Occurrences.Single().PreviewDocument).Contains("[1, 2]");
        }
        finally { if (Directory.Exists(repository)) { Directory.Delete(repository, recursive: true); } }
    }

    [Test]
    public async Task SwitchExpressionCandidatesUseMemberScopeAndIncludeBehaviorContext()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-switch-expression-candidates-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repository, "Sample.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repository, "Sample.cs"),
                "public class Sample { public int Run(int value) { int result; switch (value) { case 1: result = 10; break; default: result = 0; break; } return result; } }\n");
            await File.WriteAllTextAsync(Path.Combine(repository, ".editorconfig"),
                "root = true\n[*.cs]\ncsharp_style_prefer_switch_expression = true:warning\n");

            using var session = new RepositorySession(repository);
            Project project = session.Projects.Single();
            IReadOnlyList<SwitchExpressionMemberGroup> groups = SwitchExpressionCandidatePopulation.Build(
                project, session.GetCompilation(project), CancellationToken.None);

            await Assert.That(groups).Count().IsEqualTo(1);
            SwitchExpressionMemberGroup group = groups.Single();
            await Assert.That(group.Owner).Contains("Run(int)");
            await Assert.That(group.TotalEligibleOccurrences).IsEqualTo(1);
            await Assert.That(group.Occurrences.Single().Context).Contains("return result");
            await Assert.That(group.Occurrences.Single().PreviewDocument).Contains("switch");
        }
        finally { if (Directory.Exists(repository)) { Directory.Delete(repository, recursive: true); } }
    }

    [Test]
    public async Task ConditionalCandidatesKeepTheEnclosingMemberAndCodeFixPreview()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-conditional-candidates-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repository, "Sample.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repository, "Sample.cs"),
                "public class Sample { public int Run(bool flag) { if (flag) return 1; else return 0; } }\n");
            await File.WriteAllTextAsync(Path.Combine(repository, ".editorconfig"),
                "root = true\n[*.cs]\ncsharp_style_prefer_conditional_expression = true:warning\n");

            using var session = new RepositorySession(repository);
            Project project = session.Projects.Single();
            IReadOnlyList<ConditionalMemberGroup> groups = ConditionalCandidatePopulation.Build(
                project, session.GetCompilation(project), CancellationToken.None);

            await Assert.That(groups).Count().IsEqualTo(1);
            ConditionalMemberGroup group = groups.Single();
            await Assert.That(group.Owner).Contains("Run(bool)");
            await Assert.That(group.TotalEligibleOccurrences).IsEqualTo(1);
            await Assert.That(group.Occurrences.Single().PreviewDocument).Contains("?");
            await Assert.That(group.Occurrences.Single().Context).Contains("if (flag)");
        }
        finally { if (Directory.Exists(repository)) { Directory.Delete(repository, recursive: true); } }
    }

    [Test]
    public async Task ObjectInitializerCandidatesIncludeConstructionAndMutationContext()
    {
        string repository = Path.Combine(Path.GetTempPath(), "hygiene-object-initializer-candidates-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repository);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repository, "Sample.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repository, "Sample.cs"),
                "public class Box { public int Value { get; set; } } public class Sample { public Box Run() { var box = new Box(); box.Value = 3; return box; } }\n");
            await File.WriteAllTextAsync(Path.Combine(repository, ".editorconfig"),
                "root = true\n[*.cs]\ndotnet_style_object_initializer = true:warning\n");

            using var session = new RepositorySession(repository);
            Project project = session.Projects.Single();
            IReadOnlyList<ObjectInitializerMemberGroup> groups = ObjectInitializerCandidatePopulation.Build(
                project, session.GetCompilation(project), CancellationToken.None);

            await Assert.That(groups).Count().IsEqualTo(1);
            ObjectInitializerMemberGroup group = groups.Single();
            await Assert.That(group.Owner).Contains("Run()");
            await Assert.That(group.TotalEligibleOccurrences).IsEqualTo(1);
            await Assert.That(group.Occurrences.Single().Context).Contains("box.Value = 3");
            await Assert.That(group.Occurrences.Single().PreviewDocument).Contains("Value = 3");
        }
        finally { if (Directory.Exists(repository)) { Directory.Delete(repository, recursive: true); } }
    }
}
