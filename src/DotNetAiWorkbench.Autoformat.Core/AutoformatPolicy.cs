namespace DotNetAiWorkbench.Autoformat.Core;

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

/// <summary>The sole owner of the fixed mechanical EditorConfig policy used by Autoformat and transitional hygiene.</summary>
public static class AutoformatPolicy
{
    public const string StartMarker = "# autoformat profile:begin";
    public const string EndMarker = "# autoformat profile:end";
    public static IReadOnlyList<string> ApprovedDiagnostics { get; } = new[] { "IDE0005", "IDE0011", "IDE0036", "IDE0040", "IDE0090", "IDE0034", "IDE0004", "IDE0037" };

    public static IReadOnlyList<(string Key, string Value)> MechanicalOptions { get; } = new (string Key, string Value)[]
    {
        ("indent_style", "space"), ("indent_size", "4"), ("dotnet_sort_system_directives_first", "true"),
        ("dotnet_separate_import_directive_groups", "false"), ("csharp_prefer_braces", "true"),
        ("csharp_preferred_modifier_order", "public,private,protected,internal,file,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,required,volatile,async"),
        ("dotnet_style_require_accessibility_modifiers", "always"), ("csharp_style_implicit_object_creation_when_type_is_apparent", "true"),
        ("csharp_prefer_simple_default_expression", "true"), ("dotnet_style_coalesce_expression", "true"),
        ("dotnet_style_prefer_inferred_tuple_names", "true"), ("dotnet_style_prefer_inferred_anonymous_type_member_names", "true"),
        ("dotnet_diagnostic.IDE0005.severity", "warning"), ("dotnet_diagnostic.IDE0004.severity", "warning"),
        ("dotnet_diagnostic.IDE0011.severity", "error"), ("dotnet_diagnostic.IDE0034.severity", "warning"),
        ("dotnet_diagnostic.IDE0036.severity", "warning"), ("dotnet_diagnostic.IDE0040.severity", "error"),
        ("dotnet_diagnostic.IDE0037.severity", "warning"), ("dotnet_diagnostic.IDE0090.severity", "warning")
    };

    public static string BuildEditorConfigBlock(string start, string end) => string.Join("\n",
        new[] { start, "[*.cs]" }.Concat(MechanicalOptions.Select(option => option.Key + " = " + option.Value)).Append(end));

    /// <summary>Ensures the single mechanical policy block exists without rewriting unrelated user content.</summary>
    public static bool EnsureInitialized(string root) => EnsureInitialized(root, null, false);

    public static bool EnsureInitialized(string root, Action? beforeReplace) => EnsureInitialized(root, beforeReplace, false);

    public static bool EnsureInitializedForHygiene(string root) => EnsureInitialized(root, null, true);

