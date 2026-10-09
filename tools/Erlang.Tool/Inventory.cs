using System.Diagnostics;
using System.Formats.Tar;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Erlang.Compiler;

internal static class Inventory
{
    private const string Commit = "ad05823719d77c8faee87348ea39513d4e2f99c5";
    private sealed record Export(string Name, int Arity, string Status = "Not started");
    private sealed record Module(string Application, string Name, string Source, IReadOnlyList<Export> Exports, IReadOnlyList<string> Behaviours, IReadOnlyList<string> Callbacks, string Classification, string Status, bool RequiresPreprocessing);
    public static async Task Generate(string repo, string output)
    {
        Directory.CreateDirectory(output);
        using var verify = Start(repo, "rev-parse", "OTP-29.1.1^{commit}");
        string actual = (await verify.StandardOutput.ReadToEndAsync()).Trim();
        await verify.WaitForExitAsync();
        if (actual != Commit)
            throw new InvalidOperationException("Baseline tag does not resolve to pinned commit");
        var modules = new List<Module>();
        var unresolved = new List<string>();
        var bifs = new List<Export>();
        using var git = Start(repo, "archive", "--format=tar", Commit, "lib", "erts", "LICENSE.txt", "LICENSES", "AUTHORS");
        Task<string> errors = git.StandardError.ReadToEndAsync();
        using (var reader = new TarReader(git.StandardOutput.BaseStream, leaveOpen: true))
        {
            while (await reader.GetNextEntryAsync() is { } entry)
            {
                if (entry.DataStream is null)
                    continue;
                string path = entry.Name;
                if (path == "LICENSE.txt")
                {
                    using var sr = new StreamReader(entry.DataStream);
                    await File.WriteAllTextAsync(Path.Combine(output, "OTP-LICENSE.txt"), await sr.ReadToEndAsync());
                    continue;
                }
                if (path == "erts/emulator/beam/bif.tab")
                {
                    using var sr = new StreamReader(entry.DataStream);
                    string text = await sr.ReadToEndAsync();
                    foreach (Match m in Regex.Matches(text, @"(?m)^\s*(?:bif|ubif|hbif)\s+([a-z_]+):([a-z_0-9]+)/([0-9]+)"))
                        bifs.Add(new(m.Groups[1].Value + ":" + m.Groups[2].Value, int.Parse(m.Groups[3].Value)));
                    continue;
                }
                if (!path.EndsWith(".erl", StringComparison.Ordinal) || !path.Contains("/src/", StringComparison.Ordinal) || path.Contains("/test/", StringComparison.Ordinal) || path.Contains("/examples/", StringComparison.Ordinal))
                    continue;
                using var sourceReader = new StreamReader(entry.DataStream);
                string source = await sourceReader.ReadToEndAsync();
                string metadata = Mask(source);
                Match name = Regex.Match(metadata, @"-module\s*\(\s*('(?:\\.|[^'])*'|[a-z][\w@]*)\s*\)");
                if (!name.Success)
                {
                    unresolved.Add(path);
                    continue;
                }
                string moduleName = Atom(name.Groups[1].Value);
                var exports = new List<Export>();
                bool macros = false;
                foreach (Match block in Regex.Matches(metadata, @"-export\s*\(\s*\[(.*?)\]\s*\)", RegexOptions.Singleline))
                {
                    macros |= block.Groups[1].Value.Contains('?');
                    foreach (Match m in Regex.Matches(block.Groups[1].Value, @"('(?:\\.|[^'])*'|[a-z][\w@]*)\s*/\s*([0-9]+)"))
                        exports.Add(new(Atom(m.Groups[1].Value), int.Parse(m.Groups[2].Value)));
                }
                string[] behaviours = Regex.Matches(metadata, @"-behaviou?r\s*\(\s*([a-z][\w@]*)\s*\)").Select(m => m.Groups[1].Value).ToArray();
                string[] callbacks = Regex.Matches(metadata, @"-callback\s+('(?:\\.|[^'])*'|[a-z][\w@]*)\s*\(").Select(m => Atom(m.Groups[1].Value)).Distinct().ToArray();
                string app = path.StartsWith("lib/", StringComparison.Ordinal) ? path.Split('/')[1] : "erts";
                string classification = app == "erts" || moduleName is "ets" or "persistent_term" or "code" ? "Requires special runtime infrastructure" : app is "crypto" or "ssl" or "wx" ? "Depends on OS/platform-specific functionality" : "Requires direct port";
                modules.Add(new(app, moduleName, path, exports.Distinct().ToArray(), behaviours, callbacks, classification, "Not started", macros));
            }
        }
        await git.StandardOutput.BaseStream.CopyToAsync(Stream.Null);
        await git.WaitForExitAsync();
        string error = await errors;
        if (git.ExitCode != 0)
            throw new InvalidOperationException($"git archive exited {git.ExitCode}: {error}");
        modules = modules.OrderBy(m => m.Application, StringComparer.Ordinal).ThenBy(m => m.Name, StringComparer.Ordinal).ToList();
        var inventory = new
        {
            Tag = "OTP-29.1.1",
            Commit,
            Scope = "Production .erl sources under lib/**/src and erts/**/src; explicit exports before Erlang preprocessing",
            Modules = modules,
            Bifs = bifs,
            UnresolvedSources = unresolved,
            Limitations = new[] { "Conditional compilation and macro-generated exports require expansion", "Callback names have no parsed type contracts yet", "NIF exports, generated modules, platform contracts and compiler grammar require additional inventories" }
        };
        await File.WriteAllTextAsync(Path.Combine(output, "otp-inventory.json"), JsonSerializer.Serialize(inventory, new JsonSerializerOptions { WriteIndented = true }));
        var doc = new StringBuilder("# OTP source inventory\n\nPinned OTP-29.1.1 (`" + Commit + "`). Generated by the C# inventory tool using `git archive`; the reference checkout is not changed.\n\n");
        doc.Append($"{modules.Count} source modules, {modules.Sum(m => m.Exports.Count)} explicit exported MFAs, {bifs.Count} BIF table entries; {unresolved.Count} unresolved module attributes.\n\n");
        doc.Append("This is a source inventory, not a claim of complete expanded public-contract coverage. Macro/conditional exports, callbacks types, generated/NIF modules and OS contracts remain incomplete. JSON records source paths, behaviours, callback names, classifications, statuses and preprocessing flags. Classifications are provisional port routes, not completed reuse audits.\n\n| Application | Modules | Explicit exports |\n|---|---:|---:|\n");
        foreach (var group in modules.GroupBy(m => m.Application))
            doc.Append($"| {group.Key} | {group.Count()} | {group.Sum(m => m.Exports.Count)} |\n");
        await File.WriteAllTextAsync(Path.Combine(output, "otp-inventory.md"), doc.ToString());
        Console.WriteLine($"Inventory: {modules.Count} modules, {modules.Sum(m => m.Exports.Count)} MFAs, {bifs.Count} BIFs");
    }
    private static string Atom(string source) => Lexer.Scan(source)[0].Text;
    private static Process Start(string repo, params string[] arguments)
    {
        var info = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        info.ArgumentList.Add("-C");
        info.ArgumentList.Add(repo);
        foreach (var a in arguments)
            info.ArgumentList.Add(a);
        return Process.Start(info)!;
    }
    // Mask prose and comments before metadata extraction. Regex is only used for source inventory attributes, never language compilation.
    private static string Mask(string source)
    {
        var output = source.ToCharArray();
        int i = 0;
        while (i < source.Length)
        {
            if (source[i] == '%')
            {
                while (i < source.Length && source[i] != '\n')
                    output[i++] = ' ';
                continue;
            }
            if (source[i] == '\'')
            {
                i++;
                while (i < source.Length)
                {
                    if (source[i] == '\\')
                    {
                        i += 2;
                        continue;
                    }
                    if (source[i++] == '\'')
                        break;
                }
                continue;
            }
            if (source[i] == '"')
            {
                int width = source.AsSpan(i).StartsWith("\"\"\"", StringComparison.Ordinal) ? 3 : 1;
                for (int n = 0; n < width; n++)
                    output[i++] = ' ';
                while (i < source.Length)
                {
                    if (source.AsSpan(i).StartsWith(new string('"', width), StringComparison.Ordinal))
                    {
                        for (int n = 0; n < width; n++)
                            output[i++] = ' ';
                        break;
                    }
                    if (width == 1 && source[i] == '\\')
                    {
                        output[i++] = ' ';
                        if (i < source.Length)
                            output[i++] = ' ';
                    }
                    else
                    {
                        if (source[i] != '\n')
                            output[i] = ' ';
                        i++;
                    }
                }
                continue;
            }
            i++;
        }
        return new string(output);
    }
}
