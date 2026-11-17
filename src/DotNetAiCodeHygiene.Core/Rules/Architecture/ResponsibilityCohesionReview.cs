using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Finds changed types that combine distinct state and operation shapes.</summary>
internal sealed class ResponsibilityCohesionReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "architecture.responsibility.cohesion";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review changed type responsibility clusters without using size as a verdict.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", "Does the type have a coherent responsibility, or mix separable owners?")];
    public Rule Descriptor => RuleDescriptor; public int BatchNumber => 22; public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => "Escalate only when a material ownership or design decision cannot be resolved from current authority.";
    public string EscalatedReviewerClass => "planner";
    public RuleModuleResult Evaluate(RuleContext context)
    {
        var subjects = new List<ReviewSubject>(); var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            string path = context.RelativePath(document), source = context.Text(document).ToString(); var root = context.Root(document); var model = context.SemanticModel(document); var tree = context.Tree(document);
            foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>().Where(type => type is not InterfaceDeclarationSyntax))
            {
                ISymbol? symbol = model.GetDeclaredSymbol(type);
                if (symbol is null) { continue; }
                bool hasState = type.Members.OfType<FieldDeclarationSyntax>().Any() || type.Members.OfType<PropertyDeclarationSyntax>().Any();
                bool hasOperations = type.Members.OfType<MethodDeclarationSyntax>().Any();
                bool hasNestedOwner = type.Members.OfType<TypeDeclarationSyntax>().Any();
                // This is only a concrete structure signal; type/member counts are never scored.
                if (!(hasState && hasOperations) && !(hasNestedOwner && (hasState || hasOperations))) { continue; }
                var span = type.Span; var pos = tree.GetLineSpan(span).StartLinePosition;
                string id = context.ProjectIdentity(document) + "\0" + path + "\0" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string signal = "Type combines persistent state and operations" + (hasNestedOwner ? " with a nested type owner" : "");
                const string legitimate = "Type size, member count, and the presence of multiple methods are not defects.";
                var item = new ReviewItem("", path, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "Candidate signal: " + signal + ".", symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                subjects.Add(new(id, type.WithoutLeadingTrivia().ToFullString(), item,
                    DirectChange: context.HasChangedLine(path, pos.Line + 1, tree.GetLineSpan(span).EndLinePosition.Line + 1),
                    ObservedSignal: signal, LegitimateCase: legitimate,
                    PrimaryAnchor: new(context.ProjectIdentity(document), path, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "primary changed/reporting type")));
            }
            if (subjects.Any(x => x.Item.Path == path)) { sources[path] = source; }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }
}
