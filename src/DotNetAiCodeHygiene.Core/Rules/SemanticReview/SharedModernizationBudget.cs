using System.Security.Cryptography;
using System.Text;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Selects modernization packages under one gate-wide cap without changing legacy per-rule sampling.</summary>
internal static class SharedModernizationBudget
{
    internal const int MaximumPackages = 5;
    internal const int AlgorithmVersion = 1;

    internal static IReadOnlyList<(Rule Rule, ReviewSubject Subject)> Select(
        string gateId,
        string mode,
        IReadOnlySet<string> changedPaths,
        IEnumerable<(Rule Rule, IReadOnlyList<ReviewSubject> Population)> populations)
    {
        if (mode == "closed") { return []; }
        var rules = populations
            .SelectMany(population => population.Population.Select(subject => (population.Rule, Subject: subject)))
            .Where(candidate => mode is "all" or "paths"
                || changedPaths.Contains(candidate.Subject.Item.Path))
            .GroupBy(candidate => candidate.Rule.Id, StringComparer.Ordinal)
            .Select(group => new RulePopulation(group.First().Rule,
                group.Select(candidate => candidate.Subject)
                    .DistinctBy(subject => subject.Identity, StringComparer.Ordinal)
                    .OrderBy(subject => Key(gateId, group.Key, subject), StringComparer.Ordinal)
                    .ThenBy(subject => subject.Identity, StringComparer.Ordinal)
                    .ToArray()))
            .Where(population => population.Subjects.Length > 0)
            .OrderBy(population => population.Rule.Id, StringComparer.Ordinal)
            .ToArray();
        if (rules.Length == 0) { return []; }

        int offset = Rotation(gateId, rules.Length);
        RulePopulation[] rotated = rules.Skip(offset).Concat(rules.Take(offset)).ToArray();
        bool broad = mode is "all" or "paths";
        var direct = rotated.ToDictionary(population => population.Rule.Id,
            population => population.Subjects.Where(subject => broad || subject.DirectChange).ToArray(), StringComparer.Ordinal);
        var context = rotated.ToDictionary(population => population.Rule.Id,
            population => broad ? [] : population.Subjects.Where(subject => !subject.DirectChange).ToArray(), StringComparer.Ordinal);

        var selected = new List<(Rule Rule, ReviewSubject Subject)>(MaximumPackages);
        AddRoundRobin(rotated, direct, selected);
        if (selected.Count < MaximumPackages) { AddRoundRobin(rotated, context, selected); }
        return selected;
    }

    private static void AddRoundRobin(RulePopulation[] rules, IReadOnlyDictionary<string, ReviewSubject[]> candidates,
        List<(Rule Rule, ReviewSubject Subject)> selected)
    {
        int depth = 0;
        while (selected.Count < MaximumPackages)
        {
            bool added = false;
            foreach (RulePopulation population in rules)
            {
                ReviewSubject[] subjects = candidates[population.Rule.Id];
                if (depth >= subjects.Length) { continue; }
                selected.Add((population.Rule, subjects[depth]));
                added = true;
                if (selected.Count == MaximumPackages) { return; }
            }
            if (!added) { return; }
            depth++;
        }
    }

    private static int Rotation(string gateId, int ruleCount)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes("modernization-rotation\0" + AlgorithmVersion + "\0" + gateId));
        return (int)(System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(digest) % (uint)ruleCount);
    }

    private static string Key(string gateId, string ruleId, ReviewSubject subject) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        "modernization-package\0" + AlgorithmVersion + "\0" + gateId + "\0" + ruleId + "\0" + subject.Identity + "\0" + GateLocalReviewStore.Fingerprint(subject.Content))));

    private sealed record RulePopulation(Rule Rule, ReviewSubject[] Subjects);
}
