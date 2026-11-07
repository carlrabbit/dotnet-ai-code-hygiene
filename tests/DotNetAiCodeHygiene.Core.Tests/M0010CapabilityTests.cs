using DotNetAiCodeHygiene.Core;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class M0010CapabilityTests
{
    [Test]
    public async Task MechanicalRewriteRulesAreNotPresentedAsExecutablePerRuleRewriters()
    {
        foreach (Rule rule in RuleCatalog.All)
        {
            await Assert.That(rule.FormatRemediate).IsFalse();
            await Assert.That(rule.NormalizeRemediate).IsFalse();
        }

        await Assert.That(RuleCatalog.All.Any(rule => rule.Id == "modernization.source-exception")).IsTrue();
        await Assert.That(RuleCatalog.All.Any(rule => rule.Id == "style.braces.required" && !rule.Configurable)).IsTrue();
        await Assert.That(RuleCatalog.All.Any(rule => rule.Id == "style.accessibility.explicit" && !rule.Configurable)).IsTrue();
    }

    [Test]
    public async Task SDKFormatterUsesFixedApprovedDiagnosticInventory()
    {
        string[] expected = ["IDE0005", "IDE0011", "IDE0036", "IDE0040", "IDE0090", "IDE0034", "IDE0004", "IDE0071", "IDE0037"];
        await Assert.That(RewriteEngine.ApprovedDiagnostics).IsEquivalentTo(expected);
    }
}
