using AutoRewriteEngine = DotNetAiWorkbench.Autoformat.Core.RewriteEngine;
using AutoRewriteResult = DotNetAiWorkbench.Autoformat.Core.RewriteResult;

namespace DotNetAiCodeHygiene.Core;

public sealed record RewriteResult(string Command, bool CheckOnly, int TargetCount, int ChangedCount, int UnchangedCount, IReadOnlyList<string> ChangedPaths, IReadOnlyList<string> SelectedRuleIds);

/// <summary>Transitional adapter; mechanical formatting is owned by the standalone Autoformat engine.</summary>
public sealed class RewriteEngine
{
    internal static IReadOnlyList<string> ApprovedDiagnostics => AutoRewriteEngine.ApprovedDiagnostics;
    private readonly AutoRewriteEngine inner;
    public RewriteEngine(string? cwd = null) => inner = new(cwd, requireCleanCheckpoint: false);
    internal RewriteEngine(string? cwd, Func<string, string, string[], string[], bool, string, (int ExitCode, string Output)> phaseRunner)
        => inner = new(cwd, phaseRunner);
    public string Root => inner.Root;
    public RewriteResult Rewrite(string command, string[] paths, bool changed, bool checkOnly)
    {
        try
        {
            AutoRewriteResult result = inner.Rewrite(command, paths, changed, checkOnly);
            return new(result.Command, result.CheckOnly, result.TargetCount, result.ChangedCount, result.UnchangedCount, result.ChangedPaths, result.SelectedRuleIds);
        }
        catch (DotNetAiWorkbench.Autoformat.Core.ProductException e) { throw new ProductException(e.Message); }
        catch (DotNetAiWorkbench.Autoformat.Core.EnvironmentException e) { throw new EnvironmentException(e.Message); }
    }
}
