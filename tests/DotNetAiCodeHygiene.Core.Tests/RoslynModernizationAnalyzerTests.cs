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
}
