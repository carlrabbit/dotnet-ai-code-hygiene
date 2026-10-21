using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using DotNetAiCodeHygiene.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Builds bounded file-scoped var-style review candidates from the pinned Roslyn diagnostics.</summary>
internal static class VarStyleCandidatePopulation
{
    internal const int MaximumOccurrencesPerFile = 12;
    internal const string Question = "Would the alternative var or explicit local type improve readability here? Keep the current form if it is clearer; mark uncertain when context is insufficient.";

    internal static IReadOnlyList<VarStyleFileGroup> Build(Project project, Compilation compilation, CancellationToken cancellationToken, bool exhaustive = false)
    {
        ImmutableArray<Diagnostic> diagnostics = RoslynCodeStyleAnalyzers.Analyze(
            compilation, project.AnalyzerOptions, "IDE0007", "IDE0008");
        var byFile = new Dictionary<DocumentId, List<(Diagnostic Diagnostic, LocalDeclarationStatementSyntax Declaration)>>();
        foreach (Diagnostic diagnostic in diagnostics.Where(diagnostic => diagnostic.Id is "IDE0007" or "IDE0008")
            .OrderBy(diagnostic => diagnostic.Location.SourceTree?.FilePath, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start))
        {
            cancellationToken.ThrowIfCancellationRequested();
            SyntaxTree? tree = diagnostic.Location.SourceTree;
            if (tree is null) { continue; }
            Document? document = project.Documents.FirstOrDefault(candidate => candidate.FilePath == tree.FilePath);
            if (document is null || !RuleContext.IsEligibleSourceDocument(document)) { continue; }
            SyntaxNode root = tree.GetRoot(cancellationToken);
            LocalDeclarationStatementSyntax? declaration = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
            if (declaration is null) { continue; }
            if (!byFile.TryGetValue(document.Id, out List<(Diagnostic, LocalDeclarationStatementSyntax)>? candidates))
            {
                candidates = [];
                byFile.Add(document.Id, candidates);
            }
            candidates.Add((diagnostic, declaration));
        }

        var groups = new List<VarStyleFileGroup>();
        foreach ((DocumentId documentId, List<(Diagnostic Diagnostic, LocalDeclarationStatementSyntax Declaration)> candidates) in byFile)
        {
            Document document = project.GetDocument(documentId)!;
            SemanticModel semanticModel = compilation.GetSemanticModel(candidates[0].Declaration.SyntaxTree);
            string path = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
            var occurrences = new List<VarStyleOccurrence>();
            var duplicateOrdinals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach ((Diagnostic diagnostic, LocalDeclarationStatementSyntax declaration) in candidates
                .OrderBy(candidate => candidate.Declaration.SpanStart))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string owner = semanticModel.GetEnclosingSymbol(declaration.SpanStart, cancellationToken)?
                    .ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "<unknown-member>";
                string original = declaration.ToFullString();
                string ordinalKey = diagnostic.Id + "\0" + owner + "\0" + declaration.WithoutTrivia().ToFullString();
                int ordinal = duplicateOrdinals.TryGetValue(ordinalKey, out int previous) ? previous + 1 : 0;
                duplicateOrdinals[ordinalKey] = ordinal;
                string identity = StableId(path + "\0" + diagnostic.Id + "\0" + owner + "\0" + declaration.WithoutTrivia() + "\0" + ordinal);
                RoslynCodeFixPreview? preview = RoslynCodeStyleAnalyzers.PreviewCodeFixAsync(document, diagnostic, cancellationToken)
                    .GetAwaiter().GetResult();
                if (preview is null || string.Equals(preview.Text, document.GetTextAsync(cancellationToken).GetAwaiter().GetResult().ToString(), StringComparison.Ordinal))
                {
                    continue;
                }
                FileLinePositionSpan lineSpan = declaration.GetLocation().GetLineSpan();
                string context = declaration.Parent?.Parent?.ToString() ?? declaration.Parent?.ToString() ?? "";
                occurrences.Add(new VarStyleOccurrence(identity, diagnostic.Id, path,
                    lineSpan.StartLinePosition.Line + 1, lineSpan.StartLinePosition.Character + 1,
                    owner, original, context, preview.Title, preview.Text, Question));
            }

            if (occurrences.Count == 0) { continue; }
            VarStyleOccurrence[] bounded = exhaustive ? occurrences.ToArray() : occurrences.Take(MaximumOccurrencesPerFile).ToArray();
            groups.Add(new VarStyleFileGroup(path, occurrences.Count, bounded, occurrences.Count - bounded.Length));
        }
        return groups.OrderBy(group => group.Path, StringComparer.Ordinal).ToArray();
    }

    private static string StableId(string value) => "MO-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
}

internal sealed record VarStyleFileGroup(string Path, int TotalEligibleOccurrences,
    IReadOnlyList<VarStyleOccurrence> Occurrences, int RemainingOccurrences);

internal sealed record VarStyleOccurrence(string Id, string DiagnosticId, string Path, int Line, int Column,
    string Owner, string OriginalSyntax, string Context, string PreviewTitle, string PreviewDocument, string Question);
