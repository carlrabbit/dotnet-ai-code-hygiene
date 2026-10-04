using System.Security.Cryptography;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Build.Locator;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Beschreibt eine Hygieneregel und ihre feste Versionierung.</summary>
/// <param name="Id">Stabile Kennung der Regel.</param>
/// <param name="Version">Version der Regelbedeutung.</param>
/// <param name="OutputKind">Art der Ausgabe, die die Regel erzeugt.</param>
/// <param name="Classification">Klassifikation der Ausgabe.</param>
/// <param name="Purpose">Zweck der Regel.</param>
/// <param name="Configurable">Gibt an, ob Aufrufer die Regel aktivieren oder deaktivieren dürfen.</param>
public sealed record Rule(string Id, int Version, string OutputKind, string Classification, string Purpose, bool Configurable = true);
public sealed record Finding(string Id, string Handle, string RuleId, int RuleVersion, string Classification, string Path, int Line, int Column, string? Symbol, string Message, string Suggestion, string Observation, string Reason, string Constraint, string Anchor, string Fingerprint, string? Discriminator = null);
public sealed record ReviewQuestion(string Id, string Text);
public sealed record ReviewItem(string Id, string Path, int Line, int Column, string Symbol, string Summary, string Declaration);
public sealed record ReviewEscalation(string Condition, string Command, string ReviewerClass);
public sealed record ReviewSource(string Path, string Content);
public sealed record ReviewBatch(string Id, string Handle, string RuleId, int RuleVersion, string Mode, string ReviewerClass, int PopulationCount, int SampleCount, IReadOnlyList<ReviewQuestion> Questions, ReviewEscalation Escalation, IReadOnlyList<ReviewItem> Items, string PopulationFingerprint, IReadOnlyList<ReviewItem>? PopulationItems = null, IReadOnlyList<ReviewSource>? SourceContents = null);
public sealed record ReviewRequestSource(string RunId, string BatchHandle);
public sealed record ReviewRequestRule(string Id, int Version);
public sealed record SemanticReviewRequest(int SchemaVersion, string Kind, string HandoffId, string CreatedAtUtc, ReviewRequestSource Source, ReviewRequestRule Rule, string Mode, string ReviewerClass, int PopulationCount, IReadOnlyList<ReviewQuestion> Questions, IReadOnlyList<ReviewItem> Items, IReadOnlyList<ReviewSource> Sources);
public sealed record CheckResult(int SchemaVersion, string RunId, IReadOnlyList<Finding> Findings, int IgnoredCount, IReadOnlyList<ReviewBatch> ReviewBatches);
public sealed record IgnoreDecision(string Id, string RuleId, int RuleVersion, string Path, string Anchor, string Fingerprint, string Reason, DateTimeOffset CreatedAt, string? Discriminator = null);
public sealed record IgnoreView(string Id, string RuleId, string Path, string State, string Reason);
public sealed class ProductException(string message) : Exception(message);
public sealed class EnvironmentException(string message) : Exception(message);

