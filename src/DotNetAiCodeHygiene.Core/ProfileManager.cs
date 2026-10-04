using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Beschreibt einen Verstoß gegen das unterstützte Hygieneprofil.</summary>
/// <param name="RuleId">Kennung der verletzten Profilregel.</param>
/// <param name="Path">Repositoryrelativer Pfad der Fundstelle.</param>
/// <param name="Message">Erklärung des Profilverstoßes.</param>
/// <param name="Suggestion">Vorschlag zur Behebung des Verstoßes.</param>
public sealed record ProfileFinding(string RuleId, string Path, string Message, string Suggestion);
/// <summary>Enthält das Ergebnis eines Profilvorgangs.</summary>
/// <param name="Command">Ausgeführter Profilbefehl.</param>
/// <param name="FindingCount">Anzahl der verbleibenden Profilverstöße.</param>
/// <param name="Findings">Verbleibende Profilverstöße mit Hinweisen.</param>
/// <param name="ChangedPaths">Geänderte repositoryrelative Pfade.</param>
public sealed record ProfileResult(string Command, int FindingCount, IReadOnlyList<ProfileFinding> Findings, IReadOnlyList<string> ChangedPaths);

/// <summary>Verwaltet das feste dotnet-11-Profil und seine klar abgegrenzten Repositorydateien.</summary>
public sealed class ProfileManager
{
    private static readonly ConcurrentDictionary<string, (string Fingerprint, string Output)> Evaluations = new(StringComparer.OrdinalIgnoreCase);
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

    /// <summary>Verwaltet das Profil im angegebenen Repository.</summary>
    /// <param name="root">Pfad zum Stammverzeichnis des Repositorys.</param>
    public ProfileManager(string root) : this(root, null) { }
    /// <summary>Erstellt einen Profilmanager mit einem Hook für transaktionale Tests.</summary>
    /// <param name="root">Pfad zum Stammverzeichnis des Repositorys.</param>
    /// <param name="afterReplace">Optionaler Hook nach dem Austausch einer Datei.</param>
    internal ProfileManager(string root, Action<int>? afterReplace)
    {
        this.root = Path.GetFullPath(root);
        this.afterReplace = afterReplace;
    }
    private string P(string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Installiert das unterstützte Profil und meldet verbleibende Verstöße.</summary>
    /// <param name="output">Gewünschtes Ausgabeformat.</param>
    /// <returns>Ergebnis der Profilinstallation.</returns>
    public ProfileResult Bootstrap(string output = "text") => Apply("bootstrap", false);
    /// <summary>Gleicht die verwalteten Profilelemente mit dem aktuellen Profil ab.</summary>
    /// <param name="output">Gewünschtes Ausgabeformat.</param>
    /// <returns>Ergebnis des Profilabgleichs.</returns>
    public ProfileResult Update(string output = "text") => Apply("update", true);

    /// <summary>Prüft, ob das Repository ein unterstütztes Profil verwendet.</summary>
    /// <exception cref="ProductException">Das Profil fehlt oder wird nicht unterstützt.</exception>
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

    /// <summary>Analysiert die effektive Projektkonfiguration und die Profilartefakte.</summary>
    /// <returns>Profilverstöße in stabiler Reihenfolge.</returns>
    public IReadOnlyList<ProfileFinding> Analyze()
    {
        var findings = new List<ProfileFinding>();
        var styleCopSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var evaluatedSources = new HashSet<string>(PathComparer);
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
        string[] projects = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories).Where(p => !Ignored(p)).ToArray();
        string[] msbuildFiles = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(p => !Ignored(p) && (Path.GetExtension(p).ToLowerInvariant() is ".csproj" or ".props" or ".targets" or ".vbproj" or ".fsproj"))
            .ToArray();
        foreach (string config in msbuildFiles)
        {
            try
            {
                XDocument doc = XDocument.Load(config);
                foreach (XElement value in doc.Descendants().Where(e => e.Name.LocalName is "TreatWarningsAsErrors" or "WarningsAsErrors"))
                {
                    string property = value.Name.LocalName;
                    if (property == "TreatWarningsAsErrors" ? value.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) : !string.IsNullOrWhiteSpace(value.Value))
                    {
                        findings.Add(new("profile.dotnet.analysis.required", Rel(config), $"Repository-configured {property} violates the supported profile.", "Remove the global warning-promotion setting."));
                    }
                }
                if (doc.Descendants().Any(e => (e.Name.LocalName is "PackageReference" or "Analyzer") && IsStyleCop(((string?)e.Attribute("Include") ?? (string?)e.Attribute("Update") ?? ""))))
                {
                    styleCopSources.Add(config);
                }
            }
            catch (System.Xml.XmlException) { findings.Add(new("profile.dotnet.analysis.required", Rel(config), "Build configuration could not be parsed for profile analysis.", "Repair the project XML.")); }
        }

