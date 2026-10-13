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
    internal const string EscalationText = "Comment purpose is contextual: ordinary legal and administrative comments remain eligible, and a comment is not defective only because it does not explain implementation behavior. Escalate a material concern or uncertainty to frontier. Do not require comments or score prose mechanically.";
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
                .OrderBy(t => t.SpanStart).ToArray();
            int index = 0;
            while (index < comments.Length)
            {
                var group = new List<SyntaxTrivia> { comments[index++] };
                while (index < comments.Length && group[^1].IsKind(SyntaxKind.SingleLineCommentTrivia)
                    && comments[index].IsKind(SyntaxKind.SingleLineCommentTrivia)
                    && IsStandaloneComment(source, group[^1]) && IsStandaloneComment(source, comments[index])
                    && OnlyWhitespaceOrLineBreaks(source, group[^1].Span.End, comments[index].SpanStart))
                {
                    group.Add(comments[index++]);
                }
                SyntaxTrivia first = group[0], last = group[^1];
                bool standalone = IsStandaloneComment(source, first);
                SyntaxNode? ownerNode = first.Token.Parent?.AncestorsAndSelf().FirstOrDefault(n => n is MemberDeclarationSyntax or LocalFunctionStatementSyntax);
                ISymbol? ownerSymbol = ownerNode switch
                {
                    MemberDeclarationSyntax member => context.SemanticModel(document).GetDeclaredSymbol(member),
                    LocalFunctionStatementSyntax local => context.SemanticModel(document).GetDeclaredSymbol(local),
                    _ => null
                };
                string owner = ownerSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? path;
                SyntaxNode? contextNode = ownerNode ?? first.Token.Parent?.AncestorsAndSelf().FirstOrDefault(n => n is CompilationUnitSyntax);
                string localContext = StableCodeContext(contextNode);
                string commentText = string.Join("\n", group.Select(t => t.ToFullString()));
                string semanticOwner = context.ProjectIdentity(document) + "\0" + path + "\0" + owner;
                SyntaxNode? syntaxAnchor = first.Token.Parent?.AncestorsAndSelf().FirstOrDefault(n => n is StatementSyntax or MemberDeclarationSyntax or LocalFunctionStatementSyntax);
                string anchorSyntax = StableSyntaxAnchor(context, document, syntaxAnchor);
                string identity = semanticOwner + "\0comment:" + (standalone ? "leading" : "trailing") + ":"
                    + anchorSyntax + ":" + NormalizeComment(commentText);
                // Truly identical blocks with the same owner and same adjacent syntax have no
                // stable distinguishing information without relying on source offsets. Keep
                // those indistinguishable duplicates deterministic; edits to other blocks do
                // not renumber subjects with distinct comment or syntax anchors.
                int duplicate = subjects.Count(s => string.Equals(s.Identity, identity, StringComparison.Ordinal)
                    || s.Identity.StartsWith(identity + "\0duplicate:", StringComparison.Ordinal));
                if (duplicate > 0) { identity += "\0duplicate:" + duplicate.ToString(System.Globalization.CultureInfo.InvariantCulture); }
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
    private static bool IsStandaloneComment(string source, SyntaxTrivia trivia)
    {
        int lineStart = source.LastIndexOf('\n', Math.Max(0, trivia.SpanStart - 1)) + 1;
        return source.AsSpan(lineStart, trivia.SpanStart - lineStart).Trim().IsEmpty;
    }
    private static string NormalizeComment(string comment) => string.Join(" ", comment.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    private static string NormalizeSyntax(string syntax) => string.Join(" ", syntax.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    private static string StableSyntaxAnchor(RuleContext context, Document document, SyntaxNode? node)
    {
        if (node is null) { return ""; }
        if (node is MemberDeclarationSyntax member)
        {
            ISymbol? symbol = context.SemanticModel(document).GetDeclaredSymbol(member);
            if (symbol is not null) { return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat); }
            if (member is FieldDeclarationSyntax field)
            {
                string[] fields = field.Declaration.Variables
                    .Select(variable => context.SemanticModel(document).GetDeclaredSymbol(variable))
                    .Where(fieldSymbol => fieldSymbol is not null)
                    .Select(fieldSymbol => fieldSymbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .OrderBy(name => name, StringComparer.Ordinal).ToArray();
                if (fields.Length > 0) { return string.Join("|", fields); }
            }
        }
        return node.Kind().ToString() + ":" + NormalizeSyntax(node.WithoutTrivia().ToFullString());
    }
    private static string StableCodeContext(SyntaxNode? node) => node is null
        ? ""
        : string.Join("\0", node.DescendantTokens(descendIntoTrivia: false).Select(token => token.Text));
}
