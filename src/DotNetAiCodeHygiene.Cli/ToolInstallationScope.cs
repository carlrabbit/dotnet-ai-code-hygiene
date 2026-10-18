using System.Text.Json;

namespace DotNetAiCodeHygiene.Cli;

internal sealed record ToolInstallationScope(string? ManifestPath, string? ToolPath, string? ShimPath)
{
    internal bool IsResolved => ManifestPath is not null || ToolPath is not null;

    internal static ToolInstallationScope Resolve(string repositoryRoot, string packageId, string version, string? invocationPath, string? assemblyPath, string? dotnetHome)
    {
        if (!string.IsNullOrWhiteSpace(invocationPath))
        {
            string invoked = Path.GetFullPath(invocationPath);
            if (Path.GetFileName(invoked).StartsWith("dotnet-hygiene", StringComparison.OrdinalIgnoreCase)
                && !IsToolResolverCachePath(invoked))
            {
                // The executing apphost is the installation identity. Never scan PATH
                // for a same-named shim: PATH may contain a different installation.
                return new(null, Path.GetDirectoryName(invoked), invoked);
            }
        }

        // Local .NET tools run through dotnet.exe. Prove that the executing assembly
        // is the binary recorded in this CLI home’s resolver cache before associating
        // the invocation with an ancestor manifest.
        if (string.IsNullOrWhiteSpace(assemblyPath) || string.IsNullOrWhiteSpace(dotnetHome)) { return new(null, null, null); }
        string assembly = Path.GetFullPath(assemblyPath);
        string cache = Path.Combine(dotnetHome, ".dotnet", "toolResolverCache");
        if (!ResolverCacheMatches(cache, packageId, version, assembly)) { return new(null, null, null); }
        return FindManifest(repositoryRoot, packageId, version);
    }

    private static ToolInstallationScope FindManifest(string repositoryRoot, string packageId, string version)
    {
        foreach (string directory in Ancestors(Path.GetFullPath(repositoryRoot)))
        {
            string manifest = Path.Combine(directory, ".config", "dotnet-tools.json");
            if (!File.Exists(manifest)) { continue; }
            try
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifest));
                if (!document.RootElement.TryGetProperty("tools", out JsonElement tools) || tools.ValueKind != JsonValueKind.Object) { continue; }
                foreach (JsonProperty tool in tools.EnumerateObject())
                {
                    if (!string.Equals(tool.Name, packageId, StringComparison.OrdinalIgnoreCase)) { continue; }
                    if (!tool.Value.TryGetProperty("version", out JsonElement toolVersion)
                        || !string.Equals(toolVersion.GetString(), version, StringComparison.OrdinalIgnoreCase)) { return new(null, null, null); }
                    return new(Path.GetFullPath(manifest), null, null);
                }
            }
            catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException) { return new(null, null, null); }
        }
        return new(null, null, null);
    }

    private static bool ResolverCacheMatches(string cacheDirectory, string packageId, string version, string assemblyPath)
    {
        if (!Directory.Exists(cacheDirectory)) { return false; }
        try
        {
            foreach (string entry in Directory.EnumerateFiles(cacheDirectory, packageId, SearchOption.AllDirectories))
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(entry));
                foreach (JsonElement tool in document.RootElement.EnumerateArray())
                {
                    if (tool.TryGetProperty("Version", out JsonElement cachedVersion)
                        && string.Equals(cachedVersion.GetString(), version, StringComparison.OrdinalIgnoreCase)
                        && tool.TryGetProperty("PathToExecutable", out JsonElement executable)
                        && string.Equals(Path.GetFullPath(executable.GetString() ?? string.Empty), assemblyPath, StringComparison.OrdinalIgnoreCase)) { return true; }
                }
            }
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException or ArgumentException) { return false; }
        return false;
    }

    private static bool IsToolResolverCachePath(string path)
    {
        if (Path.GetDirectoryName(path) is not string directory) { return false; }
        for (DirectoryInfo? current = new(directory); current is not null; current = current.Parent)
        {
            if (string.Equals(current.Name, "toolResolverCache", StringComparison.OrdinalIgnoreCase)
                && string.Equals(current.Parent?.Name, ".dotnet", StringComparison.OrdinalIgnoreCase)) { return true; }
        }
        return false;
    }

    private static IEnumerable<string> Ancestors(string directory)
    {
        for (DirectoryInfo? current = new(directory); current is not null; current = current.Parent) { yield return current.FullName; }
    }
}
