using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Shortens default(T) only when the immediate declaration already states the same type.</summary>
internal sealed class DefaultLiteralRuleModule : IRuleModule, INormalizeRemediationRule
{
    internal const string RuleId = "style.default.literal";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "none", "none",
        "Use default where an immediately enclosing explicit declaration already supplies the same type.", true, Diagnose: false);
    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;

    public Document Normalize(Document document, Compilation compilation)
    {
        if (document.Project.ParseOptions is not CSharpParseOptions options || options.LanguageVersion < LanguageVersion.CSharp7_1)
        {
            return document;
        }

        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        SemanticModel model = compilation.GetSemanticModel(document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!);
        DefaultExpressionSyntax[] candidates = root.DescendantNodes().OfType<DefaultExpressionSyntax>()
            .Where(expression => expression.Parent is EqualsValueClauseSyntax)
            .Where(expression => TargetType(expression, model) is { } target
                && SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(expression).Type, target)
                && !expression.Type.DescendantTrivia(descendIntoTrivia: true).Any(trivia => trivia.IsDirective
                    || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)))
            .ToArray();
        if (candidates.Length == 0) { return document; }

        SyntaxNode rewritten = root.ReplaceNodes(candidates, (original, _) =>
            SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression).WithTriviaFrom(original));
        return document.WithSyntaxRoot(rewritten);
    }

    private static ITypeSymbol? TargetType(DefaultExpressionSyntax expression, SemanticModel model) => expression.Parent?.Parent switch
    {
        VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } when !declaration.Type.IsVar
            => (model.GetDeclaredSymbol((VariableDeclaratorSyntax)expression.Parent.Parent) as ILocalSymbol)?.Type
                ?? (model.GetDeclaredSymbol((VariableDeclaratorSyntax)expression.Parent.Parent) as IFieldSymbol)?.Type,
        PropertyDeclarationSyntax property => (model.GetDeclaredSymbol(property) as IPropertySymbol)?.Type,
        _ => null
    };
}
