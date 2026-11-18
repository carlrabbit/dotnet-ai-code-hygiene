using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class TestIsolationCouplingReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "tests.isolation.coupling";
    private static readonly Rule RuleDescriptor = new(RuleId, 2, "review-batch", "review-batch", "Review changed tests where new doubles or setup abstractions intersect production seams.", true);
    private static readonly HashSet<string> DoubleNameFragments = new(StringComparer.OrdinalIgnoreCase) { "Mock", "Fake", "Stub", "Spy" };
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 33;
    public IReadOnlyList<ReviewQuestion> Questions { get; } = [new("Q1", "Does this test introduce brittle implementation coupling or unnecessary production abstractions solely to make testing possible?")];
    public string EscalationCondition => "Escalate to frontier when the intended seam or observable contract cannot be established; a double alone is not evidence of a problem.";
    public string EscalatedReviewerClass => "frontier";

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var subjects = new List<ReviewSubject>();
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            SyntaxNode root = context.Root(document);
            SemanticModel model = context.SemanticModel(document);
            string path = context.RelativePath(document), source = context.Text(document).ToString();
            foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(method) is not IMethodSymbol symbol || !TestAssertionEffectivenessReviewRuleModule.IsTest(symbol)) { continue; }
                ITypeSymbol[] created = method.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                    .Select(creation => model.GetTypeInfo(creation).Type).Where(type => type is not null).Cast<ITypeSymbol>().ToArray();
                ITypeSymbol[] declared = method.DescendantNodes().OfType<VariableDeclarationSyntax>()
                    .SelectMany(variable => variable.Variables).Select(variable => model.GetDeclaredSymbol(variable) as ILocalSymbol)
                    .Where(local => local is not null).Select(local => local!.Type).ToArray();
                ITypeSymbol[] doubleSymbols = created.Concat(declared).Where(type => DoubleNameFragments.Any(fragment => type.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                    .Distinct(SymbolEqualityComparer.Default).Cast<ITypeSymbol>().ToArray();
                string[] doubleTypes = doubleSymbols.Select(type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).ToArray();
                string[] setupCalls = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Where(call => model.GetSymbolInfo(call).Symbol is IMethodSymbol target && IsMockFrameworkCall(target))
                    .Select(call => model.GetSymbolInfo(call).Symbol!.Name).Distinct(StringComparer.Ordinal).ToArray();
                if (doubleTypes.Length == 0 && setupCalls.Length == 0) { continue; }
                var lines = context.Tree(document).GetLineSpan(method.Span);
                string methodName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string observed = "Mechanically observed test double/fixture type(s): " + string.Join(", ", doubleTypes.Order(StringComparer.Ordinal))
                    + "; setup-related call(s): " + string.Join(", ", setupCalls.Order(StringComparer.Ordinal));
                var item = new ReviewItem("", path, lines.StartLinePosition.Line + 1, lines.StartLinePosition.Character + 1, methodName, observed, symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                string identity = context.ProjectIdentity(document) + "\0" + path + "\0test-double:" + methodName;
                ITypeSymbol[] captured = doubleSymbols;
                subjects.Add(new ReviewSubject(identity, method.WithoutLeadingTrivia().ToFullString() + "\0" + observed, item,
                    DirectChange: context.HasChangedLine(path, lines.StartLinePosition.Line + 1, lines.EndLinePosition.Line + 1),
                    ObservedSignal: observed,
                    LegitimateCase: "A fake at a real external side-effect boundary or straightforward setup helper can be appropriate; a double alone is not a defect.",
                    PrimaryAnchor: new ReviewAnchor(context.ProjectIdentity(document), path, lines.StartLinePosition.Line + 1, lines.StartLinePosition.Character + 1, methodName, "changed-test-double-or-setup"),
                    Enrich: (_, subject) =>
                    {
                        var seams = new List<ISymbol?>();
                        foreach (ITypeSymbol type in captured)
                        {
                            if (type is INamedTypeSymbol named)
                            {
                                seams.AddRange(named.AllInterfaces.Where(iface => !SymbolEqualityComparer.Default.Equals(iface.ContainingAssembly, symbol.ContainingAssembly)));
                                for (INamedTypeSymbol? baseType = named.BaseType; baseType is not null && baseType.SpecialType != SpecialType.System_Object; baseType = baseType.BaseType)
                                {
                                    if (!SymbolEqualityComparer.Default.Equals(baseType.ContainingAssembly, symbol.ContainingAssembly)) { seams.Add(baseType); }
                                }
                                seams.AddRange(named.TypeArguments.Where(argument => !SymbolEqualityComparer.Default.Equals(argument.ContainingAssembly, symbol.ContainingAssembly)));
                            }
                        }
                        ReviewAnchor[] seamAnchors = ReviewRelationshipFacts.RelatedSymbols(context, seams, "compiler-resolved-production-seam-for-test-double");
                        return subject with { RelatedAnchors = seamAnchors, ObservedSignal = observed + "; production seam relationship: " +
                            (seamAnchors.Length == 0 ? "not resolved from the test double/setup; no seam is inferred" : "compiler-resolved source declarations included"),
                            EvidenceLimitation = seamAnchors.Length == 0 ? "The changed test double/setup has no compiler-resolved production seam in loaded source; no seam is inferred." : null };
                    }));
                sources.TryAdd(path, source);
            }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }

    private static bool IsMockFrameworkCall(IMethodSymbol method)
    {
        string assembly = method.ContainingAssembly.Identity.Name;
        return assembly switch
        {
            "Moq" => method.Name is "Setup" or "Returns" or "Verify",
            "NSubstitute" => method.Name is "Returns" or "Received" or "Substitute",
            "FakeItEasy" => method.Name is "A" or "That" or "Returns" or "MustHaveHappened",
            _ => false
        };
    }
}
