using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Reviews changed abstraction decisions with selected-only implementation/consumer context.</summary>
internal sealed class AbstractionNecessityReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "architecture.abstraction.necessity";
    private static readonly Rule RuleDescriptor = new(RuleId, 2, "review-batch", "review-batch", "Review changed abstractions against current contracts and variability.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", "Does it serve a current contract, boundary, side effect or meaningful variability?")];
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 20;
    public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => "Escalate only when a material contract or design decision cannot be resolved from current authority.";
    public string EscalatedReviewerClass => "planner";

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var declarations = new Dictionary<string, List<(Document Document, SyntaxNode Node, ISymbol Symbol, string Path, string Source)>>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            string path = context.RelativePath(document), source = context.Text(document).ToString();
            SyntaxNode root = context.Root(document); SemanticModel model = context.SemanticModel(document);
            foreach (SyntaxNode node in root.DescendantNodes().Where(n => n is InterfaceDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax or MethodDeclarationSyntax))
            {
                ISymbol? symbol = node switch
                {
                    BaseTypeDeclarationSyntax type => model.GetDeclaredSymbol(type),
                    MethodDeclarationSyntax method => model.GetDeclaredSymbol(method),
                    _ => null
                };
                if (symbol is null) { continue; }
                bool genericAbstraction = symbol is INamedTypeSymbol { TypeParameters.Length: > 0, IsAbstract: true } ||
                    symbol is IMethodSymbol { TypeParameters.Length: > 0, ReturnType: INamedTypeSymbol { TypeKind: TypeKind.Interface or TypeKind.Delegate } };
                bool abstraction = node is InterfaceDeclarationSyntax || symbol.IsAbstract || genericAbstraction;
                bool factory = node is MethodDeclarationSyntax methodNode &&
                    model.GetTypeInfo(methodNode.ReturnType).Type is INamedTypeSymbol { TypeKind: TypeKind.Interface or TypeKind.Delegate } or { IsAbstract: true };
                if (!abstraction && !factory) { continue; }
                string key = context.ProjectIdentity(document) + "\0symbol:" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (!declarations.TryGetValue(key, out var group)) { declarations[key] = group = []; }
                group.Add((document, node, symbol, path, source));
            }
        }

        var subjects = new List<ReviewSubject>();
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string key, var group) in declarations)
        {
            var ordered = group.OrderBy(entry => entry.Path, StringComparer.Ordinal).ThenBy(entry => entry.Node.SpanStart).ToArray();
            var changed = ordered.Where(entry => context.HasChangedLine(entry.Path,
                context.Tree(entry.Document).GetLineSpan(entry.Node.Span).StartLinePosition.Line + 1,
                context.Tree(entry.Document).GetLineSpan(entry.Node.Span).EndLinePosition.Line + 1)).ToArray();
            var primary = (changed.Length > 0 ? changed : ordered)[0];
            SyntaxTree tree = context.Tree(primary.Document);
            var position = tree.GetLineSpan(primary.Node.Span).StartLinePosition;
            string symbolName = primary.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string signal = primary.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Interface } ? "Declared interface contract" :
                primary.Symbol.IsAbstract && primary.Symbol is INamedTypeSymbol { TypeParameters.Length: > 0 } ? "Declared abstract generic type contract" :
                primary.Symbol.IsAbstract ? "Declared abstract contract" : primary.Symbol is IMethodSymbol { TypeParameters.Length: > 0 } ? "Generic factory abstraction method" :
                "Factory-shaped method with abstract/delegate return contract";
            string content = string.Join("\n", ordered.Select(entry => entry.Node.WithoutLeadingTrivia().ToFullString()));
            ReviewAnchor[] partialAnchors = ordered.Where(entry => !ReferenceEquals(entry.Node, primary.Node)).Select(entry =>
                ReviewRelationshipFacts.Anchor(context, context.Tree(entry.Document), entry.Node.Span, entry.Symbol, "same-symbol-partial-or-secondary-declaration")).ToArray();
            var item = new ReviewItem("", primary.Path, position.Line + 1, position.Character + 1, symbolName,
                "Candidate signal: " + signal + ". Related implementation and consumer resolution is performed for selected work.", primary.Symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            const string legitimate = "A one-implementation interface may legitimately isolate a persistence, transport, or public contract.";
            ISymbol capturedSymbol = primary.Symbol;
            subjects.Add(new ReviewSubject(key, content, item,
                DirectChange: changed.Length > 0, RelatedAnchors: partialAnchors, ObservedSignal: signal,
                LegitimateCase: legitimate,
                PrimaryAnchor: new(context.ProjectIdentity(primary.Document), primary.Path, position.Line + 1, position.Character + 1, symbolName, "changed-abstraction-contract"),
                Enrich: (_, selected) => Enrich(selected, capturedSymbol, partialAnchors)));
            foreach (var entry in ordered) { sources.TryAdd(entry.Path, entry.Source); }

            ReviewSubject Enrich(ReviewSubject subject, ISymbol abstraction, ReviewAnchor[] partials)
            {
                var related = new List<ReviewAnchor>(partials);
                related.AddRange(ReviewRelationshipFacts.Declarations(context, abstraction, "compiler-resolved-abstraction-declaration"));
                if (abstraction is INamedTypeSymbol named)
                {
                    related.AddRange(ReviewRelationshipFacts.InterfaceImplementations(context, named));
                    related.AddRange(ReviewRelationshipFacts.TypeConsumers(context, named));
                }
                else if (abstraction is IMethodSymbol contractMethod)
                {
                    related.AddRange(ReviewRelationshipFacts.Callers(context, contractMethod));
                    if (contractMethod.ReturnType is INamedTypeSymbol factoryContract)
                    {
                        related.AddRange(ReviewRelationshipFacts.Declarations(context, factoryContract, "factory-returned-abstraction-contract"));
                        related.AddRange(ReviewRelationshipFacts.InterfaceImplementations(context, factoryContract));
                        related.AddRange(ReviewRelationshipFacts.TypeConsumers(context, factoryContract));
                    }
                }
                return subject with { RelatedAnchors = related.Distinct().ToArray(),
                    ObservedSignal = signal + "; compiler-resolved implementation/consumer relationship anchors: " + related.Count +
                        "; unresolved external/reflection/dynamic users are not inferred.",
                    EvidenceLimitation = related.Count == 0 ? "No source-backed implementation or consumer was resolved in loaded project context; external, reflection, and dynamic use remains unknown." : null };
            }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }
}
