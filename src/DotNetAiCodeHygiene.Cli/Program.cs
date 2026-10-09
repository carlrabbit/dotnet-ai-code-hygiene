using System.CommandLine;
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
        root.Options.Add(new VersionOption());
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
        var check = new Command("check", "Analyze selected C# source files."); check.Arguments.Add(paths); check.Options.Add(changed); check.Options.Add(output);
        check.SetAction(parse => Run(() => RenderCheck(new HygieneEngine(), parse.GetValue(paths) ?? [], parse.GetValue(changed), parse.GetValue(output) ?? "text"))); root.Subcommands.Add(check);
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
        handoff.Arguments.Add(handoffHandle); handoff.Options.Add(handoffFile);
        handoff.SetAction(parse => Run(() => { string path = new HygieneEngine().CreateReviewHandoff(parse.GetValue(handoffHandle)!, parse.GetValue(handoffFile)); Console.WriteLine($"Review handoff written: {path}"); return 0; }));
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
    private static int RenderCheck(HygieneEngine engine, string[] paths, bool changed, string output)
    {
        CheckResult r = engine.Check(paths, changed);
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
        hygiene is a repository-scoped .NET hygiene tool. Run it from a Git repository.
        Workflow: hygiene bootstrap (once per repository) -> implement/change code -> run relevant tests -> hygiene normalize -> hygiene check -> resolve findings/review work -> rerun tests/check. Use hygiene update to reconcile the installed dotnet-11 v2 profile.
        Targets: omit paths for repository C# files; pass files/directories; or use --changed (mutually exclusive with paths). Targets are de-duplicated and exclude .git, .hygiene, bin, and obj.
        Output: --output text|json. Exit 0 means command succeeded; rewrite --check may report pending changes. Exit 2 means invalid invocation, 3 unusable input/state/safety precondition, 4 missing required dependency.
        format is presentation-only and applies enabled format-remediation rules. normalize applies enabled semantics-preserving normalization rules, then enabled format rules on changed documents, validates compilation before and after, and commits all selected changes together. Both support non-mutating --check and are idempotent.
        check reports deterministic findings and statistical semantic-review batches. A batch can be 0/N when eligible work is not due. Review batches are not findings.
        After every required question is confidently acceptable and no escalation applies, use hygiene review accept <batch-handle> <item-id>... or --all to record observations. Acceptance revalidates tickets and commits them atomically.
        Negative or uncertain items remain due. Use hygiene review expand <batch-handle> or hygiene review handoff <batch-handle> [--file <path>] for the rule's full-population escalation (frontier for summaries, planner for BORINGness).
        Expansion, handoff, check, and rendering do not consume sampling state. The CLI invokes no model/provider. .hygiene/config.json and decisions.json are product state; .hygiene/.state is engine-owned. Handoff request files are explicit work products.
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
            Console.WriteLine($"  Escalate: {b.Escalation.Condition} Run: hygiene review expand {b.Handle}");
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
