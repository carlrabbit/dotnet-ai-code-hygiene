using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;

namespace DotNetAiCodeHygiene.Core;

internal sealed class RoslynFormatRuleModule : IRuleModule, IFormatRemediationRule
{
    private static readonly Rule RuleDescriptor = new("format.csharp.roslyn", 1, "none", "none", "Apply the supported project-aware Roslyn C# formatter.", true, Diagnose: false);
    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;
    public Document Format(Document document, Compilation compilation) => Formatter.FormatAsync(document).GetAwaiter().GetResult();
}