        foreach (string project in projects)
        {
            try
            {
                if (!IsSdkStyleProject(project))
                {
                    continue;
                }
                string[] configurations = ["Debug", "Release"];
                bool hasRepositoryConfiguredStyleCopSource = false;
                var projectSources = new HashSet<string>(PathComparer);
                foreach (string configuration in configurations)
                {
                    using JsonDocument evaluation = Evaluate(project, configuration, "-getProperty:AnalysisLevel,EnableNETAnalyzers,EnforceCodeStyleInBuild,ManagePackageVersionsCentrally", "-getItem:Analyzer,PackageReference,PackageVersion,Compile");
                    JsonElement properties = evaluation.RootElement.GetProperty("Properties");
                    foreach ((string property, string expected) in new[] { ("AnalysisLevel", "11"), ("EnableNETAnalyzers", "true"), ("EnforceCodeStyleInBuild", "true") })
                    {
                        string actual = Property(properties, property);
                        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                        {
                            findings.Add(new("profile.dotnet.analysis.required", Rel(project), $"Effective project property {property} must be {expected} for {configuration} (found '{actual}').", "Correct the effective MSBuild property."));
                        }
                    }
                    JsonElement items = evaluation.RootElement.GetProperty("Items");
                    foreach (JsonElement item in items.GetProperty("Compile").EnumerateArray())
                    {
                        string fullPath = ItemProperty(item, "FullPath");
                        if (fullPath.Length == 0)
                        {
                            string identity = ItemProperty(item, "Identity");
                            if (identity.Length > 0)
                            {
                                fullPath = Path.GetFullPath(identity, Path.GetDirectoryName(project)!);
                            }
                        }
                        if (fullPath.Length > 0 && Path.GetExtension(fullPath).Equals(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath) && IsWithinRoot(fullPath))
                        {
                            projectSources.Add(Path.GetFullPath(fullPath));
                        }
                    }
                    foreach (JsonElement item in items.GetProperty("Analyzer").EnumerateArray())
                    {
                        string identity = ItemProperty(item, "Identity");
                        if (IsStyleCop(identity))
                        {
                            string source = ItemProperty(item, "DefiningProjectFullPath");
                            if (File.Exists(source) && IsWithinRoot(source))
                            {
                                styleCopSources.Add(source);
                                hasRepositoryConfiguredStyleCopSource = true;
                            }
                            else
                            {
                                styleCopSources.Add(project);
                                hasRepositoryConfiguredStyleCopSource = true;
                            }
                        }
                    }
                    foreach (JsonElement item in items.GetProperty("PackageReference").EnumerateArray().Where(item => IsStyleCop(ItemProperty(item, "Identity"))))
                    {
                        string source = ItemProperty(item, "DefiningProjectFullPath");
                        if (File.Exists(source) && IsWithinRoot(source))
                        {
                            styleCopSources.Add(source);
                            hasRepositoryConfiguredStyleCopSource = true;
                        }
                    }
                    bool centralManagementActive = Property(properties, "ManagePackageVersionsCentrally").Equals("true", StringComparison.OrdinalIgnoreCase);
                    if (centralManagementActive && items.GetProperty("PackageReference").EnumerateArray().Any(item => IsStyleCop(ItemProperty(item, "Identity"))))
                    {
                        foreach (JsonElement item in items.GetProperty("PackageVersion").EnumerateArray().Where(item => IsStyleCop(ItemProperty(item, "Identity"))))
                        {
                            string source = ItemProperty(item, "DefiningProjectFullPath");
                            if (File.Exists(source) && IsWithinRoot(source))
                            {
                                styleCopSources.Add(source);
                                hasRepositoryConfiguredStyleCopSource = true;
                            }
                        }
                    }
                }
                if (HasResolvedStyleCopAnalyzer(project) && !hasRepositoryConfiguredStyleCopSource)
                {
                    styleCopSources.Add(project);
                }
                evaluatedSources.UnionWith(projectSources);
            }
            catch (Exception e) when (e is IOException or InvalidOperationException or JsonException or ProductException)
            {
                findings.Add(new("profile.dotnet.analysis.required", Rel(project), "Effective MSBuild configuration could not be evaluated.", "Repair the project/import configuration and retry profile analysis."));
            }
        }