/// <summary>Führt deterministische Hygieneanalysen für ein Repository aus.</summary>
public sealed class HygieneEngine
{
    public static readonly Rule[] Rules =
    [
        new("profile.dotnet.analysis.required", 1, "finding", "finding", "Require the supported .NET analysis profile.", false),
        new("profile.stylecop.prohibited", 1, "finding", "finding", "Prohibit StyleCop analyzers.", false),
        new("docs.summary.required", 2, "finding", "finding", "Require documentation summaries on covered API symbols."),
        new("docs.xml.consistent", 1, "finding", "finding", "Check present XML documentation structure and references."),
        new("docs.text.sentence", 1, "finding", "finding", "Require sentence punctuation in selected documentation prose."),
        new("docs.summary.quality.review", 2, "review-batch", "review-batch", "Review a deterministic sample of explicit documentation summaries for quality."),
        new("readability.long-line.review", 1, "finding", "review-candidate", "Review unusually long physical source lines."),
        new("readability.control-flow.visual-block", 1, "finding", "finding", "Separate control-flow blocks visually from preceding statements.")
    ];
    private readonly string root;
    private readonly string hygiene;
    private readonly Action? beforeAtomicReplace;
    private readonly JsonSerializerOptions json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = false };

    public HygieneEngine(string? cwd = null) : this(cwd, null) { }

    internal HygieneEngine(string? cwd, Action? beforeAtomicReplace)
    {
        this.beforeAtomicReplace = beforeAtomicReplace;
        string dir = Path.GetFullPath(cwd ?? Environment.CurrentDirectory);
        while (!Directory.Exists(Path.Combine(dir, ".git")) && !File.Exists(Path.Combine(dir, ".git")))
        {
            string? parent = Directory.GetParent(dir)?.FullName;
            if (parent is null)
            {
                throw new ProductException("Current directory is not inside a Git repository.");
            }

            dir = parent;
        }
        root = dir; hygiene = Path.Combine(root, ".hygiene");
    }
    public string Root => root;
    /// <summary>Installiert das unterstützte Profil im Repository.</summary>
    /// <param name="output">Gewünschtes Ausgabeformat.</param>
    /// <returns>Ergebnis der Profilinstallation.</returns>
    public ProfileResult Bootstrap(string output = "text") => new ProfileManager(root).Bootstrap(output);
    /// <summary>Gleicht die verwalteten Profilelemente im Repository mit der aktuellen Vorgabe ab.</summary>
    /// <param name="output">Gewünschtes Ausgabeformat.</param>
    /// <returns>Ergebnis des Profilabgleichs.</returns>
    public ProfileResult UpdateProfile(string output = "text") => new ProfileManager(root).Update(output);
    /// <summary>Prüft, ob das Repository ein unterstütztes Profil verwendet.</summary>
    public void RequireProfile() => new ProfileManager(root).RequireCurrent();
    /// <summary>Analysiert das effektive Profil im Repository.</summary>
    /// <returns>Gefundene Profilverstöße.</returns>
    public IReadOnlyList<ProfileFinding> AnalyzeProfile() => new ProfileManager(root).Analyze();

    private T Read<T>(string path, T fallback)
    {
        if (!File.Exists(path))
        {
            return fallback;
        }

        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(path), json) ?? throw new JsonException(); }
        catch (Exception e) when (e is JsonException or IOException) { throw new ProductException($"Invalid state file '{Path.GetRelativePath(root, path)}'."); }
    }
    private static string? Snapshot(string path) => File.Exists(path) ? File.ReadAllText(path) : null;
    private void WriteAtomic(string path, string content, string? expected = null, bool verifyExpected = false)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string? original = verifyExpected ? expected : Snapshot(path);
        if (Snapshot(path) != original)
        {
            throw new ProductException("State changed concurrently; retry the command.");
        }

        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, content, new UTF8Encoding(false));
            beforeAtomicReplace?.Invoke();
            if (Snapshot(path) != original)
            {
                throw new ProductException("State changed concurrently; retry the command.");
            }

            if (File.Exists(path))
            {
                File.Replace(temp, path, null);
            }
            else
            {
                File.Move(temp, path);
            }
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }
    private string ConfigPath => Path.Combine(hygiene, "config.json");
    private string DecisionsPath => Path.Combine(hygiene, "decisions.json");
    private string LatestPath => Path.Combine(hygiene, ".state", "latest-run.json");
    private string[] Disabled
    {
        get
        {
            Config value = Read(ConfigPath, new Config(1, []));
            if (value.SchemaVersion != 1 || value.DisabledRules is null || value.DisabledRules.Any(id => !Rules.Any(r => r.Id == id && r.Configurable)) || value.DisabledRules.Distinct(StringComparer.Ordinal).Count() != value.DisabledRules.Length)
            {
                throw new ProductException("Invalid or unsupported .hygiene/config.json.");
            }

            return value.DisabledRules;
        }
    }
    private DecisionFile ReadDecisions()
    {
        DecisionFile value = Read(DecisionsPath, new DecisionFile(1, []));
        if (value.SchemaVersion != 1 || value.Decisions is null || value.Decisions.Any(d => d is null || string.IsNullOrEmpty(d.Id) || d.Id.Length < 3 || !d.Id.StartsWith("I-", StringComparison.Ordinal) || !Rules.Any(r => r.Id == d.RuleId && r.Configurable) || d.RuleVersion < 1 || string.IsNullOrEmpty(d.Fingerprint) || d.Fingerprint.Length != 64) || value.Decisions.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != value.Decisions.Length)
        {
            throw new ProductException("Invalid or unsupported .hygiene/decisions.json.");
        }

        return value;
    }
    private sealed record Config(int SchemaVersion, string[] DisabledRules);
    private sealed record RunSnapshot(int SchemaVersion, string RunId, Finding[] Findings, int IgnoredCount, ReviewBatch[]? ReviewBatches = null, string[]? TargetPaths = null, bool ChangedScope = false, string[]? InputPaths = null);
    private sealed record DecisionFile(int SchemaVersion, IgnoreDecision[] Decisions);

    /// <summary>Aktiviert oder deaktiviert eine konfigurierbare Regel.</summary>
    /// <param name="id">Kennung der Regel.</param>
    /// <param name="enabled">Gibt an, ob die Regel aktiviert werden soll.</param>
    public void SetRule(string id, bool enabled)
    {
        Rule rule = Rules.SingleOrDefault(r => r.Id == id) ?? throw new ProductException($"Unknown rule ID '{id}'.");
        if (!rule.Configurable)
        {
            throw new ProductException($"Rule '{id}' is mandatory and cannot be enabled or disabled.");
        }

        string? original = Snapshot(ConfigPath);
        string[] current = Disabled;
        string[] next = enabled ? current.Where(x => x != id).ToArray() : current.Append(id).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        WriteAtomic(ConfigPath, JsonSerializer.Serialize(new Config(1, next), json), original, true);
    }
    /// <summary>Listet Regeln mit ihrem jeweiligen Aktivierungsstatus auf.</summary>
    /// <returns>Regeln und ihr Aktivierungsstatus in kanonischer Reihenfolge.</returns>
    public IReadOnlyList<(Rule Rule, bool Enabled)> ListRules() => Rules.Select(r => (r, !r.Configurable || !Disabled.Contains(r.Id, StringComparer.Ordinal))).ToArray();

    internal string[] ResolveTargets(string[] paths, bool changed)
    {
        if (changed && paths.Length > 0)
        {
            throw new ArgumentException("Explicit paths and --changed are mutually exclusive.");
        }

        IEnumerable<string> files;
        if (changed)
        {
            try { files = GitChanged().ToArray(); }
            catch (Exception e) when (e is Win32Exception or IOException) { throw new EnvironmentException("Git is unavailable."); }
        }
        else if (paths.Length == 0)
        {
            files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories);
        }
        else
        {
            var chosen = new List<string>();
            foreach (string raw in paths)
            {
                string full = Path.GetFullPath(Path.IsPathRooted(raw) ? raw : Path.Combine(root, raw));
                if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && full != root)
                {
                    throw new ProductException("Target is outside the repository.");
                }

                if (File.Exists(full))
                {
                    if (!full.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ProductException($"Unsupported explicit file: {raw}");
                    }

                    chosen.Add(full);
                }
                else if (Directory.Exists(full))
                {
                    chosen.AddRange(Directory.EnumerateFiles(full, "*.cs", SearchOption.AllDirectories));
                }
                else
                {
                    throw new ProductException($"Target does not exist: {raw}");
                }
            }
            files = chosen;
        }
        return files.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).Where(File.Exists).Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !Excluded(p)).Order(StringComparer.Ordinal).ToArray();
    }
    private bool Excluded(string p) => Path.GetRelativePath(root, p).Split(Path.DirectorySeparatorChar).Any(x => x is ".git" or ".hygiene" or "bin" or "obj");
    private IEnumerable<string> GitChanged()
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        psi.ArgumentList.Add("status"); psi.ArgumentList.Add("--porcelain"); psi.ArgumentList.Add("-z"); psi.ArgumentList.Add("--untracked-files=all");
        using var p = System.Diagnostics.Process.Start(psi) ?? throw new IOException(); string output = p.StandardOutput.ReadToEnd(); p.WaitForExit(); if (p.ExitCode != 0)
        {
            throw new IOException();
        }

        foreach (string entry in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            if (entry.Length < 4 || entry.StartsWith(" D", StringComparison.Ordinal) || entry.StartsWith("D ", StringComparison.Ordinal))
            {
                continue;
            }

            string rel = entry[3..]; if (rel.Contains(" -> "))
            {
                rel = rel[(rel.LastIndexOf(" -> ", StringComparison.Ordinal) + 4)..];
            }

            yield return Path.Combine(root, rel);
        }
    }
    private string[] FindSdkProjects()
    {
        return EnumerateRepositoryFiles("*.csproj")
            .Where(p => !Excluded(p))
            .Order(StringComparer.Ordinal)
            .Where(IsSdkStyleProject)
            .ToArray();
    }

    private IEnumerable<string> EnumerateRepositoryFiles(string pattern)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            string directory = pending.Pop();
            foreach (string file in Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly))
            {
                yield return file;
            }

            foreach (string child in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
            {
                FileAttributes attributes = File.GetAttributes(child);
                if ((attributes & FileAttributes.ReparsePoint) != 0 || Excluded(child))
                {
                    continue;
                }

                string full = Path.GetFullPath(child);
                if (full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    pending.Push(full);
                }
            }
        }
    }

    private static bool IsSdkStyleProject(string project)
    {
        try
        {
            var xml = System.Xml.Linq.XDocument.Load(project);
            var element = xml.Root;
            return element?.Attribute("Sdk") is not null || element?.Elements().Any(e => e.Name.LocalName == "Sdk") == true;
        }
        catch (System.Xml.XmlException) { return false; }
    }

    private static MSBuildWorkspace CreateWorkspace()
    {
        RoslynWorkspaceRegistration.EnsureRegistered();
        return MSBuildWorkspace.Create(new Dictionary<string, string> { ["DesignTimeBuild"] = "true" });
    }

    /// <summary>Analysiert ausgewählte Quelldateien und veröffentlicht eine erfolgreiche Prüfrunde.</summary>
    /// <param name="paths">Optionale Repositorypfade für die Analyse.</param>
    /// <param name="changed">Gibt an, ob nur geänderte Dateien analysiert werden.</param>
    /// <param name="applyIgnores">Gibt an, ob gültige Ausnahmen angewendet werden.</param>
    /// <param name="includeDisabled">Gibt an, ob deaktivierte Regeln einbezogen werden.</param>
    /// <param name="publishLatest">Gibt an, ob die erfolgreiche Runde als letzte Runde gespeichert wird.</param>
    /// <returns>Deterministische Fundstellen und semantische Prüflose.</returns>
    public CheckResult Check(string[] paths, bool changed, bool applyIgnores = true, bool includeDisabled = false, bool publishLatest = true)
    {
        RequireProfile();
        string[] targets = ResolveTargets(paths, changed);
        string[] projectPaths = FindSdkProjects();
        using MSBuildWorkspace workspace = CreateWorkspace();
        var loadedProjects = new List<Project>();
        foreach (string projectPath in projectPaths)
        {
            try
            {
                Project? existing = workspace.CurrentSolution.Projects.FirstOrDefault(p => Path.GetFullPath(p.FilePath ?? "").Equals(Path.GetFullPath(projectPath), StringComparison.OrdinalIgnoreCase));
                Project loaded = existing ?? workspace.OpenProjectAsync(projectPath).GetAwaiter().GetResult();
                loadedProjects.Add(workspace.CurrentSolution.GetProject(loaded.Id)!);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                throw new ProductException($"Unable to evaluate SDK-style project '{Path.GetRelativePath(root, projectPath)}': {e.Message}");
            }
        }
        var projectsByTarget = new Dictionary<string, Project>(StringComparer.OrdinalIgnoreCase);
        var directlyTargetedFiles = paths.Select(raw => Path.GetFullPath(Path.IsPathRooted(raw) ? raw : Path.Combine(root, raw)))
            .Where(File.Exists).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string target in targets)
        {
            Project? project = loadedProjects
                .Where(p => p.Documents.Any(d => d.FilePath is not null && Path.GetFullPath(d.FilePath).Equals(target, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(p => Path.GetFullPath(p.FilePath!).Length)
                .FirstOrDefault();
            if (project is null)
            {
                if (directlyTargetedFiles.Contains(target))
                {
                    throw new ProductException($"C# file is not included by a discoverable SDK-style project: {Path.GetRelativePath(root, target)}");
                }

                continue;
            }
            projectsByTarget[target] = project;
        }
        string[] disabled = includeDisabled ? [] : Disabled;
        IgnoreDecision[] decisions = ReadDecisions().Decisions;
        var candidates = new List<Finding>();
        var reviewSubjects = new List<(string Identity, string Content, ReviewItem Item)>();
        var reviewSourceContents = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var projectGroup in projectsByTarget.GroupBy(item => item.Value.Id))
        {
            Project project = workspace.CurrentSolution.GetProject(projectGroup.Key)!;
            Compilation compilation = project.GetCompilationAsync().GetAwaiter().GetResult()
                ?? throw new ProductException($"Roslyn did not produce a compilation for '{Path.GetRelativePath(root, project.FilePath!)}'.");
            var targetSet = projectGroup.Select(item => item.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (Document document in project.Documents.Where(d => d.FilePath is not null && targetSet.Contains(Path.GetFullPath(d.FilePath))))
            {
                SyntaxTree tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult() ?? throw new ProductException($"Roslyn did not provide syntax for '{document.FilePath}'.");
                string path = tree.FilePath;
                string text = File.ReadAllText(path); string rel = Path.GetRelativePath(root, path).Replace('\\', '/');
                var model = compilation.GetSemanticModel(tree);
                var occurrenceCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                SyntaxNode syntaxRoot = tree.GetRoot();
                foreach (SyntaxNode node in syntaxRoot.DescendantNodes())
                {
                    if (node is MemberDeclarationSyntax member && model.GetDeclaredSymbol(member) is ISymbol symbol && (symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal) && !symbol.IsImplicitlyDeclared && SummaryEligible(symbol))
                    {
                        foreach (Finding documentationFinding in DocumentationFindings(member, symbol, model, compilation, rel, tree))
                        {
                            if (documentationFinding.RuleId == Rules[3].Id ? !disabled.Contains(Rules[3].Id) : !disabled.Contains(Rules[4].Id))
                            {
                                candidates.Add(documentationFinding);
                            }
                        }

                        bool hasSummary = HasSummary(member);
                        string anchor = symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString();
                        if (!disabled.Contains(Rules[2].Id) && !hasSummary)
                        {
                            candidates.Add(Make(Rules[2], rel, tree, member.GetLocation().SourceSpan.Start, symbol.ToDisplayString(), "Public or internal symbol has no non-empty documentation summary.", "Add a concise documentation summary for this API subject.", "The declaration has no summary text.", "This rule requires documentation, not a particular wording or language.", anchor, "missing-summary:" + anchor));
                        }

                        if (!disabled.Contains(Rules[5].Id) && hasSummary)
                        {
                            string summary = GetSummaryText(member);
                            if (!string.IsNullOrWhiteSpace(summary))
                            {
                                var pos = tree.GetLineSpan(member.GetLocation().SourceSpan).StartLinePosition;
                                ReviewItem item = new("", rel, pos.Line + 1, pos.Character + 1, symbol.ToDisplayString(), summary, symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                                reviewSubjects.Add((rel + "\0" + anchor, GetSummaryContent(member), item));
                                reviewSourceContents.TryAdd(rel, tree.GetText().ToString());
                            }
                        }
                    }
                    if (node is RecordDeclarationSyntax record && record.ParameterList is not null && model.GetDeclaredSymbol(record) is INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public or Accessibility.Internal } recordSymbol)
                    {
                        string recordAnchor = recordSymbol.GetDocumentationCommentId() ?? recordSymbol.ToDisplayString();
                        foreach (ParameterSyntax parameter in record.ParameterList.Parameters)
                        {
                            string parameterName = parameter.Identifier.ValueText;
                            IPropertySymbol? synthesizedProperty = recordSymbol.GetMembers(parameterName).OfType<IPropertySymbol>().FirstOrDefault();
                            if (synthesizedProperty?.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
                            {
                                continue;
                            }

                            string parameterSummary = GetParamText(record, parameterName);
                            if (string.IsNullOrWhiteSpace(parameterSummary) && !HasInheritdoc(record))
                            {
                                string propertyAnchor = recordAnchor + "." + parameterName;
                                candidates.Add(Make(Rules[2], rel, tree, parameter.SpanStart, propertyAnchor,
                                    "Public or internal positional record property has no non-empty documentation summary.",
                                    $"Add a non-empty <param name=\"{parameterName}\"> summary to the record documentation.",
                                    "The matching record parameter has no summary prose.",
                                    "Ordinary parameters remain optional documentation subjects.", propertyAnchor, "missing-record-property-summary:" + propertyAnchor));
                            }
                            else if (!string.IsNullOrWhiteSpace(parameterSummary))
                            {
                                var position = tree.GetLineSpan(parameter.Span).StartLinePosition;
                                var item = new ReviewItem("", rel, position.Line + 1, position.Character + 1, recordAnchor + "." + parameterName, parameterSummary, recordSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                                reviewSubjects.Add((rel + "\0" + recordAnchor + "." + parameterName, GetParamContent(record, parameterName), item));
                                reviewSourceContents.TryAdd(rel, tree.GetText().ToString());
                            }
                        }
                    }
                }
                if (!disabled.Contains(Rules[6].Id))
                {
                    SourceTextLines(text, (line, number, start) =>
                    {
                        if (line.Length > 200)
                        {
                            SyntaxNode? containing = syntaxRoot.FindNode(new Microsoft.CodeAnalysis.Text.TextSpan(start, line.Length), getInnermostNodeForTie: false);
                            SyntaxNode? anchorNode = containing?.AncestorsAndSelf().FirstOrDefault(n => n is MemberDeclarationSyntax or BaseTypeDeclarationSyntax);
                            string anchor = anchorNode is not null && model.GetDeclaredSymbol(anchorNode) is ISymbol s ? s.GetDocumentationCommentId() ?? s.ToDisplayString() : rel;
                            string canonical = string.Join(" ", syntaxRoot.DescendantTokens(new Microsoft.CodeAnalysis.Text.TextSpan(start, line.Length)).Select(t => t.ToString()));
                            string discriminator = NextDiscriminator(occurrenceCounts, Rules[6], anchor, canonical);
                            candidates.Add(Make(Rules[6], rel, tree, start, anchorNode is not null && model.GetDeclaredSymbol(anchorNode) is ISymbol symbol ? symbol.ToDisplayString() : null, "Physical line exceeds 200 characters.", "Review whether the line hides multiple concepts or structures that should be made visible or named.", "The physical line exceeds 200 UTF-16 code units.", "Do not split mechanically merely to satisfy a line-length limit.", anchor, canonical + "\0" + discriminator, discriminator));
                        }
                    });
                }
                if (!disabled.Contains(Rules[7].Id))
                {
                    foreach (BlockSyntax block in syntaxRoot.DescendantNodes().OfType<BlockSyntax>())
                    {
                        StatementSyntax[] statements = block.Statements.ToArray();
                        for (int i = 1; i < statements.Length; i++)
                        {
                            StatementSyntax current = statements[i], previous = statements[i - 1];
                            if (!IsControl(current) || IsControl(previous) || previous is LocalFunctionStatementSyntax)
                            {
                                continue;
                            }

                            int prevEnd = previous.Span.End;
                            SyntaxTriviaList leading = current.GetLeadingTrivia();
                            SyntaxTrivia[] comments = leading.Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia) || t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)).ToArray();
                            int boundary = comments.Length > 0 ? comments[0].SpanStart : current.SpanStart;
                            string between = text[Math.Min(prevEnd, text.Length)..Math.Clamp(boundary, prevEnd, text.Length)];
                            string[] boundaryLines = between.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
                            if (boundaryLines.Length > 2 && boundaryLines.Skip(1).Take(boundaryLines.Length - 2).Any(string.IsNullOrWhiteSpace))
                            {
                                continue;
                            }

                            ISymbol? container = model.GetEnclosingSymbol(current.SpanStart);
                            string anchor = container?.GetDocumentationCommentId() ?? container?.ToDisplayString() ?? rel;
                            string evidence = previous.Kind().ToString() + ":" + string.Join(" ", previous.DescendantTokens().Select(t => t.ToString())) + "|" + current.Kind() + ":" + string.Join(" ", current.DescendantTokens().Select(t => t.ToString()));
                            string discriminator = NextDiscriminator(occurrenceCounts, Rules[7], anchor, evidence);
                            candidates.Add(Make(Rules[7], rel, tree, boundary, container?.ToDisplayString(), "Control-flow statement needs a blank line after the preceding linear statement.", "Insert one completely blank line before this control-flow group.", "A control-flow statement immediately follows a linear statement without a blank line.", "Keep comments documenting the control-flow statement with that statement.", anchor, evidence + "\0" + discriminator, discriminator));
                        }
                    }
                }
            }
        }
        foreach (ProfileFinding finding in AnalyzeProfile())
        {
            Rule rule = Rules.Single(r => r.Id == finding.RuleId);
            string anchor = "profile:" + finding.Path;
            string fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rule.Id + "\0" + anchor + "\0" + finding.Message)));
            candidates.Add(new Finding("", "", rule.Id, rule.Version, rule.Classification, finding.Path, 1, 1, null,
                finding.Message, finding.Suggestion, finding.Message, finding.Message, finding.Suggestion, anchor, fingerprint));
        }
        var active = candidates.Where(c => !applyIgnores || !decisions.Any(d => Same(d, c))).OrderBy(c => Array.FindIndex(Rules, r => r.Id == c.RuleId)).ThenBy(c => c.Path, StringComparer.Ordinal).ThenBy(c => c.Line).ThenBy(c => c.Column).ThenBy(c => c.Fingerprint, StringComparer.Ordinal).ToArray();
        string run = "R-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(5))[..8];
        for (int i = 0; i < active.Length; i++) { string id = "F-" + (i + 1); active[i] = active[i] with { Id = id, Handle = run + "/" + id }; }
        int ignored = candidates.Count - active.Length;
        ReviewBatch[] batches = disabled.Contains(Rules[5].Id) ? [] : [BuildReviewBatch(run, reviewSubjects, reviewSourceContents)];
        var result = new CheckResult(1, run, active, ignored, batches);
        if (publishLatest)
        {
            string[] storedTargets = targets.Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')).ToArray();
            string[] storedInputs = changed ? storedTargets : paths.Select(p => Path.GetFullPath(Path.IsPathRooted(p) ? p : Path.Combine(root, p)))
                .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')).ToArray();
            WriteAtomic(LatestPath, JsonSerializer.Serialize(new RunSnapshot(1, run, active, ignored, batches.Select(b => b with { SourceContents = null }).ToArray(), storedTargets, changed, storedInputs), json));
        }
        return result;
    }

    private static readonly ReviewQuestion[] SummaryQuestions =
    [
        new("Q1", "German quality: Is the summary natural, comprehensible German rather than awkward literal translation or merely German-looking text?"),
        new("Q2", "Technical correctness: Is it consistent with the declaration and relevant implementation/API context, without inventing behavior?"),
        new("Q3", "Information value: Does it add useful caller-relevant meaning rather than simply repeat/paraphrase the symbol name/type/signature? A concise summary is acceptable only when it still communicates useful purpose/domain meaning."),
        new("Q4", "Clarity and scope: Is it concise, specific, and clear enough to communicate responsibility without irrelevant implementation detail?")
    ];

    private static ReviewBatch BuildReviewBatch(string run, List<(string Identity, string Content, ReviewItem Item)> subjects, Dictionary<string, string> sourceContents)
    {
        var ordered = subjects.GroupBy(s => s.Identity, StringComparer.Ordinal).Select(g => g.First())
            .OrderBy(s => s.Item.Path, StringComparer.Ordinal).ThenBy(s => s.Item.Line).ThenBy(s => s.Identity, StringComparer.Ordinal)
            .Select(s => (s.Identity, s.Content, s.Item, ContentFingerprint: Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s.Content)))))
            .ToArray();
        string populationData = string.Join("\n", ordered.Select(s => s.Identity + "\0" + s.ContentFingerprint));
        string population = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Rules[5].Id + "\0" + Rules[5].Version + "\0" + populationData)));
        var ranked = ordered.Select(s => (Subject: s, Rank: Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Rules[5].Id + "\0" + Rules[5].Version + "\0" + population + "\0" + s.Identity + "\0" + s.ContentFingerprint)))))
            .OrderBy(x => x.Rank, StringComparer.Ordinal).ThenBy(x => x.Subject.Identity, StringComparer.Ordinal).Take(5).ToArray();
        ReviewItem[] all = ordered.Select((s, i) => s.Item with { Id = "RI-" + (i + 1) }).ToArray();
        var ids = all.ToDictionary(x => x.Path + "\0" + x.Line + "\0" + x.Symbol, x => x.Id, StringComparer.Ordinal);
        ReviewItem[] sample = ranked.Select(x => x.Subject.Item with { Id = ids[x.Subject.Item.Path + "\0" + x.Subject.Item.Line + "\0" + x.Subject.Item.Symbol] }).ToArray();
        ReviewSource[] sources = ordered.Select(s => s.Item.Path).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
            .Select(path => sourceContents.TryGetValue(path, out string? content) && content is not null ? new ReviewSource(path, content) : throw new ProductException($"Source context for '{path}' is unavailable."))
            .ToArray();
        return new ReviewBatch("B-1", run + "/B-1", Rules[5].Id, 2, "sample", "implementer", ordered.Length, sample.Length, SummaryQuestions,
            new ReviewEscalation("Expand if any sampled summary materially fails Q1-Q4 or the implementer cannot confidently answer any required question.", "hygiene review expand " + run + "/B-1", "frontier"), sample, population, all, sources);
    }

    public ReviewBatch ExpandReview(string handle)
    {
        RunSnapshot snapshot = Read(LatestPath, new RunSnapshot(0, "", [], 0));
        if (snapshot.SchemaVersion != 1 || string.IsNullOrWhiteSpace(snapshot.RunId) || snapshot.ReviewBatches is null || snapshot.TargetPaths is null)
        {
            throw new ProductException("Review batch is unavailable; run check again.");
        }

        string batchId = handle;
        if (handle.Contains('/'))
        {
            string[] parts = handle.Split('/');
            if (parts.Length != 2 || parts[0] != snapshot.RunId)
            {
                throw new ProductException("Review batch handle does not refer to the latest available run.");
            }

            batchId = parts[1];
        }
        ReviewBatch stored = snapshot.ReviewBatches.SingleOrDefault(b => b.Id == batchId) ?? throw new ProductException($"Review batch '{handle}' was not found in the latest run.");
        CheckResult fresh;
        string[] revalidationPaths = snapshot.ChangedScope ? snapshot.TargetPaths : snapshot.InputPaths ?? snapshot.TargetPaths;
        try { fresh = Check(revalidationPaths, false, true, false, false); }
        catch (ProductException) { throw new ProductException("Review population cannot be revalidated; rerun hygiene check."); }
        ReviewBatch current = fresh.ReviewBatches.SingleOrDefault(b => b.RuleId == stored.RuleId) ?? throw new ProductException("Review population changed; rerun hygiene check.");
        if (!StringComparer.Ordinal.Equals(stored.PopulationFingerprint, current.PopulationFingerprint))
        {
            throw new ProductException("Review population changed; rerun hygiene check.");
        }

        return stored with
        {
            Mode = "expanded",
            ReviewerClass = "frontier",
            PopulationCount = current.PopulationCount,
            SampleCount = current.PopulationCount,
            Items = current.PopulationItems ?? [],
            PopulationItems = current.PopulationItems,
            SourceContents = current.SourceContents
        };
    }

    public string CreateReviewHandoff(string handle, string? filePath = null)
    {
        ReviewBatch expanded = ExpandReview(handle);
        string[] sourceHandle = expanded.Handle.Split('/');
        string runId = sourceHandle[0];
        string runToken = runId.StartsWith("R-", StringComparison.Ordinal) ? runId[2..] : runId;
        string batchToken = expanded.Id.Replace("-", "", StringComparison.Ordinal);
        string handoffId = "HR-" + Regex.Replace(runToken, "[^A-Za-z0-9]", "", RegexOptions.CultureInvariant).ToUpperInvariant() + "-" + Regex.Replace(batchToken, "[^A-Za-z0-9]", "", RegexOptions.CultureInvariant).ToUpperInvariant();
        var request = new SemanticReviewRequest(1, "semantic-review-request", handoffId, DateTimeOffset.UtcNow.ToString("O"),
            new ReviewRequestSource(runId, expanded.Handle), new ReviewRequestRule(expanded.RuleId, expanded.RuleVersion), "expanded", "frontier",
            expanded.PopulationCount, expanded.Questions, expanded.Items, expanded.SourceContents ?? []);
        string destination = Path.GetFullPath(string.IsNullOrWhiteSpace(filePath)
            ? Path.Combine(root, ".hygiene", "reviews", handoffId, "request.json")
            : Path.IsPathRooted(filePath) ? filePath : Path.Combine(root, filePath));
        WriteNewAtomic(destination, JsonSerializer.Serialize(request, json));
        return destination;
    }

    private void WriteNewAtomic(string path, string content)
    {
        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        if (File.Exists(path))
        {
            throw new ProductException($"Review handoff destination already exists: '{path}'. Choose a new --file path or remove the existing request explicitly.");
        }

        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
                writer.Flush();
                stream.Flush(true);
            }
            beforeAtomicReplace?.Invoke();
            try { File.Move(temp, path, false); }
            catch (IOException) when (File.Exists(path))
            {
                throw new ProductException($"Review handoff destination already exists: '{path}'. Choose a new --file path or remove the existing request explicitly.");
            }
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }
    private static bool IsControl(StatementSyntax s) => s is IfStatementSyntax or SwitchStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or ForEachVariableStatementSyntax or WhileStatementSyntax or DoStatementSyntax or TryStatementSyntax or UsingStatementSyntax or LockStatementSyntax;
    private static bool Same(IgnoreDecision d, Finding f) => d.RuleId == f.RuleId && d.RuleVersion == f.RuleVersion && d.Path == f.Path && d.Anchor == f.Anchor && d.Fingerprint == f.Fingerprint && d.Discriminator == f.Discriminator;
    private static string NextDiscriminator(Dictionary<string, int> counts, Rule rule, string anchor, string evidence)
    {
        string key = rule.Id + "\0" + anchor + "\0" + evidence;
        counts.TryGetValue(key, out int occurrence);
        counts[key] = occurrence + 1;
        return "occurrence:" + occurrence;
    }
    private static bool SummaryEligible(ISymbol s) => s switch { INamedTypeSymbol => true, IPropertySymbol => true, IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.Ordinary } => true, _ => false };
    private static bool HasSummary(MemberDeclarationSyntax m)
        => !string.IsNullOrWhiteSpace(GetSummaryText(m)) || HasInheritdoc(m);

    private static DocumentationCommentTriviaSyntax? Documentation(MemberDeclarationSyntax member)
        => member.GetLeadingTrivia().Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().FirstOrDefault();

    private static bool HasInheritdoc(MemberDeclarationSyntax member)
        => Documentation(member)?.Content.OfType<XmlEmptyElementSyntax>().Any(e => e.Name.LocalName.ValueText == "inheritdoc") == true;

    private static string GetParamText(MemberDeclarationSyntax member, string name)
    {
        XmlElementSyntax? element = Documentation(member)?.DescendantNodes().OfType<XmlElementSyntax>().FirstOrDefault(e =>
            e.StartTag.Name.LocalName.ValueText == "param" && e.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().Any(a => a.Identifier.Identifier.ValueText == name));
        return element is null ? "" : XmlProse(element.ToFullString());
    }

    private static string GetParamContent(MemberDeclarationSyntax member, string name)
    {
        XmlElementSyntax? element = Documentation(member)?.DescendantNodes().OfType<XmlElementSyntax>().FirstOrDefault(e =>
            e.StartTag.Name.LocalName.ValueText == "param" && e.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().Any(a => a.Identifier.Identifier.ValueText == name));
        if (element is null)
        {
            return "";
        }

        try { return Regex.Replace(XElement.Parse(element.ToFullString()).ToString(SaveOptions.DisableFormatting), @"\s+", " ").Trim(); }
        catch (System.Xml.XmlException) { return ""; }
    }

    private static string XmlProse(string xml)
    {
        try { return Regex.Replace(XElement.Parse(xml).Value, @"\s+", " ").Trim(); }
        catch (System.Xml.XmlException) { return ""; }
    }

    private static IReadOnlyList<Finding> DocumentationFindings(MemberDeclarationSyntax member, ISymbol symbol, SemanticModel model, Compilation compilation, string path, SyntaxTree tree)
    {
        DocumentationCommentTriviaSyntax? documentation = Documentation(member);
        if (documentation is null)
        {
            return [];
        }

        var findings = new List<Finding>();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        string anchor = symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString();
        HashSet<string> parameters = symbol switch
        {
            IMethodSymbol method => method.Parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            IPropertySymbol property => property.Parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { } invoke } => invoke.Parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            INamedTypeSymbol type when member is RecordDeclarationSyntax record => (record.ParameterList?.Parameters ?? default).Select(p => p.Identifier.ValueText).ToHashSet(StringComparer.Ordinal),
            _ => new HashSet<string>(StringComparer.Ordinal)
        };
        HashSet<string> typeParameters = symbol switch
        {
            IMethodSymbol method => method.TypeParameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            INamedTypeSymbol type => type.TypeParameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            _ => new HashSet<string>(StringComparer.Ordinal)
        };
        HashSet<string> inScopeTypeParameters = symbol switch
        {
            IMethodSymbol method => method.TypeParameters.Concat(ContainingTypeParameters(method.ContainingType)).Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            INamedTypeSymbol type => type.TypeParameters.Concat(ContainingTypeParameters(type.ContainingType)).Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            IPropertySymbol property => ContainingTypeParameters(property.ContainingType).Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
            _ => new HashSet<string>(StringComparer.Ordinal)
        };
        foreach (XmlElementSyntax element in documentation.DescendantNodes().OfType<XmlElementSyntax>())
        {
            string tag = element.StartTag.Name.LocalName.ValueText;
            string? name = element.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault(a => a.Name.ToString() == "name")?.Identifier.Identifier.ValueText;
            string prose = XmlProse(element.ToFullString());
            string? issue = null;
            if (tag == "param")
            {
                if (string.IsNullOrWhiteSpace(name) || !parameters.Contains(name))
                {
                    issue = "<param> must name a declaration parameter.";
                }
                else if (!counts.TryAdd("param:" + name, 1))
                {
                    issue = $"Duplicate <param> for '{name}'.";
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = $"<param name=\"{name}\"> must contain prose.";
                }
            }
            else if (tag == "typeparam")
            {
                if (string.IsNullOrWhiteSpace(name) || !typeParameters.Contains(name))
                {
                    issue = "<typeparam> must name a declaration type parameter.";
                }
                else if (!counts.TryAdd("typeparam:" + name, 1))
                {
                    issue = $"Duplicate <typeparam> for '{name}'.";
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = $"<typeparam name=\"{name}\"> must contain prose.";
                }
            }
            else if (tag == "returns")
            {
                bool hasValueReturn = symbol switch
                {
                    IMethodSymbol callable => !callable.ReturnsVoid,
                    INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod.ReturnsVoid: false } => true,
                    _ => false
                };
                if (!counts.TryAdd("returns", 1))
                {
                    issue = "Only one <returns> element is allowed.";
                }
                else if (!hasValueReturn)
                {
                    issue = "<returns> is only valid for a value-returning callable.";
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = "<returns> must contain prose.";
                }
            }
            else if (tag == "value")
            {
                if (!counts.TryAdd("value", 1))
                {
                    issue = "Only one <value> element is allowed.";
                }
                else if (symbol is not IPropertySymbol)
                {
                    issue = "<value> is only valid on a property or indexer.";
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = "<value> must contain prose.";
                }
            }
            else if (tag == "exception")
            {
                XmlCrefAttributeSyntax? cref = element.StartTag.Attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault();
                ISymbol? target = cref is null ? null : model.GetSymbolInfo(cref.Cref).Symbol;
                INamedTypeSymbol? baseException = compilation.GetTypeByMetadataName("System.Exception");
                bool valid = target is INamedTypeSymbol exceptionType && baseException is not null && IsDerivedFrom(exceptionType, baseException);
                if (!valid)
                {
                    issue = "<exception cref> must resolve to an exception type.";
                }
                else if (string.IsNullOrWhiteSpace(prose))
                {
                    issue = "<exception> must contain prose.";
                }
            }

            if (issue is not null)
            {
                findings.Add(Make(Rules[3], path, tree, element.SpanStart, symbol.ToDisplayString(), issue, "Correct or remove the optional XML element.", issue, "Optional XML elements are checked only when present.", anchor, tag + ":" + element.ToFullString()));
            }
            if (tag is "summary" or "param" or "typeparam" or "returns" or "value" or "exception" && !string.IsNullOrWhiteSpace(prose) && prose[^1] is not ('.' or '?' or '!'))
            {
                string message = $"Explicit <{tag}> prose must end with '.', '?' or '!'.";
                findings.Add(Make(Rules[4], path, tree, element.SpanStart, symbol.ToDisplayString(), message, "Finish the prose with sentence punctuation.", prose, "Sentence punctuation is mechanical and does not judge prose quality.", anchor, tag + ":" + element.ToFullString()));
            }
        }
        foreach (XmlEmptyElementSyntax reference in documentation.DescendantNodes().OfType<XmlEmptyElementSyntax>())
        {
            string tag = reference.Name.LocalName.ValueText;
            if (tag is not ("paramref" or "typeparamref"))
            {
                continue;
            }

            string? name = reference.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault(a => a.Name.ToString() == "name")?.Identifier.Identifier.ValueText;
            bool valid = tag == "paramref" ? !string.IsNullOrEmpty(name) && parameters.Contains(name) : !string.IsNullOrEmpty(name) && inScopeTypeParameters.Contains(name);
            if (!valid)
            {
                string message = $"<{tag}> must reference a declaration parameter of the matching kind.";
                findings.Add(Make(Rules[3], path, tree, reference.SpanStart, symbol.ToDisplayString(), message, "Use a parameter or type parameter declared by this API.", name ?? "missing name", "References are validated without requiring documentation for parameters.", anchor, tag + ":" + reference.ToFullString()));
            }
        }
        return findings;
    }

    private static IEnumerable<ITypeParameterSymbol> ContainingTypeParameters(INamedTypeSymbol? type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            foreach (ITypeParameterSymbol parameter in current.TypeParameters)
            {
                yield return parameter;
            }
        }
    }

    private static bool IsDerivedFrom(INamedTypeSymbol symbol, INamedTypeSymbol baseType)
    {
        for (INamedTypeSymbol? current = symbol; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }
        return false;
    }

    private static string GetSummaryText(MemberDeclarationSyntax member)
    {
        foreach (SyntaxTrivia trivia in member.GetLeadingTrivia())
        {
            if (trivia.GetStructure() is not DocumentationCommentTriviaSyntax documentation)
            {
                continue;
            }

            foreach (XmlElementSyntax summary in documentation.DescendantNodes().OfType<XmlElementSyntax>().Where(x => x.StartTag.Name.LocalName.ValueText == "summary"))
            {
                try
                {
                    string value = XElement.Parse(summary.ToFullString(), LoadOptions.None).Value;
                    return Regex.Replace(value, @"\s+", " ").Trim();
                }
                catch (System.Xml.XmlException) { continue; }
            }
        }
        return "";
    }

    private static string GetSummaryContent(MemberDeclarationSyntax member)
    {
        foreach (SyntaxTrivia trivia in member.GetLeadingTrivia())
        {
            if (trivia.GetStructure() is not DocumentationCommentTriviaSyntax documentation)
            {
                continue;
            }

            foreach (XmlElementSyntax summary in documentation.DescendantNodes().OfType<XmlElementSyntax>().Where(x => x.StartTag.Name.LocalName.ValueText == "summary"))
            {
                try { return Regex.Replace(XElement.Parse(summary.ToFullString()).ToString(SaveOptions.DisableFormatting), @"\s+", " ").Trim(); }
                catch (System.Xml.XmlException) { continue; }
            }
        }
        return "";
    }

    private static bool HasDocumentationContent(SyntaxNode node) => node switch
    {
        XmlTextSyntax text => text.TextTokens.Any(token => token.ValueText.Any(c => !char.IsWhiteSpace(c))),
        XmlEmptyElementSyntax => true,
        XmlElementSyntax element => element.Content.Any(HasDocumentationContent),
        _ => node.ChildNodes().Any(HasDocumentationContent)
    };
    private static void SourceTextLines(string s, Action<string, int, int> action)
    {
        int start = 0, number = 1;
        for (int i = 0; i <= s.Length; i++)
        {
            if (i < s.Length && s[i] is not ('\r' or '\n'))
            {
                continue;
            }

            action(s[start..i], number++, start);
            if (i == s.Length)
            {
                break;
            }

            if (s[i] == '\r' && i + 1 < s.Length && s[i + 1] == '\n')
            {
                i++;
            }

            start = i + 1;
        }
    }
    private static Finding Make(Rule r, string path, SyntaxTree tree, int offset, string? symbol, string msg, string suggestion, string observation, string constraint, string anchor, string evidence, string? discriminator = null)
    {
        var pos = tree.GetLineSpan(new Microsoft.CodeAnalysis.Text.TextSpan(offset, 0)).StartLinePosition;
        string fp = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(evidence)));
        return new Finding("", "", r.Id, r.Version, r.Classification, path, pos.Line + 1, pos.Character + 1, symbol, msg, suggestion, observation, msg, constraint, anchor, fp, discriminator);
    }

    public Finding Explain(string handle)
    {
        RunSnapshot snapshot = Read(LatestPath, new RunSnapshot(0, "", [], 0));
        if (snapshot.SchemaVersion != 1 || string.IsNullOrWhiteSpace(snapshot.RunId) || snapshot.Findings is null || snapshot.IgnoredCount < 0 || snapshot.Findings.Any(f => f is null || string.IsNullOrEmpty(f.Id) || string.IsNullOrEmpty(f.RuleId)) || snapshot.Findings.Select(f => f.Id).Distinct(StringComparer.Ordinal).Count() != snapshot.Findings.Length)
        {
            throw new ProductException("Latest run state is unavailable or malformed; run check again.");
        }

        string id = handle;
        if (handle.Contains('/')) { string[] parts = handle.Split('/'); if (parts.Length != 2 || parts[0] != snapshot.RunId) { throw new ProductException("Finding handle does not refer to the latest available run."); } id = parts[1]; }
        return snapshot.Findings.SingleOrDefault(f => f.Id == id) ?? throw new ProductException($"Finding '{handle}' was not found in the latest run.");
    }
    public IgnoreDecision Ignore(string handle, string reason)
    {
        Finding reference = Explain(handle);
        if (!Rules.Single(r => r.Id == reference.RuleId).Configurable)
        {
            throw new ProductException($"Mandatory profile finding '{reference.RuleId}' cannot be ignored.");
        }

        CheckResult fresh = Check([reference.Path], false, false, true, false);
        Finding? current = fresh.Findings.FirstOrDefault(f => Same(new IgnoreDecision("", reference.RuleId, reference.RuleVersion, reference.Path, reference.Anchor, reference.Fingerprint, "", default, reference.Discriminator), f));
        if (current is null)
        {
            throw new ProductException("Finding is stale; run check again before ignoring it.");
        }

        string? original = Snapshot(DecisionsPath);
        DecisionFile file = ReadDecisions();
        IgnoreDecision decision = new("I-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(4)), current.RuleId, current.RuleVersion, current.Path, current.Anchor, current.Fingerprint, reason, DateTimeOffset.UtcNow, current.Discriminator);
        WriteAtomic(DecisionsPath, JsonSerializer.Serialize(new DecisionFile(1, file.Decisions.Append(decision).ToArray()), json), original, true);
        return decision;
    }
    public void Unignore(string id)
    {
        string? original = Snapshot(DecisionsPath);
        DecisionFile file = ReadDecisions();
        IgnoreDecision[] next = file.Decisions.Where(d => d.Id != id).ToArray();
        if (next.Length == file.Decisions.Length)
        {
            throw new ProductException($"Ignore decision '{id}' was not found.");
        }

        WriteAtomic(DecisionsPath, JsonSerializer.Serialize(new DecisionFile(1, next), json), original, true);
    }
    public IReadOnlyList<IgnoreView> ListIgnores(string[] paths)
    {
        DecisionFile file = ReadDecisions();
        var filters = paths.Select(p =>
        {
            string full = Path.GetFullPath(Path.IsPathRooted(p) ? p : Path.Combine(root, p));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && full != root)
            {
                throw new ProductException("Ignore filter is outside the repository.");
            }

            return (Path: Path.GetRelativePath(root, full).Replace('\\', '/').TrimEnd('/'), Directory: Directory.Exists(full));
        }).ToArray();
        var scoped = filters.Length == 0 ? file.Decisions : file.Decisions.Where(d => filters.Any(f => d.Path == f.Path || (f.Directory && d.Path.StartsWith(f.Path + "/", StringComparison.Ordinal))));
        return scoped.Select(d =>
        {
            bool active = false;
            try { active = Check([d.Path], false, false, true, false).Findings.Any(f => Same(d, f)); } catch (ProductException) { }
            return new IgnoreView(d.Id, d.RuleId, d.Path, active ? "active" : "stale", d.Reason);
        }).ToArray();
    }
}
