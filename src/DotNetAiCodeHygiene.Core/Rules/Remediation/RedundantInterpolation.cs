using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Removes interpolation syntax only from constant strings that contain no interpolation holes.</summary>
internal sealed class RedundantInterpolationRuleModule : IRuleModule, INormalizeRemediationRule
{
    internal const string RuleId = "style.interpolation.redundant";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "none", "none",
        "Remove interpolation syntax from a constant interpolated string that contains no interpolation holes.", true, Diagnose: false);
    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;

    public Document Normalize(Document document, Compilation compilation)
    {
        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        SemanticModel model = compilation.GetSemanticModel(document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!);
        InterpolatedStringExpressionSyntax[] candidates = root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>()
            .Where(expression => expression.Contents.All(content => content is InterpolatedStringTextSyntax)
                && !expression.StringStartToken.Text.Contains("\"\"\"", StringComparison.Ordinal)
                && model.GetConstantValue(expression) is { HasValue: true, Value: string value }
                && !value.Contains('\r') && !value.Contains('\n')
                && !HasCommentOrDirective(expression.Parent ?? expression))
            .ToArray();
        if (candidates.Length == 0) { return document; }

        SyntaxNode rewritten = root.ReplaceNodes(candidates, (original, _) =>
        {
            string value = model.GetConstantValue(original).Value as string ?? string.Empty;
            SyntaxToken literal = SyntaxFactory.Literal(value);
            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, literal).WithTriviaFrom(original);
        });
        return document.WithSyntaxRoot(rewritten);
    }

    private static bool HasCommentOrDirective(SyntaxNode node) => node.GetLeadingTrivia().Concat(node.GetTrailingTrivia())
        .Concat(node.DescendantTrivia(descendIntoTrivia: true)).Any(trivia => trivia.IsDirective
            || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia));
}
