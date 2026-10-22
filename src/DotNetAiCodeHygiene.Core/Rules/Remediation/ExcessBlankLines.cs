using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Trims file-edge blank lines and caps only excessive unprotected blank-line runs.</summary>
internal sealed class ExcessBlankLinesRuleModule : IRuleModule, INormalizeRemediationRule
{
    internal const string RuleId = "style.whitespace.blank-lines";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "none", "none",
        "Remove file-edge blank lines and reduce excessive unprotected blank-line runs without changing comment or literal contents.", true, Diagnose: false);

    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;

    public Document Normalize(Document document, Compilation compilation)
    {
        SourceText text = document.GetTextAsync().GetAwaiter().GetResult();
        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        TextSpan[] protectedSpans = ProtectedSpans(root);
        var changes = new List<TextChange>();
        int lineIndex = 0;
        while (lineIndex < text.Lines.Count)
        {
            if (!IsBlank(text, text.Lines[lineIndex])) { lineIndex++; continue; }
            int first = lineIndex;
            while (lineIndex < text.Lines.Count && IsBlank(text, text.Lines[lineIndex])) { lineIndex++; }
            int count = lineIndex - first;
            if (Enumerable.Range(first, count).Any(index => protectedSpans.Any(span =>
                    span.Start <= text.Lines[index].Start && text.Lines[index].Start < span.End))) { continue; }

            if (first == 0)
            {
                int end = lineIndex < text.Lines.Count ? text.Lines[lineIndex].Start : text.Length;
                if (end > 0) { changes.Add(new TextChange(TextSpan.FromBounds(0, end), string.Empty)); }
            }
            else if (lineIndex == text.Lines.Count)
            {
                changes.Add(new TextChange(TextSpan.FromBounds(text.Lines[first].Start, text.Length), string.Empty));
            }
            else if (count > 2)
            {
                changes.Add(new TextChange(TextSpan.FromBounds(text.Lines[first + 2].Start, text.Lines[lineIndex].Start), string.Empty));
            }
        }

        return changes.Count == 0 ? document : document.WithText(text.WithChanges(changes));
    }

    private static bool IsBlank(SourceText text, TextLine line) =>
        text.ToString(line.Span).All(char.IsWhiteSpace);

    private static TextSpan[] ProtectedSpans(SyntaxNode root)
    {
        IEnumerable<TextSpan> tokenSpans = root.DescendantTokens(descendIntoTrivia: true)
            .Where(token => token.Text.Contains('\n') || token.Text.Contains('\r')
                || token.Parent?.AncestorsAndSelf().Any(node => node is InterpolatedStringExpressionSyntax
                    || node is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression)) == true)
            .Select(token => token.Span);
        IEnumerable<TextSpan> triviaSpans = root.DescendantTrivia(descendIntoTrivia: true)
            .Where(trivia => trivia.IsDirective || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia) || trivia.IsKind(SyntaxKind.DisabledTextTrivia))
            .Select(trivia => trivia.Span);
        return tokenSpans.Concat(triviaSpans).ToArray();
    }
}
