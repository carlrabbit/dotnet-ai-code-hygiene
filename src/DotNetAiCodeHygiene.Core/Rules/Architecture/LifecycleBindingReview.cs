using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Finds changed service registrations, constructors, and invocation-state captures.</summary>
internal sealed class LifecycleBindingReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "architecture.lifecycle.binding";
    private static readonly Rule RuleDescriptor = new(RuleId, 2, "review-batch", "review-batch", "Review changed dependency construction and invocation-state lifetime binding.", true);
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
                ISymbol?[] dependencies = symbol.Parameters.Select(parameter => parameter.Type).Concat(ctor.Body?.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                    .Select(assignment => model.GetSymbolInfo(assignment.Left).Symbol) ?? []).ToArray();
                Add(ctor, symbol, stateParameters.Length > 0 ? "constructor receives name/type-marked invocation or resource-state parameter(s): " + string.Join(", ", stateParameters) : "constructor dependency capture",
                    dependencies);
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
                    Add(type, primaryConstructor, "primary constructor receives name/type-marked invocation or resource-state parameter(s): " + string.Join(", ", stateParameters),
                        primaryConstructor.Parameters.Select(parameter => parameter.Type));
                }
            }
            foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                ISymbol? target = model.GetSymbolInfo(call).Symbol;
                if (target is not IMethodSymbol method || method.Name is not ("AddSingleton" or "AddScoped" or "AddTransient" or "AddHostedService" or "TryAddSingleton" or "TryAddScoped" or "TryAddTransient") ||
                    !method.IsExtensionMethod || !method.ContainingNamespace.ToDisplayString().StartsWith("Microsoft.Extensions.DependencyInjection", StringComparison.Ordinal)) { continue; }
                ISymbol?[] registeredTypes = method.TypeArguments.Cast<ISymbol?>().Concat(call.ArgumentList.Arguments.Select(argument =>
                    argument.Expression is TypeOfExpressionSyntax typeOf ? model.GetTypeInfo(typeOf.Type).Type : null)).ToArray();
                Add(call, method, "compiler-resolved dependency-injection lifetime registration: " + method.Name, registeredTypes);
            }
            if (subjects.Any(x => x.Item.Path == path)) { sources[path] = source; }

            void Add(SyntaxNode node, ISymbol symbol, string signal, IEnumerable<ISymbol?>? relatedSymbols = null)
            {
                var span = node.Span; var pos = tree.GetLineSpan(span).StartLinePosition; string id = context.ProjectIdentity(document) + "\0" + path + "\0" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "\0" + pos.Line;
                const string legitimate = "A truly per-run processor may legitimately capture run state in its constructor.";
                var item = new ReviewItem("", path, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "Candidate signal: " + signal + ".", symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                ISymbol?[] capturedRelatedSymbols = (relatedSymbols ?? []).ToArray();
                subjects.Add(new(id, node.WithoutLeadingTrivia().ToFullString() + "\0" + signal + "\0" + string.Join("\0", capturedRelatedSymbols.Select(value => value?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))), item,
                    DirectChange: context.HasChangedLine(path, pos.Line + 1, tree.GetLineSpan(span).EndLinePosition.Line + 1),
                    ObservedSignal: signal, LegitimateCase: legitimate,
                    PrimaryAnchor: new(context.ProjectIdentity(document), path, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "primary changed/reporting construction"),
                    Enrich: (_, subject) =>
                    {
                        ReviewAnchor[] related = ReviewRelationshipFacts.RelatedSymbols(context, capturedRelatedSymbols, "construction-lifetime-dependency-or-registered-service");
                        return subject with { RelatedAnchors = related, ObservedSignal = signal + "; compiler-resolved related dependency/service source declarations: " +
                            (related.Length == 0 ? "none available; runtime lifetime/reachability remains unknown" : "included"),
                            EvidenceLimitation = related.Length == 0 ? "No source declaration for the related dependency or service was available; runtime lifetime and reachability remain unknown." : null };
                    }));
            }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }
}
