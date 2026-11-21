using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Build.Locator;

namespace DotNetAiWorkbench.Autoformat.Core;

internal static class RoslynWorkspaceRegistration
{
    private static readonly object Gate = new();
    internal static void EnsureRegistered()
    {
        lock (Gate)
        {
            if (!MSBuildLocator.IsRegistered) { MSBuildLocator.RegisterDefaults(); }
        }
    }
}
