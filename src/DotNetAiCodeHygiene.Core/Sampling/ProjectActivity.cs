namespace DotNetAiCodeHygiene.Core.Sampling;

internal sealed record ActivitySourceFile(string Path, string Content);
internal sealed record ProjectActivityState(string ProjectId, double AgeUnits, int EligibleLoc, ActivitySourceFile[] Sources, bool SnapshotAvailable = true);
internal sealed record ActivityTransition(int AddedLines, int DeletedLines, int PreviousLoc, double DeltaAgeUnits, double TotalAgeUnits, bool Initialized, bool Reset, int DiffFallbacks = 0);

/// <summary>Versioned, repository-local source-transition accounting. It owns no clock or Git history.</summary>
internal static class ProjectActivity
{
    internal const int StateVersion = 1;
    // The JSON writer escapes control characters, so the repository-wide UTF-16
    // budget keeps the source portion of sampling.json below roughly 6 MiB.
    internal const int MaximumSnapshotCharacters = 512 * 1024;
    internal const int MaximumRepositorySnapshotCharacters = 1024 * 1024;
    internal const int MaximumFileCharacters = 128 * 1024;
    private const int MaximumDiffLines = 12_000;
    private const int MaximumDiffFrontierCells = 2_000_000;
    private const int MaximumDiffDistance = 1_024;
    internal static ActivityTransition Observe(ProjectActivityState? previous, IReadOnlyDictionary<string, string> current, bool retainSnapshot = true)
    {
        ProjectActivityState observed = Create(previous?.ProjectId ?? string.Empty, current, previous?.AgeUnits ?? 0, retainSnapshot);
        if (!observed.SnapshotAvailable)
        {
            return new(0, 0, previous?.EligibleLoc ?? 0, 0, previous?.AgeUnits ?? 0, false, true);
        }
        ActivitySourceFile[] sources = observed.Sources;
        if (previous is null)
        {
            return new(0, 0, 0, 0, 0, true, false);
        }
        if (!previous.SnapshotAvailable)
        {
            return new(0, 0, 0, 0, previous.AgeUnits, true, true);
        }

        var before = previous.Sources.ToDictionary(x => x.Path, x => x.Content, StringComparer.Ordinal);
        var after = sources.ToDictionary(x => x.Path, x => x.Content, StringComparer.Ordinal);
        var removed = before.Keys.Except(after.Keys, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var added = after.Keys.Except(before.Keys, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        int addLines = 0, deleteLines = 0;

        // Only exact-content moves have reliable rename evidence here. Arbitrary
        // delete/add pairs remain independent source removals and additions.
        foreach (var group in removed.GroupBy(path => before[path], StringComparer.Ordinal))
        {
            string[] matching = added.Where(path => StringComparer.Ordinal.Equals(after[path], group.Key)).Order(StringComparer.Ordinal).Take(group.Count()).ToArray();
            if (matching.Length == 0) { continue; }
            foreach (string path in group.Take(matching.Length)) { removed.Remove(path); }
            foreach (string path in matching) { added.Remove(path); }
        }

        int fallbacks = 0;
        foreach (string path in removed) { deleteLines += Lines(before[path]).Length; }
        foreach (string path in added) { addLines += Lines(after[path]).Length; }
        foreach (string path in before.Keys.Intersect(after.Keys, StringComparer.Ordinal))
        {
            (int lineAdds, int lineDeletes, bool fallback) = Diff(before[path], after[path]);
            addLines += lineAdds;
            deleteLines += lineDeletes;
            if (fallback) { fallbacks++; }
        }

        int previousLoc = previous.EligibleLoc;
        double delta = 365d * (addLines + deleteLines) / Math.Max(previousLoc, 100);
        SubjectHazardSampler.ValidateHazard(delta);
        double total = previous.AgeUnits + delta;
        SubjectHazardSampler.ValidateHazard(total);
        return new(addLines, deleteLines, previousLoc, delta, total, false, false, fallbacks);
    }

    internal static bool IsValid(ProjectActivityState state) =>
        !string.IsNullOrWhiteSpace(state.ProjectId) && double.IsFinite(state.AgeUnits) && state.AgeUnits >= 0 && state.EligibleLoc >= 0 && state.Sources is not null &&
        state.Sources.All(x => x is not null && x.Path is not null && x.Content is not null) &&
        (state.SnapshotAvailable || (state.Sources.Length == 0 && state.EligibleLoc == 0)) &&
        (!state.SnapshotAvailable || state.Sources.Sum(x => (long)x.Path.Length + x.Content.Length) <= MaximumSnapshotCharacters) &&
        state.Sources.All(x => x.Content.Length <= MaximumFileCharacters) &&
        state.EligibleLoc == state.Sources.Sum(x => Lines(x.Content).Length) &&
        state.Sources.Select(x => x.Path).Distinct(StringComparer.Ordinal).Count() == state.Sources.Length;

    internal static ProjectActivityState Create(string projectId, IReadOnlyDictionary<string, string> sources, double ageUnits = 0, bool retainSnapshot = true)
    {
        ActivitySourceFile[] files = sources.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new ActivitySourceFile(x.Key, Normalize(x.Value))).ToArray();
        long size = files.Sum(x => (long)x.Path.Length + x.Content.Length);
        if (!retainSnapshot || files.Any(x => x.Content.Length > MaximumFileCharacters) || size > MaximumSnapshotCharacters)
        {
            return new(projectId, ageUnits, 0, [], false);
        }
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
    private static (int Added, int Deleted, bool Fallback) Diff(string oldText, string newText)
    {
        int oldCount = CountLines(oldText), newCount = CountLines(newText);
        if (oldCount + (long)newCount > MaximumDiffLines)
        {
            return (newCount, oldCount, true);
        }
        string[] oldLines = Lines(oldText), newLines = Lines(newText);
        int n = oldLines.Length, m = newLines.Length, max = n + m, offset = max + 1;
        if (max == 0) { return (0, 0, false); }
        int[] v = new int[2 * max + 3];
        var trace = new List<int[]>(max + 1);
        int distance = 0;
        bool done = false;
        for (int d = 0; d <= max && !done; d++)
        {
            if ((long)(d + 1) * v.Length > MaximumDiffFrontierCells || d > MaximumDiffDistance)
            {
                return (newLines.Length, oldLines.Length, true);
            }
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
        return (add, delete, false);
    }

    private static int CountLines(string value)
    {
        if (value.Length == 0) { return 0; }
        int count = 1;
        for (int i = 0; i < value.Length; i++) { if (value[i] == '\n') { count++; } }
        return value.EndsWith('\n') ? count - 1 : count;
    }
}
