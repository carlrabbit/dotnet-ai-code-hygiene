using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Builds file-scoped collection-expression candidates from the pinned Roslyn diagnostics.</summary>
internal static class CollectionExpressionCandidatePopulation
{
    internal const int MaximumOccurrencesPerFile = 12;
    internal const string Question = "Would a collection expression make this construction clearer in its file context? Keep the existing form when it communicates intent better or language/runtime constraints matter; mark uncertain when context is insufficient.";
    private static readonly string[] DiagnosticIds = ["IDE0300", "IDE0301", "IDE0302", "IDE0303", "IDE0304", "IDE0305"];

    internal static IReadOnlyList<CollectionExpressionFileGroup> Build(Project project, Compilation compilation, CancellationToken cancellationToken)
    {
        ImmutableArray<Diagnostic> diagnostics = RoslynCodeStyleAnalyzers.Analyze(compilation, project.AnalyzerOptions, DiagnosticIds);
        var byFile = new Dictionary<DocumentId, List<(Document Document, Diagnostic Diagnostic, ExpressionSyntax Expression)>>();
        foreach (Diagnostic diagnostic in diagnostics.Where(candidate => DiagnosticIds.Contains(candidate.Id, StringComparer.Ordinal)
            && candidate.Location.SourceTree is not null).OrderBy(candidate => candidate.Location.SourceTree!.FilePath, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Location.SourceSpan.Start))
        {
            cancellationToken.ThrowIfCancellationRequested();
            SyntaxTree tree = diagnostic.Location.SourceTree!;
            Document? document = project.Documents.FirstOrDefault(candidate => candidate.FilePath == tree.FilePath);
            if (document is null) { continue; }
            SyntaxNode root = tree.GetRoot(cancellationToken);
            ExpressionSyntax? expression = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .AncestorsAndSelf().OfType<ExpressionSyntax>().FirstOrDefault();
            if (expression is null) { continue; }
            if (!byFile.TryGetValue(document.Id, out List<(Document, Diagnostic, ExpressionSyntax)>? candidates))
            {
                candidates = [];
                byFile.Add(document.Id, candidates);
            }
            candidates.Add((document, diagnostic, expression));
        }

        var groups = new List<CollectionExpressionFileGroup>();
        foreach ((DocumentId documentId, List<(Document Document, Diagnostic Diagnostic, ExpressionSyntax Expression)> candidates) in byFile)
        {
            var occurrences = new List<CollectionExpressionOccurrence>();
            var duplicateOrdinals = new Dictionary<string, int>(StringComparer.Ordinal);
            var seenEquivalentFixes = new HashSet<string>(StringComparer.Ordinal);
            foreach ((Document document, Diagnostic diagnostic, ExpressionSyntax expression) in candidates.OrderBy(candidate => candidate.Expression.SpanStart))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string path = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
                string source = expression.WithoutTrivia().ToFullString();
                string ordinalKey = diagnostic.Id + "\0" + source;
                int ordinal = duplicateOrdinals.TryGetValue(ordinalKey, out int previous) ? previous + 1 : 0;
                duplicateOrdinals[ordinalKey] = ordinal;
                string id = StableId(path + "\0" + diagnostic.Id + "\0" + source + "\0" + ordinal);
                RoslynCodeFixPreview? preview = RoslynCodeStyleAnalyzers.PreviewCodeFixAsync(document, diagnostic, cancellationToken)
                    .GetAwaiter().GetResult();
                string originalDocument = document.GetTextAsync(cancellationToken).GetAwaiter().GetResult().ToString();
                if (preview is null || string.Equals(preview.Text, originalDocument, StringComparison.Ordinal)) { continue; }
                string equivalentFix = expression.SpanStart.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0"
                    + expression.Span.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0" + preview.Text;
                if (!seenEquivalentFixes.Add(equivalentFix)) { continue; }
                SyntaxNode contextNode = expression.Ancestors().FirstOrDefault(node => node is MemberDeclarationSyntax)
                    ?? expression.Ancestors().FirstOrDefault(node => node is TypeDeclarationSyntax)
                    ?? expression.Parent!;
                FileLinePositionSpan lineSpan = expression.GetLocation().GetLineSpan();
                occurrences.Add(new CollectionExpressionOccurrence(id, diagnostic.Id, path, lineSpan.StartLinePosition.Line + 1,
                    source, contextNode.ToString(), preview.Title, preview.Text, Question));
            }

            if (occurrences.Count == 0) { continue; }
            CollectionExpressionOccurrence[] bounded = occurrences.Take(MaximumOccurrencesPerFile).ToArray();
            Document file = project.GetDocument(documentId)!;
            groups.Add(new CollectionExpressionFileGroup(Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, file.FilePath!),
                occurrences.Count, bounded, occurrences.Count - bounded.Length));
        }
        return groups.OrderBy(group => group.Path, StringComparer.Ordinal).ToArray();
    }

    private static string StableId(string value) => "MO-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
}

internal sealed record CollectionExpressionFileGroup(string Path, int TotalEligibleOccurrences,
    IReadOnlyList<CollectionExpressionOccurrence> Occurrences, int RemainingOccurrences);

internal sealed record CollectionExpressionOccurrence(string Id, string DiagnosticId, string Path, int Line,
    string OriginalSyntax, string Context, string PreviewTitle, string PreviewDocument, string Question);
