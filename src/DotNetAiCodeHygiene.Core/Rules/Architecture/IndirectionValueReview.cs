using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Finds changed methods whose body is a single forwarding operation.</summary>
internal sealed class IndirectionValueReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "architecture.indirection.value";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review changed forwarding methods and delegation chains for owned behavior.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", "Does the intermediary own meaningful transformation, policy, integration or isolation?")];
    public Rule Descriptor => RuleDescriptor; public int BatchNumber => 21; public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => "Escalate only when a material ownership or boundary decision cannot be resolved from current authority.";
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
                ISymbol? symbol = model.GetDeclaredSymbol(method);
                if (symbol is null || !IsForwarder(method)) { continue; }
                InvocationExpressionSyntax? call = ForwardedInvocation(method);
                if (call is null) { continue; }
                ISymbol? target = model.GetSymbolInfo(call).Symbol;
                string relation = target?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "unresolved call target";
                string key = context.ProjectIdentity(document) + "\0" + path + "\0" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var line = tree.GetLineSpan(method.Span).StartLinePosition;
                const string signal = "Method body contains one invocation and no other executable statement";
                const string legitimate = "A cross-boundary adapter or authorization boundary can legitimately forward.";
                var item = new ReviewItem("", path, line.Line + 1, line.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "Candidate signal: " + signal + ". Callee: " + relation, symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                subjects.Add(new(key, method.WithoutLeadingTrivia().ToFullString() + "\0callee:" + relation, item,
                    DirectChange: context.HasChangedLine(path, line.Line + 1, tree.GetLineSpan(method.Span).EndLinePosition.Line + 1),
                    ObservedSignal: signal + "; resolved callee: " + relation, LegitimateCase: legitimate,
                    PrimaryAnchor: new(context.ProjectIdentity(document), path, line.Line + 1, line.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "primary changed/reporting method")));
            }
            if (subjects.Any(x => x.Item.Path == path)) { sources[path] = source; }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }
    private static bool IsForwarder(MethodDeclarationSyntax method)
    {
        if (method.ExpressionBody is not null) { return method.ExpressionBody.Expression is InvocationExpressionSyntax; }
        if (method.Body is null) { return false; }
        StatementSyntax[] executable = method.Body.Statements.Where(s => s is not LocalFunctionStatementSyntax).ToArray();
        return executable.Length == 1 && executable[0] switch
        {
            ReturnStatementSyntax { Expression: InvocationExpressionSyntax } => true,
            ExpressionStatementSyntax { Expression: InvocationExpressionSyntax } => true,
            _ => false
        };
    }

    private static InvocationExpressionSyntax? ForwardedInvocation(MethodDeclarationSyntax method)
    {
        if (method.ExpressionBody?.Expression is InvocationExpressionSyntax expressionCall) { return expressionCall; }
        if (method.Body is null) { return null; }
        StatementSyntax[] executable = method.Body.Statements.Where(statement => statement is not LocalFunctionStatementSyntax).ToArray();
        if (executable.Length != 1) { return null; }
        return executable[0] switch
        {
            ReturnStatementSyntax { Expression: InvocationExpressionSyntax returnedCall } => returnedCall,
            ExpressionStatementSyntax { Expression: InvocationExpressionSyntax statementCall } => statementCall,
            _ => null
        };
    }
}
