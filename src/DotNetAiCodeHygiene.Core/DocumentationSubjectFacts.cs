using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <inheritdoc/>
internal sealed record DocumentationSummarySubject(
    ISymbol Symbol,
    MemberDeclarationSyntax Declaration,
    string Anchor,
    string Summary,
    string CarrierContent,
    int SourceOffset,
    bool IsPositionalRecordProperty,
    bool HasSummary,
    bool HasDirectInheritdoc,
    string? ParameterName);

/// <inheritdoc/>
internal sealed record DocumentationSubjectFact(
    IReadOnlyList<MemberDeclarationSyntax> CoveredDeclarations,
    IReadOnlyList<DocumentationSummarySubject> SummarySubjects)
{
/// <inheritdoc/>
    internal static DocumentationSubjectFact Empty { get; } = new([], []);

/// <inheritdoc/>
    internal static DocumentationSubjectFact Create(SyntaxNode root, SemanticModel model)
    {
        var declarations = new List<MemberDeclarationSyntax>();
        var subjects = new List<DocumentationSummarySubject>();

        foreach (MemberDeclarationSyntax member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(member) is not ISymbol symbol
                || symbol.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal)
                || symbol.IsImplicitlyDeclared || !DocumentationSubjects.SummaryEligible(symbol))
            {
                continue;
            }

            declarations.Add(member);
            string anchor = symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString();
            string summary = DocumentationSubjects.GetSummaryText(member);
            subjects.Add(new DocumentationSummarySubject(symbol, member, anchor, summary,
                DocumentationSubjects.GetSummaryContent(member), member.GetLocation().SourceSpan.Start, false,
                DocumentationSubjects.HasSummary(member), DocumentationSubjects.HasInheritdoc(member), null));

            if (member is not RecordDeclarationSyntax { ParameterList: not null } record
                || symbol is not INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public or Accessibility.Internal } recordSymbol)
            {
                continue;
            }

            string recordAnchor = recordSymbol.GetDocumentationCommentId() ?? recordSymbol.ToDisplayString();

            foreach (ParameterSyntax parameter in record.ParameterList.Parameters)
            {
                string name = parameter.Identifier.ValueText;
                IPropertySymbol? property = recordSymbol.GetMembers(name).OfType<IPropertySymbol>().FirstOrDefault();

                if (property?.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
                {
                    continue;
                }

                string propertyAnchor = recordAnchor + "." + name;
                string parameterSummary = DocumentationSubjects.GetParamText(record, name);
                subjects.Add(new DocumentationSummarySubject(property, record, propertyAnchor, parameterSummary,
                    DocumentationSubjects.GetParamContent(record, name), parameter.SpanStart, true,
                    !string.IsNullOrWhiteSpace(parameterSummary) || DocumentationSubjects.HasInheritdoc(record),
                    DocumentationSubjects.HasInheritdoc(record), name));
            }
        }
        return new DocumentationSubjectFact(declarations.ToArray(), subjects.ToArray());
    }
}
