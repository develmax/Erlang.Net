using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class NativeInfrastructureReport
{
    internal static bool Write(
        string docs,
        string output,
        string tag,
        string commit
    )
    {
        string input = Path.Combine(docs, NativeInfrastructureNames.File);
        if (!File.Exists(input))
            return false;
        byte[] bytes = File.ReadAllBytes(input);
        using var document = JsonDocument.Parse(bytes);
        JsonElement root = document.RootElement;
        JsonElement[] entries = root.GetProperty(NativeInfrastructureNames.Entries).EnumerateArray().ToArray();
        if (root.GetProperty(NativeInfrastructureNames.SchemaVersion).GetInt32() != NativeInfrastructureNames.Schema
            || Text(root, NativeInfrastructureNames.InventoryVersion) != NativeInfrastructureNames.Version
            || entries.Length != NativeInfrastructureNames.FixedRows
            || Text(root.GetProperty(NativeInfrastructureNames.PinnedOtp), NativeInfrastructureNames.Tag) != tag
            || Text(root.GetProperty(NativeInfrastructureNames.PinnedOtp), NativeInfrastructureNames.Commit) != commit)
            throw new InvalidOperationException(NativeInfrastructureNames.Invalid);
        string[] ids = entries.Select(entry => Text(entry, NativeInfrastructureNames.Id)).ToArray();
        string roster = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', ids.Order(StringComparer.Ordinal)) + "\n")));
        if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct(StringComparer.Ordinal).Count() != entries.Length
            || roster != NativeInfrastructureNames.Roster || Text(root, NativeInfrastructureNames.InventoryIdsSha256) != roster)
            throw new InvalidOperationException(NativeInfrastructureNames.Invalid);
        string repository = Directory.GetParent(Path.GetFullPath(docs))!.FullName;
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        JsonElement evidence = root.GetProperty(NativeInfrastructureNames.Evidence);
        foreach (var item in evidence.EnumerateObject())
            ValidateHash(repository, item.Value);
        foreach (JsonElement entry in entries)
        {
            if (string.IsNullOrWhiteSpace(Text(entry, NativeInfrastructureNames.Scope))
                || string.IsNullOrWhiteSpace(Text(entry, NativeInfrastructureNames.DoneCriterion))
                || Text(entry, NativeInfrastructureNames.Implementation) is not (NativeInfrastructureNames.Implemented or NativeInfrastructureNames.Partial or NativeInfrastructureNames.NotImplemented)
                || Text(entry, NativeInfrastructureNames.Verification) is not (NativeInfrastructureNames.Accepted or NativeInfrastructureNames.Local or NativeInfrastructureNames.SourceOnly or NativeInfrastructureNames.Missing))
                throw new InvalidOperationException(NativeInfrastructureNames.Invalid);
            if (entry.GetProperty(NativeInfrastructureNames.ScopedMilestoneMet).GetBoolean()
                && (Text(entry, NativeInfrastructureNames.Verification) != NativeInfrastructureNames.Accepted
                    || Text(entry, NativeInfrastructureNames.Implementation) != NativeInfrastructureNames.Implemented
                    || !entry.GetProperty(NativeInfrastructureNames.InMain).GetBoolean()))
                throw new InvalidOperationException(NativeInfrastructureNames.Invalid);
            foreach (var reference in entry.GetProperty(NativeInfrastructureNames.EvidenceRefs).EnumerateArray())
                if (!evidence.TryGetProperty(reference.GetString()!, out _))
                    throw new InvalidOperationException(NativeInfrastructureNames.Invalid);
            foreach (JsonElement source in entry.GetProperty(NativeInfrastructureNames.SourceBindings).EnumerateArray())
            {
                string path = Text(source, NativeInfrastructureNames.Path);
                string hash = Text(source, NativeInfrastructureNames.Sha256);
                if (sources.TryGetValue(path, out string? previous) && previous != hash)
                    throw new InvalidOperationException(NativeInfrastructureNames.Invalid);
                sources[path] = hash;
                ValidateHash(repository, source);
            }
        }
        JsonElement counts = root.GetProperty(NativeInfrastructureNames.Counts);
        if (counts.GetProperty(NativeInfrastructureNames.InventoryRows).GetInt32() != entries.Length
            || counts.GetProperty(NativeInfrastructureNames.InMain).GetInt32() != entries.Count(entry => entry.GetProperty(NativeInfrastructureNames.InMain).GetBoolean())
            || counts.GetProperty(NativeInfrastructureNames.ScopedMilestonesMet).GetInt32() != entries.Count(entry => entry.GetProperty(NativeInfrastructureNames.ScopedMilestoneMet).GetBoolean())
            || root.GetProperty(NativeInfrastructureNames.SourceEquivalence).GetProperty(NativeInfrastructureNames.CurrentBoundSourceFiles).GetInt32() != sources.Count)
            throw new InvalidOperationException(NativeInfrastructureNames.Invalid);
        foreach (string category in new[] { NativeInfrastructureNames.Implementation, NativeInfrastructureNames.Verification })
        {
            var actual = entries.GroupBy(entry => Text(entry, category)).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var expected = counts.GetProperty(category).EnumerateObject().ToDictionary(property => property.Name, property => property.Value.GetInt32(), StringComparer.Ordinal);
            if (actual.Count != expected.Count || actual.Any(pair => expected.GetValueOrDefault(pair.Key) != pair.Value))
                throw new InvalidOperationException(NativeInfrastructureNames.Invalid);
        }
        int met = counts.GetProperty(NativeInfrastructureNames.ScopedMilestonesMet).GetInt32();
        var markdown = new StringBuilder().AppendLine(NativeInfrastructureNames.Heading).AppendLine()
            .AppendLine(NativeInfrastructureNames.Method).AppendLine()
            .AppendLine($"Inventory `{NativeInfrastructureNames.Version}`: **{entries.Length} units**, **{counts.GetProperty(NativeInfrastructureNames.InMain).GetInt32()} with code in Main**, **{met} scoped milestones met**, **{entries.Length - met} open**.").AppendLine()
            .AppendLine(NativeInfrastructureNames.TableHeader).AppendLine(NativeInfrastructureNames.TableSeparator);
        foreach (JsonElement entry in entries)
        {
            string[] cells = [
                Text(entry, NativeInfrastructureNames.Id) + ": " + Text(entry, NativeInfrastructureNames.Title),
                Text(entry, NativeInfrastructureNames.Implementation),
                Text(entry, NativeInfrastructureNames.Verification),
                entry.GetProperty(NativeInfrastructureNames.InMain).GetBoolean() ? Text(entry, NativeInfrastructureNames.MainCommit) : bool.FalseString,
                Text(entry, NativeInfrastructureNames.DoneCriterion),
                Join(entry, NativeInfrastructureNames.RemainingOrBlockers),
                Join(entry, NativeInfrastructureNames.Platforms) + "; " + Join(entry, NativeInfrastructureNames.EvidenceRefs)
            ];
            markdown.AppendLine("| " + string.Join(" | ", cells.Select(MfaCompatibilityMarkdown.Escape)) + " |");
        }
        markdown.AppendLine().AppendLine($"Source/evidence hashes, exclusions and criteria: [{NativeInfrastructureNames.File}]({NativeInfrastructureNames.File}).");
        Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, NativeInfrastructureNames.File), bytes);
        File.WriteAllText(
            Path.Combine(output, NativeInfrastructureNames.Markdown),
            markdown.ToString().Replace("\r\n", "\n"),
            new UTF8Encoding(false)
        );

        return true;
    }

    private static string Text(JsonElement value, string property) => value.GetProperty(property).GetString() ?? string.Empty;

    private static string Join(JsonElement value, string property) => string.Join("; ", value.GetProperty(property).EnumerateArray().Select(item => item.GetString()));

    private static void ValidateHash(string repository, JsonElement value)
    {
        string path = Path.GetFullPath(Path.Combine(repository, Text(value, NativeInfrastructureNames.Path)));
        string relative = Path.GetRelativePath(repository, path);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)
            || Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) != Text(value, NativeInfrastructureNames.Sha256))
            throw new InvalidOperationException(NativeInfrastructureNames.Invalid);
    }
}
