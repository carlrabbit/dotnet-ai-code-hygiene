using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class TestAssertionEffectivenessReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "tests.assertion.effectiveness";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review changed framework-recognized tests that assert observable outcomes.", true);
    private static readonly HashSet<string> TestAttributes = new(StringComparer.Ordinal)
    {
        "TUnit.Core.TestAttribute", "Xunit.FactAttribute", "Xunit.TheoryAttribute",
        "NUnit.Framework.TestAttribute", "NUnit.Framework.TestCaseAttribute",
        "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute", "Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethodAttribute"
    };
    private static readonly HashSet<string> OutcomeCalls = new(StringComparer.OrdinalIgnoreCase)
    {
        "Assert", "That", "Should", "Verify", "Throws", "ThrowsAsync", "DoesNotThrow", "Fail", "Pass", "Equal", "NotEqual", "True", "False", "Null", "NotNull", "Contains", "DoesNotContain", "IsType", "IsAssignableFrom"
    };
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 31;
    public IReadOnlyList<ReviewQuestion> Questions { get; } = [new("Q1", "Would its assertions detect a material contract violation, not merely non-throwing execution?")];
    public string EscalationCondition => "Escalate material uncertainty about the observable contract or whether the checks establish it to frontier; assertion presence is only a candidate signal.";
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
            foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>().OrderBy(node => node.SpanStart))
            {
                if (model.GetDeclaredSymbol(method) is not IMethodSymbol symbol || !IsTest(symbol) || method.Body is null && method.ExpressionBody is null) { continue; }
                bool hasObservableCheck = method.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(call => OutcomeCalls.Contains(GetCallName(call.Expression)));
                if (!hasObservableCheck) { continue; }
                string methodName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                int first = context.Tree(document).GetLineSpan(method.Span).StartLinePosition.Line + 1;
                int last = context.Tree(document).GetLineSpan(method.Span).EndLinePosition.Line + 1;
                var pos = context.Tree(document).GetLineSpan(method.Span).StartLinePosition;
                string identity = context.ProjectIdentity(document) + "\0" + path + "\0test:" + methodName;
                var item = new ReviewItem("", path, pos.Line + 1, pos.Character + 1, methodName, "", symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                subjects.Add(new ReviewSubject(identity, method.WithoutLeadingTrivia().ToFullString(), item,
                    DirectChange: context.HasChangedLine(path, first, last),
                    ObservedSignal: "Roslyn-resolved test attribute and assertion/outcome-check invocation are present.",
                    LegitimateCase: "Assertion presence does not establish assertion strength; observable behavior may also be checked through an integration or contract harness.",
                    PrimaryAnchor: new ReviewAnchor(context.ProjectIdentity(document), path, pos.Line + 1, pos.Character + 1, methodName, "changed-test-with-observable-check")));
                sources.TryAdd(path, source);
            }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }

    internal static bool IsTest(IMethodSymbol method) => method.GetAttributes().Any(attribute => IsKnownTestAttribute(attribute.AttributeClass));
    internal static bool IsKnownTestAttribute(INamedTypeSymbol? type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (TestAttributes.Contains(current.ToDisplayString())) { return true; }
        }
        return false;
    }
    internal static string GetCallName(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        _ => ""
    };
}
