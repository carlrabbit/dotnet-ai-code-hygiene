using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Builds method-scoped inline-declaration candidates from the concrete IDE0018 diagnostic.</summary>
internal static class InlineDeclarationCandidatePopulation
{
    internal const int MaximumOccurrencesPerMember = 12;
    internal const string Question = "Would declaring this local at its use site clarify its limited scope? Keep the separate declaration when it helps explain control flow or lifetime; mark uncertain when context is insufficient. This proposal covers IDE0018 inline declarations only, not general variable merging.";

    internal static IReadOnlyList<InlineDeclarationMemberGroup> Build(Project project, Compilation compilation, CancellationToken cancellationToken, bool exhaustive = false)
    {
        ImmutableArray<Diagnostic> diagnostics = RoslynCodeStyleAnalyzers.Analyze(compilation, project.AnalyzerOptions, "IDE0018");
        var byMember = new Dictionary<string, (Document Document, string Owner, string Context, List<(Diagnostic Diagnostic, LocalDeclarationStatementSyntax Declaration)> Candidates)>(StringComparer.Ordinal);
        foreach (Diagnostic diagnostic in diagnostics.Where(candidate => candidate.Id == "IDE0018" && candidate.Location.SourceTree is not null)
            .OrderBy(candidate => candidate.Location.SourceTree!.FilePath, StringComparer.Ordinal).ThenBy(candidate => candidate.Location.SourceSpan.Start))
        {
            cancellationToken.ThrowIfCancellationRequested();
            SyntaxTree tree = diagnostic.Location.SourceTree!;
            Document? document = project.Documents.FirstOrDefault(candidate => candidate.FilePath == tree.FilePath);
            if (document is null || !RuleContext.IsEligibleSourceDocument(document)) { continue; }
            SyntaxNode root = tree.GetRoot(cancellationToken);
            LocalDeclarationStatementSyntax? declaration = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
            MemberDeclarationSyntax? member = declaration?.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            if (declaration is null || member is null) { continue; }
            SemanticModel model = compilation.GetSemanticModel(tree);
            ISymbol? symbol = model.GetDeclaredSymbol(member, cancellationToken);
            string owner = symbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? member.Kind().ToString();
            string memberId = symbol?.GetDocumentationCommentId() ?? owner + "\0" + tree.FilePath;
            if (!byMember.TryGetValue(memberId, out var entry))
            {
                entry = (document, owner, member.ToString(), []);
                byMember.Add(memberId, entry);
            }
            entry.Candidates.Add((diagnostic, declaration));
        }

        var groups = new List<InlineDeclarationMemberGroup>();
        foreach ((string memberId, (Document document, string owner, string context, List<(Diagnostic Diagnostic, LocalDeclarationStatementSyntax Declaration)> candidates)) in byMember)
        {
            var occurrences = new List<InlineDeclarationOccurrence>();
            var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach ((Diagnostic diagnostic, LocalDeclarationStatementSyntax declaration) in candidates.OrderBy(candidate => candidate.Declaration.SpanStart))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string path = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
                string original = declaration.ToFullString();
                int ordinal = ordinals.TryGetValue(original, out int previous) ? previous + 1 : 0;
                ordinals[original] = ordinal;
                string id = StableId(path + "\0" + memberId + "\0" + original + "\0" + ordinal);
                RoslynCodeFixPreview? preview = RoslynCodeStyleAnalyzers.PreviewCodeFixAsync(document, diagnostic, cancellationToken)
                    .GetAwaiter().GetResult();
                string originalDocument = document.GetTextAsync(cancellationToken).GetAwaiter().GetResult().ToString();
                if (preview is null || string.Equals(preview.Text, originalDocument, StringComparison.Ordinal)) { continue; }
                FileLinePositionSpan lineSpan = declaration.GetLocation().GetLineSpan();
                occurrences.Add(new InlineDeclarationOccurrence(id, path, lineSpan.StartLinePosition.Line + 1,
                    owner, original, context, preview.Title, preview.Text, Question));
            }
            if (occurrences.Count == 0) { continue; }
            InlineDeclarationOccurrence[] bounded = exhaustive ? occurrences.ToArray() : occurrences.Take(MaximumOccurrencesPerMember).ToArray();
            string groupPath = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
            groups.Add(new InlineDeclarationMemberGroup(memberId, groupPath, owner, occurrences.Count, bounded, occurrences.Count - bounded.Length));
        }
        return groups.OrderBy(group => group.Path, StringComparer.Ordinal).ThenBy(group => group.MemberId, StringComparer.Ordinal).ToArray();
    }

    private static string StableId(string value) => "MO-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
}

internal sealed record InlineDeclarationMemberGroup(string MemberId, string Path, string Owner, int TotalEligibleOccurrences,
    IReadOnlyList<InlineDeclarationOccurrence> Occurrences, int RemainingOccurrences);

internal sealed record InlineDeclarationOccurrence(string Id, string Path, int Line, string Owner,
    string OriginalSyntax, string Context, string PreviewTitle, string PreviewDocument, string Question);
