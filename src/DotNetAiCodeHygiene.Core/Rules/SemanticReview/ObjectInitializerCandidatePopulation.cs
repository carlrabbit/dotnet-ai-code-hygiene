using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Builds method-scoped object-initializer candidates from IDE0017.</summary>
internal static class ObjectInitializerCandidatePopulation
{
    internal const int MaximumOccurrencesPerMember = 12;
    internal const string Question = "Would moving these assignments into the object initializer clarify construction without obscuring evaluation order or side effects? Keep the existing sequence when it is clearer; mark uncertain when context is insufficient.";

    internal static IReadOnlyList<ObjectInitializerMemberGroup> Build(Project project, Compilation compilation, CancellationToken cancellationToken)
    {
        ImmutableArray<Diagnostic> diagnostics = RoslynCodeStyleAnalyzers.Analyze(compilation, project.AnalyzerOptions, "IDE0017");
        var byMember = new Dictionary<string, (Document Document, string Owner, string Context, List<(Diagnostic Diagnostic, SyntaxNode Candidate)> Candidates)>(StringComparer.Ordinal);
        foreach (Diagnostic diagnostic in diagnostics.Where(candidate => candidate.Id == "IDE0017" && candidate.Location.SourceTree is not null)
            .OrderBy(candidate => candidate.Location.SourceTree!.FilePath, StringComparer.Ordinal).ThenBy(candidate => candidate.Location.SourceSpan.Start))
        {
            cancellationToken.ThrowIfCancellationRequested();
            SyntaxTree tree = diagnostic.Location.SourceTree!;
            Document? document = project.Documents.FirstOrDefault(candidate => candidate.FilePath == tree.FilePath);
            if (document is null) { continue; }
            SyntaxNode root = tree.GetRoot(cancellationToken);
            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            SyntaxNode? candidateNode = node.AncestorsAndSelf().FirstOrDefault(ancestor => ancestor is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax);
            MemberDeclarationSyntax? member = candidateNode?.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            if (candidateNode is null || member is null) { continue; }
            SemanticModel model = compilation.GetSemanticModel(tree);
            ISymbol? symbol = model.GetDeclaredSymbol(member, cancellationToken);
            string owner = symbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? member.Kind().ToString();
            string memberId = symbol?.GetDocumentationCommentId() ?? owner + "\0" + tree.FilePath;
            if (!byMember.TryGetValue(memberId, out var entry))
            {
                entry = (document, owner, member.ToString(), []);
                byMember.Add(memberId, entry);
            }
            entry.Candidates.Add((diagnostic, candidateNode));
        }

        var groups = new List<ObjectInitializerMemberGroup>();
        foreach ((string memberId, (Document document, string owner, string context, List<(Diagnostic Diagnostic, SyntaxNode Candidate)> candidates)) in byMember)
        {
            var occurrences = new List<ObjectInitializerOccurrence>();
            var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
            var seenFixes = new HashSet<string>(StringComparer.Ordinal);
            foreach ((Diagnostic diagnostic, SyntaxNode candidate) in candidates.OrderBy(item => item.Candidate.SpanStart))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string path = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
                string original = candidate.ToFullString();
                int ordinal = ordinals.TryGetValue(original, out int previous) ? previous + 1 : 0;
                ordinals[original] = ordinal;
                string id = StableId(path + "\0" + memberId + "\0" + original + "\0" + ordinal);
                RoslynCodeFixPreview? preview = RoslynCodeStyleAnalyzers.PreviewCodeFixAsync(document, diagnostic, cancellationToken)
                    .GetAwaiter().GetResult();
                string originalDocument = document.GetTextAsync(cancellationToken).GetAwaiter().GetResult().ToString();
                if (preview is null || string.Equals(preview.Text, originalDocument, StringComparison.Ordinal)) { continue; }
                string fixKey = candidate.SpanStart.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0"
                    + candidate.Span.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0" + preview.Text;
                if (!seenFixes.Add(fixKey)) { continue; }
                FileLinePositionSpan lineSpan = candidate.GetLocation().GetLineSpan();
                occurrences.Add(new ObjectInitializerOccurrence(id, path, lineSpan.StartLinePosition.Line + 1,
                    owner, original, context, preview.Title, preview.Text, Question));
            }
            if (occurrences.Count == 0) { continue; }
            ObjectInitializerOccurrence[] bounded = occurrences.Take(MaximumOccurrencesPerMember).ToArray();
            string groupPath = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath!)!, document.FilePath!);
            groups.Add(new ObjectInitializerMemberGroup(memberId, groupPath, owner, occurrences.Count, bounded, occurrences.Count - bounded.Length));
        }
        return groups.OrderBy(group => group.Path, StringComparer.Ordinal).ThenBy(group => group.MemberId, StringComparer.Ordinal).ToArray();
    }

    private static string StableId(string value) => "MO-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
}

internal sealed record ObjectInitializerMemberGroup(string MemberId, string Path, string Owner, int TotalEligibleOccurrences,
    IReadOnlyList<ObjectInitializerOccurrence> Occurrences, int RemainingOccurrences);

internal sealed record ObjectInitializerOccurrence(string Id, string Path, int Line, string Owner,
    string OriginalSyntax, string Context, string PreviewTitle, string PreviewDocument, string Question);
