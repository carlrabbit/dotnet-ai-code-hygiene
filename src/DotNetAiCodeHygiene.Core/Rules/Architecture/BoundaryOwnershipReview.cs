using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Finds changed public boundaries and calls crossing project assembly boundaries.</summary>
internal sealed class BoundaryOwnershipReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "architecture.boundary.ownership";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review changed consumer-to-provider and public-contract ownership.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", "Does the operation use the component/public contract that owns the behavior and authority?")];
    public Rule Descriptor => RuleDescriptor; public int BatchNumber => 23; public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => "Escalate only when a material ownership, interface, or design decision cannot be resolved from current authority.";
    public string EscalatedReviewerClass => "planner";
    public RuleModuleResult Evaluate(RuleContext context)
    {
        var subjects = new List<ReviewSubject>(); var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            string path = context.RelativePath(document), source = context.Text(document).ToString(); var root = context.Root(document); var model = context.SemanticModel(document); var tree = context.Tree(document);
            foreach (SyntaxNode node in root.DescendantNodes().Where(n => n is BaseTypeDeclarationSyntax or MethodDeclarationSyntax or PropertyDeclarationSyntax or InvocationExpressionSyntax))
            {
                ISymbol? symbol = node switch
                {
                    BaseTypeDeclarationSyntax t => model.GetDeclaredSymbol(t), MethodDeclarationSyntax m => model.GetDeclaredSymbol(m),
                    PropertyDeclarationSyntax p => model.GetDeclaredSymbol(p), InvocationExpressionSyntax i => model.GetSymbolInfo(i).Symbol, _ => null
                };
                bool publicBoundary = node is not InvocationExpressionSyntax && symbol is not null && symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal;
                ISymbol? target = node is InvocationExpressionSyntax invocation ? model.GetSymbolInfo(invocation).Symbol : null;
                Project? targetProject = target?.Locations.Where(location => location.IsInSource && location.SourceTree?.FilePath is not null)
                    .Select(location => context.Session.Projects.FirstOrDefault(project => project.Documents.Any(candidate =>
                        string.Equals(candidate.FilePath, location.SourceTree!.FilePath, StringComparison.OrdinalIgnoreCase))))
                    .FirstOrDefault(project => project is not null && !string.Equals(context.ProjectIdentity(project), context.ProjectIdentity(document), StringComparison.Ordinal));
                bool crossProject = targetProject is not null && target is not null && !SymbolEqualityComparer.Default.Equals(target.ContainingAssembly, model.Compilation.Assembly);
                if (!publicBoundary && !crossProject) { continue; }
                symbol ??= target;
                if (symbol is null) { continue; }
                var span = node.Span; var pos = tree.GetLineSpan(span).StartLinePosition;
                string targetInfo = crossProject ? "Cross-assembly target: " + target!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " [" + target!.ContainingAssembly.Identity.Name + "]." : "Changed externally visible declaration boundary.";
                string id = context.ProjectIdentity(document) + "\0" + path + "\0" + node.RawKind + "\0" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "\0" + pos.Line;
                string legitimate = "A legitimate application composition root may coordinate several components.";
                var item = new ReviewItem("", path, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "Candidate signal: " + targetInfo, symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                ReviewAnchor[] related = [];
                if (crossProject && target is not null && target.Locations.FirstOrDefault(location => location.IsInSource && location.SourceTree is not null) is Location targetLocation)
                {
                    string targetPath = targetLocation.SourceTree!.FilePath;
                    if (Path.IsPathRooted(targetPath) && context.Session.WithinRoot(targetPath))
                    {
                        string targetRelative = context.Session.Relative(targetPath);
                        var targetPos = targetLocation.GetLineSpan().StartLinePosition;
                        related = [new ReviewAnchor(context.ProjectIdentity(targetProject!), targetRelative, targetPos.Line + 1, targetPos.Character + 1,
                            target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "resolved provider implementation")];
                    }
                }
                subjects.Add(new(id, node.WithoutLeadingTrivia().ToFullString() + "\0" + targetInfo, item,
                    DirectChange: context.HasChangedLine(path, pos.Line + 1, tree.GetLineSpan(span).EndLinePosition.Line + 1),
                    RelatedChange: related.Any(anchor => context.Gate.ChangedPaths.Contains(anchor.Path)), RelatedAnchors: related,
                    ObservedSignal: targetInfo, LegitimateCase: legitimate,
                    PrimaryAnchor: new(context.ProjectIdentity(document), path, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "primary changed/reporting side")));
            }
            if (subjects.Any(x => x.Item.Path == path)) { sources[path] = source; }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }
}
