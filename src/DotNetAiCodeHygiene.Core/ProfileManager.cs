using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace DotNetAiCodeHygiene.Core;

public sealed record ProfileFinding(string RuleId, string Path, string Message, string Suggestion);
public sealed record ProfileResult(string Command, int FindingCount, IReadOnlyList<ProfileFinding> Findings, IReadOnlyList<string> ChangedPaths);

/// <summary>Owns the fixed dotnet-11 profile and its explicitly delimited repository artifacts.</summary>
public sealed class ProfileManager
{
    private const string Start = "<!-- hygiene profile:begin -->";
    private const string End = "<!-- hygiene profile:end -->";
    private const string Import = "<!-- hygiene profile import:begin -->";
    private const string ImportEnd = "<!-- hygiene profile import:end -->";
    private readonly string root;
    private readonly Action<int>? afterReplace;
    private readonly JsonSerializerOptions json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly string Marker = "{\n  \"schemaVersion\": 1,\n  \"profile\": \"dotnet-11\",\n  \"version\": 1\n}\n";
    private static readonly string Props = "<Project>\n  <PropertyGroup>\n    <AnalysisLevel>11</AnalysisLevel>\n    <EnableNETAnalyzers>true</EnableNETAnalyzers>\n    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>\n  </PropertyGroup>\n</Project>\n";
    private static readonly string EditorBlock = $"{Start}\n[*.cs]\ncsharp_prefer_braces = true\ndotnet_diagnostic.IDE0011.severity = error\ndotnet_style_require_accessibility_modifiers = always\ndotnet_diagnostic.IDE0040.severity = error\n{End}";
    private static readonly string ImportBlock = $"{Import}\n  <Import Project=\"$(MSBuildThisFileDirectory).hygiene/profile/Hygiene.props\" Condition=\"Exists('$(MSBuildThisFileDirectory).hygiene/profile/Hygiene.props')\" />\n  {ImportEnd}";

