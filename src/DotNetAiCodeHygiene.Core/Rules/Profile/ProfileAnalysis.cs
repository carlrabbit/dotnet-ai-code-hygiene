using System.Security.Cryptography;
using System.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed class ProfileAnalysisRuleModule : IRuleModule
{
    internal const string RuleId = "profile.dotnet.analysis.required";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "finding", "finding", "Require the supported .NET analysis profile.", false);

    internal const string ArtifactMissing = "The generated supported-profile MSBuild artifact is missing or drifted.";
    internal const string ArtifactSuggestion = "Run hygiene update to reconcile hygiene-owned profile state.";
    internal const string ImportMissing = "The root hygiene profile import is missing or drifted.";
    internal const string ImportSuggestion = "Run hygiene update to reconcile the managed import.";
    internal const string EditorBlockMissing = "The root hygiene profile EditorConfig block is missing or drifted.";
    internal const string EditorBlockSuggestion = "Run hygiene update to reconcile the managed section.";
    internal const string WarningPromotionSuggestion = "Remove the global warning-promotion setting.";
    internal const string BuildParseFailure = "Build configuration could not be parsed for profile analysis.";
    internal const string BuildParseSuggestion = "Repair the project XML.";
    internal const string PropertySuggestion = "Correct the effective MSBuild property.";
    internal const string EvaluationFailure = "Effective MSBuild configuration could not be evaluated.";
    internal const string EvaluationSuggestion = "Repair the project/import configuration and retry profile analysis.";
    internal static string WarningPromotion(string property) => $"Repository-configured {property} violates the supported profile.";
    internal static string PropertyMismatch(string property, string expected, string configuration, string actual) => $"Effective project property {property} must be {expected} for {configuration} (found '{actual}').";
    internal static string EditorSettingMismatch(string key, string expected, string? actual) => $"Effective EditorConfig setting {key} must be {expected} (found '{actual ?? "<unset>"}').";
    internal static string EditorSettingSuggestion(string key, string expected) => $"Set {key} = {expected} in the effective configuration.";
    public Rule Descriptor => RuleDescriptor;

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
