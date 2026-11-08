namespace DotNetAiCodeHygiene.Core;

/// <summary>Stable catalog entries for the fixed v3 formatter policy and review-only historical cleanup rules.</summary>
internal sealed class FixedMechanicalRuleModule(string id, string disposition) : IRuleModule
{
    public Rule Descriptor { get; } = new(id, 1, "none", "none", disposition, Configurable: false, Diagnose: false);
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;

    internal static IReadOnlyList<IRuleModule> All { get; } =
    [
        new FixedMechanicalRuleModule("format.csharp.roslyn", "SDK dotnet format whitespace; see the v3 formatter allowlist."),
        new FixedMechanicalRuleModule("style.qualification.this.unnecessary", "Review-only; no equivalent approved SDK formatter disposition."),
        new FixedMechanicalRuleModule("style.qualification.redundant", "Review-only; no equivalent approved SDK formatter disposition."),
        new FixedMechanicalRuleModule("style.object-creation.target-typed", "SDK style diagnostic IDE0090."),
        new FixedMechanicalRuleModule("style.default.literal", "SDK style diagnostic IDE0034."),
        new FixedMechanicalRuleModule("style.cast.redundant", "SDK style diagnostic IDE0004."),
        new FixedMechanicalRuleModule("style.interpolation.redundant", "Review-only: IDE0071 simplifies explicit ToString calls inside interpolations; it is not equivalent to converting interpolation without holes to a string literal."),
        new FixedMechanicalRuleModule("style.names.inferred", "SDK style diagnostic IDE0037."),
        new FixedMechanicalRuleModule("style.usings.unused", "SDK style diagnostic IDE0005."),
        new FixedMechanicalRuleModule("style.usings.organize", "SDK import organization and whitespace formatting."),
        new FixedMechanicalRuleModule("style.modifiers.organize", "SDK style diagnostic IDE0036."),
        new FixedMechanicalRuleModule("style.null-forgiving.redundant", "Review-only; the prior narrow custom flow analysis is not applied automatically."),
        new FixedMechanicalRuleModule("style.whitespace.blank-lines", "SDK dotnet format whitespace; exact legacy excess-blank-line parity is not claimed."),
        new FixedMechanicalRuleModule("style.parentheses.redundant", "Review-only; IDE0047 is outside the approved automatic diagnostic set.")
    ];
}
