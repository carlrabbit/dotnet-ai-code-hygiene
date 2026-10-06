using System.Security.Cryptography;
using System.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed class ProfileAnalysisRuleModule : IRuleModule
{
    public Rule Descriptor => RuleCatalog.Get("profile.dotnet.analysis.required");

    public RuleModuleResult Evaluate(RuleContext context)
    {
        IReadOnlyList<ProfileFinding> diagnosis = context.Session.GetFact(
            ("profile-rule", Descriptor.Id), context.ProfileManager.AnalyzeRequiredProfile);
        return RuleModuleResult.FindingsOnly(Materialize(diagnosis, Descriptor));
    }

    internal static IReadOnlyList<Finding> Materialize(IEnumerable<ProfileFinding> diagnosis, Rule rule) => diagnosis.Select(finding =>
    {
        string anchor = "profile:" + finding.Path;
        string fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rule.Id + "\0" + anchor + "\0" + finding.Message)));
        return new Finding("", "", rule.Id, rule.Version, rule.Classification, finding.Path, 1, 1, null,
            finding.Message, finding.Suggestion, finding.Message, finding.Message, finding.Suggestion, anchor, fingerprint);
    }).ToArray();
}

internal sealed class ProfileStyleCopRuleModule : IRuleModule
{
    public Rule Descriptor => RuleCatalog.Get("profile.stylecop.prohibited");

    public RuleModuleResult Evaluate(RuleContext context)
    {
        IReadOnlyList<ProfileFinding> diagnosis = context.Session.GetFact(
            ("profile-rule", Descriptor.Id), context.ProfileManager.AnalyzeStyleCop);
        return RuleModuleResult.FindingsOnly(ProfileAnalysisRuleModule.Materialize(diagnosis, Descriptor));
    }
}
