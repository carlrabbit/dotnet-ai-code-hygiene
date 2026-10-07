using System.Security.Cryptography;
using System.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed class ProfileStyleCopRuleModule : IRuleModule
{
    internal const string RuleId = "profile.stylecop.prohibited";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "finding", "finding", "Prohibit StyleCop analyzers.", false);

    internal const string FindingMessage = "StyleCop analyzers are prohibited by the supported profile.";
    internal const string FindingSuggestion = "Resolve the analyzer dependency and its policy impact explicitly.";

    public Rule Descriptor => RuleDescriptor;

    public RuleModuleResult Evaluate(RuleContext context)
    {
        IReadOnlyList<ProfileFinding> diagnosis = context.Session.GetFact(
            ("profile-rule", Descriptor.Id), context.ProfileManager.AnalyzeStyleCop);
        return RuleModuleResult.FindingsOnly(ProfileAnalysisRuleModule.Materialize(diagnosis, Descriptor));
    }
}