    private static bool EnsureInitialized(string root, Action? beforeReplace, bool ignoreInheritedOverrides)
    {
        root = Path.GetFullPath(root);
        string path = Path.Combine(root, ".editorconfig");
        bool existed = File.Exists(path);
        byte[] original = existed ? File.ReadAllBytes(path) : [];
        string text = new System.Text.UTF8Encoding(false, true).GetString(original);
        if (text.Contains(StartMarker, StringComparison.Ordinal) || text.Contains(EndMarker, StringComparison.Ordinal))
        {
            int start = text.IndexOf(StartMarker, StringComparison.Ordinal), end = text.IndexOf(EndMarker, StringComparison.Ordinal);
            if (start < 0 || end < start || start != text.LastIndexOf(StartMarker, StringComparison.Ordinal) || end != text.LastIndexOf(EndMarker, StringComparison.Ordinal))
            {
                throw new ProductException("Malformed or duplicate Autoformat policy markers; repair .editorconfig before initialization.");
            }

            ValidateEffectiveMechanicalSettings(root, ignoreInheritedOverrides);
            return false;
        }

        string hygieneStart = "# hygiene profile:begin", hygieneEnd = "# hygiene profile:end";
        if (text.Contains(hygieneStart, StringComparison.Ordinal) != text.Contains(hygieneEnd, StringComparison.Ordinal))
        {
            throw new ProductException("Malformed hygiene-managed EditorConfig markers; repair .editorconfig before initialization.");
        }
        if (text.Contains(hygieneStart, StringComparison.Ordinal) &&
             (text.IndexOf(hygieneStart, StringComparison.Ordinal) != text.LastIndexOf(hygieneStart, StringComparison.Ordinal) || text.IndexOf(hygieneEnd, StringComparison.Ordinal) != text.LastIndexOf(hygieneEnd, StringComparison.Ordinal)))
        {
            throw new ProductException("Duplicate or invalid hygiene-managed EditorConfig markers; repair .editorconfig before initialization.");
        }
        if (text.Contains(hygieneStart, StringComparison.Ordinal) && text.IndexOf(hygieneEnd, StringComparison.Ordinal) < text.IndexOf(hygieneStart, StringComparison.Ordinal))
        {
            throw new ProductException("Reversed hygiene-managed EditorConfig markers; repair .editorconfig before initialization.");
        }

        ValidateEffectiveMechanicalSettings(root, ignoreInheritedOverrides);
        string newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        if (text.Contains(hygieneStart, StringComparison.Ordinal))
        {
            int start = text.IndexOf(hygieneStart, StringComparison.Ordinal), end = text.IndexOf(hygieneEnd, StringComparison.Ordinal) + hygieneEnd.Length;
            string owned = text[start..end];
            string[] lines = owned.Split(["\r\n", "\n"], StringSplitOptions.None);
            for (int index = 0; index < lines.Length; index++)
            {
                int equals = lines[index].IndexOf('=');
                if (equals > 0 && MechanicalOptions.Any(option => option.Key.Equals(lines[index][..equals].Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    lines[index] = "";
                }
            }
            text = text[..start] + string.Join(newline, lines) + text[end..];
        }
        string separator = text.Length == 0 || text.EndsWith("\n", StringComparison.Ordinal) ? "" : newline;
        string block = BuildEditorConfigBlock(StartMarker, EndMarker).Replace("\n", newline, StringComparison.Ordinal);
        byte[] next = new System.Text.UTF8Encoding(false).GetBytes(text + separator + (separator.Length == 0 && text.Length > 0 ? newline : "") + block + newline);
        if (File.Exists(path) != existed || (existed && !original.AsSpan().SequenceEqual(File.ReadAllBytes(path))))
        {
            throw new ProductException(".editorconfig changed concurrently during Autoformat initialization; no changes were written.");
        }

        string temporary = path + ".autoformat-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(next); stream.Flush(true); }
            beforeReplace?.Invoke();
            if (File.Exists(path) != existed || (existed && !original.AsSpan().SequenceEqual(File.ReadAllBytes(path))))
            {
                throw new ProductException(".editorconfig changed concurrently during Autoformat initialization; no changes were written.");
            }

            if (File.Exists(path))
            {
                File.Replace(temporary, path, null);
            }
            else
            {
                File.Move(temporary, path);
            }
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
        return true;
    }

    public static void ValidateEffectiveMechanicalSettings(string root, bool ignoreInheritedOverrides = false)
    {
        foreach (var finding in AnalyzeEffectiveMechanicalSettings(root, ignoreInheritedOverrides).Where(finding => finding.Actual is not null))
        {
            throw new ProductException($"Effective mechanical setting conflict for '{finding.Key}' on '{finding.Path}': '{finding.Actual}'. Resolve this applicable override before Autoformat.");
        }
    }

    public static IReadOnlyList<(string Path, string Key, string? Actual)> AnalyzeEffectiveMechanicalSettings(string root, bool ignoreInheritedOverrides = false)
    {
        var findings = new List<(string Path, string Key, string? Actual)>();
        string fullRoot = Path.GetFullPath(root);
        using var session = new RepositorySession(fullRoot);
        string[] targets = session.ResolveTargets([], false);
        Dictionary<string, (Project Project, Document Document)> eligible = session.AssignTargets(targets, []);
        foreach (var (source, assignment) in eligible)
        {
            IReadOnlyDictionary<string, string> values = EffectiveOptions(source, fullRoot, ignoreInheritedOverrides);
            foreach (var option in MechanicalOptions)
            {
                values.TryGetValue(option.Key, out string? actual);
                if (actual is null || !actual.Split(':', 2)[0].Trim().Equals(option.Value, StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add((session.Relative(source), option.Key, actual));
                }
            }
        }
        return findings;
    }

    private static IReadOnlyDictionary<string, string> EffectiveOptions(string source, string root, bool ignoreInheritedOverrides)
    {
        string directory = Path.GetDirectoryName(source)!;
        var chain = new List<string>();
        for (string? current = directory; current is not null; current = Directory.GetParent(current)?.FullName)
        {
            string config = Path.Combine(current, ".editorconfig");
            if (File.Exists(config))
            {
                chain.Add(config);
                if (File.ReadLines(config).Any(line => line.Trim().Equals("root=true", StringComparison.OrdinalIgnoreCase) || line.Trim().Equals("root = true", StringComparison.OrdinalIgnoreCase)))
                {
                    break;
                }
            }
            if (ignoreInheritedOverrides && current.Equals(root, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string config in chain.AsEnumerable().Reverse())
        {
            string section = "*";
            string relative = Path.GetRelativePath(Path.GetDirectoryName(config)!, source).Replace('\\', '/');
            foreach (string raw in File.ReadLines(config))
            {
                string line = raw.Trim();
                if (line.StartsWith('[') && line.EndsWith(']')) { section = line[1..^1].Trim(); continue; }
                if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
                {
                    continue;
                }

                int equals = line.IndexOf('=');
                if (equals <= 0 || (section != "*" && !MatchesEditorConfig(section, relative)))
                {
                    continue;
                }

                string key = line[..equals].Trim();
                if (MechanicalOptions.Any(option => option.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
                {
                    values[key] = line[(equals + 1)..].Trim();
                }
            }
        }
        return values;
    }

    private static bool MatchesEditorConfig(string pattern, string relativePath)
    {
        string candidate = pattern.Contains('/') ? relativePath : Path.GetFileName(relativePath);
        string glob = Regex.Escape(pattern.Replace('\\', '/'))
            .Replace("\\*\\*/", "(?:.*/)?", StringComparison.Ordinal)
            .Replace("\\*", "[^/]*", StringComparison.Ordinal)
            .Replace("\\?", "[^/]", StringComparison.Ordinal);
        glob = Regex.Replace(glob, @"\\\{([^}]+)\\\}", match => "(?:" + string.Join('|', match.Groups[1].Value.Split(',')) + ")");
        return Regex.IsMatch(candidate, "^" + glob + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
