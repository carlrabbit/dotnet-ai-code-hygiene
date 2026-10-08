using Microsoft.CodeAnalysis;

namespace DotNetAiCodeHygiene.Core;

internal sealed class BracesStyleRuleModule : IRuleModule, IEditorConfigProjectionRule
{
    internal const string RuleId = "style.braces.required";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "finding", "finding", "Require braces through the built-in IDE0011 policy.");
    public Rule Descriptor => RuleDescriptor;
    public IReadOnlyList<KeyValuePair<string, string>> EditorConfigEntries { get; } =
    [new("csharp_prefer_braces", "true"), new("dotnet_diagnostic.IDE0011.severity", "error")];
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.FindingsOnly(ProfileAnalysisRuleModule.Materialize(
        context.ProfileManager.AnalyzeEditorConfigRule(Descriptor.Id, EditorConfigEntries), Descriptor));
}
