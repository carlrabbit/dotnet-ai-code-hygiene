using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Removes explicit tuple/anonymous-member names only when an identifier infers the identical name.</summary>
internal sealed class InferredNamesRuleModule : IRuleModule, INormalizeRemediationRule
{
    internal const string RuleId = "style.names.inferred";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "none", "none",
        "Omit tuple and anonymous-object member names only when the identifier expression infers the same name.", true, Diagnose: false);
    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;

    public Document Normalize(Document document, Compilation compilation)
    {
        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        ArgumentSyntax[] tuples = root.DescendantNodes().OfType<TupleExpressionSyntax>()
            .SelectMany(tuple => tuple.Arguments)
            .Where(argument => argument.NameColon is not null && argument.Expression is IdentifierNameSyntax identifier
                && StringComparer.Ordinal.Equals(argument.NameColon.Name.Identifier.ValueText, identifier.Identifier.ValueText)
                && !HasCommentOrDirective(argument))
            .ToArray();
        AnonymousObjectMemberDeclaratorSyntax[] members = root.DescendantNodes().OfType<AnonymousObjectMemberDeclaratorSyntax>()
            .Where(member => member.NameEquals is { } name && member.Expression is IdentifierNameSyntax identifier
                && StringComparer.Ordinal.Equals(name.Name.Identifier.ValueText, identifier.Identifier.ValueText)
                && !HasCommentOrDirective(member))
            .ToArray();
        if (tuples.Length == 0 && members.Length == 0) { return document; }

        SyntaxNode rewritten = root.ReplaceNodes(tuples.Cast<SyntaxNode>().Concat(members), (original, _) => original switch
        {
            ArgumentSyntax argument => SyntaxFactory.Argument(argument.Expression.WithTriviaFrom(argument.NameColon!)).WithTriviaFrom(argument),
            AnonymousObjectMemberDeclaratorSyntax member => member.WithNameEquals(null),
            _ => original
        });
        return document.WithSyntaxRoot(rewritten);
    }

    private static bool HasCommentOrDirective(SyntaxNode node) => node.GetLeadingTrivia().Concat(node.GetTrailingTrivia())
        .Concat(node.DescendantTrivia(descendIntoTrivia: true)).Any(trivia => trivia.IsDirective
            || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia));
}
