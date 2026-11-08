using Microsoft.CodeAnalysis;

namespace DotNetAiCodeHygiene.Core;

internal static class CandidatePathFilter
{
    internal static bool Matches(Diagnostic diagnostic, string? onlyPath)
    {
        if (onlyPath is null) { return true; }
        string? sourcePath = diagnostic.Location.SourceTree?.FilePath;
        return sourcePath is not null && string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(onlyPath), StringComparison.OrdinalIgnoreCase);
    }
}
