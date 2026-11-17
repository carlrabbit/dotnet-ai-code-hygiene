using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Finds changed service registrations, constructors, and invocation-state captures.</summary>
internal sealed class LifecycleBindingReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "architecture.lifecycle.binding";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review changed dependency construction and invocation-state lifetime binding.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", "Are construction-time dependencies and invocation-specific or resource state bound to the intended lifetime?")];
    private static readonly string[] StateMarkers = ["Request", "RunState", "Session", "Context", "CancellationToken", "Stream", "Transaction"];
    public Rule Descriptor => RuleDescriptor; public int BatchNumber => 25; public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => "Escalate only when a material lifetime or ownership contract cannot be resolved from current authority.";
    public string EscalatedReviewerClass => "planner";
    public RuleModuleResult Evaluate(RuleContext context)
    {
        var subjects = new List<ReviewSubject>(); var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            string path = context.RelativePath(document), source = context.Text(document).ToString(); var root = context.Root(document); var model = context.SemanticModel(document); var tree = context.Tree(document);
            foreach (ConstructorDeclarationSyntax ctor in root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
            {
                IMethodSymbol? symbol = model.GetDeclaredSymbol(ctor) as IMethodSymbol;
                if (symbol is null) { continue; }
                string[] stateParameters = symbol.Parameters.Where(p => StateMarkers.Any(marker => p.Type.Name.Contains(marker, StringComparison.OrdinalIgnoreCase) || p.Name.Contains(marker, StringComparison.OrdinalIgnoreCase))).Select(p => p.ToDisplayString()).ToArray();
                bool storesDependency = ctor.Body?.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left is IdentifierNameSyntax or MemberAccessExpressionSyntax) == true || ctor.ExpressionBody is not null;
                if (stateParameters.Length == 0 && (!storesDependency || symbol.Parameters.Length == 0)) { continue; }
                Add(ctor, symbol, stateParameters.Length > 0 ? "invocation/resource-state constructor parameter(s): " + string.Join(", ", stateParameters) : "constructor dependency capture");
            }
            foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>().Where(declaration => declaration.ParameterList is not null))
            {
                if (model.GetDeclaredSymbol(type) is not INamedTypeSymbol typeSymbol) { continue; }
                IMethodSymbol? primaryConstructor = typeSymbol.InstanceConstructors.FirstOrDefault(constructor => constructor.Parameters.Length > 0);
                if (primaryConstructor is null) { continue; }
                string[] stateParameters = primaryConstructor.Parameters.Where(parameter => StateMarkers.Any(marker =>
                    parameter.Type.Name.Contains(marker, StringComparison.OrdinalIgnoreCase) || parameter.Name.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                    .Select(parameter => parameter.ToDisplayString()).ToArray();
                if (stateParameters.Length > 0)
                {
                    Add(type, primaryConstructor, "invocation/resource-state primary-constructor parameter(s): " + string.Join(", ", stateParameters));
                }
            }
            foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                ISymbol? target = model.GetSymbolInfo(call).Symbol;
                if (target is not IMethodSymbol method || method.Name is not ("AddSingleton" or "AddScoped" or "AddTransient" or "AddHostedService" or "TryAddSingleton" or "TryAddScoped" or "TryAddTransient")) { continue; }
                Add(call, method, "dependency-injection lifetime registration: " + method.Name);
            }
            if (subjects.Any(x => x.Item.Path == path)) { sources[path] = source; }

            void Add(SyntaxNode node, ISymbol symbol, string signal)
            {
                var span = node.Span; var pos = tree.GetLineSpan(span).StartLinePosition; string id = context.ProjectIdentity(document) + "\0" + path + "\0" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "\0" + pos.Line;
                const string legitimate = "A truly per-run processor may legitimately capture run state in its constructor.";
                var item = new ReviewItem("", path, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "Candidate signal: " + signal + ".", symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                subjects.Add(new(id, node.WithoutLeadingTrivia().ToFullString() + "\0" + signal, item,
                    DirectChange: context.HasChangedLine(path, pos.Line + 1, tree.GetLineSpan(span).EndLinePosition.Line + 1),
                    ObservedSignal: signal, LegitimateCase: legitimate,
                    PrimaryAnchor: new(context.ProjectIdentity(document), path, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "primary changed/reporting construction")));
            }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }
}
