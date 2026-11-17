using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Finds related writes to version/configuration/materialization-like state.</summary>
internal sealed class AuthorityUniquenessReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "architecture.authority.uniqueness";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review related configuration, version, and derived-state writes for source-of-truth clarity.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", "Is a derived value incorrectly treated as an independently authoritative choice?")];
    private static readonly string[] StateMarkers = ["version", "config", "option", "snapshot", "cache", "derived", "materialized", "sourceoftruth"];
    public Rule Descriptor => RuleDescriptor; public int BatchNumber => 24; public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => "Escalate only when a material authority or state-provenance decision cannot be resolved from current authority.";
    public string EscalatedReviewerClass => "planner";
    public RuleModuleResult Evaluate(RuleContext context)
    {
        var subjects = new List<ReviewSubject>(); var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            string path = context.RelativePath(document), source = context.Text(document).ToString(); var root = context.Root(document); var model = context.SemanticModel(document); var tree = context.Tree(document);
            foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                AssignmentExpressionSyntax[] relatedWrites = method.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                    .Where(a => StateMarkers.Any(marker => model.GetSymbolInfo(a.Left).Symbol?.Name.Contains(marker, StringComparison.OrdinalIgnoreCase) == true))
                    .ToArray();
                if (relatedWrites.Length < 2) { continue; }
                IMethodSymbol? symbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
                if (symbol is null) { continue; }
                string[] names = relatedWrites.Select(a => model.GetSymbolInfo(a.Left).Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? a.Left.ToString()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
                var span = method.Span; var pos = tree.GetLineSpan(span).StartLinePosition; string id = context.ProjectIdentity(document) + "\0" + path + "\0" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string signal = "One method writes multiple configuration/version/materialization-like members: " + string.Join(", ", names) + ".";
                const string legitimate = "Caches, snapshots, and versioned materializations can be correct when provenance and invalidation are explicit.";
                var item = new ReviewItem("", path, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "Candidate signal: " + signal, symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                subjects.Add(new(id, method.WithoutLeadingTrivia().ToFullString() + "\0" + string.Join("\0", names), item,
                    DirectChange: context.HasChangedLine(path, pos.Line + 1, tree.GetLineSpan(span).EndLinePosition.Line + 1),
                    ObservedSignal: signal, LegitimateCase: legitimate,
                    PrimaryAnchor: new(context.ProjectIdentity(document), path, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "primary changed/reporting method")));
            }
            if (subjects.Any(x => x.Item.Path == path)) { sources[path] = source; }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }
}
