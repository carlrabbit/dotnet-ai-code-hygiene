using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed class DocumentationSummaryRequiredRuleModule : IRuleModule
{
    internal const string RuleId = "docs.summary.required";
    private static readonly Rule RuleDescriptor = new(RuleId, 2, "finding", "finding", "Require documentation summaries on covered API symbols.", true);

    public Rule Descriptor => RuleDescriptor;

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var findings = new List<Finding>();
        foreach (Document document in context.ReportingDocuments)
        {
            SyntaxTree tree = context.Tree(document);
            string path = context.RelativePath(document);
            foreach (DocumentationSummarySubject subject in context.DocumentationSubjects(document).SummarySubjects)
            {
                if (subject.IsPositionalRecordProperty)
                {
                    if (!subject.HasSummary && !subject.HasDirectInheritdoc)
                    {
                        findings.Add(HygieneEngine.Make(Descriptor, path, tree, subject.SourceOffset, subject.Anchor,
                            "Public or internal positional record property has no non-empty documentation summary.",
                            $"Add a non-empty <param name=\"{subject.ParameterName}\"> summary to the record documentation.",
                            "The matching record parameter has no summary prose.",
                            "Ordinary parameters remain optional documentation subjects.", subject.Anchor, "missing-record-property-summary:" + subject.Anchor));
                    }
                }
                else if (!subject.HasSummary)
                {
                    findings.Add(HygieneEngine.Make(Descriptor, path, tree, subject.SourceOffset, subject.Symbol.ToDisplayString(),
                        "Public or internal symbol has no non-empty documentation summary.",
                        "Add a concise documentation summary for this API subject.", "The declaration has no summary text.",
                        "This rule requires documentation, not a particular wording or language.", subject.Anchor, "missing-summary:" + subject.Anchor));
                }
            }
        }
        return RuleModuleResult.FindingsOnly(findings);
    }
}
