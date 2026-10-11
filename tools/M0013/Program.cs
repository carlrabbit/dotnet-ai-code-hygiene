using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DotNetAiCodeHygiene.Core;
using DotNetAiCodeHygiene.Core.Sampling;
using Microsoft.CodeAnalysis;

namespace M0013Replay;

internal sealed record Variant(string Id, string Policy, string Observation, string Seed, string StatePath);
internal sealed record RunConfig(string Root, string RepositoryId, string Commit, long TimestampUnixMilliseconds, string VariantsPath, string OutputPath);
internal sealed record Candidate(string Identity, string Path, int Line, string Symbol, string Declaration);
internal sealed record ModuleStats(string RuleId, int PopulationCount, int DueBeforeObservation, int SelectedCount,
    int AcceptedCount, int DueAfterObservation, string[] SelectedIds, Candidate[] SelectedCandidates);
internal sealed record VariantResult(string VariantId, string Policy, string Observation, string SeedId,
    string Commit, long TimestampUnixMilliseconds, long WorkspaceSetupMilliseconds, long FirstRunMilliseconds,
    long RepeatRunMilliseconds, long SamplingStateBytes, int EligibleProjects, int EligibleSourceFiles,
    int EligibleSourceLines, int ProjectAgeChangeCount, double ProjectAgeDeltaUnits,
    double RepeatProjectAgeDeltaUnits, ModuleStats[] Rules, Candidate[]? EligibleBoringnessCandidates);

