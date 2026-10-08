using Microsoft.CodeAnalysis;

namespace DotNetAiCodeHygiene.Core;

internal sealed class AccessibilityStyleRuleModule : IRuleModule, IEditorConfigProjectionRule
{
    internal const string RuleId = "style.accessibility.explicit";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "finding", "finding", "Require explicit accessibility through the built-in IDE0040 policy.");
    public Rule Descriptor => RuleDescriptor;
    public IReadOnlyList<KeyValuePair<string, string>> EditorConfigEntries { get; } =
    [new("dotnet_style_require_accessibility_modifiers", "always"), new("dotnet_diagnostic.IDE0040.severity", "error")];
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.FindingsOnly(ProfileAnalysisRuleModule.Materialize(
        context.ProfileManager.AnalyzeEditorConfigRule(Descriptor.Id, EditorConfigEntries), Descriptor));
}
