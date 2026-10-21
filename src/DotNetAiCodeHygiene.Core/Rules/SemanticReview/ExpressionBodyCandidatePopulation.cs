using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Builds type-scoped expression-body candidates from the pinned Roslyn diagnostics.</summary>
internal static class ExpressionBodyCandidatePopulation
{
    internal const int MaximumOccurrencesPerType = 12;
    internal const string Question = "Would an expression body make this member easier to read in its type context? Keep the block when it communicates structure or intent; mark uncertain when context is insufficient.";
    private static readonly string[] DiagnosticIds = ["IDE0021", "IDE0022", "IDE0023", "IDE0024", "IDE0025", "IDE0026", "IDE0027"];

    internal static IReadOnlyList<ExpressionBodyTypeGroup> Build(Project project, Compilation compilation, CancellationToken cancellationToken)
    {
        ImmutableArray<Diagnostic> diagnostics = RoslynCodeStyleAnalyzers.Analyze(compilation, project.AnalyzerOptions, DiagnosticIds);
        var byType = new Dictionary<string, (INamedTypeSymbol Symbol, List<(Document Document, Diagnostic Diagnostic, MemberDeclarationSyntax Member, TypeDeclarationSyntax Type)> Candidates)>(StringComparer.Ordinal);
        foreach (Diagnostic diagnostic in diagnostics.Where(candidate => DiagnosticIds.Contains(candidate.Id, StringComparer.Ordinal)
            && candidate.Location.SourceTree is not null).OrderBy(candidate => candidate.Location.SourceTree!.FilePath, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Location.SourceSpan.Start))
        {
            cancellationToken.ThrowIfCancellationRequested();
            SyntaxTree tree = diagnostic.Location.SourceTree!;
            Document? document = project.Documents.FirstOrDefault(candidate => candidate.FilePath == tree.FilePath);
            if (document is null) { continue; }
            SyntaxNode root = tree.GetRoot(cancellationToken);
            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            MemberDeclarationSyntax? member = node.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            TypeDeclarationSyntax? type = node.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (member is null || type is null) { continue; }
            INamedTypeSymbol? symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(type, cancellationToken) as INamedTypeSymbol;
            if (symbol is null) { continue; }
            string typeId = symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!byType.TryGetValue(typeId, out var entry))
            {
                entry = (symbol, []);
                byType.Add(typeId, entry);
            }
            entry.Candidates.Add((document, diagnostic, member, type));
        }

        var groups = new List<ExpressionBodyTypeGroup>();
        foreach ((string typeId, (INamedTypeSymbol symbol, List<(Document Document, Diagnostic Diagnostic, MemberDeclarationSyntax Member, TypeDeclarationSyntax Type)> candidates)) in byType)
        {
            var occurrences = new List<ExpressionBodyOccurrence>();
            var duplicateOrdinals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach ((Document document, Diagnostic diagnostic, MemberDeclarationSyntax member, TypeDeclarationSyntax type) in candidates
                .OrderBy(candidate => candidate.Member.SyntaxTree.FilePath, StringComparer.Ordinal).ThenBy(candidate => candidate.Member.SpanStart))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string path = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
                string memberText = member.WithoutTrivia().ToFullString();
                string ordinalKey = diagnostic.Id + "\0" + memberText;
                int ordinal = duplicateOrdinals.TryGetValue(ordinalKey, out int previous) ? previous + 1 : 0;
                duplicateOrdinals[ordinalKey] = ordinal;
                string identity = StableId(path + "\0" + typeId + "\0" + diagnostic.Id + "\0" + memberText + "\0" + ordinal);
                SemanticModel model = compilation.GetSemanticModel(member.SyntaxTree);
                string owner = model.GetDeclaredSymbol(type, cancellationToken)?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? symbol.Name;
                RoslynCodeFixPreview? preview = RoslynCodeStyleAnalyzers.PreviewCodeFixAsync(document, diagnostic, cancellationToken)
                    .GetAwaiter().GetResult();
                string originalDocument = document.GetTextAsync(cancellationToken).GetAwaiter().GetResult().ToString();
                if (preview is null || string.Equals(preview.Text, originalDocument, StringComparison.Ordinal)) { continue; }
                FileLinePositionSpan lineSpan = member.GetLocation().GetLineSpan();
                string context = string.Join("\n\n", symbol.DeclaringSyntaxReferences
                    .Select(reference => reference.GetSyntax(cancellationToken).ToString())
                    .OrderBy(text => text, StringComparer.Ordinal));
                occurrences.Add(new ExpressionBodyOccurrence(identity, diagnostic.Id, path, lineSpan.StartLinePosition.Line + 1,
                    owner, member.ToFullString(), context, preview.Title, preview.Text, Question));
            }

            if (occurrences.Count == 0) { continue; }
            ExpressionBodyOccurrence[] bounded = occurrences.Take(MaximumOccurrencesPerType).ToArray();
            groups.Add(new ExpressionBodyTypeGroup(typeId, symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                occurrences.Count, bounded, occurrences.Count - bounded.Length));
        }
        return groups.OrderBy(group => group.TypeId, StringComparer.Ordinal).ToArray();
    }

    private static string StableId(string value) => "MO-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
}

internal sealed record ExpressionBodyTypeGroup(string TypeId, string TypeName, int TotalEligibleOccurrences,
    IReadOnlyList<ExpressionBodyOccurrence> Occurrences, int RemainingOccurrences);

internal sealed record ExpressionBodyOccurrence(string Id, string DiagnosticId, string Path, int Line, string Owner,
    string OriginalSyntax, string Context, string PreviewTitle, string PreviewDocument, string Question);
