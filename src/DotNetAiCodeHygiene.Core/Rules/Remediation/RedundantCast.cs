using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Removes only casts whose operand already has the exact annotated target type and identity conversion.</summary>
internal sealed class RedundantCastRuleModule : IRuleModule, INormalizeRemediationRule
{
    internal const string RuleId = "style.cast.redundant";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "none", "none",
        "Remove a cast only when the operand already has the exact cast type and Roslyn binds an identity conversion.", true, Diagnose: false);
    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;

    public Document Normalize(Document document, Compilation compilation)
    {
        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        SyntaxTree tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!;
        SemanticModel model = compilation.GetSemanticModel(tree);
        CastExpressionSyntax[] candidates = root.DescendantNodes().OfType<CastExpressionSyntax>()
            .Where(cast => model.GetConversion(cast).IsIdentity
                && SymbolEqualityComparer.IncludeNullability.Equals(model.GetTypeInfo(cast.Type).Type, model.GetTypeInfo(cast.Expression).Type)
                && !HasCommentOrDirective(cast.Parent ?? cast))
            .ToArray();
        if (candidates.Length == 0) { return document; }

        SyntaxNode rewritten = root.ReplaceNodes(candidates, (original, _) =>
            SyntaxFactory.ParenthesizedExpression(original.Expression.WithoutTrivia())
                .WithTriviaFrom(original)
                .WithAdditionalAnnotations(Simplifier.Annotation));
        return Simplifier.ReduceAsync(document.WithSyntaxRoot(rewritten), Simplifier.Annotation).GetAwaiter().GetResult();
    }

    private static bool HasCommentOrDirective(SyntaxNode node) => node.GetLeadingTrivia().Concat(node.GetTrailingTrivia())
        .Concat(node.DescendantTrivia(descendIntoTrivia: true)).Any(trivia => trivia.IsDirective
            || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia));
}