    public ProfileManager(string root) : this(root, null) { }
    internal ProfileManager(string root, Action<int>? afterReplace)
    {
        this.root = Path.GetFullPath(root);
        this.afterReplace = afterReplace;
    }
    private string P(string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

    public ProfileResult Bootstrap(string output = "text") => Apply("bootstrap", false);
    public ProfileResult Update(string output = "text") => Apply("update", true);

    public void RequireCurrent()
    {
        string path = P(".hygiene/profile.json");
        if (!File.Exists(path))
        {
            throw new ProductException("Supported hygiene profile is missing; run hygiene bootstrap.");
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement e = doc.RootElement;
            string[] names = e.ValueKind == JsonValueKind.Object ? e.EnumerateObject().Select(p => p.Name).ToArray() : [];
            if (names.Length != 3 || !names.ToHashSet(StringComparer.Ordinal).SetEquals(["schemaVersion", "profile", "version"]) ||
                e.GetProperty("schemaVersion").GetInt32() != 1 || e.GetProperty("profile").GetString() != "dotnet-11" || e.GetProperty("version").GetInt32() != 1)
            {
                throw new ProductException("Unsupported hygiene profile; run hygiene update when a migration is available, or resolve the profile state manually.");
            }
        }
        catch (ProductException) { throw; }
        catch (Exception e) when (e is JsonException or IOException or KeyNotFoundException or InvalidOperationException)
        { throw new ProductException("Invalid .hygiene/profile.json; run hygiene bootstrap after resolving the malformed profile state."); }
    }

    public IReadOnlyList<ProfileFinding> Analyze()
    {
        var findings = new List<ProfileFinding>();
        if (!File.Exists(P(".hygiene/profile/Hygiene.props")) || File.ReadAllText(P(".hygiene/profile/Hygiene.props")) != Props)
        {
            findings.Add(new("profile.dotnet.analysis.required", ".hygiene/profile/Hygiene.props", "The generated supported-profile MSBuild artifact is missing or drifted.", "Run hygiene update to reconcile hygiene-owned profile state."));
        }
        if (!File.Exists(P("Directory.Build.props")) || !File.ReadAllText(P("Directory.Build.props")).Contains(Import, StringComparison.Ordinal) || !File.ReadAllText(P("Directory.Build.props")).Contains(ImportEnd, StringComparison.Ordinal))
        {
            findings.Add(new("profile.dotnet.analysis.required", "Directory.Build.props", "The root hygiene profile import is missing or drifted.", "Run hygiene update to reconcile the managed import."));
        }
        if (!File.Exists(P(".editorconfig")) || !File.ReadAllText(P(".editorconfig")).Contains(EditorBlock, StringComparison.Ordinal) || !File.ReadAllText(P(".editorconfig")).Split('\n').Any(line => line.Trim().Equals("root = true", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(new("profile.dotnet.analysis.required", ".editorconfig", "The root hygiene profile EditorConfig block is missing or drifted.", "Run hygiene update to reconcile the managed section."));
        }
        foreach (string project in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories).Where(p => !Ignored(p)))
        {
            try
            {
                XDocument doc = XDocument.Load(project);
                foreach (string property in new[] { "TreatWarningsAsErrors", "WarningsAsErrors" })
                {
                    foreach (XElement value in doc.Descendants().Where(e => e.Name.LocalName == property))
                    {
                        if (property == "TreatWarningsAsErrors" ? value.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) : !string.IsNullOrWhiteSpace(value.Value))
                        {
                            findings.Add(new("profile.dotnet.analysis.required", Rel(project), $"Repository-configured {property} violates the supported profile.", "Remove the global warning-promotion setting."));
                        }
                    }
                }

                foreach (string property in new[] { "AnalysisLevel", "EnableNETAnalyzers", "EnforceCodeStyleInBuild" })
                {
                    string expected = property == "AnalysisLevel" ? "11" : "true";
                    foreach (XElement value in doc.Descendants().Where(e => e.Name.LocalName == property))
                    {
                        if (!value.Value.Trim().Equals(expected, StringComparison.OrdinalIgnoreCase))
                        {
                            findings.Add(new("profile.dotnet.analysis.required", Rel(project), $"Project property {property} must be {expected}.", "Remove or correct the project-local override."));
                        }
                    }
                }

                if (doc.Descendants().Any(e => (e.Name.LocalName is "PackageReference" or "Analyzer") && ((string?)e.Attribute("Include") ?? (string?)e.Attribute("Update") ?? "").StartsWith("StyleCop.Analyzers", StringComparison.OrdinalIgnoreCase)))
                {
                    findings.Add(new("profile.stylecop.prohibited", Rel(project), "StyleCop.Analyzers is prohibited by the supported profile.", "Resolve the analyzer dependency and its policy impact explicitly."));
                }
            }
            catch (System.Xml.XmlException) { findings.Add(new("profile.dotnet.analysis.required", Rel(project), "Project configuration could not be parsed for profile analysis.", "Repair the project XML.")); }
        }
        foreach (string props in Directory.EnumerateFiles(root, "Directory.Build.props", SearchOption.AllDirectories).Where(p => !Ignored(p)))
        {
            try
            {
                XDocument doc = XDocument.Load(props);
                foreach (string property in new[] { "TreatWarningsAsErrors", "WarningsAsErrors" })
                {
                    foreach (XElement value in doc.Descendants().Where(e => e.Name.LocalName == property))
                    {
                        if (property == "TreatWarningsAsErrors" ? value.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) : !string.IsNullOrWhiteSpace(value.Value))
                        {
                            findings.Add(new("profile.dotnet.analysis.required", Rel(props), $"Repository-configured {property} violates the supported profile.", "Remove the global warning-promotion setting."));
                        }
                    }
                }
            }
            catch (System.Xml.XmlException) { findings.Add(new("profile.dotnet.analysis.required", Rel(props), "Build properties could not be parsed for profile analysis.", "Repair the MSBuild XML.")); }
        }
        foreach (string config in Directory.EnumerateFiles(root, ".editorconfig", SearchOption.AllDirectories).Where(p => !Ignored(p)))
        {
            string contents = File.ReadAllText(config);
            foreach ((string key, string expected) in new[] { ("csharp_prefer_braces", "true"), ("dotnet_diagnostic.IDE0011.severity", "error"), ("dotnet_style_require_accessibility_modifiers", "always"), ("dotnet_diagnostic.IDE0040.severity", "error") })
            {
                foreach (string line in contents.Split('\n'))
                {
                    string[] pair = line.Split('=', 2);
                    if (pair.Length == 2 && pair[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase) && !pair[1].Trim().Equals(expected, StringComparison.OrdinalIgnoreCase))
                    {
                        findings.Add(new("profile.dotnet.analysis.required", Rel(config), $"EditorConfig setting {key} weakens the supported profile.", $"Set {key} = {expected} in the overriding configuration."));
                    }
                }
            }
        }
        foreach (string config in Directory.EnumerateFiles(root, "*.props", SearchOption.AllDirectories).Concat(Directory.EnumerateFiles(root, "*.targets", SearchOption.AllDirectories)).Where(p => !Ignored(p)))
        {
            try
            {
                XDocument doc = XDocument.Load(config);
                foreach (string property in new[] { "AnalysisLevel", "EnableNETAnalyzers", "EnforceCodeStyleInBuild" })
                {
                    string expected = property == "AnalysisLevel" ? "11" : "true";
                    if (doc.Descendants().Any(e => e.Name.LocalName == property && !e.Value.Trim().Equals(expected, StringComparison.OrdinalIgnoreCase)))
                    {
                        findings.Add(new("profile.dotnet.analysis.required", Rel(config), $"Build property {property} weakens the supported profile.", "Remove or correct the overriding MSBuild property."));
                    }
                }
                if (doc.Descendants().Any(e => (e.Name.LocalName is "PackageReference" or "Analyzer") && ((string?)e.Attribute("Include") ?? (string?)e.Attribute("Update") ?? "").StartsWith("StyleCop.Analyzers", StringComparison.OrdinalIgnoreCase)))
                {
                    findings.Add(new("profile.stylecop.prohibited", Rel(config), "StyleCop.Analyzers is prohibited by the supported profile.", "Resolve the analyzer dependency and its policy impact explicitly."));
                }
            }
            catch (System.Xml.XmlException) { }
        }
        return findings.GroupBy(f => (f.RuleId, f.Path, f.Message)).Select(g => g.First()).OrderBy(f => f.RuleId, StringComparer.Ordinal).ThenBy(f => f.Path, StringComparer.Ordinal).ToArray();
    }

