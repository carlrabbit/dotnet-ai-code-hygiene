using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Builds method-scoped switch-expression candidates from IDE0066.</summary>
internal static class SwitchExpressionCandidatePopulation
{
    internal const int MaximumOccurrencesPerMember = 12;
    internal const string Question = "Would a switch expression make this decision clearer with its enclosing member behavior visible? Keep the statement form when it communicates control flow better; mark uncertain when context is insufficient.";

    internal static IReadOnlyList<SwitchExpressionMemberGroup> Build(Project project, Compilation compilation, CancellationToken cancellationToken, bool exhaustive = false)
    {
        ImmutableArray<Diagnostic> diagnostics = RoslynCodeStyleAnalyzers.Analyze(compilation, project.AnalyzerOptions, "IDE0066");
        var byMember = new Dictionary<string, (Document Document, string Owner, string Context, List<(Diagnostic Diagnostic, SwitchStatementSyntax Switch)> Candidates)>(StringComparer.Ordinal);
        foreach (Diagnostic diagnostic in diagnostics.Where(candidate => candidate.Id == "IDE0066" && candidate.Location.SourceTree is not null)
            .OrderBy(candidate => candidate.Location.SourceTree!.FilePath, StringComparer.Ordinal).ThenBy(candidate => candidate.Location.SourceSpan.Start))
        {
            cancellationToken.ThrowIfCancellationRequested();
            SyntaxTree tree = diagnostic.Location.SourceTree!;
            Document? document = project.Documents.FirstOrDefault(candidate => candidate.FilePath == tree.FilePath);
            if (document is null || !RuleContext.IsEligibleSourceDocument(document)) { continue; }
            SyntaxNode root = tree.GetRoot(cancellationToken);
            SwitchStatementSyntax? switchStatement = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .AncestorsAndSelf().OfType<SwitchStatementSyntax>().FirstOrDefault();
            MemberDeclarationSyntax? member = switchStatement?.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            if (switchStatement is null || member is null) { continue; }
            SemanticModel model = compilation.GetSemanticModel(tree);
            string owner = model.GetDeclaredSymbol(member, cancellationToken)?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? member.Kind().ToString();
            string memberId = model.GetDeclaredSymbol(member, cancellationToken)?.GetDocumentationCommentId() ?? owner + "\0" + tree.FilePath;
            string context = member.ToString();
            if (!byMember.TryGetValue(memberId, out var entry))
            {
                entry = (document, owner, context, []);
                byMember.Add(memberId, entry);
            }
            entry.Candidates.Add((diagnostic, switchStatement));
        }

        var groups = new List<SwitchExpressionMemberGroup>();
        foreach ((string memberId, (Document document, string owner, string context, List<(Diagnostic Diagnostic, SwitchStatementSyntax Switch)> candidates)) in byMember)
        {
            var occurrences = new List<SwitchExpressionOccurrence>();
            var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach ((Diagnostic diagnostic, SwitchStatementSyntax switchStatement) in candidates.OrderBy(candidate => candidate.Switch.SpanStart))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string path = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
                string original = switchStatement.ToFullString();
                int ordinal = ordinals.TryGetValue(original, out int previous) ? previous + 1 : 0;
                ordinals[original] = ordinal;
                string id = StableId(path + "\0" + memberId + "\0" + original + "\0" + ordinal);
                RoslynCodeFixPreview? preview = RoslynCodeStyleAnalyzers.PreviewCodeFixAsync(document, diagnostic, cancellationToken)
                    .GetAwaiter().GetResult();
                string originalDocument = document.GetTextAsync(cancellationToken).GetAwaiter().GetResult().ToString();
                if (preview is null || string.Equals(preview.Text, originalDocument, StringComparison.Ordinal)) { continue; }
                FileLinePositionSpan lineSpan = switchStatement.GetLocation().GetLineSpan();
                occurrences.Add(new SwitchExpressionOccurrence(id, path, lineSpan.StartLinePosition.Line + 1, owner,
                    original, context, preview.Title, preview.Text, Question));
            }

            if (occurrences.Count == 0) { continue; }
            SwitchExpressionOccurrence[] bounded = exhaustive ? occurrences.ToArray() : occurrences.Take(MaximumOccurrencesPerMember).ToArray();
            string groupPath = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
            groups.Add(new SwitchExpressionMemberGroup(memberId, groupPath, owner, occurrences.Count, bounded, occurrences.Count - bounded.Length));
        }
        return groups.OrderBy(group => group.Path, StringComparer.Ordinal).ThenBy(group => group.MemberId, StringComparer.Ordinal).ToArray();
    }

    private static string StableId(string value) => "MO-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
}

internal sealed record SwitchExpressionMemberGroup(string MemberId, string Path, string Owner, int TotalEligibleOccurrences,
    IReadOnlyList<SwitchExpressionOccurrence> Occurrences, int RemainingOccurrences);

internal sealed record SwitchExpressionOccurrence(string Id, string Path, int Line, string Owner, string OriginalSyntax,
    string Context, string PreviewTitle, string PreviewDocument, string Question);
