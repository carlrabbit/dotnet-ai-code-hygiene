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
        if (parsed.Errors.Count > 0) { foreach (var error in parsed.Errors) Console.Error.WriteLine(error.Message); return 2; }
        try { return await parsed.InvokeAsync(); }
        catch (ArgumentException e) { Console.Error.WriteLine(e.Message); return 2; }
        catch (ProductException e) { Console.Error.WriteLine(e.Message); return 3; }
        catch (EnvironmentException e) { Console.Error.WriteLine(e.Message); return 4; }
        catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }
    private static RootCommand Build()
    {
        var root = new RootCommand("AI-first code hygiene tooling.");
        var format = new Command("format", "Reserved for a future formatter.");
        format.SetAction(_ => Run(() => { Console.Error.WriteLine("format is not implemented until M0004."); return 3; })); root.Subcommands.Add(format);
        var normalize = new Command("normalize", "Reserved for a future normalizer.");
        normalize.SetAction(_ => Run(() => { Console.Error.WriteLine("normalize is not implemented until M0004."); return 3; })); root.Subcommands.Add(normalize);
        var output = new Option<string>("--output") { DefaultValueFactory = _ => "text" };
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
        var review = new Command("review", "Expand or create a durable handoff for a semantic review batch.");
        var expandHandle = new Argument<string>("batch-handle"); var expand = new Command("expand", "Expand the latest run's complete eligible review population.");
        expand.Arguments.Add(expandHandle); expand.Options.Add(output);
        expand.SetAction(parse => Run(() => RenderReview(new HygieneEngine().ExpandReview(parse.GetValue(expandHandle)!), parse.GetValue(output) ?? "text")));
        var handoffHandle = new Argument<string>("batch-handle"); var handoffFile = new Option<string?>("--file");
        var handoff = new Command("handoff", "Create a durable, transport-neutral frontier-review request.");
        handoff.Arguments.Add(handoffHandle); handoff.Options.Add(handoffFile);
        handoff.SetAction(parse => Run(() => { string path = new HygieneEngine().CreateReviewHandoff(parse.GetValue(handoffHandle)!, parse.GetValue(handoffFile)); Console.WriteLine($"Review handoff written: {path}"); return 0; }));
        review.Subcommands.Add(expand); review.Subcommands.Add(handoff); root.Subcommands.Add(review);
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
        if (output == "json") Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, r.RunId, findings = r.Findings.Select(PublicFinding), r.IgnoredCount, reviewBatches = r.ReviewBatches.Select(PublicBatch) }, Json));
        else if (output == "text") { Console.WriteLine($"Run {r.RunId}: {r.Findings.Count} finding(s), {r.IgnoredCount} ignored."); foreach (Finding f in r.Findings) { string classification = f.Classification == "review-candidate" ? " [review-candidate]" : ""; Console.WriteLine($"{f.Id} {f.RuleId}{classification} {f.Path}:{f.Line}:{f.Column} {f.Message} {f.Suggestion}"); } foreach (ReviewBatch b in r.ReviewBatches) RenderBatchText(b); }
        else throw new ArgumentException("--output must be text or json.");
        return 0;
    }
    private static object PublicFinding(Finding f) => new { f.Id, f.Handle, f.RuleId, f.RuleVersion, f.Classification, f.Path, f.Line, f.Column, f.Symbol, f.Message, f.Suggestion };
    private static object PublicBatch(ReviewBatch b) => new { b.Id, b.Handle, b.RuleId, b.RuleVersion, b.Mode, b.ReviewerClass, b.PopulationCount, b.SampleCount, b.Questions, b.Escalation, items = b.Items.Select(i => new { i.Id, i.Path, i.Line, i.Column, i.Symbol, i.Summary, i.Declaration }) };
    private static int RenderReview(ReviewBatch batch, string output)
    {
        if (output == "json") Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, reviewBatch = PublicBatch(batch) }, Json));
        else if (output == "text") RenderBatchText(batch);
        else throw new ArgumentException("--output must be text or json.");
        return 0;
    }
    private static void RenderBatchText(ReviewBatch b)
    {
        Console.WriteLine($"{b.Id} {b.RuleId} — {b.Mode} {b.SampleCount}/{b.PopulationCount} — reviewer: {b.ReviewerClass}");
        foreach (ReviewItem item in b.Items) Console.WriteLine($"  {item.Id} {item.Path}:{item.Line}:{item.Column} {item.Symbol} — {item.Summary}");
        foreach (ReviewQuestion question in b.Questions) Console.WriteLine($"  {question.Id}: {question.Text}");
        if (b.Mode == "sample") Console.WriteLine($"  Escalate: {b.Escalation.Condition} Run: hygiene review expand {b.Handle}");
    }
    private static int RenderFinding(Finding f, string output)
    {
        if (output == "json") Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, f.RuleId, f.RuleVersion, f.Classification, f.Path, f.Line, f.Column, f.Observation, f.Reason, f.Suggestion, f.Constraint }, Json));
        else if (output == "text") Console.WriteLine($"{f.RuleId} v{f.RuleVersion} ({f.Classification})\n{f.Path}:{f.Line}:{f.Column}\nObservation: {f.Observation}\nWhy: {f.Reason}\nSuggestion: {f.Suggestion}\nConstraint: {f.Constraint}");
        else throw new ArgumentException("--output must be text or json."); return 0;
    }
    private static int RenderRules(HygieneEngine e, string output)
    {
        var values = e.ListRules().Select(x => new { x.Rule.Id, x.Rule.Version, x.Rule.OutputKind, x.Rule.Classification, x.Rule.Purpose, x.Enabled });
        if (output == "json") Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, rules = values }, Json));
        else if (output == "text") foreach (var r in values) Console.WriteLine($"{r.Id} v{r.Version} [{r.OutputKind}; {(r.Enabled ? "enabled" : "disabled")}] {r.Purpose}");
        else throw new ArgumentException("--output must be text or json."); return 0;
    }
    private static int RenderIgnores(IReadOnlyList<IgnoreView> values, string output)
    {
        if (output == "json") Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, ignores = values }, Json));
        else if (output == "text") foreach (var x in values) Console.WriteLine($"{x.Id} {x.State} {x.RuleId} {x.Path} {x.Reason}");
        else throw new ArgumentException("--output must be text or json."); return 0;
    }
}
