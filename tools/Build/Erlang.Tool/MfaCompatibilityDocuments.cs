using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class MfaCompatibilityDocuments
{
    private const int PageBytes = 24 * 1024;
    private const string DirectoryName = "api";
    private const string Generated = "Generated from the baseline-bound compatibility ledger. Edit the ledger, then regenerate. Rendering this snapshot does not revalidate current implementation bindings, refresh runtime registration counts or grant compatibility credit.";

    public static async Task Write(MfaCompatibilityLedger ledger, string docs, string output)
    {
        var files = Generate(ledger, docs, output);
        foreach (var file in files)
        {
            string target = Path.Combine(output, file.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            await File.WriteAllTextAsync(target, file.Value, new UTF8Encoding(false));
        }

        // Only prune files owned by the preceding generation, never arbitrary user files.
        string manifestPath = Path.Combine(output, DirectoryName, "generated-files.json");
        if (File.Exists(manifestPath))
        {
            foreach (string previous in JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(manifestPath))!)
            {
                string full = Path.GetFullPath(Path.Combine(output, previous));
                string ownedRoot = Path.GetFullPath(Path.Combine(output, DirectoryName)) + Path.DirectorySeparatorChar;
                if (previous.EndsWith(".md", StringComparison.Ordinal) && full.StartsWith(ownedRoot, StringComparison.OrdinalIgnoreCase) && !files.ContainsKey(previous))
                    File.Delete(full);
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(files.Keys.Order(StringComparer.Ordinal)) + "\n", new UTF8Encoding(false));
    }

    public static Dictionary<string, string> Generate(MfaCompatibilityLedger ledger, string docs, string output)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        var bundles = new Dictionary<string, string>(StringComparer.Ordinal);

        string Link(string from, string to) => string.Join(
            "/",
            Path.GetRelativePath(Path.GetDirectoryName(Path.Combine(output, from))!, to).Replace('\\', '/').Split('/').Select(Uri.EscapeDataString)
        );

        string EvidenceLink(string from, MfaCompatibilityEvidence evidence) => Link(from, Path.Combine(docs, evidence.Path));

        string Header(string title) => "# " + title + "\n\n" + Generated + "\n\n";

        string Normal(string value) => value.Replace("\r\n", "\n");

        foreach (var entry in ledger.Entries)
        {
            string identity = JsonSerializer.Serialize(entry.Evidence);
            if (entry.Evidence.Length == 0 || bundles.ContainsKey(identity))
                continue;

            string id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))).ToLowerInvariant();
            string prefix = DirectoryName + "/evidence/" + id;
            string first = prefix + "-001.md";
            bundles.Add(identity, first);
            var blocks = entry.Evidence.Select(evidence =>
                "- [" + MfaCompatibilityMarkdown.Escape(evidence.Kind) + "](" + EvidenceLink(first, evidence) + ") " +
                "`" + evidence.Sha256 + "`: " + MfaCompatibilityMarkdown.Escape(evidence.Scope) + "\n\n").ToArray();
            AddPages(
                prefix,
                "Evidence " + id[..12],
                blocks,
                files,
                title => Header(title) + "[All modules](../../API_COMPATIBILITY.md)\n\n"
            );
        }

        var root = new StringBuilder(Header("API compatibility"))
            .AppendLine(MfaCompatibilityNames.Method).AppendLine()
            .AppendLine($"Pinned baseline: {ledger.Tag} (`{ledger.Commit}`).").AppendLine()
            .AppendLine($"[Source ledger]({Link(MfaCompatibilityNames.Report, Path.Combine(docs, MfaCompatibilityNames.File))}) retains every evidence path, SHA-256, scope and limit. Identical evidence sequences share a linked record; their order and full contents are preserved.").AppendLine()
            .AppendLine("[Generation and verification](api/README.md).").AppendLine()
            .AppendLine("## Modules").AppendLine()
            .AppendLine("| Module | Commands | 100% compatible | Known differences | Needs verification |")
            .AppendLine("| --- | ---: | ---: | ---: | ---: |");
        foreach (var group in ledger.Entries.GroupBy(entry => entry.Module).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            // Module names have already passed ledger validation, but encode file-system segments independently.
            string module = Uri.EscapeDataString(group.Key);
            string index = DirectoryName + "/" + module + "/index.md";
            root.AppendLine($"| [{group.Key}]({index}) | {group.Count()} | {group.Count(e => e.Status == MfaCompatibilityNames.Verified)} | {group.Count(e => e.Status == MfaCompatibilityNames.Different)} | {group.Count(e => e.Status == MfaCompatibilityNames.Pending)} |");
            var overview = new StringBuilder(Header(group.Key + " API compatibility"))
                .AppendLine("[All modules](../../API_COMPATIBILITY.md)").AppendLine();
            foreach (var topic in group.GroupBy(MfaCompatibilityTopics.For).OrderBy(topic => topic.Key, StringComparer.Ordinal))
            {
                string prefix = DirectoryName + "/" + module + "/" + topic.Key;
                var rows = topic.Select(entry =>
                {
                    string proof = entry.Evidence.Length == 0 ? MfaCompatibilityNames.PendingAudit :
                        "[Full evidence](" + Link(prefix + "-001.md", Path.Combine(output, bundles[JsonSerializer.Serialize(entry.Evidence)])) + ")";

                    return $"| {MfaCompatibilityMarkdown.Code(entry.Key)} | {MfaCompatibilityMarkdown.Escape(MfaCompatibilityNames.DisplayStatus(entry.Status))} | {MfaCompatibilityMarkdown.Escape(entry.Scope)} | {proof} | {MfaCompatibilityMarkdown.Escape(string.Join("; ", entry.Limits))} |\n";
                }).ToArray();
                string table = MfaCompatibilityNames.TableHeader + "\n" + MfaCompatibilityNames.TableSeparator + "\n";
                var pages = AddPages(
                    prefix,
                    group.Key + ": " + topic.Key,
                    rows,
                    files,
                    title => Header(title) + "[Module index](index.md) · [All modules](../../API_COMPATIBILITY.md)\n\n" + table
                );
                foreach (string page in pages)
                    overview.AppendLine($"- [{topic.Key} — {Path.GetFileNameWithoutExtension(page).Split('-').Last()}]({Path.GetFileName(page)})");
            }

            files.Add(index, Normal(overview.ToString()));
        }

        // Preserve the old report's status and module anchors as navigation entry points.
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string status in new[] { MfaCompatibilityNames.Verified, MfaCompatibilityNames.Different, MfaCompatibilityNames.Pending })
        {
            root.AppendLine().AppendLine("## " + MfaCompatibilityNames.DisplayStatus(status)).AppendLine();
            foreach (string module in ledger.Entries.Where(entry => entry.Status == status).Select(entry => entry.Module).Distinct(StringComparer.Ordinal))
            {
                int count = counts.GetValueOrDefault(module);
                counts[module] = count + 1;
                string anchor = module + (count == 0 ? "" : "-" + count);
                root.AppendLine($"<a id=\"{anchor}\"></a>").AppendLine()
                    .AppendLine($"- [{module}]({DirectoryName}/{Uri.EscapeDataString(module)}/index.md): {ledger.Entries.Count(entry => entry.Module == module && entry.Status == status)} commands.");
            }
        }

        files.Add(MfaCompatibilityNames.Report, Normal(root.ToString()));
        files.Add(
            DirectoryName + "/README.md",
            Header("API documentation maintenance") +
            "[All modules](../API_COMPATIBILITY.md)\n\n" +
            "Module pages contain complete command scopes and limits. The erlang module is divided by runtime topic. Tables and full evidence sequences are paginated at paragraph/row boundaries; identical evidence sequences share content-addressed records. Every evidence kind, path, SHA-256, scope and sequence position is retained.\n\n" +
            "## Regeneration\n\nRun from the repository root:\n\n```powershell\n" +
            "dotnet run --project tools/Build/Erlang.Tool -- api-compatibility docs docs\n" +
            "node tools/Validation/check-api-documentation.cjs\n```\n\n" +
            "The documentation-only command validates ledger structure, baseline and all evidence file hashes. It preserves the ledger snapshot, including recorded source bindings, without checking those bindings against a changing worktree or recalculating runtime registration. The readiness command still validates actual runtime registration and current implementation source bindings before producing readiness reports; its validation is unchanged.\n\n" +
            "The checker compares every generated command, status, scope, limit and ordered evidence item with the ledger, verifies evidence bytes and checks local paths and anchors. Its report is written to artifacts/api-doc-split/verification.json. Output directories outside docs are supported; evidence links resolve to the source docs directory.\n\n" +
            "Generated files are listed in api/generated-files.json. Regeneration removes only previously listed, obsolete Markdown pages under api/; it preserves unlisted files. Historical evidence and the ledger are never rewritten by rendering.\n"
        );

        return files;
    }

    private static string[] AddPages(
        string prefix,
        string title,
        string[] blocks,
        Dictionary<string, string> files,
        Func<string, string> header
    )
    {
        var pages = new List<string>();
        var bodies = new List<string>();
        var body = new StringBuilder();
        foreach (string block in blocks)
        {
            if (body.Length > 0 && Encoding.UTF8.GetByteCount(body.ToString() + block) > PageBytes - 2048)
            {
                bodies.Add(body.ToString());
                body.Clear();
            }

            body.Append(block);
        }

        if (body.Length > 0)
            bodies.Add(body.ToString());
        for (int i = 0; i < bodies.Count; i++)
        {
            string name = prefix + "-" + (i + 1).ToString("D3") + ".md";
            pages.Add(name);
            string navigation = "\n";
            if (i > 0)
                navigation += $"[Previous]({Path.GetFileName(prefix)}-{i:D3}.md) ";
            if (i + 1 < bodies.Count)
                navigation += $"[Next]({Path.GetFileName(prefix)}-{i + 2:D3}.md)";
            files.Add(name, (header(title + " — " + (i + 1)) + bodies[i] + navigation + "\n").Replace("\r\n", "\n"));
        }

        return pages.ToArray();
    }
}
