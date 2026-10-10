using System.Security.Cryptography;
using System.Text;

namespace DotNetAiCodeHygiene.Core.Sampling;

internal sealed record ActivitySourceFile(string Path, string? Content, string? Fingerprint = null, int? LineCount = null, int? CharacterCount = null);
internal sealed record ProjectActivityState(string ProjectId, double AgeUnits, int EligibleLoc, ActivitySourceFile[] Sources, bool SnapshotAvailable = true);
internal sealed record ActivityTransition(int AddedLines, int DeletedLines, int PreviousLoc, double DeltaAgeUnits, double TotalAgeUnits, bool Initialized, bool Reset, int DiffFallbacks = 0);

/// <summary>Versioned, repository-local source-transition accounting. It owns no clock or Git history.</summary>
internal static class ProjectActivity
{
    internal const int StateVersion = 3;
    internal const int MaximumSnapshotCharacters = 512 * 1024;
    internal const int MaximumRepositorySnapshotCharacters = 1024 * 1024;
    internal const int MaximumFileCharacters = 128 * 1024;
    private const int MaximumDiffLines = 12_000;
    private const int MaximumDiffFrontierCells = 2_000_000;
    private const int MaximumDiffDistance = 1_024;

    internal static ActivityTransition Observe(ProjectActivityState? previous, IReadOnlyDictionary<string, string> current)
    {
        ProjectActivityState observed = Create(previous?.ProjectId ?? string.Empty, current, previous?.AgeUnits ?? 0);
        if (previous is null) { return new(0, 0, 0, 0, 0, true, false); }
        if (!previous.SnapshotAvailable) { return new(0, 0, 0, 0, previous.AgeUnits, true, true); }

        var before = previous.Sources.ToDictionary(x => x.Path, EnsureMetadata, StringComparer.Ordinal);
        var after = observed.Sources.ToDictionary(x => x.Path, EnsureMetadata, StringComparer.Ordinal);
        var removed = before.Keys.Except(after.Keys, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var added = after.Keys.Except(before.Keys, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        int addLines = 0, deleteLines = 0, fallbacks = 0;

        // A matching content fingerprint is reliable evidence that a path move
        // preserves source content, even when the bounded snapshot omitted text.
        foreach (var group in removed.GroupBy(path => before[path].Fingerprint, StringComparer.Ordinal))
        {
            string[] matching = added.Where(path => StringComparer.Ordinal.Equals(after[path].Fingerprint, group.Key))
                .Order(StringComparer.Ordinal).Take(group.Count()).ToArray();
            foreach (string path in group.Take(matching.Length)) { removed.Remove(path); }
            foreach (string path in matching) { added.Remove(path); }
        }

        foreach (string path in removed) { deleteLines += before[path].LineCount!.Value; }
        foreach (string path in added) { addLines += after[path].LineCount!.Value; }
        foreach (string path in before.Keys.Intersect(after.Keys, StringComparer.Ordinal))
        {
            ActivitySourceFile oldFile = before[path], newFile = after[path];
            if (StringComparer.Ordinal.Equals(oldFile.Fingerprint, newFile.Fingerprint)) { continue; }
            if (oldFile.Content is not null && newFile.Content is not null)
            {
                (int lineAdds, int lineDeletes, bool fallback) = Diff(oldFile.Content, newFile.Content);
                addLines += lineAdds;
                deleteLines += lineDeletes;
                if (fallback) { fallbacks++; }
            }
            else
            {
                // Metadata-only files have no comparable text. Treat a changed
                // fingerprint as a complete file replacement, bounded by LOC.
                addLines += newFile.LineCount!.Value;
                deleteLines += oldFile.LineCount!.Value;
                fallbacks++;
            }
        }

        int previousLoc = previous.EligibleLoc;
        double delta = 365d * (addLines + deleteLines) / Math.Max(previousLoc, 100);
        SubjectHazardSampler.ValidateHazard(delta);
        double total = previous.AgeUnits + delta;
        SubjectHazardSampler.ValidateHazard(total);
        return new(addLines, deleteLines, previousLoc, delta, total, false, false, fallbacks);
    }

    internal static ProjectActivityState Create(string projectId, IReadOnlyDictionary<string, string> sources, double ageUnits = 0)
    {
        var files = new List<ActivitySourceFile>(sources.Count);
        long projectCharacters = 0;
        int eligibleLoc = 0;
        foreach (KeyValuePair<string, string> source in sources.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            string content = Normalize(source.Value);
            int lines = CountLines(content);
            eligibleLoc = checked(eligibleLoc + lines);
            string fingerprint = Fingerprint(content);
            projectCharacters += source.Key.Length + (long)content.Length;
            bool retainContent = content.Length <= MaximumFileCharacters && projectCharacters <= MaximumSnapshotCharacters;
            files.Add(new(source.Key, retainContent ? content : null, fingerprint, lines, content.Length));
        }
        return new(projectId, ageUnits, eligibleLoc, files.ToArray());
    }

    internal static bool IsValid(ProjectActivityState state) =>
        !string.IsNullOrWhiteSpace(state.ProjectId) && double.IsFinite(state.AgeUnits) && state.AgeUnits >= 0 && state.EligibleLoc >= 0 && state.Sources is not null &&
        state.Sources.All(x => x is not null && x.Path is not null &&
            (x.Fingerprint is null || IsFingerprint(x.Fingerprint)) && (x.LineCount is null || x.LineCount >= 0) && (x.CharacterCount is null || x.CharacterCount >= 0) &&
            (x.Content is null || x.CharacterCount is null || x.CharacterCount == x.Content.Length)) &&
        (state.SnapshotAvailable || (state.Sources.Length == 0 && state.EligibleLoc == 0)) &&
        state.EligibleLoc == state.Sources.Sum(x => x.LineCount ?? (x.Content is null ? 0 : CountLines(Normalize(x.Content)))) &&
        state.Sources.Select(x => x.Path).Distinct(StringComparer.Ordinal).Count() == state.Sources.Length;

    internal static bool HasCompleteMetadata(ProjectActivityState state) => state.Sources.All(x => x.Fingerprint is not null && x.LineCount is not null && x.CharacterCount is not null);

    internal static ProjectActivityState Upgrade(ProjectActivityState state)
    {
        if (!state.SnapshotAvailable) { return state; }
        if (HasCompleteMetadata(state)) { return state; }
        if (state.Sources.All(x => x.Content is not null))
        {
            var sources = state.Sources.ToDictionary(x => x.Path, x => x.Content!, StringComparer.Ordinal);
            return Create(state.ProjectId, sources, state.AgeUnits);
        }
        ActivitySourceFile[] migrated = state.Sources.Select(source =>
        {
            if (source.Content is not null)
            {
                string content = Normalize(source.Content);
                return source with { Content = content, Fingerprint = Fingerprint(content), LineCount = CountLines(content), CharacterCount = content.Length };
            }
            // Older metadata-only snapshots did not retain character counts.
            // Use the file ceiling as a conservative repository-budget estimate.
            return source.Fingerprint is not null && source.LineCount is not null
                ? source with { CharacterCount = source.CharacterCount ?? MaximumFileCharacters + 1 }
                : source with { Fingerprint = string.Empty, LineCount = -1 };
        }).ToArray();
        if (migrated.Any(source => source.Fingerprint == string.Empty || source.LineCount < 0))
        {
            return state with { Sources = [], EligibleLoc = 0, SnapshotAvailable = false };
        }
        return state with { Sources = migrated };
    }

    internal static ProjectActivityState[] CompactRepositorySnapshots(ProjectActivityState[] projects)
    {
        long size = projects.Sum(project => project.Sources.Sum(source => source.Path.Length + (long)(source.CharacterCount ?? source.Content?.Length ?? 0)));
        if (size <= MaximumRepositorySnapshotCharacters) { return projects; }
        return projects.Select(project => project with
        {
            Sources = project.Sources.Select(source => source with { Content = null }).ToArray()
        }).ToArray();
    }

    private static ActivitySourceFile EnsureMetadata(ActivitySourceFile source)
    {
        if (source.Fingerprint is not null && source.LineCount is not null) { return source; }
        if (source.Content is null) { throw new InvalidOperationException("Activity state lacks source metadata."); }
        string content = Normalize(source.Content);
        return source with { Content = content, Fingerprint = Fingerprint(content), LineCount = CountLines(content), CharacterCount = content.Length };
    }

    private static bool IsFingerprint(string value) => value.Length == 64 && value.All(Uri.IsHexDigit) && value == value.ToUpperInvariant();
    private static string Normalize(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    private static string Fingerprint(string value)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Encoder encoder = Encoding.UTF8.GetEncoder();
        Span<byte> buffer = stackalloc byte[4096];
        int offset = 0;
        while (offset < value.Length)
        {
            encoder.Convert(value.AsSpan(offset), buffer, false, out int charsUsed, out int bytesUsed, out _);
            hash.AppendData(buffer[..bytesUsed]);
            offset += charsUsed;
        }
        encoder.Convert(ReadOnlySpan<char>.Empty, buffer, true, out _, out int finalBytes, out _);
        hash.AppendData(buffer[..finalBytes]);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static int CountLines(string value)
    {
        if (value.Length == 0) { return 0; }
        int count = 1;
        for (int i = 0; i < value.Length; i++) { if (value[i] == '\n') { count++; } }
        return value.EndsWith('\n') ? count - 1 : count;
    }

    // Myers shortest-edit script with a fixed frontier memory and edit-work cap.
    private static (int Added, int Deleted, bool Fallback) Diff(string oldText, string newText)
    {
        int oldCount = CountLines(oldText), newCount = CountLines(newText);
        if (oldCount + (long)newCount > MaximumDiffLines) { return (newCount, oldCount, true); }
        string[] oldLines = Lines(oldText), newLines = Lines(newText);
        int n = oldLines.Length, m = newLines.Length, max = n + m, offset = max + 1;
        if (max == 0) { return (0, 0, false); }
        int[] v = new int[2 * max + 3];
        var trace = new List<int[]>(max + 1);
        int distance = 0;
        bool done = false;
        for (int d = 0; d <= max && !done; d++)
        {
            if ((long)(d + 1) * v.Length > MaximumDiffFrontierCells || d > MaximumDiffDistance) { return (newLines.Length, oldLines.Length, true); }
            trace.Add((int[])v.Clone());
            for (int k = -d; k <= d; k += 2)
            {
                int x = k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1]) ? v[offset + k + 1] : v[offset + k - 1] + 1;
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

    private static string[] Lines(string value)
    {
        if (value.Length == 0) { return []; }
        string[] lines = value.Split('\n');
        return value.EndsWith('\n') ? lines[..^1] : lines;
    }
}
