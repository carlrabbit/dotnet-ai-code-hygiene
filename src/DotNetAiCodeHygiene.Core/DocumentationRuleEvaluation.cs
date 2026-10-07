using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal static class DocumentationRuleEvaluation
{
    private sealed record SymbolScope(HashSet<string> Parameters, HashSet<string> TypeParameters, HashSet<string> InScopeTypeParameters);

    internal static IReadOnlyList<Finding> EvaluateXml(MemberDeclarationSyntax member, ISymbol symbol, SemanticModel model,
        Compilation compilation, string path, SyntaxTree tree, Rule rule)
    {
        DocumentationCommentTriviaSyntax? documentation = DocumentationSubjects.Documentation(member);
        if (documentation is null)
        {
            return [];
        }

        SymbolScope scope = GetScope(member, symbol);
        var findings = new List<Finding>();
        var counts = new HashSet<string>(StringComparer.Ordinal);
        string anchor = symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString();

        foreach (XmlElementSyntax element in documentation.DescendantNodes().OfType<XmlElementSyntax>())
        {
            string tag = element.StartTag.Name.LocalName.ValueText;
            string? name = Name(element);
            string prose = XmlProse(element.ToFullString());
            string? issue = null;
            if (tag == "param")
            {
                if (string.IsNullOrWhiteSpace(name) || !scope.Parameters.Contains(name))
                {
                    issue = DocumentationXmlConsistencyRuleModule.InvalidParam;
                }
                else if (!counts.Add("param:" + name))
                {
                    issue = DocumentationXmlConsistencyRuleModule.DuplicateParamFor(name);
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = $"<param name=\"{name}\"> must contain prose.";
                }
            }
            else if (tag == "typeparam")
            {
                if (string.IsNullOrWhiteSpace(name) || !scope.TypeParameters.Contains(name))
                {
                    issue = DocumentationXmlConsistencyRuleModule.InvalidTypeParam;
                }
                else if (!counts.Add("typeparam:" + name))
                {
                    issue = DocumentationXmlConsistencyRuleModule.DuplicateTypeParamFor(name);
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = $"<typeparam name=\"{name}\"> must contain prose.";
                }
            }
            else if (tag == "returns")
            {
                bool returnsValue = symbol switch
                {
                    IMethodSymbol callable => !callable.ReturnsVoid,
                    INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod.ReturnsVoid: false } => true,
                    _ => false
                };
                if (!counts.Add("returns"))
                {
                    issue = DocumentationXmlConsistencyRuleModule.DuplicateReturns;
                }
                else if (!returnsValue)
                {
                    issue = DocumentationXmlConsistencyRuleModule.InvalidReturns;
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = DocumentationXmlConsistencyRuleModule.EmptyReturns;
                }
            }
            else if (tag == "value")
            {
                if (!counts.Add("value"))
                {
                    issue = DocumentationXmlConsistencyRuleModule.DuplicateValue;
                }
                else if (symbol is not IPropertySymbol)
                {
                    issue = DocumentationXmlConsistencyRuleModule.InvalidValue;
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = DocumentationXmlConsistencyRuleModule.EmptyValue;
                }
            }
            else if (tag == "exception")
            {
                XmlCrefAttributeSyntax? cref = element.StartTag.Attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault();
                ISymbol? target = cref is null ? null : model.GetSymbolInfo(cref.Cref).Symbol;
                INamedTypeSymbol? baseException = compilation.GetTypeByMetadataName("System.Exception");
                bool valid = target is INamedTypeSymbol exceptionType && baseException is not null && IsDerivedFrom(exceptionType, baseException);
                if (!valid)
                {
                    issue = DocumentationXmlConsistencyRuleModule.InvalidException;
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = DocumentationXmlConsistencyRuleModule.EmptyException;
                }
            }

            if (issue is not null)
            {
                findings.Add(HygieneEngine.Make(rule, path, tree, element.SpanStart, symbol.ToDisplayString(), issue,
                    DocumentationXmlConsistencyRuleModule.XmlSuggestion, issue, DocumentationXmlConsistencyRuleModule.XmlConstraint, anchor,
                    tag + ":" + element.ToFullString()));
            }
        }

        foreach (XmlEmptyElementSyntax reference in documentation.DescendantNodes().OfType<XmlEmptyElementSyntax>())
        {
            string tag = reference.Name.LocalName.ValueText;
            if (tag is not ("paramref" or "typeparamref"))
            {
                continue;
            }

            string? name = reference.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault(a => a.Name.ToString() == "name")?.Identifier.Identifier.ValueText;
            bool valid = tag == "paramref" ? !string.IsNullOrEmpty(name) && scope.Parameters.Contains(name)
                : !string.IsNullOrEmpty(name) && scope.InScopeTypeParameters.Contains(name);
            if (!valid)
            {
                string message = DocumentationXmlConsistencyRuleModule.InvalidReference(tag);
                findings.Add(HygieneEngine.Make(rule, path, tree, reference.SpanStart, symbol.ToDisplayString(), message,
                    DocumentationXmlConsistencyRuleModule.ReferenceSuggestion, name ?? "missing name",
                    DocumentationXmlConsistencyRuleModule.ReferenceConstraint, anchor, tag + ":" + reference.ToFullString()));
            }
        }
        return findings;
    }

    internal static IReadOnlyList<Finding> EvaluateSentences(MemberDeclarationSyntax member, ISymbol symbol, string path, SyntaxTree tree, Rule rule)
    {
        DocumentationCommentTriviaSyntax? documentation = DocumentationSubjects.Documentation(member);
        if (documentation is null)
        {
            return [];
        }

        var findings = new List<Finding>();
        string anchor = symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString();
        foreach (XmlElementSyntax element in documentation.DescendantNodes().OfType<XmlElementSyntax>())
        {
            string tag = element.StartTag.Name.LocalName.ValueText;
            if (tag is not ("summary" or "param" or "typeparam" or "returns" or "value" or "exception"))
            {
                continue;
            }

            string prose = XmlProse(element.ToFullString());
            if (string.IsNullOrWhiteSpace(prose) || prose[^1] is '.' or '?' or '!')
            {
                continue;
            }

            string message = DocumentationSentenceRuleModule.Message(tag);
            findings.Add(HygieneEngine.Make(rule, path, tree, element.SpanStart, symbol.ToDisplayString(), message,
                DocumentationSentenceRuleModule.Suggestion, prose,
                DocumentationSentenceRuleModule.Constraint, anchor, tag + ":" + element.ToFullString()));
        }
        return findings;
    }

    private static SymbolScope GetScope(MemberDeclarationSyntax member, ISymbol symbol)
    {
        HashSet<string> parameters = symbol switch
        {
            IMethodSymbol method => method.Parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            IPropertySymbol property => property.Parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { } invoke } => invoke.Parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            INamedTypeSymbol when member is RecordDeclarationSyntax record => (record.ParameterList?.Parameters ?? default).Select(p => p.Identifier.ValueText).ToHashSet(StringComparer.Ordinal),
            _ => new(StringComparer.Ordinal)
        };
        HashSet<string> typeParameters = symbol switch
        {
            IMethodSymbol method => method.TypeParameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            INamedTypeSymbol type => type.TypeParameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            _ => new(StringComparer.Ordinal)
        };
        HashSet<string> inScopeTypeParameters = symbol switch
        {
            IMethodSymbol method => method.TypeParameters.Concat(ContainingTypeParameters(method.ContainingType)).Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            INamedTypeSymbol type => type.TypeParameters.Concat(ContainingTypeParameters(type.ContainingType)).Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            IPropertySymbol property => ContainingTypeParameters(property.ContainingType).Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            _ => new(StringComparer.Ordinal)
        };
        return new(parameters, typeParameters, inScopeTypeParameters);
    }

    private static string? Name(XmlElementSyntax element) => element.StartTag.Attributes.OfType<XmlNameAttributeSyntax>()
        .FirstOrDefault(a => a.Name.ToString() == "name")?.Identifier.Identifier.ValueText;

    private static string XmlProse(string xml)
    {
        try { return Regex.Replace(XElement.Parse(xml).Value, @"\s+", " ").Trim(); }
        catch (System.Xml.XmlException) { return ""; }
    }

    private static IEnumerable<ITypeParameterSymbol> ContainingTypeParameters(INamedTypeSymbol? type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            foreach (ITypeParameterSymbol parameter in current.TypeParameters)
            {
                yield return parameter;
            }
        }
    }

    private static bool IsDerivedFrom(INamedTypeSymbol symbol, INamedTypeSymbol baseType)
    {
        for (INamedTypeSymbol? current = symbol; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }
}
