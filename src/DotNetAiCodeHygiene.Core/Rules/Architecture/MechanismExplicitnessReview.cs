using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Finds explicit dynamic, reflection, and service-location constructs for review.</summary>
internal sealed class MechanismExplicitnessReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "architecture.mechanism.explicitness";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review changed dynamic and reflective mechanisms against simpler explicit alternatives.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", "Is the implicit machinery justified by a current need that simpler explicit C# cannot meet?")];
    private static readonly HashSet<string> MechanismMembers = new(StringComparer.Ordinal)
    { "CreateInstance", "GetType", "GetTypes", "GetMethods", "GetMethod", "GetProperty", "GetField", "Invoke", "GetService", "GetRequiredService", "GetServices", "MakeGenericType", "MakeGenericMethod", "LoadFrom", "LoadFile" };
    public Rule Descriptor => RuleDescriptor; public int BatchNumber => 26; public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => "Escalate only when a material contract or design decision cannot be resolved from current authority.";
    public string EscalatedReviewerClass => "planner";
    public RuleModuleResult Evaluate(RuleContext context)
    {
        var subjects = new List<ReviewSubject>(); var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            string path = context.RelativePath(document), source = context.Text(document).ToString(); var root = context.Root(document); var model = context.SemanticModel(document); var tree = context.Tree(document);
            var nodes = root.DescendantNodes().Where(node => node is InvocationExpressionSyntax or VariableDeclarationSyntax or ParameterSyntax or PropertyDeclarationSyntax);
            foreach (SyntaxNode node in nodes)
            {
                bool dynamicType = node switch
                {
                    VariableDeclarationSyntax v => v.Type is IdentifierNameSyntax { Identifier.ValueText: "dynamic" },
                    ParameterSyntax p => p.Type is IdentifierNameSyntax { Identifier.ValueText: "dynamic" },
                    PropertyDeclarationSyntax p => p.Type is IdentifierNameSyntax { Identifier.ValueText: "dynamic" },
                    _ => false
                };
                ISymbol? method = node is InvocationExpressionSyntax invocation ? model.GetSymbolInfo(invocation).Symbol : null;
                bool reflection = method is IMethodSymbol m && IsExplicitMechanism(m);
                if (!dynamicType && !reflection) { continue; }
                var span = node.Span; var pos = tree.GetLineSpan(span).StartLinePosition; ISymbol? anchor = method ?? model.GetDeclaredSymbol(node.Parent ?? node);
                string symbolName = anchor?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? node.ToString();
                string signal = dynamicType ? "dynamic-typed declaration" : "resolved mechanism call " + method!.ContainingType.ToDisplayString() + "." + method.Name;
                string id = context.ProjectIdentity(document) + "\0" + path + "\0" + symbolName + "\0" + pos.Line + "\0" + node.RawKind;
                const string legitimate = "Framework binding, serialization, interoperability, or a stable plugin ecosystem can justify dynamic mechanisms.";
                var item = new ReviewItem("", path, pos.Line + 1, pos.Character + 1, symbolName, "Candidate signal: " + signal + ".", anchor?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? signal);
                subjects.Add(new(id, node.WithoutLeadingTrivia().ToFullString() + "\0" + signal, item,
                    DirectChange: context.HasChangedLine(path, pos.Line + 1, tree.GetLineSpan(span).EndLinePosition.Line + 1),
                    ObservedSignal: signal, LegitimateCase: legitimate,
                    PrimaryAnchor: new(context.ProjectIdentity(document), path, pos.Line + 1, pos.Character + 1, symbolName, "primary changed/reporting mechanism")));
            }
            if (subjects.Any(x => x.Item.Path == path)) { sources[path] = source; }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }

    private static bool IsExplicitMechanism(IMethodSymbol method)
    {
        string type = method.ContainingType.ToDisplayString();
        string assembly = method.ContainingAssembly.Identity.Name;
        if ((assembly is "System.Reflection" or "System.Runtime") && type.StartsWith("System.Reflection.", StringComparison.Ordinal) && MechanismMembers.Contains(method.Name)) { return true; }
        if (assembly == "System.Runtime" && (type is "System.Type" or "System.Activator") && MechanismMembers.Contains(method.Name)) { return true; }
        return method.ContainingAssembly.Identity.Name == "Microsoft.Extensions.DependencyInjection.Abstractions" &&
            method.ContainingNamespace.ToDisplayString().StartsWith("Microsoft.Extensions.DependencyInjection", StringComparison.Ordinal) &&
            method.Name is "GetService" or "GetRequiredService" or "GetServices";
    }
}
