using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Removes null-forgiving operators only when Roslyn's pre-operator flow state is already not-null.</summary>
internal sealed class RedundantNullForgivingRuleModule : IRuleModule, INormalizeRemediationRule
{
    internal const string RuleId = "style.null-forgiving.redundant";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "none", "none",
        "Remove a null-forgiving operator only when nullable flow analysis proves the operand already not-null.", true, Diagnose: false);

    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;

    public Document Normalize(Document document, Compilation compilation)
    {
        SyntaxTree tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!;
        SemanticModel model = compilation.GetSemanticModel(tree);
        SyntaxNode root = tree.GetRoot();
        PostfixUnaryExpressionSyntax[] candidates = root.DescendantNodes().OfType<PostfixUnaryExpressionSyntax>()
            .Where(expression => expression.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            .Where(expression => model.GetTypeInfo(expression.Operand).Type is { TypeKind: not TypeKind.Error }
                && model.GetTypeInfo(expression.Operand).Nullability.FlowState == NullableFlowState.NotNull)
            .Where(expression => !expression.DescendantTrivia(descendIntoTrivia: true).Any(trivia => trivia.IsDirective
                || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.DisabledTextTrivia)))
            .Where(expression => !HasCommentOrDirective(expression.GetLeadingTrivia()) && !HasCommentOrDirective(expression.GetTrailingTrivia()))
            .ToArray();
        if (candidates.Length == 0) { return document; }

        SyntaxNode rewritten = root.ReplaceNodes(candidates, (original, _) => original.Operand.WithTriviaFrom(original));
        return document.WithSyntaxRoot(rewritten);
    }

    private static bool HasCommentOrDirective(SyntaxTriviaList trivia) => trivia.Any(item => item.IsDirective
        || item.IsKind(SyntaxKind.SingleLineCommentTrivia)
        || item.IsKind(SyntaxKind.MultiLineCommentTrivia)
        || item.IsKind(SyntaxKind.DisabledTextTrivia));
}
