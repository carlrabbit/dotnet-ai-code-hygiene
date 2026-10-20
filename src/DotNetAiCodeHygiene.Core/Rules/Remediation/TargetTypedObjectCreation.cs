using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>
/// Uses Roslyn's simplifier only when an explicitly declared local, field, or property
/// supplies the exact target type. Contextual expressions such as returns are excluded.
/// </summary>
internal sealed class TargetTypedObjectCreationRuleModule : IRuleModule, INormalizeRemediationRule
{
    internal const string RuleId = "style.object-creation.target-typed";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "none", "none",
        "Use target-typed object creation only when an explicit local, field, or property type is immediately visible.", true, Diagnose: false);

    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;

    public Document Normalize(Document document, Compilation compilation)
    {
        if (document.Project.ParseOptions is not CSharpParseOptions options || options.LanguageVersion < LanguageVersion.CSharp9)
        {
            return document;
        }

        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        SemanticModel model = compilation.GetSemanticModel(document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!);
        ObjectCreationExpressionSyntax[] candidates = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
            .Where(HasExplicitVisibleTarget)
            .Where(creation => TargetType(creation, model) is { } target
                && SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(creation).Type, target)
                && !creation.Type.DescendantTrivia(descendIntoTrivia: true).Any(trivia => trivia.IsDirective || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)))
            .ToArray();

        if (candidates.Length == 0)
        {
            return document;
        }

        SyntaxNode rewritten = root.ReplaceNodes(candidates, (original, _) =>
            SyntaxFactory.ImplicitObjectCreationExpression(original.NewKeyword, original.ArgumentList ?? SyntaxFactory.ArgumentList(), original.Initializer)
                .WithTriviaFrom(original));
        return document.WithSyntaxRoot(rewritten);
    }

    private static bool HasExplicitVisibleTarget(ObjectCreationExpressionSyntax creation) => creation.Parent switch
    {
        EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } }
            => !declaration.Type.IsVar,
        EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax property }
            => !property.Type.IsVar,
        _ => false
    } || creation.Parent is EqualsValueClauseSyntax
        {
            Parent: VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax }
        };

    private static ITypeSymbol? TargetType(ObjectCreationExpressionSyntax creation, SemanticModel model) => creation.Parent switch
    {
        EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variable }
            => (model.GetDeclaredSymbol(variable) as ILocalSymbol)?.Type
                ?? (model.GetDeclaredSymbol(variable) as IFieldSymbol)?.Type,
        EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax property }
            => (model.GetDeclaredSymbol(property) as IPropertySymbol)?.Type,
        _ => null
    };
}
