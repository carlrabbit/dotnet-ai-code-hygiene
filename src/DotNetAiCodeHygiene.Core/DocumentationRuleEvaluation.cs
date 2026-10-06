using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <inheritdoc/>
internal static class DocumentationRuleEvaluation
{
    private static string XmlProse(string xml)
    {
        try { return Regex.Replace(XElement.Parse(xml).Value, @"\s+", " ").Trim(); }
        catch (System.Xml.XmlException) { return ""; }
    }
/// <inheritdoc/>
    internal static IReadOnlyList<Finding> Evaluate(MemberDeclarationSyntax member, ISymbol symbol, SemanticModel model, Compilation compilation, string path, SyntaxTree tree)
    {
        DocumentationCommentTriviaSyntax? documentation = DocumentationSubjects.Documentation(member);

        if (documentation is null)
        {
            return [];
        }

        var findings = new List<Finding>();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        string anchor = symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString();
        HashSet<string> parameters = symbol switch
        {
            IMethodSymbol method => method.Parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            IPropertySymbol property => property.Parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { } invoke } => invoke.Parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            INamedTypeSymbol type when member is RecordDeclarationSyntax record => (record.ParameterList?.Parameters ?? default).Select(p => p.Identifier.ValueText).ToHashSet(StringComparer.Ordinal),
            _ => new HashSet<string>(StringComparer.Ordinal)
        };
        HashSet<string> typeParameters = symbol switch
        {
            IMethodSymbol method => method.TypeParameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            INamedTypeSymbol type => type.TypeParameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            _ => new HashSet<string>(StringComparer.Ordinal)
        };
        HashSet<string> inScopeTypeParameters = symbol switch
        {
            IMethodSymbol method => method.TypeParameters.Concat(ContainingTypeParameters(method.ContainingType)).Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            INamedTypeSymbol type => type.TypeParameters.Concat(ContainingTypeParameters(type.ContainingType)).Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            IPropertySymbol property => ContainingTypeParameters(property.ContainingType).Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            _ => new HashSet<string>(StringComparer.Ordinal)
        };

        foreach (XmlElementSyntax element in documentation.DescendantNodes().OfType<XmlElementSyntax>())
        {
            string tag = element.StartTag.Name.LocalName.ValueText;
            string? name = element.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault(a => a.Name.ToString() == "name")?.Identifier.Identifier.ValueText;
            string prose = XmlProse(element.ToFullString());
            string? issue = null;

            if (tag == "param")
            {
                if (string.IsNullOrWhiteSpace(name) || !parameters.Contains(name))
                {
                    issue = "<param> must name a declaration parameter.";
                }
                else if (!counts.TryAdd("param:" + name, 1))
                {
                    issue = $"Duplicate <param> for '{name}'.";
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = $"<param name=\"{name}\"> must contain prose.";
                }
            }
            else if (tag == "typeparam")
            {
                if (string.IsNullOrWhiteSpace(name) || !typeParameters.Contains(name))
                {
                    issue = "<typeparam> must name a declaration type parameter.";
                }
                else if (!counts.TryAdd("typeparam:" + name, 1))
                {
                    issue = $"Duplicate <typeparam> for '{name}'.";
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = $"<typeparam name=\"{name}\"> must contain prose.";
                }
            }
            else if (tag == "returns")
            {
                bool hasValueReturn = symbol switch
                {
                    IMethodSymbol callable => !callable.ReturnsVoid,
                    INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod.ReturnsVoid: false } => true,
                    _ => false
                };

                if (!counts.TryAdd("returns", 1))
                {
                    issue = "Only one <returns> element is allowed.";
                }
                else if (!hasValueReturn)
                {
                    issue = "<returns> is only valid for a value-returning callable.";
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = "<returns> must contain prose.";
                }
            }
            else if (tag == "value")
            {
                if (!counts.TryAdd("value", 1))
                {
                    issue = "Only one <value> element is allowed.";
                }
                else if (symbol is not IPropertySymbol)
                {
                    issue = "<value> is only valid on a property or indexer.";
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = "<value> must contain prose.";
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
                    issue = "<exception cref> must resolve to an exception type.";
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = "<exception> must contain prose.";
                }
            }

            if (issue is not null)
            {
                findings.Add(HygieneEngine.Make(
                    RuleCatalog.All[3], path, tree, element.SpanStart, symbol.ToDisplayString(), issue,
                    "Correct or remove the optional XML element.", issue,
                    "Optional XML elements are checked only when present.", anchor, tag + ":" + element.ToFullString()));
            }
            if (tag is "summary" or "param" or "typeparam" or "returns" or "value" or "exception" && !string.IsNullOrWhiteSpace(prose) && prose[^1] is not ('.' or '?' or '!'))
            {
                string message = $"Explicit <{tag}> prose must end with '.', '?' or '!'.";
                findings.Add(HygieneEngine.Make(
                    RuleCatalog.All[4], path, tree, element.SpanStart, symbol.ToDisplayString(), message,
                    "Finish the prose with sentence punctuation.", prose,
                    "Sentence punctuation is mechanical and does not judge prose quality.", anchor, tag + ":" + element.ToFullString()));
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
            bool valid = tag == "paramref" ? !string.IsNullOrEmpty(name) && parameters.Contains(name) : !string.IsNullOrEmpty(name) && inScopeTypeParameters.Contains(name);

            if (!valid)
            {
                string message = $"<{tag}> must reference a declaration parameter of the matching kind.";
                findings.Add(HygieneEngine.Make(
                    RuleCatalog.All[3], path, tree, reference.SpanStart, symbol.ToDisplayString(), message,
                    "Use a parameter or type parameter declared by this API.", name ?? "missing name",
                    "References are validated without requiring documentation for parameters.", anchor, tag + ":" + reference.ToFullString()));
            }
        }
        return findings;
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
