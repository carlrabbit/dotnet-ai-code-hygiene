using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class CommentIntentReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "comments.intent.review";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review ordinary implementation comments for useful, accurate intent and rationale.", true);
    internal static readonly ReviewQuestion[] RuleQuestions =
    [
        new("Q1", "Intent: Does the comment provide useful non-obvious rationale, constraint or meaning rather than narrate nearby syntax?"),
        new("Q2", "Fidelity: Is the comment consistent with current implementation and relevant local context?"),
        new("Q3", "Clarity: Is the explanation sufficiently specific, scoped and concise to help a reader without redundant narrative?")
    ];
    internal const string EscalationText = "Escalate a material concern or uncertainty to frontier. Do not require comments or score prose mechanically.";
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 6;
    public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => EscalationText;
    public string EscalatedReviewerClass => "frontier";

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var subjects = new List<ReviewSubject>();
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            SyntaxNode root = context.Root(document);
            string path = context.RelativePath(document), source = context.Text(document).ToString();
            // Only ordinary trivia in active source is admitted. Descending into
            // structured directives/documentation or DisabledText would surface
            // comment-like contents that are not implementation comment blocks.
            var comments = root.DescendantTrivia(descendIntoTrivia: false)
                .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
                .Where(t => !t.IsPartOfStructuredTrivia())
                .Where(t => !IsLicenseHeader(source, t.SpanStart))
                .OrderBy(t => t.SpanStart).ToArray();
            int index = 0;
            while (index < comments.Length)
            {
                var group = new List<SyntaxTrivia> { comments[index++] };
                while (index < comments.Length && group[^1].IsKind(SyntaxKind.SingleLineCommentTrivia)
                    && comments[index].IsKind(SyntaxKind.SingleLineCommentTrivia)
                    && OnlyWhitespaceOrLineBreaks(source, group[^1].Span.End, comments[index].SpanStart))
                {
                    group.Add(comments[index++]);
                }
                SyntaxTrivia first = group[0], last = group[^1];
                SyntaxNode? ownerNode = first.Token.Parent?.AncestorsAndSelf().FirstOrDefault(n => n is MemberDeclarationSyntax or LocalFunctionStatementSyntax);
                ISymbol? ownerSymbol = ownerNode switch
                {
                    MemberDeclarationSyntax member => context.SemanticModel(document).GetDeclaredSymbol(member),
                    LocalFunctionStatementSyntax local => context.SemanticModel(document).GetDeclaredSymbol(local),
                    _ => null
                };
                string owner = ownerSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? path;
                SyntaxNode? contextNode = ownerNode ?? first.Token.Parent?.AncestorsAndSelf().FirstOrDefault(n => n is CompilationUnitSyntax);
                string localContext = contextNode?.WithoutLeadingTrivia().ToFullString() ?? "";
                string commentText = string.Join("\n", group.Select(t => t.ToFullString()));
                string semanticOwner = context.ProjectIdentity(document) + "\0" + path + "\0" + owner;
                string anchor = NormalizeComment(commentText);
                int duplicate = subjects.Count(s => s.Identity.StartsWith(semanticOwner + "\0comment:" + anchor + "\0", StringComparison.Ordinal));
                string identity = semanticOwner + "\0comment:" + anchor + "\0" + duplicate.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var position = context.Tree(document).GetLineSpan(new Microsoft.CodeAnalysis.Text.TextSpan(first.SpanStart, 0)).StartLinePosition;
                int endLine = context.Tree(document).GetLineSpan(new Microsoft.CodeAnalysis.Text.TextSpan(last.Span.End, 0)).EndLinePosition.Line + 1;
                var item = new ReviewItem("", path, position.Line + 1, position.Character + 1, owner, "", ownerSymbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "");
                subjects.Add(new ReviewSubject(identity, commentText + "\0" + localContext, item, DirectChange: context.HasChangedLine(path, position.Line + 1, endLine)));
                sources.TryAdd(path, source);
            }
        }
        var result = new RuleModuleResult([], subjects, sources);
        return result with { SelectedReviewSubjects = context.SelectGate(Descriptor, subjects), ReviewerClass = "implementer" };
    }

    private static bool OnlyWhitespaceOrLineBreaks(string text, int start, int end)
    {
        if (start > end) { return false; }
        string between = text[start..end];
        return between.All(char.IsWhiteSpace) && between.Count(character => character == '\n') <= 1;
    }
    private static bool IsLicenseHeader(string source, int offset)
    {
        int line = source.AsSpan(0, Math.Clamp(offset, 0, source.Length)).Count('\n') + 1;
        if (line > 10) { return false; }
        string header = string.Join("\n", source.Split('\n').Take(10));
        return header.Contains("copyright", StringComparison.OrdinalIgnoreCase) || header.Contains("license", StringComparison.OrdinalIgnoreCase);
    }
    private static string NormalizeComment(string comment) => string.Join(" ", comment.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
