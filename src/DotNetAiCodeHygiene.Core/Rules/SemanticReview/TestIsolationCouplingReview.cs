using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class TestIsolationCouplingReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "tests.isolation.coupling";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review changed tests where new doubles or setup abstractions intersect production seams.", true);
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
                var createdTypes = method.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                    .Select(creation => model.GetTypeInfo(creation).Type).Where(type => type is not null).Select(type => type!.Name).ToArray();
                var declaredTypes = method.DescendantNodes().OfType<VariableDeclarationSyntax>()
                    .SelectMany(variable => variable.Variables).Select(variable => model.GetDeclaredSymbol(variable) as ILocalSymbol)
                    .Where(local => local is not null).Select(local => local!.Type.Name).ToArray();
                string[] doubleTypes = createdTypes.Concat(declaredTypes).Where(name => DoubleNameFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase))).Distinct(StringComparer.Ordinal).ToArray();
                string[] setupCalls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(call => TestAssertionEffectivenessReviewRuleModule.GetCallName(call.Expression))
                    .Where(name => name is "Setup" or "SetUp" or "BeforeEach" or "BeforeAll" or "Returns" or "Substitute").Distinct(StringComparer.Ordinal).ToArray();
                if (doubleTypes.Length == 0 && setupCalls.Length == 0) { continue; }
                var lines = context.Tree(document).GetLineSpan(method.Span);
                string methodName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string observed = "Mechanically observed test double/fixture type(s): " + string.Join(", ", doubleTypes.Order(StringComparer.Ordinal))
                    + "; setup-related call(s): " + string.Join(", ", setupCalls.Order(StringComparer.Ordinal));
                var item = new ReviewItem("", path, lines.StartLinePosition.Line + 1, lines.StartLinePosition.Character + 1, methodName, observed, symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                string identity = context.ProjectIdentity(document) + "\0" + path + "\0test-double:" + methodName;
                subjects.Add(new ReviewSubject(identity, method.WithoutLeadingTrivia().ToFullString() + "\0" + observed, item,
                    DirectChange: context.HasChangedLine(path, lines.StartLinePosition.Line + 1, lines.EndLinePosition.Line + 1),
                    ObservedSignal: observed,
                    LegitimateCase: "A fake at a real external side-effect boundary or straightforward setup helper can be appropriate; a double alone is not a defect.",
                    PrimaryAnchor: new ReviewAnchor(context.ProjectIdentity(document), path, lines.StartLinePosition.Line + 1, lines.StartLinePosition.Character + 1, methodName, "changed-test-double-or-setup")));
                sources.TryAdd(path, source);
            }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }
}
