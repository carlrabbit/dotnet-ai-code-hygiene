using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Finds changed explicit abstractions for independent contract review.</summary>
internal sealed class AbstractionNecessityReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "architecture.abstraction.necessity";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review changed abstractions against current contracts and variability.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", "Does it serve a current contract, boundary, side effect or meaningful variability?")];
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 20;
    public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => "Escalate only when a material contract or design decision cannot be resolved from current authority.";
    public string EscalatedReviewerClass => "planner";

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var subjects = new List<ReviewSubject>();
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            string path = context.RelativePath(document), source = context.Text(document).ToString();
            var root = context.Root(document); SemanticModel model = context.SemanticModel(document); SyntaxTree tree = context.Tree(document);
            foreach (SyntaxNode node in root.DescendantNodes().Where(n => n is InterfaceDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax or MethodDeclarationSyntax))
            {
                ISymbol? symbol = node switch
                {
                    BaseTypeDeclarationSyntax type => model.GetDeclaredSymbol(type),
                    MethodDeclarationSyntax method => model.GetDeclaredSymbol(method),
                    _ => null
                };
                if (symbol is null) { continue; }
                bool interfaceOrAbstract = node is InterfaceDeclarationSyntax || symbol.IsAbstract || symbol is INamedTypeSymbol { TypeParameters.Length: > 0 } || symbol is IMethodSymbol { TypeParameters.Length: > 0 };
                bool factory = node is MethodDeclarationSyntax m && (m.Identifier.ValueText.StartsWith("Create", StringComparison.Ordinal) || m.Identifier.ValueText.StartsWith("Build", StringComparison.Ordinal) || model.GetTypeInfo(m.ReturnType).Type is INamedTypeSymbol { TypeKind: TypeKind.Interface or TypeKind.Delegate } or { IsAbstract: true });
                if (!interfaceOrAbstract && !factory) { continue; }
                string identity = context.ProjectIdentity(document) + "\0" + path + "\0" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var span = node.Span; var pos = tree.GetLineSpan(span).StartLinePosition;
                const string signal = "Declared interface/abstract/generic contract or factory-shaped method";
                const string legitimate = "A one-implementation interface may legitimately isolate a persistence, transport, or public contract.";
                var item = new ReviewItem("", path, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "Candidate signal: " + signal + ".", symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                subjects.Add(new(identity, node.WithoutLeadingTrivia().ToFullString(), item,
                    DirectChange: context.HasChangedLine(path, pos.Line + 1, tree.GetLineSpan(span).EndLinePosition.Line + 1),
                    ObservedSignal: signal, LegitimateCase: legitimate,
                    PrimaryAnchor: new(context.ProjectIdentity(document), path, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "primary changed/reporting declaration")));
            }
            if (subjects.Any(x => x.Item.Path == path)) { sources[path] = source; }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }
}
