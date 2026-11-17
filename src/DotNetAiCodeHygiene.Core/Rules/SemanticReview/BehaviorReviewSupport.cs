using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Mechanical source anchoring shared by fixed behavior-review contracts.</summary>
internal static class BehaviorReviewSupport
{
    internal static ReviewSubject Create(RuleContext context, Document document, SyntaxNode signal, ISymbol? owner,
        string signalLabel, string legitimateCase, Dictionary<string, int> duplicateOrdinals)
    {
        string path = context.RelativePath(document);
        string project = context.ProjectIdentity(document);
        string symbol = owner?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? signal.Kind().ToString();
        string normalized = string.Join(" ", signal.WithoutTrivia().ToFullString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        string key = project + "\0" + path + "\0" + signalLabel + "\0" + symbol + "\0" + normalized;
        int ordinal = duplicateOrdinals.GetValueOrDefault(key);
        duplicateOrdinals[key] = ordinal + 1;
        var span = signal.Span;
        var position = context.Tree(document).GetLineSpan(span).StartLinePosition;
        int endLine = context.Tree(document).GetLineSpan(span).EndLinePosition.Line + 1;
        string declaration = owner?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "";
        string display = symbol + " — " + signalLabel;
        var item = new ReviewItem("", path, position.Line + 1, position.Character + 1, display, "", declaration);
        string content = signal.WithoutLeadingTrivia().ToFullString() + "\0owner:" + OwnerContext(signal, owner);
        bool changed = context.HasChangedLine(path, position.Line + 1, endLine);
        return new ReviewSubject(key + "\0" + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture), content, item,
            DirectChange: changed, ObservedSignal: signalLabel + " syntax: " + normalized, LegitimateCase: legitimateCase,
            PrimaryAnchor: new ReviewAnchor(project, path, position.Line + 1, position.Character + 1, symbol, "changed behavior signal"));
    }

    internal static SyntaxNode? OwnerNode(SyntaxNode node) => node.AncestorsAndSelf().FirstOrDefault(candidate => candidate is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or PropertyDeclarationSyntax or FieldDeclarationSyntax or ConstructorDeclarationSyntax);

    internal static ISymbol? DeclaredOwner(RuleContext context, Document document, SyntaxNode node) => OwnerNode(node) switch
    {
        BaseMethodDeclarationSyntax method => context.SemanticModel(document).GetDeclaredSymbol(method),
        AccessorDeclarationSyntax accessor => context.SemanticModel(document).GetDeclaredSymbol(accessor),
        LocalFunctionStatementSyntax local => context.SemanticModel(document).GetDeclaredSymbol(local),
        PropertyDeclarationSyntax property => context.SemanticModel(document).GetDeclaredSymbol(property),
        FieldDeclarationSyntax field => field.Declaration.Variables.Select(variable => context.SemanticModel(document).GetDeclaredSymbol(variable)).FirstOrDefault(symbol => symbol is not null),
        _ => null
    };

    private static string OwnerContext(SyntaxNode node, ISymbol? owner)
    {
        if (OwnerNode(node) is SyntaxNode ownerNode) { return ownerNode.WithoutLeadingTrivia().ToFullString(); }
        return owner?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "";
    }
}
