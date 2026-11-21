using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Small selected-work Roslyn queries for rule-owned relationship evidence.</summary>
internal static class ReviewRelationshipFacts
{
    internal static ReviewAnchor[] Declarations(RuleContext context, ISymbol? symbol, string relation)
    {
        if (symbol is null) { return []; }
        var syntaxAnchors = symbol.DeclaringSyntaxReferences.Select(reference =>
        {
            SyntaxNode node = reference.GetSyntax();
            return Anchor(context, reference.SyntaxTree, node.Span, symbol, relation);
        });
        var locationAnchors = symbol.Locations.Where(location => location.IsInSource && location.SourceTree is not null)
            .Select(location => Anchor(context, location.SourceTree!, location.SourceSpan, symbol, relation));
        return syntaxAnchors.Concat(locationAnchors).Distinct().ToArray();
    }

    internal static ReviewAnchor Anchor(RuleContext context, SyntaxTree tree, Microsoft.CodeAnalysis.Text.TextSpan span, ISymbol? symbol, string relation)
    {
        string fullPath = tree.FilePath;
        string path = context.Session.Relative(fullPath);
        Document? document = context.Session.Projects.SelectMany(project => project.Documents)
            .FirstOrDefault(candidate => string.Equals(candidate.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));
        string project = document is null ? Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "" : context.ProjectIdentity(document);
        Microsoft.CodeAnalysis.Text.LinePosition position = tree.GetLineSpan(span).StartLinePosition;
        return new(project, path, position.Line + 1, position.Character + 1,
            symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "unresolved symbol", relation);
    }

    internal static ReviewAnchor[] InterfaceImplementations(RuleContext context, INamedTypeSymbol abstraction)
    {
        var anchors = new List<ReviewAnchor>();
        foreach (Project project in context.Session.Projects)
        {
            Compilation compilation = context.Session.GetCompilation(project);
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                context.RecordRelationshipAnalysis();
                SemanticModel model = compilation.GetSemanticModel(tree);
                foreach (BaseTypeDeclarationSyntax declaration in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                {
                    if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol type) { continue; }
                    bool implements = abstraction.TypeKind == TypeKind.Interface
                        ? type.AllInterfaces.Any(contract => SymbolEqualityComparer.Default.Equals(contract.OriginalDefinition, abstraction.OriginalDefinition))
                        : BaseTypes(type).Any(baseType => SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, abstraction.OriginalDefinition));
                    if (!implements) { continue; }
                    anchors.Add(Anchor(context, tree, declaration.Identifier.Span, type, "compiler-resolved-abstraction-implementation"));
                }
            }
        }
        return anchors.Distinct().ToArray();
    }

    internal static ReviewAnchor[] TypeConsumers(RuleContext context, INamedTypeSymbol abstraction)
    {
        var anchors = new List<ReviewAnchor>();
        foreach (Project project in context.Session.Projects)
        {
            Compilation compilation = context.Session.GetCompilation(project);
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                context.RecordRelationshipAnalysis();
                SemanticModel model = compilation.GetSemanticModel(tree);
                foreach (TypeSyntax typeSyntax in tree.GetRoot().DescendantNodes().OfType<TypeSyntax>())
                {
                    if (model.GetTypeInfo(typeSyntax).Type is not INamedTypeSymbol type ||
                        !SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, abstraction.OriginalDefinition)) { continue; }
                    SyntaxNode owner = typeSyntax.Ancestors().FirstOrDefault(node => node is BaseMethodDeclarationSyntax or BaseTypeDeclarationSyntax or PropertyDeclarationSyntax or FieldDeclarationSyntax) ?? typeSyntax;
                    ISymbol? ownerSymbol = owner switch
                    {
                        BaseMethodDeclarationSyntax method => model.GetDeclaredSymbol(method),
                        BaseTypeDeclarationSyntax declaration => model.GetDeclaredSymbol(declaration),
                        PropertyDeclarationSyntax property => model.GetDeclaredSymbol(property),
                        FieldDeclarationSyntax field => field.Declaration.Variables.Select(variable => model.GetDeclaredSymbol(variable)).FirstOrDefault(symbol => symbol is not null),
                        _ => null
                    };
                    anchors.Add(Anchor(context, tree, owner.Span, ownerSymbol, "compiler-resolved-abstraction-consumer"));
                }
            }
        }
        return anchors.Distinct().ToArray();
    }

    private static IEnumerable<INamedTypeSymbol> BaseTypes(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType) { yield return current; }
    }

    internal static ReviewAnchor[] Callers(RuleContext context, IMethodSymbol target)
    {
        var anchors = new List<ReviewAnchor>();
        foreach (Project project in context.Session.Projects)
        {
            Compilation compilation = context.Session.GetCompilation(project);
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                context.RecordRelationshipAnalysis();
                SemanticModel model = compilation.GetSemanticModel(tree);
                foreach (InvocationExpressionSyntax invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol called ||
                        !SymbolEqualityComparer.Default.Equals(called.OriginalDefinition, target.OriginalDefinition)) { continue; }
                    SyntaxNode owner = invocation.Ancestors().FirstOrDefault(node => node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax) ?? invocation;
                    ISymbol? ownerSymbol = owner switch
                    {
                        BaseMethodDeclarationSyntax method => model.GetDeclaredSymbol(method),
                        LocalFunctionStatementSyntax local => model.GetDeclaredSymbol(local),
                        AccessorDeclarationSyntax accessor => model.GetDeclaredSymbol(accessor),
                        _ => null
                    };
                    anchors.Add(Anchor(context, tree, owner.Span, ownerSymbol, "compiler-resolved-caller-of-intermediary"));
                }
            }
        }
        return anchors.Distinct().ToArray();
    }

    internal static ReviewAnchor[] RelatedSymbols(RuleContext context, IEnumerable<ISymbol?> symbols, string relation)
    {
        return symbols.Where(symbol => symbol is not null).SelectMany(symbol => Declarations(context, symbol, relation)).Distinct().ToArray();
    }
}
