namespace DotNetAiCodeHygiene.Core;

/// <inheritdoc/>
internal interface IProfileRuleModule
{
/// <inheritdoc/>
    public string RuleId { get; }
/// <inheritdoc/>
    public IReadOnlyList<ProfileFinding> Evaluate(ProfileManager manager, RepositorySession session);
}

/// <summary>Runs mandatory profile diagnosis modules.</summary>
internal sealed class ProfileRuleRunner(IReadOnlyList<IProfileRuleModule> modules)
{
    /// <summary>Returns deterministic profile findings without applying remediation.</summary>
    internal IReadOnlyList<ProfileFinding> Run(ProfileManager manager, RepositorySession session) =>
        modules.SelectMany(module => module.Evaluate(manager, session)).ToArray();
}

/// <inheritdoc/>
internal sealed class ProfileAnalysisRuleModule : IProfileRuleModule
{
/// <inheritdoc/>
    public string RuleId => "profile.dotnet.analysis.required";
/// <inheritdoc/>
    public IReadOnlyList<ProfileFinding> Evaluate(ProfileManager manager, RepositorySession session) =>
        session.GetFact("profile-diagnosis", manager.Analyze).Where(finding => finding.RuleId == RuleId).ToArray();
}

/// <inheritdoc/>
internal sealed class ProfileStyleCopRuleModule : IProfileRuleModule
{
/// <inheritdoc/>
    public string RuleId => "profile.stylecop.prohibited";
/// <inheritdoc/>
    public IReadOnlyList<ProfileFinding> Evaluate(ProfileManager manager, RepositorySession session) =>
        session.GetFact("profile-diagnosis", manager.Analyze).Where(finding => finding.RuleId == RuleId).ToArray();
}
