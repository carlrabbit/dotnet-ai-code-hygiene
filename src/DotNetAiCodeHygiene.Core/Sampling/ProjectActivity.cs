namespace DotNetAiCodeHygiene.Core.Sampling;

internal sealed record ActivitySourceFile(string Path, string Content);
internal sealed record ProjectActivityState(string ProjectId, double AgeUnits, int EligibleLoc, ActivitySourceFile[] Sources);
internal sealed record ActivityTransition(int AddedLines, int DeletedLines, int PreviousLoc, double DeltaAgeUnits, double TotalAgeUnits, bool Initialized, bool Reset);

/// <summary>Versioned, repository-local source-transition accounting. It owns no clock or Git history.</summary>
internal static class ProjectActivity
{
    internal const int StateVersion = 1;
    internal static ActivityTransition Observe(ProjectActivityState? previous, IReadOnlyDictionary<string, string> current)
    {
        ActivitySourceFile[] sources = current.OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => new ActivitySourceFile(x.Key, Normalize(x.Value))).ToArray();
        if (previous is null)
        {
            return new(0, 0, 0, 0, 0, true, false);
        }

        var before = previous.Sources.ToDictionary(x => x.Path, x => x.Content, StringComparer.Ordinal);
        var after = sources.ToDictionary(x => x.Path, x => x.Content, StringComparer.Ordinal);
        var removed = before.Keys.Except(after.Keys, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var added = after.Keys.Except(before.Keys, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        int addLines = 0, deleteLines = 0;

        // Pair byte-identical removed/added files so a path-only rename contributes no churn.
        foreach (string path in removed.ToArray())
        {
            string? matching = added.Order(StringComparer.Ordinal).FirstOrDefault(candidate => StringComparer.Ordinal.Equals(before[path], after[candidate]));
            if (matching is null) { continue; }
            removed.Remove(path);
            added.Remove(matching);
        }
        // A single remaining delete/add pair is an unambiguous edited rename; diff its content.
        if (removed.Count == 1 && added.Count == 1)
        {
            string oldPath = removed.Single(), newPath = added.Single();
            (int renamedAdds, int renamedDeletes) = Diff(Lines(before[oldPath]), Lines(after[newPath]));
            addLines = renamedAdds;
            deleteLines = renamedDeletes;
            removed.Clear();
            added.Clear();
        }

        foreach (string path in removed) { deleteLines += Lines(before[path]).Length; }
        foreach (string path in added) { addLines += Lines(after[path]).Length; }
        foreach (string path in before.Keys.Intersect(after.Keys, StringComparer.Ordinal))
        {
            (int lineAdds, int lineDeletes) = Diff(Lines(before[path]), Lines(after[path]));
            addLines += lineAdds;
            deleteLines += lineDeletes;
        }

        int previousLoc = previous.EligibleLoc;
        double delta = 365d * (addLines + deleteLines) / Math.Max(previousLoc, 100);
        SubjectHazardSampler.ValidateHazard(delta);
        double total = previous.AgeUnits + delta;
        SubjectHazardSampler.ValidateHazard(total);
        return new(addLines, deleteLines, previousLoc, delta, total, false, false);
    }

    internal static bool IsValid(ProjectActivityState state) =>
        !string.IsNullOrWhiteSpace(state.ProjectId) && double.IsFinite(state.AgeUnits) && state.AgeUnits >= 0 && state.EligibleLoc >= 0 && state.Sources is not null &&
        state.Sources.All(x => x is not null && x.Path is not null && x.Content is not null) &&
        state.EligibleLoc == state.Sources.Sum(x => Lines(x.Content).Length) &&
        state.Sources.Select(x => x.Path).Distinct(StringComparer.Ordinal).Count() == state.Sources.Length;

    internal static ProjectActivityState Create(string projectId, IReadOnlyDictionary<string, string> sources, double ageUnits = 0)
    {
        ActivitySourceFile[] files = sources.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new ActivitySourceFile(x.Key, Normalize(x.Value))).ToArray();
        return new(projectId, ageUnits, files.Sum(x => Lines(x.Content).Length), files);
    }

    private static string Normalize(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    private static string[] Lines(string value)
    {
        if (value.Length == 0) { return []; }
        string[] lines = value.Split('\n');
        return value.EndsWith('\n') ? lines[..^1] : lines;
    }

    // Myers shortest-edit script; exact and deterministic while storing only the frontier snapshots.
    private static (int Added, int Deleted) Diff(string[] oldLines, string[] newLines)
    {
        int n = oldLines.Length, m = newLines.Length, max = n + m, offset = max + 1;
        if (max == 0) { return (0, 0); }
        int[] v = new int[2 * max + 3];
        var trace = new List<int[]>(max + 1);
        int distance = 0;
        bool done = false;
        for (int d = 0; d <= max && !done; d++)
        {
            trace.Add((int[])v.Clone());
            for (int k = -d; k <= d; k += 2)
            {
                int x = k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1])
                    ? v[offset + k + 1] : v[offset + k - 1] + 1;
                int y = x - k;
                while (x < n && y < m && StringComparer.Ordinal.Equals(oldLines[x], newLines[y])) { x++; y++; }
                v[offset + k] = x;
                if (x >= n && y >= m) { distance = d; done = true; break; }
            }
        }
        int add = 0, delete = 0, currentX = n, currentY = m;
        for (int d = distance; d >= 0; d--)
        {
            int[] previous = trace[d];
            int k = currentX - currentY;
            int previousK = k == -d || (k != d && previous[offset + k - 1] < previous[offset + k + 1]) ? k + 1 : k - 1;
            int previousX = previous[offset + previousK], previousY = previousX - previousK;
            while (currentX > previousX && currentY > previousY) { currentX--; currentY--; }
            if (d == 0) { break; }
            if (currentX == previousX) { add++; currentY--; }
            else { delete++; currentX--; }
        }
        return (add, delete);
    }
}
