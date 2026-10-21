using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Removes parentheses only around a single identifier or literal with identical Roslyn binding.</summary>
internal sealed class RedundantParenthesesRuleModule : IRuleModule, INormalizeRemediationRule
{
    internal const string RuleId = "style.parentheses.redundant";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "none", "none",
        "Remove only uncommented parentheses around a single identifier or literal when Roslyn confirms identical binding.", true, Diagnose: false);

    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;

    public Document Normalize(Document document, Compilation compilation)
    {
        SyntaxTree tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!;
        SemanticModel model = compilation.GetSemanticModel(tree);
        SyntaxNode root = tree.GetRoot();
        ParenthesizedExpressionSyntax[] candidates = root.DescendantNodes().OfType<ParenthesizedExpressionSyntax>()
            .Where(expression => IsSimplePrimary(expression.Expression))
            .Where(expression => SameBinding(expression, model))
            .Where(expression => !HasCommentOrDirective(expression.DescendantTrivia(descendIntoTrivia: true))
                && !HasCommentOrDirective(expression.GetLeadingTrivia())
                && !HasCommentOrDirective(expression.GetTrailingTrivia()))
            .ToArray();
        if (candidates.Length == 0) { return document; }

        SyntaxNode rewritten = root.ReplaceNodes(candidates, (original, _) => original.Expression.WithTriviaFrom(original));
        return document.WithSyntaxRoot(rewritten);
    }

    private static bool IsSimplePrimary(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax => true,
        LiteralExpressionSyntax literal when literal.Kind() is SyntaxKind.NumericLiteralExpression or SyntaxKind.StringLiteralExpression
            or SyntaxKind.CharacterLiteralExpression or SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression
            or SyntaxKind.NullLiteralExpression => true,
        ThisExpressionSyntax or BaseExpressionSyntax => true,
        _ => false
    };

    private static bool SameBinding(ParenthesizedExpressionSyntax expression, SemanticModel model)
    {
        TypeInfo outer = model.GetTypeInfo(expression);
        TypeInfo inner = model.GetTypeInfo(expression.Expression);
        return outer.Type is not null && inner.Type is not null
            && SymbolEqualityComparer.Default.Equals(outer.Type, inner.Type)
            && SymbolEqualityComparer.Default.Equals(outer.ConvertedType, inner.ConvertedType)
            && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(expression).Symbol, model.GetSymbolInfo(expression.Expression).Symbol);
    }

    private static bool HasCommentOrDirective(IEnumerable<SyntaxTrivia> trivia) => trivia.Any(item => item.IsDirective
        || item.IsKind(SyntaxKind.SingleLineCommentTrivia)
        || item.IsKind(SyntaxKind.MultiLineCommentTrivia)
        || item.IsKind(SyntaxKind.DisabledTextTrivia));
}