        foreach (string source in evaluatedSources)
        {
            IReadOnlyDictionary<string, string> effective = EffectiveEditorConfig(source);
            foreach ((string key, string expected) in new[] { ("csharp_prefer_braces", "true"), ("dotnet_diagnostic.IDE0011.severity", "error"), ("dotnet_style_require_accessibility_modifiers", "always"), ("dotnet_diagnostic.IDE0040.severity", "error") })
            {
                if (!effective.TryGetValue(key, out string? actual) || !OptionEquals(key, actual, expected))
                {
                    findings.Add(new("profile.dotnet.analysis.required", Rel(source), $"Effective EditorConfig setting {key} must be {expected} (found '{actual ?? "<unset>"}').", $"Set {key} = {expected} in the effective configuration."));
                }
            }
        }

        foreach (string source in styleCopSources)
        {
            findings.Add(new("profile.stylecop.prohibited", Rel(source), "StyleCop analyzers are prohibited by the supported profile.", "Resolve the analyzer dependency and its policy impact explicitly."));
        }
        return findings.GroupBy(f => (f.RuleId, f.Path, f.Message)).Select(g => g.First()).OrderBy(f => f.RuleId, StringComparer.Ordinal).ThenBy(f => f.Path, StringComparer.Ordinal).ToArray();
    }

    private static bool IsStyleCop(string value) => value.Contains("stylecop", StringComparison.OrdinalIgnoreCase);
    private bool IsWithinRoot(string path)
    {
        string relative = Path.GetRelativePath(root, path);
        return !Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
    private static string Property(JsonElement properties, string key) => properties.TryGetProperty(key, out JsonElement value) ? value.GetString() ?? "" : "";
    private static string ItemProperty(JsonElement item, string key) => item.TryGetProperty(key, out JsonElement value) ? value.GetString() ?? "" : "";
    private static StringComparer PathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private static bool IsSdkStyleProject(string project)
    {
        try
        {
            XDocument doc = XDocument.Load(project);
            return doc.Root?.Attribute("Sdk") is not null || doc.Root?.Elements().Any(e => e.Name.LocalName == "Import" && ((string?)e.Attribute("Project"))?.Contains("Sdk.props", StringComparison.OrdinalIgnoreCase) == true) == true;
        }
        catch (System.Xml.XmlException) { return false; }
    }

    private JsonDocument Evaluate(string project, string configuration, string properties, string items)
    {
        string fingerprint = ConfigFingerprint();
        string cacheKey = project + "|" + configuration;
        if (Evaluations.TryGetValue(cacheKey, out var cached) && cached.Fingerprint == fingerprint)
        {
            return JsonDocument.Parse(cached.Output);
        }
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("msbuild"); start.ArgumentList.Add(project); start.ArgumentList.Add("-property:Configuration=" + configuration); start.ArgumentList.Add(properties); start.ArgumentList.Add(items);
        using Process process = Process.Start(start) ?? throw new ProductException("Could not start MSBuild evaluation.");
        string stdout = process.StandardOutput.ReadToEnd(); string stderr = process.StandardError.ReadToEnd(); process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new ProductException("MSBuild evaluation failed: " + stderr);
        }
        try
        {
            Evaluations[cacheKey] = (fingerprint, stdout);
            return JsonDocument.Parse(stdout);
        }
        catch (JsonException) { throw new ProductException("MSBuild returned invalid evaluation data: " + stderr); }
    }

    private string ConfigFingerprint()
    {
        var inputs = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => !Ignored(path) && (Path.GetExtension(path) is ".csproj" or ".props" or ".targets" or ".vbproj" or ".fsproj"))
            .Order(StringComparer.OrdinalIgnoreCase);
        var signature = inputs.Select(path => Rel(path) + "|" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))).ToList();
        foreach (string project in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories).Where(path => !Ignored(path)))
        {
            string assets = Path.Combine(Path.GetDirectoryName(project)!, "obj", "project.assets.json");
            if (File.Exists(assets))
            {
                signature.Add(Rel(assets) + "|" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assets))));
            }
        }
        return string.Join("\n", signature);
    }

    private static bool HasResolvedStyleCopAnalyzer(string project)
    {
        string assets = Path.Combine(Path.GetDirectoryName(project)!, "obj", "project.assets.json");
        if (!File.Exists(assets))
        {
            return false;
        }
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(assets));
            if (!document.RootElement.TryGetProperty("targets", out JsonElement targets))
            {
                return false;
            }
            foreach (JsonProperty target in targets.EnumerateObject())
            {
                foreach (JsonProperty library in target.Value.EnumerateObject())
                {
                    if (library.Value.TryGetProperty("analyzers", out JsonElement analyzers) &&
                        (IsStyleCop(library.Name) || (analyzers.ValueKind == JsonValueKind.Object && analyzers.EnumerateObject().Any(analyzer => IsStyleCop(analyzer.Name))) ||
                         (analyzers.ValueKind == JsonValueKind.Array && analyzers.EnumerateArray().Any(analyzer => IsStyleCop(analyzer.TryGetProperty("path", out JsonElement path) ? path.GetString() ?? "" : "")))))
                    {
                        return true;
                    }
                }
            }
        }
        catch (JsonException) { }
        return false;
    }

    private IReadOnlyDictionary<string, string> EffectiveEditorConfig(string source)
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
        }
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string config in chain.AsEnumerable().Reverse())
        {
            string[] lines = File.ReadAllLines(config); string section = "*"; string relative = Path.GetRelativePath(Path.GetDirectoryName(config)!, source).Replace('\\', '/');
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    section = trimmed[1..^1].Trim();
                    continue;
                }
                if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(';'))
                {
                    continue;
                }
                int equals = trimmed.IndexOf('=');
                if (equals < 0)
                {
                    continue;
                }
                string key = trimmed[..equals].Trim(); string value = trimmed[(equals + 1)..].Trim();
                if (section == "*" || EditorPatternMatches(section, relative))
                {
                    values[key] = value;
                }
            }
        }
        return values;
    }

    private static bool EditorPatternMatches(string pattern, string relativePath)
    {
        string candidate = pattern.Contains('/') ? relativePath : Path.GetFileName(relativePath);
        string glob = Regex.Escape(pattern.Replace('\\', '/')).Replace("\\*\\*/", "(?:.*/)?").Replace("\\*", "[^/]*").Replace("\\?", "[^/]");
        return Regex.IsMatch(candidate, "^" + glob + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool OptionEquals(string key, string actual, string expected)
    {
        string value = actual.Split(':', 2)[0].Trim();
        return value.Equals(expected, StringComparison.OrdinalIgnoreCase);
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
