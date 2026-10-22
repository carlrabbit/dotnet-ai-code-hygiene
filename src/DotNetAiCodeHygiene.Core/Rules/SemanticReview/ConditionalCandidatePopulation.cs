using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Builds method-scoped conditional conversion candidates from IDE0045 and IDE0046.</summary>
internal static class ConditionalCandidatePopulation
{
    internal const int MaximumOccurrencesPerMember = 12;
    internal const string Question = "Would converting between the conditional expression and if form clarify this control flow? Keep the current form when it better communicates branching or evaluation order; mark uncertain when context is insufficient.";
    private static readonly string[] DiagnosticIds = ["IDE0045", "IDE0046"];

    internal static IReadOnlyList<ConditionalMemberGroup> Build(Project project, Compilation compilation, CancellationToken cancellationToken, bool exhaustive = false)
    {
        ImmutableArray<Diagnostic> diagnostics = RoslynCodeStyleAnalyzers.Analyze(compilation, project.AnalyzerOptions, DiagnosticIds);
        var byMember = new Dictionary<string, (Document Document, string Owner, string Context, List<(Diagnostic Diagnostic, SyntaxNode Candidate)> Candidates)>(StringComparer.Ordinal);
        foreach (Diagnostic diagnostic in diagnostics.Where(candidate => DiagnosticIds.Contains(candidate.Id, StringComparer.Ordinal)
            && candidate.Location.SourceTree is not null).OrderBy(candidate => candidate.Location.SourceTree!.FilePath, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Location.SourceSpan.Start))
        {
            cancellationToken.ThrowIfCancellationRequested();
            SyntaxTree tree = diagnostic.Location.SourceTree!;
            Document? document = project.Documents.FirstOrDefault(candidate => candidate.FilePath == tree.FilePath);
            if (document is null || !RuleContext.IsEligibleSourceDocument(document)) { continue; }
            SyntaxNode root = tree.GetRoot(cancellationToken);
            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            SyntaxNode? candidateNode = node.AncestorsAndSelf().FirstOrDefault(ancestor => ancestor is IfStatementSyntax or ConditionalExpressionSyntax);
            MemberDeclarationSyntax? member = candidateNode?.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            if (candidateNode is null || member is null) { continue; }
            SemanticModel model = compilation.GetSemanticModel(tree);
            ISymbol? memberSymbol = model.GetDeclaredSymbol(member, cancellationToken);
            string owner = memberSymbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? member.Kind().ToString();
            string memberId = memberSymbol?.GetDocumentationCommentId() ?? owner + "\0" + tree.FilePath;
            if (!byMember.TryGetValue(memberId, out var entry))
            {
                entry = (document, owner, member.ToString(), []);
                byMember.Add(memberId, entry);
            }
            entry.Candidates.Add((diagnostic, candidateNode));
        }

        var groups = new List<ConditionalMemberGroup>();
        foreach ((string memberId, (Document document, string owner, string context, List<(Diagnostic Diagnostic, SyntaxNode Candidate)> candidates)) in byMember)
        {
            var occurrences = new List<ConditionalOccurrence>();
            var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
            var seenEquivalentFixes = new HashSet<string>(StringComparer.Ordinal);
            foreach ((Diagnostic diagnostic, SyntaxNode candidate) in candidates.OrderBy(item => item.Candidate.SpanStart))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string path = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
                string original = candidate.ToFullString();
                int ordinal = ordinals.TryGetValue(original, out int previous) ? previous + 1 : 0;
                ordinals[original] = ordinal;
                string id = StableId(path + "\0" + memberId + "\0" + diagnostic.Id + "\0" + original + "\0" + ordinal);
                RoslynCodeFixPreview? preview = RoslynCodeStyleAnalyzers.PreviewCodeFixAsync(document, diagnostic, cancellationToken)
                    .GetAwaiter().GetResult();
                string originalDocument = document.GetTextAsync(cancellationToken).GetAwaiter().GetResult().ToString();
                if (preview is null || string.Equals(preview.Text, originalDocument, StringComparison.Ordinal)) { continue; }
                string fixKey = candidate.SpanStart.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0"
                    + candidate.Span.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0" + preview.Text;
                if (!seenEquivalentFixes.Add(fixKey)) { continue; }
                FileLinePositionSpan lineSpan = candidate.GetLocation().GetLineSpan();
                occurrences.Add(new ConditionalOccurrence(id, diagnostic.Id, path, lineSpan.StartLinePosition.Line + 1,
                    owner, original, context, preview.Title, preview.Text, Question));
            }
            if (occurrences.Count == 0) { continue; }
            ConditionalOccurrence[] bounded = exhaustive ? occurrences.ToArray() : occurrences.Take(MaximumOccurrencesPerMember).ToArray();
            string groupPath = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
            groups.Add(new ConditionalMemberGroup(memberId, groupPath, owner, occurrences.Count, bounded, occurrences.Count - bounded.Length));
        }
        return groups.OrderBy(group => group.Path, StringComparer.Ordinal).ThenBy(group => group.MemberId, StringComparer.Ordinal).ToArray();
    }

    private static string StableId(string value) => "MO-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
}

internal sealed record ConditionalMemberGroup(string MemberId, string Path, string Owner, int TotalEligibleOccurrences,
    IReadOnlyList<ConditionalOccurrence> Occurrences, int RemainingOccurrences);

internal sealed record ConditionalOccurrence(string Id, string DiagnosticId, string Path, int Line, string Owner,
    string OriginalSyntax, string Context, string PreviewTitle, string PreviewDocument, string Question);
