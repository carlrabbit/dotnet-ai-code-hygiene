using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Resolves local documentation summaries and their carriers.</summary>
internal static class DocumentationSubjects
{
    /// <summary>Determines whether a symbol is a covered documentation subject.</summary>
    internal static bool SummaryEligible(ISymbol symbol) => symbol switch
    {
        INamedTypeSymbol => true,
        IPropertySymbol => true,
        IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.Ordinary } => true,
        _ => false
    };

    /// <summary>Determines whether a declaration supplies a summary or direct inheritdoc.</summary>
    internal static bool HasSummary(MemberDeclarationSyntax member) =>
        !string.IsNullOrWhiteSpace(GetSummaryText(member)) || HasInheritdoc(member);

    /// <summary>Gets documentation trivia attached to a declaration.</summary>
    internal static DocumentationCommentTriviaSyntax? Documentation(MemberDeclarationSyntax member) =>
        member.GetLeadingTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().FirstOrDefault();

    /// <summary>Determines whether a declaration has a direct inheritdoc carrier.</summary>
    internal static bool HasInheritdoc(MemberDeclarationSyntax member) =>
        Documentation(member)?.Content.OfType<XmlEmptyElementSyntax>().Any(element => element.Name.LocalName.ValueText == "inheritdoc") == true;

    /// <summary>Reads prose from a matching XML parameter carrier.</summary>
    internal static string GetParamText(MemberDeclarationSyntax member, string name)
    {
        XmlElementSyntax? element = Documentation(member)?.DescendantNodes().OfType<XmlElementSyntax>().FirstOrDefault(candidate =>
            candidate.StartTag.Name.LocalName.ValueText == "param"
            && candidate.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().Any(attribute => attribute.Identifier.Identifier.ValueText == name));
        return element is null ? "" : XmlProse(element.ToFullString());
    }

    /// <summary>Reads XML content from a matching parameter carrier.</summary>
    internal static string GetParamContent(MemberDeclarationSyntax member, string name)
    {
        XmlElementSyntax? element = Documentation(member)?.DescendantNodes().OfType<XmlElementSyntax>().FirstOrDefault(candidate =>
            candidate.StartTag.Name.LocalName.ValueText == "param"
            && candidate.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().Any(attribute => attribute.Identifier.Identifier.ValueText == name));

        if (element is null)
        {
            return "";
        }

        try
        {
            return Regex.Replace(XElement.Parse(element.ToFullString()).ToString(SaveOptions.DisableFormatting), @"\s+", " ").Trim();
        }
        catch (System.Xml.XmlException)
        {
            return "";
        }
    }

    /// <summary>Gets normalized prose from the declaration's summary element.</summary>
    internal static string GetSummaryText(MemberDeclarationSyntax member)
    {
        foreach (SyntaxTrivia trivia in member.GetLeadingTrivia())
        {
            if (trivia.GetStructure() is not DocumentationCommentTriviaSyntax documentation)
            {
                continue;
            }

            foreach (XmlElementSyntax summary in documentation.DescendantNodes().OfType<XmlElementSyntax>().Where(element => element.StartTag.Name.LocalName.ValueText == "summary"))
            {
                try
                {
                    return Regex.Replace(XElement.Parse(summary.ToFullString(), LoadOptions.None).Value, @"\s+", " ").Trim();
                }
                catch (System.Xml.XmlException)
                {
                    continue;
                }
            }
        }
        return "";
    }

    /// <summary>Gets normalized XML content from the declaration's summary element.</summary>
    internal static string GetSummaryContent(MemberDeclarationSyntax member)
    {
        foreach (SyntaxTrivia trivia in member.GetLeadingTrivia())
        {
            if (trivia.GetStructure() is not DocumentationCommentTriviaSyntax documentation)
            {
                continue;
            }

            foreach (XmlElementSyntax summary in documentation.DescendantNodes().OfType<XmlElementSyntax>().Where(element => element.StartTag.Name.LocalName.ValueText == "summary"))
            {
                try
                {
                    return Regex.Replace(XElement.Parse(summary.ToFullString()).ToString(SaveOptions.DisableFormatting), @"\s+", " ").Trim();
                }
                catch (System.Xml.XmlException)
                {
                    continue;
                }
            }
        }
        return "";
    }

    private static string XmlProse(string xml)
    {
        try
        {
            return Regex.Replace(XElement.Parse(xml).Value, @"\s+", " ").Trim();
        }
        catch (System.Xml.XmlException)
        {
            return "";
        }
    }
}