internal static class Program
{
    private const string BoringnessRuleId = "architecture.boringness.review";
    private const double YearlyHazardRate = 1d / (365d * 24d * 60d * 60d);
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private static int Main(string[] args)
    {
        try
        {
            var values = ParseArguments(args);
            var config = JsonSerializer.Deserialize<RunConfig>(File.ReadAllText(values["config"]), Json)
                ?? throw new InvalidOperationException("Replay config was empty.");
            var variants = JsonSerializer.Deserialize<Variant[]>(File.ReadAllText(config.VariantsPath), Json)
                ?? throw new InvalidOperationException("Variant list was empty.");
            string root = Path.GetFullPath(config.Root);
            var setupTimer = Stopwatch.StartNew();
            using var repository = new RepositorySession(root);
            string[] targets = repository.ResolveTargets([], changed: false);
            var assignments = repository.AssignTargets(targets, []);
            Document[] documents = assignments.Values.Select(x => x.Document).DistinctBy(x => x.Id).ToArray();
            // Force project discovery once; RepositorySession caches compilation and source facts across policy arms.
            int eligibleProjects = repository.Projects.Count;
            setupTimer.Stop();
            string[] disabled = ReadDisabledRules(root);
            var results = new List<VariantResult>(variants.Length);
            bool includePopulation = true;
            foreach (Variant variant in variants)
            {
                if (variant.Policy is not ("A" or "B" or "C")) { throw new InvalidOperationException("Unknown policy arm."); }
                if (variant.Observation is not ("all" or "none")) { throw new InvalidOperationException("Unknown observation arm."); }
                VariantResult result = RunVariant(repository, documents, config, variant, disabled,
                    setupTimer.ElapsedMilliseconds, includePopulation);
                results.Add(result);
                includePopulation = false;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(config.OutputPath))!);
            File.WriteAllText(config.OutputPath, JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                experiment = "M0013",
                repositoryId = config.RepositoryId,
                commit = config.Commit,
                timestampUnixMilliseconds = config.TimestampUnixMilliseconds,
                workspaceSetupMilliseconds = setupTimer.ElapsedMilliseconds,
                eligibleProjects,
                results
            }, Json));
            Console.WriteLine($"M0013 replayed {results.Count} fixed policy/seed/observation states at {config.Commit}.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"M0013 replay failed: {error.GetType().FullName}: {error.Message}");
            return 1;
        }
    }

    private static VariantResult RunVariant(RepositorySession repository, Document[] documents, RunConfig config,
        Variant variant, string[] disabled, long setupMilliseconds, bool includePopulation)
    {
        var before = ReadProjectStates(variant.StatePath);
        RunPass first = RunPassOnce(repository, documents, config, variant, disabled, before);
        var afterFirst = ReadProjectStates(variant.StatePath);
        RunPass repeat = RunPassOnce(repository, documents, config, variant, disabled, afterFirst);
        var afterRepeat = ReadProjectStates(variant.StatePath);
        double ageDelta = DifferenceTotal(before, afterFirst);
        double repeatDelta = DifferenceTotal(afterFirst, afterRepeat);
        int ageChangeCount = ChangedProjectCount(before, afterFirst);
        long stateBytes = File.Exists(variant.StatePath) ? new FileInfo(variant.StatePath).Length : 0;
        return new(variant.Id, variant.Policy, variant.Observation, variant.Id.Split('|')[2], config.Commit,
            config.TimestampUnixMilliseconds, setupMilliseconds, first.ElapsedMilliseconds, repeat.ElapsedMilliseconds,
            stateBytes, afterRepeat.Count, afterRepeat.Values.Sum(x => x.Files), afterRepeat.Values.Sum(x => x.Loc),
            ageChangeCount, ageDelta, repeatDelta, first.Modules, includePopulation ? first.Candidates : null);
    }

    private static RunPass RunPassOnce(RepositorySession repository, Document[] documents, RunConfig config,
        Variant variant, string[] disabled, Dictionary<string, ProjectMetric> beforeProjects)
    {
        string samplingPath = Path.Combine(repository.Root, ".hygiene", ".state", "sampling.json");
        Directory.CreateDirectory(Path.GetDirectoryName(samplingPath)!);
        if (File.Exists(variant.StatePath))
        {
            File.Copy(variant.StatePath, samplingPath, overwrite: true);
        }
        else
        {
            var initial = new SamplingStateFile(2, variant.Seed, [], ProjectActivity.StateVersion, []);
            File.WriteAllText(samplingPath, JsonSerializer.Serialize(initial, Json));
        }

        var timer = Stopwatch.StartNew();
        var context = new RuleContext(repository, documents);
        Dictionary<string, double> currentProjectAges = new(StringComparer.Ordinal);
        Document[] activityDocuments = documents.Where(RuleContext.IsEligibleSourceDocument).ToArray();
        if (variant.Policy is "A" or "C")
        {
            foreach (Document document in activityDocuments)
            {
                string projectId = context.ProjectIdentity(document);
                currentProjectAges[projectId] = context.ProjectActivityAge(document);
            }
        }

        ISemanticReviewRuleModule[] modules = RuleCatalog.SemanticReviewModules
            .Where(module => !disabled.Contains(module.Descriptor.Id, StringComparer.Ordinal)).ToArray();
        if (variant.Policy == "A")
        {
            foreach (ISemanticReviewRuleModule module in modules.Where(x => x is SummaryQualityReviewRuleModule or SummaryGermanReviewRuleModule))
            {
                RuleModuleResult population = SummaryReviewPopulation.CreatePopulation(context);
                SubjectHazardSampler sampler = context.Sampling.SubjectSampler(module.Descriptor.Id, module.Descriptor.Version);
                foreach (ReviewSubject subject in population.ReviewSubjects)
                {
                    sampler.AccrueElapsed(subject.Identity, config.TimestampUnixMilliseconds, YearlyHazardRate);
                }
            }
        }

        PopulationHazardSampler? populationSampler = null;
        HashSet<string> activeUnits = [];
        if (modules.Any(x => x.Descriptor.Id == BoringnessRuleId) && (variant.Policy is "A" or "C"))
        {
            populationSampler = context.Sampling.PopulationSampler(BoringnessRuleId, modules.Single(x => x.Descriptor.Id == BoringnessRuleId).Descriptor.Version);
            foreach (Document document in activityDocuments)
            {
                string path = context.RelativePath(document);
                string unit = context.ProjectIdentity(document) + "\0" + path;
                activeUnits.Add(unit);
                PopulationHazardState? old = populationSampler.States.FirstOrDefault(x => StringComparer.Ordinal.Equals(x.UnitId, unit));
                if (old is not null && variant.Policy == "A")
                {
                    populationSampler.AccrueElapsed(unit, config.TimestampUnixMilliseconds,
                        (old.LastCandidateCount ?? 0) * YearlyHazardRate);
                }
            }
            var positioned = populationSampler.States.Select(state =>
            {
                int separator = state.UnitId.LastIndexOf('\0');
                if (separator < 0) { return state; }
                string owner = state.UnitId[..separator];
                return currentProjectAges.TryGetValue(owner, out double age) ? state with { LastActivityAgeUnits = age } : state;
            }).ToArray();
            populationSampler.Restore(positioned);
        }

        RuleModuleExecution[] executions = modules
            .Select(module => new RuleModuleExecution(module.Descriptor, module.Evaluate(context))).ToArray();
        var documentProjects = activityDocuments.ToDictionary(document => context.RelativePath(document), document => context.ProjectIdentity(document), StringComparer.Ordinal);
        var moduleStats = new List<ModuleStats>(executions.Length);
        Candidate[] candidates = [];
        foreach (RuleModuleExecution execution in executions)
        {
            RuleModuleResult result = execution.Result;
            ReviewSubject[] eligible = result.ReviewSubjects.ToArray();
            ReviewSubject[] selected = (result.SelectedReviewSubjects ?? result.ReviewSubjects).ToArray();
            SubjectHazardSampler? subjectSampler = result.SubjectSamplerForResearch(context, execution.Descriptor.Id, execution.Descriptor.Version) as SubjectHazardSampler;
            PopulationHazardSampler? modulePopulationSampler = null;
            string[] populationUnits = [];
            int dueCount;
            if (subjectSampler is not null)
            {
                dueCount = eligible.Count(subject => subjectSampler.Due(subject.Identity) is not null);
            }
            else
            {
                modulePopulationSampler = context.Sampling.PopulationSampler(execution.Descriptor.Id, execution.Descriptor.Version);
                populationUnits = eligible.Select(subject => subject.Item.Path)
                    .Distinct(StringComparer.Ordinal)
                    .Select(path => documentProjects.TryGetValue(path, out string? project) ? project + "\0" + path : null)
                    .Where(x => x is not null).Cast<string>().ToArray();
                dueCount = populationUnits.Sum(unit => modulePopulationSampler.DueEvents(unit).Count);
            }
            int accepted = 0;
            if (variant.Observation == "all")
            {
                foreach (ReviewSubject subject in selected)
                {
                    if (subject.SubjectTicket is SubjectTicket ticket)
                    {
                        SubjectHazardSampler sampler = context.Sampling.SubjectSampler(ticket.RuleId, ticket.RuleVersion, ticket.ModelVersion);
                        if (sampler.Observe(ticket, at: DateTimeOffset.FromUnixTimeMilliseconds(config.TimestampUnixMilliseconds))) { accepted++; }
                    }
                    if (subject.PopulationTicket is PopulationTicket eventTicket)
                    {
                        PopulationHazardSampler sampler = context.Sampling.PopulationSampler(eventTicket.RuleId, eventTicket.RuleVersion, eventTicket.ModelVersion);
                        if (sampler.Observe(eventTicket)) { accepted++; }
                    }
                }
            }
            if (execution.Descriptor.Id == BoringnessRuleId)
            {
                candidates = eligible.Select(x => ToCandidate(x)).ToArray();
            }
            int dueAfterObservation = subjectSampler is not null
                ? eligible.Count(subject => subjectSampler.Due(subject.Identity) is not null)
                : populationUnits.Sum(unit => modulePopulationSampler!.DueEvents(unit).Count);
            moduleStats.Add(new(execution.Descriptor.Id, eligible.Length, dueCount, selected.Length, accepted,
                dueAfterObservation, selected.Select(x => x.Identity).ToArray(), selected.Select(ToCandidate).ToArray()));
        }

        if (variant.Policy == "A" && populationSampler is not null)
        {
            populationSampler.Restore(populationSampler.States.Select(state => activeUnits.Contains(state.UnitId)
                ? state with { LastEvaluationCursorUnixMilliseconds = config.TimestampUnixMilliseconds }
                : state));
        }
        context.CompleteSampling();
        timer.Stop();
        File.Copy(samplingPath, variant.StatePath, overwrite: true);
        var after = ReadProjectStates(variant.StatePath);
        VerifyUnchangedProjectAges(beforeProjects, after);
        return new(moduleStats.ToArray(), candidates, timer.ElapsedMilliseconds);
    }

    private static Candidate ToCandidate(ReviewSubject subject) => new(subject.Identity, subject.Item.Path,
        subject.Item.Line, subject.Item.Symbol ?? string.Empty, subject.Item.Declaration ?? string.Empty);

    private static string[] ReadDisabledRules(string root)
    {
        string path = Path.Combine(root, ".hygiene", "config.json");
        if (!File.Exists(path)) { return []; }
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("disabledRules", out JsonElement values)
            ? values.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray()
            : [];
    }

    private static Dictionary<string, ProjectMetric> ReadProjectStates(string statePath)
    {
        if (!File.Exists(statePath)) { return new(StringComparer.Ordinal); }
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(statePath));
        if (!document.RootElement.TryGetProperty("Projects", out JsonElement projects)) { return new(StringComparer.Ordinal); }
        var result = new Dictionary<string, ProjectMetric>(StringComparer.Ordinal);
        foreach (JsonElement project in projects.EnumerateArray())
        {
            string id = project.GetProperty("ProjectId").GetString() ?? string.Empty;
            int files = 0, loc = 0;
            var fingerprints = new List<string>();
            foreach (JsonElement source in project.GetProperty("Sources").EnumerateArray())
            {
                files++;
                loc += source.TryGetProperty("LineCount", out JsonElement lines) ? lines.GetInt32() : 0;
                fingerprints.Add((source.GetProperty("Path").GetString() ?? string.Empty) + "\0" + (source.TryGetProperty("Fingerprint", out JsonElement fp) ? fp.GetString() : string.Empty));
            }
            string fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", fingerprints.Order(StringComparer.Ordinal)))));
            result[id] = new(project.GetProperty("AgeUnits").GetDouble(), files, loc, fingerprint);
        }
        return result;
    }

    private static double DifferenceTotal(Dictionary<string, ProjectMetric> before, Dictionary<string, ProjectMetric> after) =>
        after.Sum(pair => pair.Value.Age - before.GetValueOrDefault(pair.Key)?.Age ?? 0);

    private static int ChangedProjectCount(Dictionary<string, ProjectMetric> before, Dictionary<string, ProjectMetric> after) =>
        after.Count(pair => Math.Abs(pair.Value.Age - (before.GetValueOrDefault(pair.Key)?.Age ?? 0)) > 0d);

    private static void VerifyUnchangedProjectAges(Dictionary<string, ProjectMetric> before, Dictionary<string, ProjectMetric> after)
    {
        foreach (var pair in before)
        {
            if (after.TryGetValue(pair.Key, out ProjectMetric? current) && pair.Value.Fingerprint == current.Fingerprint && pair.Value.Age != current.Age)
            {
                throw new InvalidOperationException("An unchanged project source fingerprint changed activity age.");
            }
        }
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Length) { throw new ArgumentException("Expected --name value arguments."); }
            result.Add(args[i][2..], args[++i]);
        }
        if (!result.ContainsKey("config")) { throw new ArgumentException("--config is required."); }
        return result;
    }

    private sealed record ProjectMetric(double Age, int Files, int Loc, string Fingerprint);
    private sealed record RunPass(ModuleStats[] Modules, Candidate[] Candidates, long ElapsedMilliseconds);
}

internal static class ResearchSamplerAccess
{
    internal static object? SubjectSamplerForResearch(this RuleModuleResult result, RuleContext context, string ruleId, int ruleVersion)
    {
        return ruleId switch
        {
            "docs.summary.quality.review" or "docs.summary.language.german.review" => context.Sampling.SubjectSampler(ruleId, ruleVersion),
            _ => null
        };
    }
}
