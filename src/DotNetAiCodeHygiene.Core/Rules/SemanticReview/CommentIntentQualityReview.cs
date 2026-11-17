using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class CommentIntentQualityReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "comments.intent.review";
    private static readonly Rule RuleDescriptor = new(RuleId, 2, "review-batch", "review-batch", "Review ordinary active-source comments for useful, accurate intent in context.", true);
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 36;
    public IReadOnlyList<ReviewQuestion> Questions { get; } = [new("Q1", "Does the comment convey accurate, useful rationale or meaning without narrating syntax?")];
    public string EscalationCondition => "Legal, administrative and other legitimate comments remain eligible. Escalate a material concern or uncertainty to frontier; do not require comments or grade prose mechanically.";
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
            SyntaxTrivia[] comments = root.DescendantTrivia(descendIntoTrivia: false)
                .Where(trivia => (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)) && !trivia.IsPartOfStructuredTrivia())
                .OrderBy(trivia => trivia.SpanStart).ToArray();
            for (int index = 0; index < comments.Length;)
            {
                var group = new List<SyntaxTrivia> { comments[index++] };
                while (index < comments.Length && group[^1].IsKind(SyntaxKind.SingleLineCommentTrivia) && comments[index].IsKind(SyntaxKind.SingleLineCommentTrivia)
                    && Standalone(source, group[^1]) && Standalone(source, comments[index]) && OnlyWhitespaceOrLineBreaks(source, group[^1].Span.End, comments[index].SpanStart))
                {
                    group.Add(comments[index++]);
                }
                SyntaxTrivia first = group[0], last = group[^1];
                SyntaxNode? owner = first.Token.Parent?.AncestorsAndSelf().FirstOrDefault(node => node is MemberDeclarationSyntax or LocalFunctionStatementSyntax);
                ISymbol? symbol = owner switch
                {
                    MemberDeclarationSyntax member => context.SemanticModel(document).GetDeclaredSymbol(member),
                    LocalFunctionStatementSyntax local => context.SemanticModel(document).GetDeclaredSymbol(local),
                    _ => null
                };
                string ownerName = symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? path;
                SyntaxNode? contextNode = owner ?? first.Token.Parent?.AncestorsAndSelf().FirstOrDefault(node => node is CompilationUnitSyntax);
                string codeContext = contextNode is null ? "" : string.Join("\0", contextNode.DescendantTokens(descendIntoTrivia: false).Select(token => token.Text));
                string text = string.Join("\n", group.Select(trivia => trivia.ToFullString()));
                bool leading = Standalone(source, first);
                string stableOwner = context.ProjectIdentity(document) + "\0" + path + "\0" + ownerName;
                SyntaxNode? anchorNode = first.Token.Parent?.AncestorsAndSelf().FirstOrDefault(node => node is StatementSyntax or MemberDeclarationSyntax or LocalFunctionStatementSyntax);
                string anchor = anchorNode?.Kind().ToString() + ":" + (anchorNode is null ? "" : Normalize(anchorNode.WithoutTrivia().ToFullString()));
                string identity = stableOwner + "\0comment:" + (leading ? "leading" : "trailing") + ":" + anchor + ":" + Normalize(text);
                int duplicate = subjects.Count(subject => subject.Identity == identity || subject.Identity.StartsWith(identity + "\0duplicate:", StringComparison.Ordinal));
                if (duplicate > 0) { identity += "\0duplicate:" + duplicate.ToString(System.Globalization.CultureInfo.InvariantCulture); }
                var position = context.Tree(document).GetLineSpan(new Microsoft.CodeAnalysis.Text.TextSpan(first.SpanStart, 0)).StartLinePosition;
                int endLine = context.Tree(document).GetLineSpan(new Microsoft.CodeAnalysis.Text.TextSpan(last.Span.End, 0)).EndLinePosition.Line + 1;
                var item = new ReviewItem("", path, position.Line + 1, position.Character + 1, ownerName, "", symbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "");
                subjects.Add(new ReviewSubject(identity, text + "\0" + codeContext, item,
                    DirectChange: context.HasChangedLine(path, position.Line + 1, endLine),
                    ObservedSignal: "Active Roslyn source trivia contains an ordinary line or block comment group.",
                    LegitimateCase: "Legal, license, administrative, and other valid non-code explanatory comments remain eligible; a comment is not defective merely because it does not explain implementation.",
                    PrimaryAnchor: new ReviewAnchor(context.ProjectIdentity(document), path, position.Line + 1, position.Character + 1, ownerName, leading ? "leading-comment-group" : "trailing-comment")));
                sources.TryAdd(path, source);
            }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }

    private static bool Standalone(string source, SyntaxTrivia trivia)
    {
        int start = source.LastIndexOf('\n', Math.Max(0, trivia.SpanStart - 1)) + 1;
        return source.AsSpan(start, trivia.SpanStart - start).Trim().IsEmpty;
    }
    private static bool OnlyWhitespaceOrLineBreaks(string source, int start, int end)
    {
        if (start > end) { return false; }
        string between = source[start..end];
        return between.All(char.IsWhiteSpace) && between.Count(character => character == '\n') <= 1;
    }
    private static string Normalize(string value) => string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
