using AutoRewriteEngine = DotNetAiWorkbench.Autoformat.Core.RewriteEngine;
using AutoRewriteResult = DotNetAiWorkbench.Autoformat.Core.RewriteResult;

namespace DotNetAiCodeHygiene.Core;

public sealed record RewriteResult(string Command, int TargetCount, int ChangedCount, int UnchangedCount, IReadOnlyList<string> ChangedPaths, IReadOnlyList<string> SelectedRuleIds);

/// <summary>Transitional adapter; mechanical formatting is owned by the standalone Autoformat engine.</summary>
public sealed class RewriteEngine
{
    internal static IReadOnlyList<string> ApprovedDiagnostics => AutoRewriteEngine.ApprovedDiagnostics;
    private readonly AutoRewriteEngine inner;
    public RewriteEngine(string? cwd = null) => inner = new(cwd, requireCleanCheckpoint: false);
    internal RewriteEngine(string? cwd, Func<string, string, string[], string[], string, (int ExitCode, string Output)> phaseRunner)
        => inner = new(cwd, (workspace, phase, files, diagnostics, _, reportDirectory)
            => phaseRunner(workspace, phase, files, diagnostics, reportDirectory));
    public string Root => inner.Root;
    public RewriteResult Rewrite(string command, string[] paths)
    {
        try
        {
            AutoRewriteResult result = inner.Rewrite(command, paths);
            return new(result.Command, result.TargetCount, result.ChangedCount, result.UnchangedCount, result.ChangedPaths, result.SelectedRuleIds);
        }
        catch (DotNetAiWorkbench.Autoformat.Core.ProductException e) { throw new ProductException(e.Message); }
        catch (DotNetAiWorkbench.Autoformat.Core.EnvironmentException e) { throw new EnvironmentException(e.Message); }
    }
}
