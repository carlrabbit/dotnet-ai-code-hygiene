using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Builds type-scoped primary-constructor candidates from IDE0290.</summary>
internal static class PrimaryConstructorCandidatePopulation
{
    internal const int MaximumOccurrencesPerType = 12;
    internal const string Question = "Would moving constructor parameters to a primary constructor simplify this type without obscuring state, initialization order, or the public API? Keep the existing constructor when it communicates ownership better; mark uncertain when context is insufficient.";

    internal static IReadOnlyList<PrimaryConstructorTypeGroup> Build(Project project, Compilation compilation, CancellationToken cancellationToken, bool exhaustive = false)
    {
        ImmutableArray<Diagnostic> diagnostics = RoslynCodeStyleAnalyzers.Analyze(compilation, project.AnalyzerOptions, "IDE0290");
        var byType = new Dictionary<string, (INamedTypeSymbol Symbol, List<(Document Document, Diagnostic Diagnostic, TypeDeclarationSyntax Type)> Candidates)>(StringComparer.Ordinal);
        foreach (Diagnostic diagnostic in diagnostics.Where(candidate => candidate.Id == "IDE0290" && candidate.Location.SourceTree is not null)
            .OrderBy(candidate => candidate.Location.SourceTree!.FilePath, StringComparer.Ordinal).ThenBy(candidate => candidate.Location.SourceSpan.Start))
        {
            cancellationToken.ThrowIfCancellationRequested();
            SyntaxTree tree = diagnostic.Location.SourceTree!;
            Document? document = project.Documents.FirstOrDefault(candidate => candidate.FilePath == tree.FilePath);
            if (document is null || !RuleContext.IsEligibleSourceDocument(document)) { continue; }
            SyntaxNode root = tree.GetRoot(cancellationToken);
            TypeDeclarationSyntax? type = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (type is null) { continue; }
            INamedTypeSymbol? symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(type, cancellationToken) as INamedTypeSymbol;
            if (symbol is null) { continue; }
            string typeId = symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!byType.TryGetValue(typeId, out var entry))
            {
                entry = (symbol, []);
                byType.Add(typeId, entry);
            }
            entry.Candidates.Add((document, diagnostic, type));
        }

        var groups = new List<PrimaryConstructorTypeGroup>();
        foreach ((string typeId, (INamedTypeSymbol symbol, List<(Document Document, Diagnostic Diagnostic, TypeDeclarationSyntax Type)> candidates)) in byType)
        {
            var occurrences = new List<PrimaryConstructorOccurrence>();
            var seenFixes = new HashSet<string>(StringComparer.Ordinal);
            foreach ((Document document, Diagnostic diagnostic, TypeDeclarationSyntax type) in candidates.OrderBy(candidate => candidate.Type.SyntaxTree.FilePath, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string path = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
                string original = type.ToFullString();
                string id = StableId(path + "\0" + typeId + "\0" + original);
                RoslynCodeFixPreview? preview = RoslynCodeStyleAnalyzers.PreviewCodeFixAsync(document, diagnostic, cancellationToken)
                    .GetAwaiter().GetResult();
                string originalDocument = document.GetTextAsync(cancellationToken).GetAwaiter().GetResult().ToString();
                if (preview is null || string.Equals(preview.Text, originalDocument, StringComparison.Ordinal)) { continue; }
                if (!seenFixes.Add(preview.Text)) { continue; }
                FileLinePositionSpan lineSpan = type.GetLocation().GetLineSpan();
                string context = string.Join("\n\n", symbol.DeclaringSyntaxReferences
                    .Select(reference => reference.GetSyntax(cancellationToken).ToString()).OrderBy(text => text, StringComparer.Ordinal));
                occurrences.Add(new PrimaryConstructorOccurrence(id, path, lineSpan.StartLinePosition.Line + 1,
                    symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), original, context, preview.Title, preview.Text, Question));
            }
            if (occurrences.Count == 0) { continue; }
            PrimaryConstructorOccurrence[] bounded = exhaustive ? occurrences.ToArray() : occurrences.Take(MaximumOccurrencesPerType).ToArray();
            groups.Add(new PrimaryConstructorTypeGroup(typeId, symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                occurrences.Count, bounded, occurrences.Count - bounded.Length));
        }
        return groups.OrderBy(group => group.TypeId, StringComparer.Ordinal).ToArray();
    }

    private static string StableId(string value) => "MO-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
}

internal sealed record PrimaryConstructorTypeGroup(string TypeId, string TypeName, int TotalEligibleOccurrences,
    IReadOnlyList<PrimaryConstructorOccurrence> Occurrences, int RemainingOccurrences);

internal sealed record PrimaryConstructorOccurrence(string Id, string Path, int Line, string Owner,
    string OriginalSyntax, string Context, string PreviewTitle, string PreviewDocument, string Question);
