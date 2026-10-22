using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Orders declaration modifiers using the effective IDE0036 option.</summary>
internal sealed class OrganizeModifiersRuleModule : IRuleModule, INormalizeRemediationRule
{
    internal const string RuleId = "style.modifiers.organize";
    private const string DefaultOrder = "public,private,protected,internal,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,volatile,async";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "none", "none",
        "Order declaration modifiers using csharp_preferred_modifier_order while retaining comment and directive boundaries.", true, Diagnose: false);

    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;

    public Document Normalize(Document document, Compilation compilation)
    {
        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        SyntaxTree tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!;
        AnalyzerConfigOptions options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);
        string[] order = ParseOrder(options);
        var rank = order.Select((modifier, index) => (modifier, index))
            .ToDictionary(pair => pair.modifier, pair => pair.index, StringComparer.Ordinal);
        MemberDeclarationSyntax[] members = root.DescendantNodesAndSelf().OfType<MemberDeclarationSyntax>()
            .Where(member => Modifiers(member).Count > 1)
            .Where(member => CanOrder(Modifiers(member), rank))
            .ToArray();
        if (members.Length == 0) { return document; }
        SyntaxNode rewritten = root.ReplaceNodes(members, (original, rewrittenMember) =>
        {
            SyntaxTokenList current = Modifiers(rewrittenMember);
            SyntaxTokenList sorted = SyntaxFactory.TokenList(current
                .Select((token, index) => (token, index))
                .OrderBy(pair => rank[pair.token.ValueText])
                .ThenBy(pair => pair.index)
                .Select(pair => pair.token));
            return WithModifiers(rewrittenMember, sorted);
        });
        return rewritten.ToFullString() == root.ToFullString() ? document : document.WithSyntaxRoot(rewritten);
    }

    private static string[] ParseOrder(AnalyzerConfigOptions options)
    {
        if (!options.TryGetValue("csharp_preferred_modifier_order", out string? value) || string.IsNullOrWhiteSpace(value))
        {
            value = DefaultOrder;
        }
        string[] order = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return order.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static bool CanOrder(SyntaxTokenList modifiers, IReadOnlyDictionary<string, int> rank)
    {
        if (modifiers.Any(token => !rank.ContainsKey(token.ValueText))) { return false; }
        return !modifiers.SelectMany(token => token.LeadingTrivia.Concat(token.TrailingTrivia))
            .Any(trivia => trivia.IsDirective || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.DisabledTextTrivia));
    }

    private static SyntaxTokenList Modifiers(MemberDeclarationSyntax member) => member switch
    {
        BaseTypeDeclarationSyntax value => value.Modifiers,
        DelegateDeclarationSyntax value => value.Modifiers,
        FieldDeclarationSyntax value => value.Modifiers,
        EventFieldDeclarationSyntax value => value.Modifiers,
        EventDeclarationSyntax value => value.Modifiers,
        MethodDeclarationSyntax value => value.Modifiers,
        PropertyDeclarationSyntax value => value.Modifiers,
        IndexerDeclarationSyntax value => value.Modifiers,
        ConstructorDeclarationSyntax value => value.Modifiers,
        DestructorDeclarationSyntax value => value.Modifiers,
        OperatorDeclarationSyntax value => value.Modifiers,
        ConversionOperatorDeclarationSyntax value => value.Modifiers,
        _ => default
    };

    private static MemberDeclarationSyntax WithModifiers(MemberDeclarationSyntax member, SyntaxTokenList modifiers) => member switch
    {
        ClassDeclarationSyntax value => value.WithModifiers(modifiers),
        StructDeclarationSyntax value => value.WithModifiers(modifiers),
        InterfaceDeclarationSyntax value => value.WithModifiers(modifiers),
        RecordDeclarationSyntax value => value.WithModifiers(modifiers),
        EnumDeclarationSyntax value => value.WithModifiers(modifiers),
        DelegateDeclarationSyntax value => value.WithModifiers(modifiers),
        FieldDeclarationSyntax value => value.WithModifiers(modifiers),
        EventFieldDeclarationSyntax value => value.WithModifiers(modifiers),
        EventDeclarationSyntax value => value.WithModifiers(modifiers),
        MethodDeclarationSyntax value => value.WithModifiers(modifiers),
        PropertyDeclarationSyntax value => value.WithModifiers(modifiers),
        IndexerDeclarationSyntax value => value.WithModifiers(modifiers),
        ConstructorDeclarationSyntax value => value.WithModifiers(modifiers),
        DestructorDeclarationSyntax value => value.WithModifiers(modifiers),
        OperatorDeclarationSyntax value => value.WithModifiers(modifiers),
        ConversionOperatorDeclarationSyntax value => value.WithModifiers(modifiers),
        _ => member
    };
}
