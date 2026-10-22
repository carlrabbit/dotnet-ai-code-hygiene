using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace DotNetAiCodeHygiene.Core;

internal sealed class AccessibilityStyleRuleModule : IRuleModule, IEditorConfigProjectionRule, INormalizeRemediationRule
{
    internal const string RuleId = "style.accessibility.explicit";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "finding", "finding", "Require explicit accessibility through the built-in IDE0040 policy.");
    public Rule Descriptor => RuleDescriptor;
    public IReadOnlyList<KeyValuePair<string, string>> EditorConfigEntries { get; } =
    [new("dotnet_style_require_accessibility_modifiers", "always"), new("dotnet_diagnostic.IDE0040.severity", "error")];
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.FindingsOnly(ProfileAnalysisRuleModule.Materialize(
        context.ProfileManager.AnalyzeEditorConfigRule(Descriptor.Id, EditorConfigEntries), Descriptor));

    public Document Normalize(Document document, Compilation compilation)
    {
        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        MemberDeclarationSyntax[] candidates = root.DescendantNodes().OfType<MemberDeclarationSyntax>()
            .Where(NeedsAccessibility)
            .ToArray();
        if (candidates.Length == 0) { return document; }

        SyntaxNode rewritten = root.ReplaceNodes(candidates, (original, current) => AddAccessibility(current, AccessibilityKind(original)!.Value));
        Document updated = document.WithSyntaxRoot(rewritten);
        return Formatter.FormatAsync(updated, Formatter.Annotation).GetAwaiter().GetResult();
    }

    private static bool NeedsAccessibility(MemberDeclarationSyntax node) => !Modifiers(node).Any(token => token.IsKind(SyntaxKind.PublicKeyword)
            || token.IsKind(SyntaxKind.PrivateKeyword) || token.IsKind(SyntaxKind.ProtectedKeyword) || token.IsKind(SyntaxKind.InternalKeyword))
        && AccessibilityKind(node) is not null
        && node switch
        {
            MethodDeclarationSyntax { ExplicitInterfaceSpecifier: not null }
                or PropertyDeclarationSyntax { ExplicitInterfaceSpecifier: not null }
                or IndexerDeclarationSyntax { ExplicitInterfaceSpecifier: not null }
                or EventDeclarationSyntax { ExplicitInterfaceSpecifier: not null } => false,
            DestructorDeclarationSyntax or GlobalStatementSyntax or IncompleteMemberSyntax => false,
            ConstructorDeclarationSyntax { Modifiers: var modifiers } when modifiers.Any(token => token.IsKind(SyntaxKind.StaticKeyword)) => false,
            _ => true
        };

    private static SyntaxKind? AccessibilityKind(MemberDeclarationSyntax node)
    {
        bool isType = node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax;
        bool topLevelType = isType && (node.Parent is CompilationUnitSyntax || node.Parent is BaseNamespaceDeclarationSyntax);
        bool interfaceMember = !isType && node.Parent is InterfaceDeclarationSyntax;
        if (isType && topLevelType) { return SyntaxKind.InternalKeyword; }
        if (interfaceMember || (isType && node.Parent is InterfaceDeclarationSyntax)) { return SyntaxKind.PublicKeyword; }
        return node switch
        {
            BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or FieldDeclarationSyntax or EventFieldDeclarationSyntax
                or EventDeclarationSyntax or MethodDeclarationSyntax or PropertyDeclarationSyntax or IndexerDeclarationSyntax
                or ConstructorDeclarationSyntax or OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax
                => SyntaxKind.PrivateKeyword,
            _ => null
        };
    }

    private static MemberDeclarationSyntax AddAccessibility(MemberDeclarationSyntax node, SyntaxKind kind)
    {
        SyntaxTokenList modifiers = Modifiers(node);
        SyntaxTriviaList leading = modifiers.Count > 0
            ? modifiers[0].LeadingTrivia
            : node.AttributeLists.Count == 0 ? node.GetLeadingTrivia() : default;
        if (modifiers.Count > 0)
        {
            modifiers = modifiers.Replace(modifiers[0], modifiers[0].WithLeadingTrivia(SyntaxFactory.Space));
        }
        else if (node.AttributeLists.Count == 0)
        {
            node = (MemberDeclarationSyntax)node.WithLeadingTrivia(default(SyntaxTriviaList));
        }
        SyntaxToken accessibility = SyntaxFactory.Token(kind).WithLeadingTrivia(leading).WithTrailingTrivia(SyntaxFactory.Space);
        modifiers = modifiers.Insert(0, accessibility);
        return WithModifiers(node, modifiers).WithAdditionalAnnotations(Formatter.Annotation);
    }

    private static SyntaxTokenList Modifiers(MemberDeclarationSyntax node) => node switch
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
        OperatorDeclarationSyntax value => value.Modifiers,
        ConversionOperatorDeclarationSyntax value => value.Modifiers,
        _ => default
    };

    private static MemberDeclarationSyntax WithModifiers(MemberDeclarationSyntax node, SyntaxTokenList modifiers) => node switch
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
        OperatorDeclarationSyntax value => value.WithModifiers(modifiers),
        ConversionOperatorDeclarationSyntax value => value.WithModifiers(modifiers),
        _ => node
    };
}
