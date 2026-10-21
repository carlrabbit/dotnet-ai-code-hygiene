using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Builds type-scoped readonly-field candidates from IDE0044.</summary>
internal static class ReadonlyFieldCandidatePopulation
{
    internal const int MaximumOccurrencesPerType = 12;
    internal const string Question = "Should this field be readonly given all writes, constructor assignments, and uses across the type? Keep it mutable when later mutation is part of the design; mark uncertain when assignment behavior is unclear.";

    internal static IReadOnlyList<ReadonlyFieldTypeGroup> Build(Project project, Compilation compilation, CancellationToken cancellationToken)
    {
        ImmutableArray<Diagnostic> diagnostics = RoslynCodeStyleAnalyzers.Analyze(compilation, project.AnalyzerOptions, "IDE0044");
        var byType = new Dictionary<string, (INamedTypeSymbol Symbol, List<(Document Document, Diagnostic Diagnostic, FieldDeclarationSyntax Field, TypeDeclarationSyntax Type)> Candidates)>(StringComparer.Ordinal);
        foreach (Diagnostic diagnostic in diagnostics.Where(candidate => candidate.Id == "IDE0044" && candidate.Location.SourceTree is not null)
            .OrderBy(candidate => candidate.Location.SourceTree!.FilePath, StringComparer.Ordinal).ThenBy(candidate => candidate.Location.SourceSpan.Start))
        {
            cancellationToken.ThrowIfCancellationRequested();
            SyntaxTree tree = diagnostic.Location.SourceTree!;
            Document? document = project.Documents.FirstOrDefault(candidate => candidate.FilePath == tree.FilePath);
            if (document is null) { continue; }
            SyntaxNode root = tree.GetRoot(cancellationToken);
            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            FieldDeclarationSyntax? field = node.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().FirstOrDefault();
            TypeDeclarationSyntax? type = field?.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (field is null || type is null) { continue; }
            INamedTypeSymbol? symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(type, cancellationToken) as INamedTypeSymbol;
            if (symbol is null) { continue; }
            string typeId = symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!byType.TryGetValue(typeId, out var entry))
            {
                entry = (symbol, []);
                byType.Add(typeId, entry);
            }
            entry.Candidates.Add((document, diagnostic, field, type));
        }

        var groups = new List<ReadonlyFieldTypeGroup>();
        foreach ((string typeId, (INamedTypeSymbol symbol, List<(Document Document, Diagnostic Diagnostic, FieldDeclarationSyntax Field, TypeDeclarationSyntax Type)> candidates)) in byType)
        {
            var occurrences = new List<ReadonlyFieldOccurrence>();
            var seenFixes = new HashSet<string>(StringComparer.Ordinal);
            foreach ((Document document, Diagnostic diagnostic, FieldDeclarationSyntax field, TypeDeclarationSyntax type) in candidates.OrderBy(candidate => candidate.Field.SyntaxTree.FilePath, StringComparer.Ordinal).ThenBy(candidate => candidate.Field.SpanStart))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string path = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
                string original = field.ToFullString();
                string id = StableId(path + "\0" + typeId + "\0" + original);
                RoslynCodeFixPreview? preview = RoslynCodeStyleAnalyzers.PreviewCodeFixAsync(document, diagnostic, cancellationToken)
                    .GetAwaiter().GetResult();
                string originalDocument = document.GetTextAsync(cancellationToken).GetAwaiter().GetResult().ToString();
                if (preview is null || string.Equals(preview.Text, originalDocument, StringComparison.Ordinal)) { continue; }
                if (!seenFixes.Add(preview.Text)) { continue; }
                FileLinePositionSpan lineSpan = field.GetLocation().GetLineSpan();
                string context = string.Join("\n\n", symbol.DeclaringSyntaxReferences
                    .Select(reference => reference.GetSyntax(cancellationToken).ToString()).OrderBy(text => text, StringComparer.Ordinal));
                occurrences.Add(new ReadonlyFieldOccurrence(id, path, lineSpan.StartLinePosition.Line + 1,
                    symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), original, context, preview.Title, preview.Text, Question));
            }
            if (occurrences.Count == 0) { continue; }
            ReadonlyFieldOccurrence[] bounded = occurrences.Take(MaximumOccurrencesPerType).ToArray();
            groups.Add(new ReadonlyFieldTypeGroup(typeId, symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                occurrences.Count, bounded, occurrences.Count - bounded.Length));
        }
        return groups.OrderBy(group => group.TypeId, StringComparer.Ordinal).ToArray();
    }

    private static string StableId(string value) => "MO-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
}

internal sealed record ReadonlyFieldTypeGroup(string TypeId, string TypeName, int TotalEligibleOccurrences,
    IReadOnlyList<ReadonlyFieldOccurrence> Occurrences, int RemainingOccurrences);

internal sealed record ReadonlyFieldOccurrence(string Id, string Path, int Line, string Owner,
    string OriginalSyntax, string Context, string PreviewTitle, string PreviewDocument, string Question);