    private ProfileResult Apply(string command, bool requireMarker)
    {
        if (requireMarker)
        {
            RequireCurrent();
        }

        string editorPath = P(".editorconfig"), directoryPropsPath = P("Directory.Build.props");
        string editor = File.Exists(editorPath) ? File.ReadAllText(editorPath) : "root = true\n";
        string props = File.Exists(directoryPropsPath) ? File.ReadAllText(directoryPropsPath) : "<Project>\n</Project>\n";
        string nextEditor = ReconcileEditor(editor);
        string nextProps = ReconcileImport(props);
        var changes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [P(".hygiene/profile.json")] = Marker,
            [P(".hygiene/profile/Hygiene.props")] = Props,
            [editorPath] = nextEditor,
            [directoryPropsPath] = nextProps
        };
        string[] changed = Commit(changes);
        var findings = Analyze();
        return new(command, findings.Count, findings, changed.Select(Rel).Order(StringComparer.Ordinal).ToArray());
    }

    private string ReconcileEditor(string text)
    {
        string clean = RemoveManaged(text, Start, End);
        string[] lines = clean.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        bool rootSeen = false;
        for (int i = 0; i < lines.Length; i++)
        {
            int equals = lines[i].IndexOf('=');
            if (equals < 0 || !lines[i][..equals].Trim().Equals("root", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!rootSeen) { lines[i] = lines[i][..(equals + 1)] + " true"; rootSeen = true; }
            else
            {
                lines[i] = "";
            }
        }
        clean = string.Join("\n", lines);
        if (!rootSeen)
        {
            clean = "root = true\n" + clean;
        }

        return clean.TrimEnd() + "\n\n" + EditorBlock + "\n";
    }
    private static string ReconcileImport(string text)
    {
        string clean = RemoveManaged(text, Import, ImportEnd);
        int close = clean.LastIndexOf("</Project>", StringComparison.OrdinalIgnoreCase);
        if (close < 0)
        {
            throw new ProductException("Root Directory.Build.props must have a Project root to install the hygiene profile.");
        }

        return clean.Insert(close, "  " + ImportBlock + "\n");
    }
    private static string RemoveManaged(string text, string start, string end)
    {
        int a = text.IndexOf(start, StringComparison.Ordinal), b = text.IndexOf(end, StringComparison.Ordinal);
        if ((a < 0) != (b < 0) || (a >= 0 && b < a))
        {
            throw new ProductException("Conflicting or incomplete hygiene-managed section; repair its boundary markers before retrying.");
        }

        if (a < 0)
        {
            return text;
        }

        int lineStart = text.LastIndexOf('\n', a);
        lineStart = lineStart < 0 ? 0 : lineStart + 1;
        if (text[lineStart..a].All(char.IsWhiteSpace))
        {
            a = lineStart;
        }

        int lineEnd = text.IndexOf('\n', b + end.Length);
        int removeEnd = lineEnd < 0 ? text.Length : lineEnd + 1;
        return text.Remove(a, removeEnd - a);
    }
    private string[] Commit(Dictionary<string, string> changes)
    {
        var plan = changes.ToDictionary(kv => kv.Key, kv => (Old: File.Exists(kv.Key) ? File.ReadAllBytes(kv.Key) : null, New: new UTF8Encoding(false).GetBytes(kv.Value)), StringComparer.OrdinalIgnoreCase);
        var committed = new List<string>();
        string[] changed = plan.Where(kv => kv.Value.Old is null || !kv.Value.Old.SequenceEqual(kv.Value.New)).Select(kv => kv.Key).ToArray();
        try
        {
            foreach (var (path, pair) in plan)
            {
                if (pair.Old is not null && pair.Old.SequenceEqual(pair.New))
                {
                    continue;
                }

                if (File.Exists(path) != (pair.Old is not null) || (pair.Old is not null && !File.ReadAllBytes(path).SequenceEqual(pair.Old)))
                {
                    throw new ProductException("Profile state changed concurrently; retry the command.");
                }
            }
            foreach (var (path, pair) in plan)
            {
                if (pair.Old is not null && pair.Old.SequenceEqual(pair.New))
                {
                    continue;
                }

                if (File.Exists(path) != (pair.Old is not null) || (pair.Old is not null && !File.ReadAllBytes(path).SequenceEqual(pair.Old)))
                {
                    throw new ProductException("Profile state changed concurrently; retry the command.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllBytes(temp, pair.New);
                if (pair.Old is not null)
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }

                committed.Add(path);
                afterReplace?.Invoke(committed.Count);
            }
            return changed;
        }
        catch
        {
            foreach (string path in committed.AsEnumerable().Reverse())
            {
                var old = plan[path].Old;
                if (old is null)
                {
                    File.Delete(path);
                }
                else
                {
                    File.WriteAllBytes(path, old);
                }
            }
            throw;
        }
    }
    private string Rel(string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
    private bool Ignored(string path) => Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar).Any(x => x is ".git" or ".hygiene" or "bin" or "obj");
}
