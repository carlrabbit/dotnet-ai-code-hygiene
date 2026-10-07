using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed class DocumentationXmlConsistencyRuleModule : IRuleModule
{
    internal const string RuleId = "docs.xml.consistent";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "finding", "finding", "Check present XML documentation structure and references.", true);

    internal const string InvalidParam = "<param> must name a declaration parameter.";
    internal const string DuplicateParam = "Duplicate <param> for '{0}'.";
    internal const string EmptyParam = "<param name=\"{0}\"> must contain prose.";
    internal const string InvalidTypeParam = "<typeparam> must name a declaration type parameter.";
    internal const string DuplicateTypeParam = "Duplicate <typeparam> for '{0}'.";
    internal const string EmptyTypeParam = "<typeparam name=\"{0}\"> must contain prose.";
    internal const string DuplicateReturns = "Only one <returns> element is allowed.";
    internal const string InvalidReturns = "<returns> is only valid on a value-returning callable.";
    internal const string EmptyReturns = "<returns> must contain prose.";
    internal const string DuplicateValue = "Only one <value> element is allowed.";
    internal const string InvalidValue = "<value> is only valid on a property or indexer.";
    internal const string EmptyValue = "<value> must contain prose.";
    internal const string InvalidException = "<exception cref> must resolve to an exception type.";
    internal const string EmptyException = "<exception> must contain prose.";
    internal const string XmlSuggestion = "Correct or remove the optional XML element.";
    internal const string XmlConstraint = "Optional XML elements are checked only when present.";
    internal const string ReferenceSuggestion = "Use a parameter or type parameter declared by this API.";
    internal const string ReferenceConstraint = "References are validated without requiring documentation for parameters.";
    internal static string DuplicateParamFor(string name) => string.Format(System.Globalization.CultureInfo.InvariantCulture, DuplicateParam, name);
    internal static string EmptyParamFor(string name) => string.Format(System.Globalization.CultureInfo.InvariantCulture, EmptyParam, name);
    internal static string DuplicateTypeParamFor(string name) => string.Format(System.Globalization.CultureInfo.InvariantCulture, DuplicateTypeParam, name);
    internal static string EmptyTypeParamFor(string name) => string.Format(System.Globalization.CultureInfo.InvariantCulture, EmptyTypeParam, name);
    internal static string InvalidReference(string tag) => $"<{tag}> must reference a declaration parameter of the matching kind.";
    public Rule Descriptor => RuleDescriptor;

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var findings = new List<Finding>();
        foreach (Document document in context.ReportingDocuments)
        {
            DocumentationSubjectFact subjects = context.DocumentationSubjects(document);
            SemanticModel model = context.SemanticModel(document);
            Compilation compilation = context.Compilation(document);
            foreach (MemberDeclarationSyntax declaration in subjects.CoveredDeclarations)
            {
                ISymbol symbol = model.GetDeclaredSymbol(declaration)!;
                findings.AddRange(DocumentationRuleEvaluation.EvaluateXml(declaration, symbol, model, compilation,
                    context.RelativePath(document), context.Tree(document), Descriptor));
            }
        }
        return RuleModuleResult.FindingsOnly(findings);
    }
}
