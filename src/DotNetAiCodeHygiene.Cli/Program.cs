using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using DotNetAiCodeHygiene.Core;

namespace DotNetAiCodeHygiene.Cli;

internal static class Program
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public static async Task<int> Main(string[] args)
    {
        RootCommand root = Build();
        ParseResult parsed = root.Parse(args);
        if (parsed.Errors.Count > 0) { foreach (var error in parsed.Errors) { Console.Error.WriteLine(error.Message); } return 2; }
        try { return await parsed.InvokeAsync(); }
        catch (ArgumentException e) { Console.Error.WriteLine(e.Message); return 2; }
        catch (ProductException e) { Console.Error.WriteLine(e.Message); return 3; }
        catch (EnvironmentException e) { Console.Error.WriteLine(e.Message); return 4; }
        catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }
    private static RootCommand Build()
    {
        var root = new RootCommand("Deterministic .NET hygiene, formatting, and normalization tools.");
        var output = new Option<string>("--output") { DefaultValueFactory = _ => "text" };
        root.Options.Add(output);
        root.SetAction(parse => Run(() => RunAutonomous(parse.GetValue(output) ?? "text")));
        var baseline = new Command("baseline", "Review the project bootstrap baseline decision.");
        var acceptBaseline = new Command("accept", "Record an external baseline approval.");
        var approver = new Option<string>("--approver") { Description = "Name or identity of the authorized external approver." };
        acceptBaseline.Options.Add(approver);
        acceptBaseline.SetAction(parse => Run(() => AcceptBaseline(parse.GetValue(approver) ?? "", parse.GetValue(output) ?? "text")));
        baseline.Subcommands.Add(acceptBaseline);
        root.Subcommands.Add(baseline);
        foreach (string name in new[] { "bootstrap", "update" })
        {
            var profile = new Command(name, name == "bootstrap" ? "Install the supported dotnet-11 hygiene profile." : "Reconcile the installed hygiene profile.");
            profile.Options.Add(output);
            profile.SetAction(parse => Run(() => RenderProfile(name == "bootstrap" ? new HygieneEngine().Bootstrap(parse.GetValue(output) ?? "text") : new HygieneEngine().UpdateProfile(parse.GetValue(output) ?? "text"), parse.GetValue(output) ?? "text")));
            root.Subcommands.Add(profile);
        }
        foreach (string name in new[] { "format", "normalize" })
        {
            var command = new Command(name, name == "format" ? "Apply enabled presentation-only format rules to selected C# source." : "Apply enabled semantics-preserving normalize rules to selected C# source.");
            var pathsArg = new Argument<string[]>("paths") { Arity = ArgumentArity.ZeroOrMore };
            var changedOpt = new Option<bool>("--changed"); var checkOpt = new Option<bool>("--check");
            command.Arguments.Add(pathsArg); command.Options.Add(changedOpt); command.Options.Add(checkOpt); command.Options.Add(output);
            command.SetAction(parse => Run(() => RenderRewrite(new RewriteEngine(), name, parse.GetValue(pathsArg) ?? [], parse.GetValue(changedOpt), parse.GetValue(checkOpt), parse.GetValue(output) ?? "text")));
            root.Subcommands.Add(command);
        }
        var agentHelp = new Command("help", "Show coding-agent workflow guidance."); var agent = new Option<bool>("--agent") { Description = "Show stable coding-agent guidance." }; agentHelp.Options.Add(agent);
        agentHelp.SetAction(parse => Run(() => { if (!parse.GetValue(agent)) { Console.WriteLine(root.Description); return 0; } Console.WriteLine(AgentGuidance); return 0; })); root.Subcommands.Add(agentHelp);
        var paths = new Argument<string[]>("paths") { Arity = ArgumentArity.ZeroOrMore };
        var changed = new Option<bool>("--changed");
        var baseRef = new Option<string?>("--base") { Description = "Inspect current eligible C# changes relative to the merge base with this Git ref." };
        var reviewAll = new Option<bool>("--review-all") { Description = "Explicitly request a bounded full-repository semantic review." };
        var check = new Command("check", "Analyze selected C# source files and optionally inspect semantic review work."); check.Arguments.Add(paths); check.Options.Add(changed); check.Options.Add(baseRef); check.Options.Add(reviewAll); check.Options.Add(output);
        check.SetAction(parse => Run(() => RenderCheck(new HygieneEngine(), parse.GetValue(paths) ?? [], parse.GetValue(changed), parse.GetValue(output) ?? "text", parse.GetValue(baseRef), parse.GetValue(reviewAll)))); root.Subcommands.Add(check);
        var explainHandle = new Argument<string>("finding"); var explain = new Command("explain", "Explain a finding from the latest run."); explain.Arguments.Add(explainHandle); explain.Options.Add(output);
        explain.SetAction(parse => Run(() => RenderFinding(new HygieneEngine().Explain(parse.GetValue(explainHandle)!), parse.GetValue(output) ?? "text"))); root.Subcommands.Add(explain);
        var ignoreHandle = new Argument<string>("finding"); var reason = new Option<string>("--reason") { DefaultValueFactory = _ => "" }; var ignore = new Command("ignore", "Persist a reviewed finding exception."); ignore.Arguments.Add(ignoreHandle); ignore.Options.Add(reason);
        ignore.SetAction(parse => Run(() => { var d = new HygieneEngine().Ignore(parse.GetValue(ignoreHandle)!, parse.GetValue(reason) ?? ""); Console.WriteLine($"Ignored {d.Id} ({d.RuleId})"); return 0; })); root.Subcommands.Add(ignore);
        var unignoreId = new Argument<string>("ignore-id"); var unignore = new Command("unignore", "Remove a persisted ignore decision."); unignore.Arguments.Add(unignoreId);
        unignore.SetAction(parse => Run(() => { new HygieneEngine().Unignore(parse.GetValue(unignoreId)!); Console.WriteLine($"Removed {parse.GetValue(unignoreId)}"); return 0; })); root.Subcommands.Add(unignore);
        var ignorePaths = new Argument<string[]>("paths") { Arity = ArgumentArity.ZeroOrMore }; var ignores = new Command("ignores", "List persisted ignore decisions."); ignores.Arguments.Add(ignorePaths); ignores.Options.Add(output);
        ignores.SetAction(parse => Run(() => RenderIgnores(new HygieneEngine().ListIgnores(parse.GetValue(ignorePaths) ?? []), parse.GetValue(output) ?? "text"))); root.Subcommands.Add(ignores);
        var rules = new Command("rules", "List and configure hygiene rules."); rules.Options.Add(output);
        rules.SetAction(parse => Run(() => RenderRules(new HygieneEngine(), parse.GetValue(output) ?? "text")));
        var enableId = new Argument<string>("rule-id"); var enable = new Command("enable", "Enable a rule."); enable.Arguments.Add(enableId); enable.SetAction(parse => Run(() => { new HygieneEngine().SetRule(parse.GetValue(enableId)!, true); return 0; })); rules.Subcommands.Add(enable);
        var disableId = new Argument<string>("rule-id"); var disable = new Command("disable", "Disable a rule."); disable.Arguments.Add(disableId); disable.SetAction(parse => Run(() => { new HygieneEngine().SetRule(parse.GetValue(disableId)!, false); return 0; })); rules.Subcommands.Add(disable);
        root.Subcommands.Add(rules);
        var review = new Command("review", "Accept sampled review work or expand/create a durable handoff.");
        var acceptHandle = new Argument<string>("batch-handle"); var acceptItems = new Argument<string[]>("item-id") { Arity = ArgumentArity.ZeroOrMore };
        var acceptAll = new Option<bool>("--all") { Description = "Accept every item in the current normal sample." };
        var accept = new Command("accept", "Record explicit acceptance of current sampled review items.");
        accept.Arguments.Add(acceptHandle); accept.Arguments.Add(acceptItems); accept.Options.Add(acceptAll); accept.Options.Add(output);
        accept.SetAction(parse => Run(() =>
        {
            string[] itemIds = parse.GetValue(acceptItems) ?? [];
            bool all = parse.GetValue(acceptAll);
            if (all && itemIds.Length > 0) { throw new ArgumentException("Use either item IDs or --all, not both."); }
            return RenderAcceptance(new HygieneEngine().AcceptReview(parse.GetValue(acceptHandle)!, itemIds, all), parse.GetValue(output) ?? "text");
        }));
        var expandHandle = new Argument<string>("batch-handle"); var expand = new Command("expand", "Expand the latest run's complete eligible review population.");
        expand.Arguments.Add(expandHandle); expand.Options.Add(output);
        expand.SetAction(parse => Run(() => RenderReview(new HygieneEngine().ExpandReview(parse.GetValue(expandHandle)!), parse.GetValue(output) ?? "text")));
        var handoffHandle = new Argument<string>("batch-handle"); var handoffFile = new Option<string?>("--file");
        var handoff = new Command("handoff", "Create a durable, transport-neutral frontier-review request.");
        handoff.Arguments.Add(handoffHandle); handoff.Options.Add(handoffFile); handoff.Options.Add(output);
        handoff.SetAction(parse => Run(() =>
        {
            string path = new HygieneEngine().CreateReviewHandoff(parse.GetValue(handoffHandle)!, parse.GetValue(handoffFile));
            SemanticReviewRequest request = JsonSerializer.Deserialize<SemanticReviewRequest>(File.ReadAllText(path), Json)
                ?? throw new ProductException("Review handoff could not be read back for its completion report.");
            var report = new { schemaVersion = 1, status = "escalation-required", lifecycle = "review-handoff", handoffPath = Path.GetRelativePath(new HygieneEngine().Root, path).Replace('\\', '/'), reviewerClass = request.ReviewerClass, rule = request.Rule.Id, batchHandle = request.Source.BatchHandle, itemCount = request.Items.Count, action = "The selected review work has been handed off for the designated reviewer judgment; keep it outstanding until the decision returns and rerun dotnet hygiene." };
            string handoffOutput = parse.GetValue(output) ?? "text";
            if (handoffOutput == "json") { Console.WriteLine(JsonSerializer.Serialize(report, Json)); }
            else if (handoffOutput == "text") { Console.WriteLine("Status: escalation-required"); Console.WriteLine(report.action); Console.WriteLine($"Review handoff written: {path}"); }
            else { throw new ArgumentException("--output must be text or json."); }
            return 0;
        }));
        review.Subcommands.Add(accept); review.Subcommands.Add(expand); review.Subcommands.Add(handoff); root.Subcommands.Add(review);
        return root;
    }
    internal static int Run(Func<int> action)
    {
        try { return action(); }
        catch (ArgumentException e) { Console.Error.WriteLine(e.Message); return 2; }
        catch (ProductException e) { Console.Error.WriteLine(e.Message); return 3; }
        catch (EnvironmentException e) { Console.Error.WriteLine(e.Message); return 4; }
        catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }
    private sealed record FormatObservation(string Project, string Status, string Output, IReadOnlyList<string> ChangedPaths);
    private sealed record WorkOrderItem(string RuleId, int RuleVersion, string BatchHandle, string ItemId, int Line, string Symbol, string Summary, string Declaration, IReadOnlyList<ReviewQuestion> Questions, ReviewEscalation Escalation);
    private sealed record WorkOrderFile(string Path, string CurrentContentSha256, string CurrentContent, IReadOnlyList<WorkOrderItem> Items);
    private sealed record AutonomousUpdateResult(int ExitCode, object? RevalidatedLifecycle, string Message, bool Handoff = false);
    private static object? releaseUpdateState;
    private static string ToolVersion => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";

    private static AutonomousUpdateResult TryUpdateInstalledTool()
    {
        const string packageId = "DotNetAiCodeHygiene.Tool";
        string root = new HygieneEngine().Root;
        string currentVersion = ToolVersion;
        string tempRoot = Path.Combine(Path.GetTempPath(), "dotnet-hygiene-update-" + Guid.NewGuid().ToString("N"));
        string stage = Path.Combine(tempRoot, "stage");
        string cache = Path.Combine(tempRoot, "packages");
        string cliHome = Path.Combine(tempRoot, "cli-home");
        bool handoffStarted = false;
        try
        {
            Directory.CreateDirectory(stage);
            var isolatedEnvironment = new Dictionary<string, string?> { ["NUGET_PACKAGES"] = cache, ["DOTNET_CLI_HOME"] = cliHome };
            (int installCode, string installOut, string installErr) = RunProcess("dotnet", root, isolatedEnvironment, "tool", "install", packageId, "--tool-path", stage, "--verbosity", "quiet");
            if (installCode != 0)
            {
                releaseUpdateState = new { status = "unavailable", currentVersion, detail = JoinOutput(installOut, installErr), action = "The configured NuGet sources did not provide a stable package for release discovery. Continue with this compatible installation or use the documented dotnet tool update command when online." };
                return new(0, null, "Release discovery was unavailable; continuing with the compatible installed tool.");
            }
            (int listCode, string installedList, string listError) = RunProcess("dotnet", root, isolatedEnvironment, "tool", "list", "--tool-path", stage, "--format", "json");
            string? stagedVersion = null;
            if (listCode == 0)
            {
                using JsonDocument list = JsonDocument.Parse(installedList);
                if (list.RootElement.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Array)
                {
                    stagedVersion = data.EnumerateArray().Where(item => item.TryGetProperty("packageId", out JsonElement id) && string.Equals(id.GetString(), packageId, StringComparison.OrdinalIgnoreCase))
                        .Select(item => item.GetProperty("version").GetString()).FirstOrDefault();
                }
            }
            if (!Version.TryParse(stagedVersion, out Version? candidate) || !Version.TryParse(currentVersion, out Version? current))
            {
                releaseUpdateState = new { status = "incompatible", currentVersion, detail = JoinOutput(installedList, listError, installOut, installErr), action = "Resolve the installed or staged package version before an automatic update can be applied." };
                return new(0, null, "Release discovery returned an invalid package version; continuing with the current installation.");
            }
            if (candidate <= current)
            {
                releaseUpdateState = new { status = candidate < current ? "newer-than-configured-stable" : "current", currentVersion, latestStableVersion = candidate.ToString(), feedPolicy = "configured NuGet sources; stable packages only", action = candidate < current ? "This installation is newer than the latest stable package visible in configured sources; no downgrade was attempted." : null };
                return new(0, null, "The installed tool is current.");
            }

            string? manifest = FindLocalToolManifest(root, packageId);
            string stagedShim = Path.Combine(stage, OperatingSystem.IsWindows() ? "dotnet-hygiene.exe" : "dotnet-hygiene");
            string? shim = FindOnPath(stagedShim);
            string? installPath = null;
            if (manifest is not null)
            {
                installPath = null;
            }
            else if (shim is not null)
            {
                installPath = Path.GetDirectoryName(shim)!;
            }
            else
            {
                releaseUpdateState = new { status = "update-available", currentVersion, latestStableVersion = candidate.ToString(), action = "A stable update is available, but the installation scope could not be identified safely. Run dotnet tool update DotNetAiCodeHygiene.Tool using the same scope and NuGet configuration used for installation, then restart and rerun dotnet hygiene." };
                return new(0, null, "A release is available, but the tool could not identify its install scope; follow the update instruction.");
            }

            string requestPath = Path.Combine(tempRoot, "update-request.json");
            string helperPath = Path.Combine(tempRoot, "update-after-exit.ps1");
            var request = new { root, tempRoot, packageId, version = candidate.ToString(), manifest, installPath, shim, parentProcessId = Environment.ProcessId, previousVersion = current.ToString() };
            File.WriteAllText(requestPath, JsonSerializer.Serialize(request, Json), new System.Text.UTF8Encoding(false));
            File.WriteAllText(helperPath, UpdateHelperScript, new System.Text.UTF8Encoding(false));
            var helper = new ProcessStartInfo("powershell.exe") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true };
            helper.ArgumentList.Add("-NoProfile"); helper.ArgumentList.Add("-NonInteractive"); helper.ArgumentList.Add("-File"); helper.ArgumentList.Add(helperPath); helper.ArgumentList.Add(requestPath);
            using (Process? background = Process.Start(helper))
            {
                if (background is null) { throw new EnvironmentException("Could not start the out-of-process release update helper."); }
            }
            handoffStarted = true;
            return new(0, null, "A stable release is available. The updater will wait for this process to exit, update the tool, restart it, and revalidate the lifecycle.", Handoff: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or EnvironmentException or InvalidOperationException or JsonException or FormatException)
        {
            releaseUpdateState = new { status = "unavailable", currentVersion, detail = error.Message, action = "Continue with the compatible installed tool and retry release discovery when the configured NuGet sources are available." };
            return new(0, null, "Release discovery was unavailable; continuing with the installed tool.");
        }
        finally
        {
            if (!handoffStarted) { try { if (Directory.Exists(tempRoot)) { Directory.Delete(tempRoot, true); } } catch (IOException) { } }
        }
    }

    private static string JoinOutput(params string[] values) => string.Join(Environment.NewLine, values.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();

    private const string UpdateHelperScript = @"
param([Parameter(Mandatory=$true)][string]$RequestPath)
$ErrorActionPreference = 'Stop'
$request = Get-Content -LiteralPath $RequestPath -Raw | ConvertFrom-Json
while (Get-Process -Id $request.parentProcessId -ErrorAction SilentlyContinue) { Start-Sleep -Milliseconds 100 }
$updateArgs = @('tool', 'update', $request.packageId)
if ($request.manifest) { $updateArgs += @('--local', '--tool-manifest', $request.manifest) }
else { $updateArgs += @('--tool-path', $request.installPath) }
$updateArgs += @('--version', $request.version, '--verbosity', 'quiet')
$updateOutput = (& dotnet @updateArgs 2>&1 | Out-String).Trim()
$updateCode = $LASTEXITCODE
$updateInfo = @{ status = 'update-failed'; previousVersion = $request.previousVersion; latestStableVersion = $request.version; detail = $updateOutput; action = 'Resolve the configured feed or installation-scope issue, then rerun dotnet hygiene.' }
if ($updateCode -eq 0) {
  $env:DOTNET_HYGIENE_SKIP_UPDATE = '1'
  if ($request.manifest) { $childOutput = (& dotnet hygiene --output json 2>&1 | Out-String).Trim() }
  else { $childOutput = (& $request.shim --output json 2>&1 | Out-String).Trim() }
  $childCode = $LASTEXITCODE
  try {
    $result = $childOutput | ConvertFrom-Json
    $updateInfo = @{ status = $(if ($childCode -eq 0) { 'updated-and-revalidated' } else { 'revalidation-failed' }); previousVersion = $request.previousVersion; installedVersion = $request.version; revalidatedStatus = $result.status; updateOutput = $updateOutput }
    $result | Add-Member -NotePropertyName releaseUpdate -NotePropertyValue $updateInfo -Force
    $result | ConvertTo-Json -Depth 64 -Compress
  } catch {
    @{ schemaVersion = 1; status = 'incomplete'; lifecycle = 'release-update'; action = 'The installed tool restarted but returned invalid lifecycle output; inspect the installation and rerun dotnet hygiene.'; releaseUpdate = @{ status = 'revalidation-failed'; previousVersion = $request.previousVersion; installedVersion = $request.version; detail = $childOutput; updateOutput = $updateOutput } } | ConvertTo-Json -Depth 32 -Compress
  }
} else {
  @{ schemaVersion = 1; status = 'incomplete'; lifecycle = 'release-update'; action = 'The update did not complete. Resolve the configured NuGet source or installation state, then rerun dotnet hygiene.'; releaseUpdate = $updateInfo } | ConvertTo-Json -Depth 32 -Compress
}
Remove-Item -LiteralPath $request.tempRoot -Recurse -Force -ErrorAction SilentlyContinue
";

    private static string? FindOnPath(string expectedShim)
    {
        string shimName = Path.GetFileName(expectedShim);
        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(directory.Trim('"'), shimName);
            if (File.Exists(candidate)) { return Path.GetFullPath(candidate); }
        }
        return null;
    }

    private static string? FindLocalToolManifest(string root, string packageId)
    {
        string path = Path.Combine(root, ".config", "dotnet-tools.json");
        if (!File.Exists(path)) { return null; }
        try
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.GetProperty("tools").TryGetProperty(packageId, out _) ? path : null;
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException) { return null; }
    }

    private sealed class AutonomousMutationTransaction : IDisposable
    {
        private readonly string root;
        private readonly Dictionary<string, byte[]?> original;
        private Dictionary<string, byte[]?> expected;
        private bool rolledBack;

        public AutonomousMutationTransaction(string root)
        {
            this.root = root;
            original = Capture();
            expected = Capture();
        }

        public void Checkpoint() => expected = Capture();

        public IReadOnlyList<string> Rollback()
        {
            if (rolledBack) { return []; }
            rolledBack = true;
            var conflicts = new List<string>();
            foreach (string path in original.Keys.Union(expected.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
            {
                byte[]? originalBytes = original.GetValueOrDefault(path);
                byte[]? expectedBytes = expected.GetValueOrDefault(path);
                if (Equal(originalBytes, expectedBytes)) { continue; }
                try
                {
                    byte[]? current = File.Exists(path) ? File.ReadAllBytes(path) : null;
                    if (!Equal(current, expectedBytes)) { conflicts.Add(Path.GetRelativePath(root, path).Replace('\\', '/')); continue; }
                    if (originalBytes is null) { if (File.Exists(path)) { File.Delete(path); } }
                    else { ReplaceAtomically(path, originalBytes); }
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                { conflicts.Add(Path.GetRelativePath(root, path).Replace('\\', '/')); }
            }
            return conflicts;
        }

        public void Dispose() { }

        private Dictionary<string, byte[]?> Capture()
        {
            var paths = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part is "bin" or "obj" or ".git"))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (string relative in new[] { ".editorconfig", "Directory.Build.props", ".hygiene/profile.json", ".hygiene/profile/Hygiene.props", ".hygiene/.state/baseline-approval.json" })
            { paths.Add(Path.Combine(root, relative)); }
            string reports = Path.Combine(root, ".hygiene", "reports");
            if (Directory.Exists(reports)) { paths.UnionWith(Directory.EnumerateFiles(reports, "*", SearchOption.AllDirectories)); }
            return paths.ToDictionary(path => path, path => File.Exists(path) ? File.ReadAllBytes(path) : null, StringComparer.OrdinalIgnoreCase);
        }

        private static bool Equal(byte[]? left, byte[]? right) => left is null ? right is null : right is not null && left.AsSpan().SequenceEqual(right);

        private static void ReplaceAtomically(string path, byte[] content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".rollback";
            File.WriteAllBytes(temp, content);
            if (File.Exists(path)) { File.Replace(temp, path, null); } else { File.Move(temp, path); }
        }
    }

    private static int RunAutonomous(string output)
    {
        if (output is not ("text" or "json")) { throw new ArgumentException("--output must be text or json."); }
        if (Environment.GetEnvironmentVariable("DOTNET_HYGIENE_SKIP_UPDATE") != "1")
        {
            AutonomousUpdateResult update = TryUpdateInstalledTool();
            if (update.Handoff) { return update.ExitCode; }
            if (update.RevalidatedLifecycle is not null) { WriteLifecycle(update.RevalidatedLifecycle, output, update.Message); return update.ExitCode; }
        }
        return RunAutonomousLifecycle(output);
    }

    private static int RunAutonomousLifecycle(string output)
    {
        var engine = new HygieneEngine();
        using var transaction = new AutonomousMutationTransaction(engine.Root);
        try { return RunAutonomousLifecycleCore(output, engine, transaction); }
        catch (Exception error)
        {
            IReadOnlyList<string> conflicts = transaction.Rollback();
            string recovery = conflicts.Count == 0 ? "Repository changes made by this run were rolled back." : $"Rollback preserved concurrent or conflicting edits at: {string.Join(", ", conflicts)}.";
            throw new ProductException($"Autonomous hygiene stopped before completion. {recovery} Original error: {error.Message}");
        }
    }

    private static int RunAutonomousLifecycleCore(string output, HygieneEngine engine, AutonomousMutationTransaction transaction)
    {
        string marker = Path.Combine(engine.Root, ".hygiene", "profile.json");
        if (!File.Exists(marker))
        {
            FormatObservation[] preFormat = AssessExistingFormat(engine.Root);
            if (preFormat.Any(item => item.Status == "assessment-unavailable"))
            {
                WriteLifecycle(new { schemaVersion = 1, status = "incomplete", lifecycle = "bootstrap", action = "Restore SDK project dependencies or provide the formatter required for a nonmutating pre-hygiene assessment, then rerun dotnet hygiene.", preHygieneFormatting = preFormat }, output, "Bootstrap stopped before changing repository policy because the original formatting assessment was unavailable.");
                return 0;
            }
            ProfileResult profile = engine.Bootstrap(output);
            transaction.Checkpoint();
            FormatObservation[] restrictedStyle = ApplyRestrictedFormatting(engine.Root, engine);
            transaction.Checkpoint();
            if (restrictedStyle.Any(item => item.Status == "failed"))
            {
                IReadOnlyList<string> conflicts = transaction.Rollback();
                var failed = new { schemaVersion = 1, status = "incomplete", lifecycle = "bootstrap", action = "Resolve the formatter failure and rerun dotnet hygiene; baseline changes were rolled back.", rolledBack = conflicts.Count == 0, rollbackConflicts = conflicts, preHygieneFormatting = preFormat, failedFormatterSteps = restrictedStyle };
                WriteLifecycle(failed, output, "Bootstrap failed during restricted formatting. No baseline was accepted or installed as complete.");
                return 0;
            }
            RewriteResult normalized = new RewriteEngine().Rewrite("normalize", [], false, false);
            transaction.Checkpoint();
            RewriteResult formatted = new RewriteEngine().Rewrite("format", [], false, false);
            transaction.Checkpoint();
            string baselineStatus = restrictedStyle.Any(item => item.Status == "failed") ? "incomplete" : "approval-required";
            string reportPath = SaveBaselineReport(engine.Root, preFormat, profile, restrictedStyle, normalized, formatted, baselineStatus);
            transaction.Checkpoint();
            var report = new
            {
                schemaVersion = 1,
                status = baselineStatus,
                lifecycle = "bootstrap",
                toolVersion = ToolVersion,
                profile = new { id = "dotnet-11", version = 2 },
                changedPaths = profile.ChangedPaths.Concat(restrictedStyle.SelectMany(item => item.ChangedPaths)).Concat(normalized.ChangedPaths).Concat(formatted.ChangedPaths).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
                preHygieneFormatting = preFormat,
                postHygieneFormatting = new { policy = "dotnet-format whitespace and explicit IDE0011/IDE0040 style allowlist", style = restrictedStyle, selectedRuleIds = normalized.SelectedRuleIds.Concat(formatted.SelectedRuleIds), changedPaths = normalized.ChangedPaths.Concat(formatted.ChangedPaths).Distinct(StringComparer.Ordinal), idempotenceCheck = "available by rerunning dotnet hygiene" },
                remainingProfileFindings = profile.Findings,
                reportPath = Path.GetRelativePath(engine.Root, reportPath).Replace('\\', '/')
            };
            WriteLifecycle(report, output, "Baseline prepared; an authorized external approver must review and explicitly accept it before this repository is treated as initialized.");
            return 0;
        }

        string approvalStatePath = Path.Combine(engine.Root, ".hygiene", ".state", "baseline-approval.json");
        if (File.Exists(approvalStatePath))
        {
            using JsonDocument approvalState = JsonDocument.Parse(File.ReadAllText(approvalStatePath));
            if (approvalState.RootElement.GetProperty("status").GetString() != "accepted")
            {
                string pendingStatus = approvalState.RootElement.GetProperty("status").GetString() ?? "incomplete";
                var approvalRequired = new { schemaVersion = 1, status = pendingStatus, lifecycle = "baseline", action = pendingStatus == "incomplete" ? "Resolve failed baseline validation, then rerun dotnet hygiene." : "Review the baseline report, then record the authorized external decision with dotnet hygiene baseline accept --approver <identity>.", reportPath = approvalState.RootElement.GetProperty("reportPath").GetString() };
                WriteLifecycle(approvalRequired, output, pendingStatus == "incomplete" ? "Baseline validation is incomplete." : "The repository baseline still requires explicit authorized external approval.");
                return 0;
            }
        }
        FormatObservation[] priorFormatting = AssessExistingFormat(engine.Root);
        if (priorFormatting.Any(item => item.Status == "assessment-unavailable"))
        {
            WriteLifecycle(new { schemaVersion = 1, status = "incomplete", lifecycle = "profile-update", action = "Restore SDK project dependencies or provide the formatter required for a nonmutating assessment, then rerun dotnet hygiene.", preHygieneFormatting = priorFormatting }, output, "Profile reconciliation stopped because the formatting assessment was unavailable.");
            return 0;
        }
        ProfileResult reconciliation;
        try { reconciliation = engine.UpdateProfile(); }
        catch (ProductException error)
        {
            var incompatible = new { schemaVersion = 1, status = "incomplete", lifecycle = "profile-update", action = "Resolve the unsupported or malformed hygiene profile without discarding repository data, then rerun dotnet hygiene.", compatibilityError = error.Message };
            WriteLifecycle(incompatible, output, "The installed tool cannot safely reconcile this profile state.");
            return 0;
        }
        transaction.Checkpoint();
        if (reconciliation.ChangedPaths.Count > 0)
        {
            FormatObservation[] restrictedStyle = ApplyRestrictedFormatting(engine.Root, engine);
            transaction.Checkpoint();
            if (restrictedStyle.Any(item => item.Status == "failed"))
            {
                IReadOnlyList<string> conflicts = transaction.Rollback();
                var failed = new { schemaVersion = 1, status = "incomplete", lifecycle = "profile-update", action = "Resolve the formatter failure and rerun dotnet hygiene; profile migration and source changes were rolled back.", rolledBack = conflicts.Count == 0, rollbackConflicts = conflicts, failedFormatterSteps = restrictedStyle };
                WriteLifecycle(failed, output, "Profile update failed during restricted formatting. No new baseline was accepted.");
                return 0;
            }
            RewriteResult normalized = new RewriteEngine().Rewrite("normalize", [], false, false);
            transaction.Checkpoint();
            RewriteResult formatted = new RewriteEngine().Rewrite("format", [], false, false);
            transaction.Checkpoint();
            string updateStatus = restrictedStyle.Any(item => item.Status == "failed") ? "incomplete" : "approval-required";
            string reportPath = SaveBaselineReport(engine.Root, priorFormatting, reconciliation, restrictedStyle, normalized, formatted, updateStatus);
            transaction.Checkpoint();
            var updateReport = new { schemaVersion = 1, status = updateStatus, lifecycle = "profile-update", toolVersion = ToolVersion, changedPaths = reconciliation.ChangedPaths.Concat(restrictedStyle.SelectMany(item => item.ChangedPaths)).Concat(normalized.ChangedPaths).Concat(formatted.ChangedPaths).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal), preHygieneFormatting = priorFormatting, restrictedStyle, reportPath = Path.GetRelativePath(engine.Root, reportPath).Replace('\\', '/') };
            WriteLifecycle(updateReport, output, updateStatus == "incomplete" ? "Profile update validation is incomplete." : "Profile policy changed. Review the new baseline and obtain explicit authorized external approval before proceeding.");
            return 0;
        }
        engine.RequireProfile();
        FormatObservation[] restrictedStyleGate = ApplyRestrictedFormatting(engine.Root, engine);
        transaction.Checkpoint();
        if (restrictedStyleGate.Any(item => item.Status == "failed"))
        {
            IReadOnlyList<string> conflicts = transaction.Rollback();
            var failed = new { schemaVersion = 1, status = "incomplete", lifecycle = "gate", action = "Resolve the formatter failure and rerun dotnet hygiene; all changes from this gate were rolled back.", rolledBack = conflicts.Count == 0, rollbackConflicts = conflicts, failedFormatterSteps = restrictedStyleGate };
            WriteLifecycle(failed, output, "Gate failed during restricted formatting. The gate is incomplete.");
            return 0;
        }
        RewriteResult normalizedGate = new RewriteEngine().Rewrite("normalize", [], false, false);
        transaction.Checkpoint();
        RewriteResult formattedGate = new RewriteEngine().Rewrite("format", [], false, false);
        transaction.Checkpoint();
        string[] changedPaths = normalizedGate.ChangedPaths.Concat(formattedGate.ChangedPaths).Distinct(StringComparer.Ordinal).ToArray();
        string[] changedSources = GitLines(engine.Root, "status", "--porcelain", "--", "*.cs");
        string branch = GitLine(engine.Root, "branch", "--show-current");
        string defaultBase = DetectLocalDefaultBase(engine.Root);
        string defaultBranch = defaultBase.StartsWith("origin/", StringComparison.Ordinal) ? defaultBase["origin/".Length..] : "";
        CheckResult check;
        string gate;
        if (!string.IsNullOrWhiteSpace(branch) && branch != defaultBranch && defaultBase.Length > 0 && GitExit(engine.Root, "merge-base", defaultBase, "HEAD") == 0)
        {
            gate = "base:" + defaultBase;
            check = engine.Check([], false, baseRef: defaultBase);
        }
        else if (changedSources.Length > 0)
        {
            gate = "worktree";
            check = engine.Check([], true);
        }
        else if (branch == defaultBranch || branch is "main" or "master")
        {
            gate = "closed";
            check = engine.Check([], false);
        }
        else
        {
            var incomplete = new { schemaVersion = 1, status = "incomplete", lifecycle = "gate", action = "Identify a comparison base for this feature branch (for example, dotnet hygiene check --base <trusted-ref>); no remote or branch was inferred.", gate = (string?)null };
            WriteLifecycle(incomplete, output, "Cannot determine a safe source-change gate. Identify the intended comparison base or create a committed worktree baseline.");
            return 0;
        }

        WorkOrderFile[] workOrder = GroupWorkOrder(engine.Root, check.ReviewBatches);
        bool hasSemanticWork = check.ReviewBatches.Any(batch => batch.SampleCount > 0);
        string status = restrictedStyleGate.Any(item => item.Status == "failed") || check.Findings.Count != 0 || hasSemanticWork ? "incomplete" : "complete";
        var gateReport = new
        {
            schemaVersion = 1,
            status,
            lifecycle = "gate",
            gate,
            runId = check.RunId,
            changedPaths,
            restrictedStyle = restrictedStyleGate,
            findings = check.Findings.Select(PublicFinding),
            workOrder,
            batches = check.ReviewBatches.Select(PublicBatch),
            completion = new
            {
                commandExecution = "succeeded",
                semanticWork = hasSemanticWork ? "selected work requires agent inspection and explicit per-rule acceptance" : "no selected semantic work",
                deterministicFindings = check.Findings.Count == 0 ? "none" : "resolve findings and rerun dotnet hygiene"
            }
        };
        WriteLifecycle(gateReport, output, status == "complete" ? $"Gate {gate} is complete." : $"Gate {gate} has work to resolve. Follow only the selected work order, then rerun dotnet hygiene.");
        return 0;
    }

    private static int AcceptBaseline(string approver, string output)
    {
        if (string.IsNullOrWhiteSpace(approver)) { throw new ArgumentException("--approver must identify the authorized external approver."); }
        var engine = new HygieneEngine();
        engine.RequireProfile();
        string statePath = Path.Combine(engine.Root, ".hygiene", ".state", "baseline-approval.json");
        if (!File.Exists(statePath)) { throw new ProductException("No pending autonomous baseline approval was found."); }
        using JsonDocument current = JsonDocument.Parse(File.ReadAllText(statePath));
        if (current.RootElement.GetProperty("status").GetString() == "accepted") { throw new ProductException("The baseline has already been accepted."); }
        if (current.RootElement.GetProperty("status").GetString() != "approval-required") { throw new ProductException("The baseline has failed validation and cannot be accepted."); }
        var state = new { schemaVersion = 1, status = "accepted", acceptedAtUtc = DateTimeOffset.UtcNow, approver, reportPath = current.RootElement.GetProperty("reportPath").GetString(), authorization = "caller assertion that the named approver was authorized and approved the inspected baseline" };
        string temp = statePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(state, Json), new System.Text.UTF8Encoding(false));
        File.Move(temp, statePath, true);
        var response = new { schemaVersion = 1, status = "accepted", approver, reportPath = state.reportPath };
        WriteLifecycle(response, output, $"Baseline acceptance recorded for {approver}.");
        return 0;
    }

    private static WorkOrderFile[] GroupWorkOrder(string root, IReadOnlyList<ReviewBatch> batches)
    {
        return batches.SelectMany(batch => batch.Items.Select(item => (batch, item)))
            .GroupBy(pair => pair.item.Path, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                string content = File.ReadAllText(Path.Combine(root, group.Key.Replace('/', Path.DirectorySeparatorChar)));
                string[] checkedContexts = group.SelectMany(pair => pair.batch.SourceContents ?? [])
                    .Where(source => source.Path == group.Key).Select(source => source.Content).Distinct(StringComparer.Ordinal).ToArray();
                if (checkedContexts.Length != 1 || checkedContexts[0] != content)
                {
                    throw new ProductException($"Source context changed while preparing the selected work order for '{group.Key}'; rerun dotnet hygiene.");
                }
                string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content)));
                WorkOrderItem[] items = group.Select(pair => new WorkOrderItem(
                    pair.batch.RuleId, pair.batch.RuleVersion, pair.batch.Handle, pair.item.Id, pair.item.Line, pair.item.Symbol,
                    pair.item.Summary, pair.item.Declaration, pair.batch.Questions, pair.batch.Escalation))
                    .OrderBy(item => item.RuleId, StringComparer.Ordinal).ThenBy(item => item.ItemId, StringComparer.Ordinal).ToArray();
                return new WorkOrderFile(group.Key, hash, content, items);
            })
            .ToArray();
    }

    private static FormatObservation[] AssessExistingFormat(string root)
    {
        string[] projects = EnumerateProjects(root);
        if (projects.Length == 0) { return []; }
        string formatter = ResolveDotnetFormat(root);
        return projects.Select(project =>
        {
            (int exitCode, string stdout, string stderr) = RunProcess("dotnet", root, formatter, project, "whitespace", "--verify-no-changes", "--no-restore");
            string status = exitCode == 0 ? "clean" : exitCode == 2 ? "drift" : "assessment-unavailable";
            string details = string.Join(Environment.NewLine, new[] { stdout, stderr }.Where(value => !string.IsNullOrWhiteSpace(value)));
            return new FormatObservation(Path.GetRelativePath(root, project).Replace('\\', '/'), status, details.Trim(), []);
        }).ToArray();
    }

    private static FormatObservation[] ApplyRestrictedFormatting(string root, HygieneEngine engine)
    {
        string[] diagnostics = engine.ListRules().Where(entry => entry.Enabled).Select(entry => entry.Rule.Id switch
        {
            "style.braces.required" => "IDE0011",
            "style.accessibility.explicit" => "IDE0040",
            _ => ""
        }).Where(id => id.Length > 0).ToArray();
        string[] projects = EnumerateProjects(root);
        if (projects.Length == 0) { return []; }
        string formatter = ResolveDotnetFormat(root);
        Dictionary<string, string> original = EnumerateSources(root).ToDictionary(path => path, File.ReadAllText, StringComparer.OrdinalIgnoreCase);
        var observations = new List<FormatObservation>();
        foreach (string project in projects)
        {
            try
            {
                (int whitespaceCode, string whitespaceOut, string whitespaceErr) = RunProcess("dotnet", root, formatter, project, "whitespace", "--no-restore");
                string styleOutput = "";
                string styleStatus = "not-enabled";
                if (diagnostics.Length > 0)
                {
                    string[] args = [formatter, "style", project, "--diagnostics", .. diagnostics, "--no-restore"];
                    (int styleCode, string styleOut, string styleErr) = RunProcess("dotnet", root, args);
                    styleOutput = string.Join(Environment.NewLine, new[] { styleOut, styleErr }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
                    styleStatus = styleCode == 0 ? "applied" : "failed";
                }
                string output = string.Join(Environment.NewLine, new[] { whitespaceOut, whitespaceErr, styleOutput }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
                string projectDirectory = Path.GetDirectoryName(project)!;
                string[] changed = original.Where(pair => pair.Key.StartsWith(projectDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                        && File.Exists(pair.Key) && File.ReadAllText(pair.Key) != pair.Value)
                    .Select(pair => Path.GetRelativePath(root, pair.Key).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();
                observations.Add(new FormatObservation(Path.GetRelativePath(root, project).Replace('\\', '/'), whitespaceCode == 0 && styleStatus != "failed" ? "applied" : "failed", $"whitespace=exit:{whitespaceCode}; style={styleStatus}; {output}".Trim(), changed));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or EnvironmentException)
            {
                observations.Add(new FormatObservation(Path.GetRelativePath(root, project).Replace('\\', '/'), "failed", error.Message, []));
            }
        }
        return observations.ToArray();
    }

    private static string[] EnumerateProjects(string root) => Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
        .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part is "bin" or "obj" or ".git"))
        .Order(StringComparer.OrdinalIgnoreCase).ToArray();

    private static string[] EnumerateSources(string root) => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
        .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part is "bin" or "obj" or ".git"))
        .Order(StringComparer.OrdinalIgnoreCase).ToArray();

    private static string ResolveDotnetFormat(string root)
    {
        (int exit, string info, string error) = RunProcess("dotnet", root, "--info");
        if (exit != 0) { throw new EnvironmentException("Could not identify the active .NET SDK for dotnet-format."); }
        string? basePath = info.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim()).Where(line => line.StartsWith("Base Path:", StringComparison.OrdinalIgnoreCase))
            .Select(line => line["Base Path:".Length..].Trim()).FirstOrDefault();
        string? formatter = basePath is null ? null : Path.GetFullPath(Path.Combine(basePath, "DotnetTools", "dotnet-format", "dotnet-format.dll"));
        if (formatter is null || !File.Exists(formatter)) { throw new EnvironmentException($"The active .NET SDK does not provide dotnet-format. {error}".Trim()); }
        return formatter;
    }

    private static string SaveBaselineReport(string root, FormatObservation[] preFormat, ProfileResult profile, FormatObservation[] restrictedStyle, RewriteResult normalized, RewriteResult formatted, string status)
    {
        string directory = Path.Combine(root, ".hygiene", "reports");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "baseline-" + DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + ".json");
        var report = new
        {
            schemaVersion = 1,
            status,
            createdAtUtc = DateTimeOffset.UtcNow,
            tool = new { package = "DotNetAiCodeHygiene.Tool", version = ToolVersion },
            profile = new { id = "dotnet-11", version = 2 },
            ruleVersions = HygieneEngine.Rules.Select(rule => new { rule.Id, rule.Version }),
            changedPaths = profile.ChangedPaths.Concat(restrictedStyle.SelectMany(item => item.ChangedPaths)).Concat(normalized.ChangedPaths).Concat(formatted.ChangedPaths).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
            preHygieneFormatting = preFormat,
            postHygieneFormatting = new { policy = "dotnet-format whitespace and allowlisted style diagnostics", style = restrictedStyle, selectedRuleIds = normalized.SelectedRuleIds.Concat(formatted.SelectedRuleIds), changedPaths = restrictedStyle.SelectMany(item => item.ChangedPaths).Concat(normalized.ChangedPaths).Concat(formatted.ChangedPaths).Distinct(StringComparer.Ordinal) },
            remainingProfileFindings = profile.Findings,
            validation = new { profileFindingCount = profile.FindingCount, normalizedChangedCount = normalized.ChangedCount, formatterChangedCount = formatted.ChangedCount },
            approval = new { required = true, accepted = false, instruction = status == "incomplete" ? "Resolve all validation failures; this baseline cannot be accepted." : "An authorized external approver must inspect the baseline and explicitly record acceptance." }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(report, Json), new System.Text.UTF8Encoding(false));
        string approvalState = Path.Combine(root, ".hygiene", ".state", "baseline-approval.json");
        Directory.CreateDirectory(Path.GetDirectoryName(approvalState)!);
        File.WriteAllText(approvalState, JsonSerializer.Serialize(new { schemaVersion = 1, status, reportPath = Path.GetRelativePath(root, path).Replace('\\', '/') }, Json), new System.Text.UTF8Encoding(false));
        return path;
    }

    private static void WriteLifecycle<T>(T report, string output, string text)
    {
        if (output == "json")
        {
            if (releaseUpdateState is null) { Console.WriteLine(JsonSerializer.Serialize(report, Json)); }
            else
            {
                Dictionary<string, JsonElement> envelope = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(report, Json), Json)!;
                envelope["releaseUpdate"] = JsonSerializer.SerializeToElement(releaseUpdateState, Json);
                Console.WriteLine(JsonSerializer.Serialize(envelope, Json));
            }
        }
        else
        {
            using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(report, Json));
            JsonElement root = document.RootElement;
            Console.WriteLine($"Status: {GetString(root, "status") ?? "incomplete"}");
            Console.WriteLine(text);
            if (releaseUpdateState is not null) { Console.WriteLine($"Release update: {JsonSerializer.Serialize(releaseUpdateState, Json)}"); }
            if (GetString(root, "gate") is string gate) { Console.WriteLine($"Gate: {gate}"); }
            if (GetString(root, "action") is string action) { Console.WriteLine($"Next action: {action}"); }
            if (GetString(root, "reportPath") is string path) { Console.WriteLine($"Baseline report: {path}"); }
            if (root.TryGetProperty("changedPaths", out JsonElement changed) && changed.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement changedPath in changed.EnumerateArray()) { Console.WriteLine($"Changed: {changedPath.GetString()}"); }
            }
            if (root.TryGetProperty("findings", out JsonElement findings) && findings.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement finding in findings.EnumerateArray()) { Console.WriteLine($"Finding: {GetString(finding, "ruleId")} {GetString(finding, "path")}:{GetString(finding, "line")} {GetString(finding, "message")}"); }
            }
            if (root.TryGetProperty("workOrder", out JsonElement workOrder) && workOrder.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement file in workOrder.EnumerateArray())
                {
                    Console.WriteLine($"Selected work: {GetString(file, "path")} (source SHA-256 {GetString(file, "currentContentSha256")})");
                    foreach (JsonElement item in file.GetProperty("items").EnumerateArray())
                    {
                        Console.WriteLine($"  {GetString(item, "ruleId")} {GetString(item, "batchHandle")} {GetString(item, "itemId")} {GetString(item, "declaration")}");
                        foreach (JsonElement question in item.GetProperty("questions").EnumerateArray()) { Console.WriteLine($"    {GetString(question, "id")}: {GetString(question, "text")}"); }
                        JsonElement escalation = item.GetProperty("escalation");
                        Console.WriteLine($"    Escalate to {GetString(escalation, "reviewerClass")}: {GetString(escalation, "condition")}");
                        Console.WriteLine($"    Action: {GetString(escalation, "command")}");
                    }
                }
            }
        }
    }

    private static string? GetString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out JsonElement value)) { return null; }
        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static string[] GitLines(string root, params string[] args)
    {
        (int code, string stdout, _) = RunProcess("git", root, args);
        if (code != 0) { return []; }
        return stdout.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
    }

    private static string GitLine(string root, params string[] args) => GitLines(root, args).FirstOrDefault() ?? "";
    private static int GitExit(string root, params string[] args) => RunProcess("git", root, args).ExitCode;

    private static string DetectLocalDefaultBase(string root)
    {
        string symbolic = GitLine(root, "symbolic-ref", "--short", "refs/remotes/origin/HEAD");
        if (symbolic.StartsWith("origin/", StringComparison.Ordinal)
            && GitExit(root, "show-ref", "--verify", "--quiet", "refs/remotes/" + symbolic) == 0)
        {
            return symbolic;
        }
        bool main = GitExit(root, "show-ref", "--verify", "--quiet", "refs/remotes/origin/main") == 0;
        bool master = GitExit(root, "show-ref", "--verify", "--quiet", "refs/remotes/origin/master") == 0;
        if (main == master) { return ""; }
        return main ? "origin/main" : "origin/master";
    }

    private static (int ExitCode, string Stdout, string Stderr) RunProcess(string executable, string workingDirectory, params string[] args) => RunProcess(executable, workingDirectory, null, args);

    private static (int ExitCode, string Stdout, string Stderr) RunProcess(string executable, string workingDirectory, IReadOnlyDictionary<string, string?>? environment, params string[] args)
    {
        var start = new ProcessStartInfo(executable) { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        if (environment is not null) { foreach ((string key, string? value) in environment) { if (value is null) { start.Environment.Remove(key); } else { start.Environment[key] = value; } } }
        foreach (string arg in args) { start.ArgumentList.Add(arg); }
        using Process process = Process.Start(start) ?? throw new EnvironmentException($"Could not start '{executable}'.");
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(stdoutTask, stderrTask);
        string stdout = stdoutTask.Result;
        string stderr = stderrTask.Result;
        return (process.ExitCode, stdout, stderr);
    }

    private static int RenderCheck(HygieneEngine engine, string[] paths, bool changed, string output, string? baseRef, bool reviewAll)
    {
        CheckResult r = engine.Check(paths, changed, baseRef: baseRef, reviewAll: reviewAll);
        if (output == "json")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, r.RunId, findings = r.Findings.Select(PublicFinding), r.IgnoredCount, reviewBatches = r.ReviewBatches.Select(PublicBatch) }, Json));
        }
        else if (output == "text")
        {
            Console.WriteLine($"Run {r.RunId}: {r.Findings.Count} finding(s), {r.IgnoredCount} ignored."); foreach (Finding f in r.Findings) { string classification = f.Classification == "review-candidate" ? " [review-candidate]" : ""; Console.WriteLine($"{f.Id} {f.RuleId}{classification} {f.Path}:{f.Line}:{f.Column} {f.Message} {f.Suggestion}"); }
            foreach (ReviewBatch b in r.ReviewBatches)
            {
                RenderBatchText(b);
            }
        }
        else
        {
            throw new ArgumentException("--output must be text or json.");
        }

        return 0;
    }
    private static int RenderProfile(ProfileResult r, string output)
    {
        if (output == "json")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, r.Command, r.FindingCount, r.Findings, r.ChangedPaths }, Json));
        }
        else if (output == "text")
        {
            Console.WriteLine($"{r.Command}: {r.ChangedPaths.Count} profile file(s) reconciled, {r.FindingCount} finding(s)"); foreach (var f in r.Findings)
            {
                Console.WriteLine($"{f.RuleId} {f.Path}: {f.Message} {f.Suggestion}");
            }
        }
        else
        {
            throw new ArgumentException("--output must be text or json.");
        }

        return 0;
    }
    private const string AgentGuidance = """
        The only supported command is dotnet hygiene. Run it from a Git repository; no arguments starts the autonomous lifecycle.
        Workflow: bootstrap and approve the baseline -> implement/change code -> run relevant tests -> deterministic formatting/normalization -> resolve selected findings/review work -> rerun dotnet hygiene. Targets: paths, --changed, --base <ref>, and --review-all are mutually exclusive. Manual --check is non-mutating.
        First use assesses existing formatting without mutation, bootstraps every eligible repository project, applies hygiene-owned formatting and normalization, and writes an approval-required baseline report. Inspect the diff and report. Only an authorized external approver may accept with dotnet hygiene baseline accept --approver <identity>; this command records the caller's assertion and does not verify authority.
        On an approved repository, dotnet hygiene formats and normalizes, detects a trusted source-change gate, runs deterministic checks, and prints selected-only semantic work grouped by file. Inspect each rule independently, remediate in context, and rerun dotnet hygiene after source changes. A complete status covers selected obligations only, not the entire repository population.
        When a feature-branch comparison is unambiguous, the lifecycle uses local origin/main; otherwise it uses actual worktree source changes. If the intended base is ambiguous, use dotnet hygiene check --base <trusted-ref>. The tool never fetches, pushes, commits, invents a remote, or calls a model/provider.
        Manual diagnostics: dotnet hygiene check [paths...], --changed, --base <ref>, --review-all; dotnet hygiene rules; dotnet hygiene explain; dotnet hygiene format --check; dotnet hygiene normalize --check; dotnet hygiene review expand <batch>, dotnet hygiene review handoff <batch> --output json, dotnet hygiene review accept <batch> <item>... . Handoff returns escalation-required and keeps those items outstanding. Format is presentation-only; normalize performs semantics-preserving code transformations. --review-all remains bounded; --yolo-all belongs to M0019.
        Each enabled semantic-review rule selects independently, up to five items; a batch may be 0/N or 0/0. The agent must inspect every fixed question before explicit per-rule acceptance. Negative or uncertain summary, test-quality, and comment-intent items go to the frontier; BORINGness and type-responsibility concerns go to the planner. Acceptance revalidates source identity; changed items become stale. Visual-block is a contextual review candidate, never an automatic formatting request. Review batches are not findings and sampling is not population certification.
        Output: --output text|json. Exit 0 means command execution succeeded; inspect the lifecycle status to know whether the gate is complete, approval-required, escalation-required, or incomplete. Exit 2 means invalid invocation, 3 unusable input/state/safety precondition, 4 missing required dependency. Review state is under .hygiene/.state; the CLI invokes no model/provider. The old bare hygiene command and former CLI shapes have no pre-1.0 compatibility guarantee.
        """;
    private static int RenderRewrite(RewriteEngine engine, string command, string[] paths, bool changed, bool checkOnly, string output)
    {
        if (output is not ("text" or "json"))
        {
            throw new ArgumentException("--output must be text or json.");
        }

        RewriteResult r = engine.Rewrite(command, paths, changed, checkOnly);
        if (output == "json")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, r.Command, r.CheckOnly, r.TargetCount, r.ChangedCount, r.UnchangedCount, r.ChangedPaths, r.SelectedRuleIds }, Json));
        }
        else if (output == "text")
        {
            Console.WriteLine($"{r.Command}: {r.ChangedCount} changed, {r.UnchangedCount} unchanged of {r.TargetCount} target(s){(r.CheckOnly ? " (check only)" : "")}");
            Console.WriteLine("Rules: " + string.Join(", ", r.SelectedRuleIds)); foreach (string path in r.ChangedPaths)
            {
                Console.WriteLine($"  {path}");
            }
        }
        else
        {
            throw new ArgumentException("--output must be text or json.");
        }

        return 0;
    }
    private static object PublicFinding(Finding f) => new { f.Id, f.Handle, f.RuleId, f.RuleVersion, f.Classification, f.Path, f.Line, f.Column, f.Symbol, f.Message, f.Suggestion };
    private static object PublicBatch(ReviewBatch b) => new { b.Id, b.Handle, b.RuleId, b.RuleVersion, b.Mode, b.ReviewerClass, b.PopulationCount, b.SampleCount, b.Questions, b.Escalation, items = b.Items.Select(i => new { i.Id, i.Path, i.Line, i.Column, i.Symbol, i.Summary, i.Declaration }) };
    private static int RenderReview(ReviewBatch batch, string output)
    {
        if (output == "json")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, reviewBatch = PublicBatch(batch) }, Json));
        }
        else if (output == "text")
        {
            RenderBatchText(batch);
        }
        else
        {
            throw new ArgumentException("--output must be text or json.");
        }

        return 0;
    }

    private static int RenderAcceptance(ReviewAcceptance acceptance, string output)
    {
        if (output == "json")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, acceptance.BatchHandle, acceptance.RuleId, acceptedItemIds = acceptance.ItemIds, acceptance.AcceptedCount }, Json));
        }
        else if (output == "text")
        {
            Console.WriteLine($"Accepted {acceptance.AcceptedCount} review item(s) from {acceptance.BatchHandle} ({acceptance.RuleId}): {string.Join(", ", acceptance.ItemIds)}");
        }
        else { throw new ArgumentException("--output must be text or json."); }
        return 0;
    }
    private static void RenderBatchText(ReviewBatch b)
    {
        Console.WriteLine($"{b.Id} {b.RuleId} — {b.Mode} {b.SampleCount}/{b.PopulationCount} — reviewer: {b.ReviewerClass}");
        foreach (ReviewItem item in b.Items)
        {
            Console.WriteLine($"  {item.Id} {item.Path}:{item.Line}:{item.Column} {item.Symbol} — {item.Summary}");
        }

        foreach (ReviewQuestion question in b.Questions)
        {
            Console.WriteLine($"  {question.Id}: {question.Text}");
        }

        if (b.Mode == "sample")
        {
            Console.WriteLine($"  Escalate: {b.Escalation.Condition} Run: dotnet hygiene review expand {b.Handle}");
        }
    }
    private static int RenderFinding(Finding f, string output)
    {
        if (output == "json")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, f.RuleId, f.RuleVersion, f.Classification, f.Path, f.Line, f.Column, f.Observation, f.Reason, f.Suggestion, f.Constraint }, Json));
        }
        else if (output == "text")
        {
            Console.WriteLine($"{f.RuleId} v{f.RuleVersion} ({f.Classification})\n{f.Path}:{f.Line}:{f.Column}\nObservation: {f.Observation}\nWhy: {f.Reason}\nSuggestion: {f.Suggestion}\nConstraint: {f.Constraint}");
        }
        else
        {
            throw new ArgumentException("--output must be text or json.");
        }

        return 0;
    }
    private static int RenderRules(HygieneEngine e, string output)
    {
        var values = e.ListRules().Select(x => new { x.Rule.Id, x.Rule.Version, x.Rule.OutputKind, x.Rule.Classification, x.Rule.Purpose, x.Rule.Configurable, x.Enabled,
            capabilities = new { check = x.Rule.Diagnose, format = x.Rule.FormatRemediate, normalize = x.Rule.NormalizeRemediate, editorConfig = x.Rule.EditorConfigProject, profile = x.Rule.ProfileRemediate } });
        if (output == "json")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, rules = values }, Json));
        }
        else if (output == "text")
        {
            foreach (var r in values)
            {
                Console.WriteLine($"{r.Id} v{r.Version} [{string.Join(",", new[] { r.capabilities.check ? "check" : null, r.capabilities.format ? "format" : null, r.capabilities.normalize ? "normalize" : null, r.capabilities.editorConfig ? "editorconfig" : null, r.capabilities.profile ? "profile" : null }.Where(x => x is not null))}; {(r.Configurable ? "configurable" : "mandatory")}; {(r.Enabled ? "enabled" : "disabled")}] {r.Purpose}");
            }
        }
        else
        {
            throw new ArgumentException("--output must be text or json.");
        }

        return 0;
    }
    private static int RenderIgnores(IReadOnlyList<IgnoreView> values, string output)
    {
        if (output == "json")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, ignores = values }, Json));
        }
        else if (output == "text")
        {
            foreach (var x in values)
            {
                Console.WriteLine($"{x.Id} {x.State} {x.RuleId} {x.Path} {x.Reason}");
            }
        }
        else
        {
            throw new ArgumentException("--output must be text or json.");
        }

        return 0;
    }
}
