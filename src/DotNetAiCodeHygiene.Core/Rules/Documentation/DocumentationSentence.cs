using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed class DocumentationSentenceRuleModule : IRuleModule
{
    internal const string RuleId = "docs.text.sentence";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "finding", "finding", "Require sentence punctuation in selected documentation prose.", true);

    internal const string Suggestion = "Finish the prose with sentence punctuation.";
    internal const string Constraint = "Sentence punctuation is mechanical and does not judge prose quality.";
    internal static string Message(string tag) => $"Explicit <{tag}> prose must end with '.', '?' or '!'.";
    public Rule Descriptor => RuleDescriptor;

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var findings = new List<Finding>();
        foreach (Document document in context.ReportingDocuments)
        {
            DocumentationSubjectFact subjects = context.DocumentationSubjects(document);
            SemanticModel model = context.SemanticModel(document);
            foreach (MemberDeclarationSyntax declaration in subjects.CoveredDeclarations)
            {
                ISymbol symbol = model.GetDeclaredSymbol(declaration)!;
                findings.AddRange(DocumentationRuleEvaluation.EvaluateSentences(declaration, symbol,
                    context.RelativePath(document), context.Tree(document), Descriptor));
            }
        }
        return RuleModuleResult.FindingsOnly(findings);
    }
}
