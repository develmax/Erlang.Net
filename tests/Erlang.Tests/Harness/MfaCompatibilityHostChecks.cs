using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Erlang.Otp;

namespace Erlang.Tests;

public static class MfaCompatibilityHostChecks
{
    public static async Task Run()
    {
        string temp = Path.GetFullPath(Path.GetTempPath());
        string root = Path.GetFullPath(Path.Combine(temp, "erlang-readiness-" + Guid.NewGuid().ToString("N")));
        Require(root.StartsWith(
            temp + (temp.EndsWith(Path.DirectorySeparatorChar) ? "" : Path.DirectorySeparatorChar.ToString()),
            StringComparison.OrdinalIgnoreCase
        ));
        Directory.CreateDirectory(root);
        try
        {
            CheckAdmission(root);
            await CheckGenerator(root);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static MfaCompatibilityEntry Pending(string module, string function, int arity) => new()
    {
        Module = module,
        Function = function,
        Arity = arity,
        Status = MfaCompatibilityNames.Pending,
        Scope = MfaCompatibilityHostNames.Scope,
        Evidence = [],
        Limits = [MfaCompatibilityHostNames.PendingLimit]
    };

    private static MfaCompatibilityLedger Ledger(MfaCompatibilityEntry[] entries) => new()
    {
        SchemaVersion = MfaCompatibilityNames.SchemaVersion,
        Tag = MfaCompatibilityHostNames.Tag,
        Commit = MfaCompatibilityHostNames.Commit,
        VerifiedSourceFiles = [],
        Entries = entries
    };

    private static void CheckAdmission(string root)
    {
        string docs = Path.Combine(root, "admission");
        Directory.CreateDirectory(docs);
        var entry = Pending("erlang", "self", 0);
        var ledger = Ledger([entry]);
        var registered = new HashSet<string>(StringComparer.Ordinal) { entry.Key };

        void Write(MfaCompatibilityLedger value) => File.WriteAllText(Path.Combine(docs, MfaCompatibilityNames.File), JsonSerializer.Serialize(value));

        void Load() => MfaCompatibility.Load(
            docs,
            MfaCompatibilityHostNames.Tag,
            MfaCompatibilityHostNames.Commit,
            registered
        );

        void Reject(MfaCompatibilityLedger value)
        {
            Write(value);
            MustReject(Load);
        }
        Write(ledger);
        Load();
        Reject(ledger with { Commit = new string('0', 40) });
        Reject(ledger with { Tag = "other" });
        Reject(ledger with { SchemaVersion = 0 });
        Reject(ledger with { Entries = [entry, entry] });
        Reject(ledger with { Entries = [] });
        Reject(ledger with { Entries = [entry with { Function = "unknown" }] });
        Reject(ledger with { Entries = [entry with { Status = "Implemented" }] });
        Reject(ledger with { Entries = [entry with { Scope = "" }] });
        Reject(ledger with { Entries = [entry with { Limits = [] }] });
        Reject(ledger with { Entries = [entry with { Status = MfaCompatibilityNames.Verified, Limits = [] }] });
        foreach (string missing in new[] { "Status", "Evidence", "Arity" })
        {
            JsonNode node = JsonNode.Parse(JsonSerializer.Serialize(ledger))!;
            node["Entries"]![0]!.AsObject().Remove(missing);
            File.WriteAllText(Path.Combine(docs, MfaCompatibilityNames.File), node.ToJsonString());
            MustReject(Load);
        }
        File.WriteAllText(Path.Combine(docs, MfaCompatibilityHostNames.OriginalFile), MfaCompatibilityHostNames.Scope);
        File.WriteAllText(Path.Combine(docs, MfaCompatibilityHostNames.AuditFile), MfaCompatibilityHostNames.Scope);

        MfaCompatibilityEvidence Proof(string kind, string path) => new()
        {
            Kind = kind,
            Path = path,
            Scope = MfaCompatibilityHostNames.Scope,
            Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(docs, path))))
        };
        var original = Proof(MfaCompatibilityNames.OriginalComparison, MfaCompatibilityHostNames.OriginalFile);
        var audit = Proof(MfaCompatibilityNames.Audit, MfaCompatibilityHostNames.AuditFile);
        string sourcePath = Path.Combine(root, "src", "readiness-admission-source.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        File.WriteAllText(sourcePath, MfaCompatibilityHostNames.Scope);
        var source = new MfaCompatibilitySourceFile
        {
            Path = "src/readiness-admission-source.cs",
            Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath)))
        };
        ledger = ledger with { VerifiedSourceFiles = [source] };
        var verified = entry with { Status = MfaCompatibilityNames.Verified, Limits = [], Evidence = [original, audit] };
        Write(ledger with { Entries = [verified] });
        Load();
        MfaCompatibility.Load(
            docs + Path.DirectorySeparatorChar,
            MfaCompatibilityHostNames.Tag,
            MfaCompatibilityHostNames.Commit,
            registered
        );
        Reject(ledger with { Entries = [verified], VerifiedSourceFiles = [] });
        File.WriteAllText(sourcePath, MfaCompatibilityNames.Pending);
        Reject(ledger with { Entries = [verified] });
        File.WriteAllText(sourcePath, MfaCompatibilityHostNames.Scope);
        Reject(ledger with { Entries = [verified with { Limits = [MfaCompatibilityHostNames.PendingLimit] }] });
        Reject(ledger with { Entries = [verified with { Evidence = [original] }] });
        Reject(ledger with { Entries = [verified with { Evidence = [original with { Kind = MfaCompatibilityNames.LocalValidation }, audit] }] });
        Reject(ledger with { Entries = [verified with { Evidence = [original with { Kind = "mfa/erlang:self/0" }, audit] }] });
        Reject(ledger with { Entries = [verified with { Evidence = [original with { Path = "../original-proof.txt" }, audit] }] });
        Reject(ledger with { Entries = [verified with { Evidence = [original with { Path = "absent.txt" }, audit] }] });
        Reject(ledger with { Entries = [verified with { Evidence = [original with { Sha256 = new string('0', 64) }, audit] }] });
        Reject(ledger with { Entries = [entry with { Status = MfaCompatibilityNames.Different }] });
        Write(ledger with { Entries = [entry with { Status = MfaCompatibilityNames.Different, Evidence = [audit] }] });
        Load();
    }

    private static async Task CheckGenerator(string root)
    {
        string docs = Path.Combine(root, "docs");
        string output = Path.Combine(root, "output");
        Directory.CreateDirectory(docs);
        var runtime = new ModuleRegistry();
        CoreModules.Register(runtime);
        OtpModules.Register(runtime);
        var exports = runtime.Exports.ToArray();
        var ledger = Ledger(exports.Select(value => Pending(value.Module, value.Function, value.Arity)).ToArray());

        void Write(string path, object value) => File.WriteAllText(path, JsonSerializer.Serialize(value));
        Write(Path.Combine(root, ReadinessText.BaselineFile), new { MfaCompatibilityHostNames.Tag, MfaCompatibilityHostNames.Commit });
        Write(
            Path.Combine(docs, ReadinessText.InventoryFile),
            new
            {
                MfaCompatibilityHostNames.Tag,
                MfaCompatibilityHostNames.Commit,
                Modules = Array.Empty<object>(),
                Bifs = exports.Select(value => new { Name = value.Module + ":" + value.Function, value.Arity }).ToArray()
            }
        );
        Write(
            Path.Combine(docs, ReadinessText.RegistryFile),
            exports.Select(value => new
            {
                value.Module,
                value.Function,
                value.Arity,
                Evidence = ReadinessText.MfaTestPrefix + value.Module + ":" + value.Function + "/" + value.Arity
            }).ToArray()
        );
        var tests = new
        {
            Passed = exports.Length,
            Failed = 0,
            Tests = exports.Select(value => new { Name = ReadinessText.MfaTestPrefix + value.Module + ":" + value.Function + "/" + value.Arity, Status = ReadinessText.Passed }).ToArray()
        };
        string testPath = Path.Combine(root, "tests.json");
        Write(testPath, tests);
        Write(Path.Combine(docs, "previous-tests.json"), tests);
        Write(
            Path.Combine(docs, ReadinessText.ScopeFile),
            new { ComparisonTestReport = "previous-tests.json", Components = Array.Empty<object>(), FocusModules = Array.Empty<string>() }
        );
        Write(Path.Combine(docs, MfaCompatibilityNames.File), ledger);
        await Readiness.Generate(docs, testPath, output);
        using var report = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, ReadinessText.ReportJson)));
        var data = report.RootElement;
        Require(data.GetProperty("RegisteredMfas").GetInt32() == exports.Length);
        Require(data.GetProperty("VerifiedCompatibleMfas").GetInt32() == 0);
        Require(data.GetProperty("NeedsVerificationMfas").GetInt32() == exports.Length);
        Require(data.GetProperty("KnownDifferenceMfas").GetInt32() == 0);
        Require(data.GetProperty("MfaCompatibility").EnumerateArray().Select(value => value.GetProperty("Key").GetString()).SequenceEqual(ledger.Entries.Select(value => value.Key)));
        foreach (var module in data.GetProperty("Modules").EnumerateArray())
            Require(module.GetProperty("RegisteredMfas").GetInt32() == module.GetProperty("NeedsVerificationMfas").GetInt32());
        string summary = File.ReadAllText(Path.Combine(output, ReadinessText.ReportMarkdown));
        Require(summary.IndexOf(ReadinessText.ModuleHeading, StringComparison.Ordinal) < summary.IndexOf(ReadinessText.ComponentHeading, StringComparison.Ordinal));
        string commands = File.ReadAllText(Path.Combine(output, MfaCompatibilityNames.Report));
        Require(ledger.Entries.All(value => commands.Contains(value.Key, StringComparison.Ordinal)));
        foreach (string command in new[] { "erlang:</2", "erlang:>/2", "erlang:=</2", "erlang:>=/2" })
            Require(commands.Contains("| `" + command + "` |", StringComparison.Ordinal));
        Require(MfaCompatibilityMarkdown.Code("`") == "`` ` ``");
        CheckIndex(output, ledger);
        await CheckMixedIndex(
            root,
            docs,
            testPath,
            ledger
        );
        Write(Path.Combine(docs, MfaCompatibilityNames.File), ledger with { Commit = new string('0', 40) });
        string invalidOutput = Path.Combine(root, "invalid-output");
        bool rejected = false;
        try
        {
            await Readiness.Generate(docs, testPath, invalidOutput);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }
        Require(rejected && !Directory.Exists(invalidOutput));
    }

    private static async Task CheckMixedIndex(
        string root,
        string docs,
        string tests,
        MfaCompatibilityLedger ledger
    )
    {
        Require(ledger.Entries.Length >= 3);
        foreach (string proof in new[] { MfaCompatibilityHostNames.OriginalFile, MfaCompatibilityHostNames.AuditFile })
            File.WriteAllText(Path.Combine(docs, proof), MfaCompatibilityHostNames.Scope);
        string source = Path.Combine(root, MfaCompatibilityHostNames.IndexSourcePath);
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        File.WriteAllText(source, MfaCompatibilityHostNames.Scope);

        MfaCompatibilityEvidence Proof(string kind, string name) => new()
        {
            Kind = kind,
            Path = name,
            Scope = MfaCompatibilityHostNames.Scope,
            Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(docs, name))))
        };
        var original = Proof(MfaCompatibilityNames.OriginalComparison, MfaCompatibilityHostNames.OriginalFile);
        var audit = Proof(MfaCompatibilityNames.Audit, MfaCompatibilityHostNames.AuditFile);
        MfaCompatibilityEntry[] entries = ledger.Entries.Select((entry, index) => index switch
        {
            0 => entry with { Status = MfaCompatibilityNames.Verified, Limits = [], Evidence = [original, audit] },
            1 => entry with { Status = MfaCompatibilityNames.Different, Evidence = [audit] },
            _ => entry
        }).ToArray();
        var mixed = ledger with
        {
            Entries = entries,
            VerifiedSourceFiles = [new MfaCompatibilitySourceFile
            {
                Path = MfaCompatibilityHostNames.IndexSourcePath,
                Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source)))
            }]
        };
        string ledgerPath = Path.Combine(docs, MfaCompatibilityNames.File);
        File.WriteAllText(ledgerPath, JsonSerializer.Serialize(mixed));
        byte[] input = File.ReadAllBytes(ledgerPath);
        string output = Path.Combine(root, MfaCompatibilityHostNames.MixedIndexOutput);
        Require(!Directory.Exists(output));
        await Readiness.Generate(docs, tests, output);
        Require(input.AsSpan().SequenceEqual(File.ReadAllBytes(ledgerPath)));
        CheckIndex(output, mixed);
        using var report = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(output, ReadinessText.ReportJson)));
        Require(report.RootElement.GetProperty("VerifiedCompatibleMfas").GetInt32() == 1
            && report.RootElement.GetProperty("KnownDifferenceMfas").GetInt32() == 1
            && report.RootElement.GetProperty("NeedsVerificationMfas").GetInt32() == entries.Length - 2);
    }

    private static void CheckIndex(string output, MfaCompatibilityLedger ledger)
    {
        string index = File.ReadAllText(Path.Combine(output, MfaCompatibilityIndexNames.File));
        string full = File.ReadAllText(Path.Combine(output, MfaCompatibilityNames.Report));
        int verified = ledger.Entries.Count(entry => entry.Status == MfaCompatibilityNames.Verified);
        int different = ledger.Entries.Count(entry => entry.Status == MfaCompatibilityNames.Different);
        int pending = ledger.Entries.Count(entry => entry.Status == MfaCompatibilityNames.Pending);
        Require(index.Contains(
            string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                MfaCompatibilityIndexNames.Summary,
                verified,
                different,
                pending
            ),
            StringComparison.Ordinal
        ));
        string[] lines = index.Split('\n');
        int totalRows = 0;
        foreach (string status in new[] { MfaCompatibilityNames.Verified, MfaCompatibilityNames.Different, MfaCompatibilityNames.Pending })
        {
            var expected = ledger.Entries.Where(entry => entry.Status == status)
                .OrderBy(entry => entry.Module, StringComparer.Ordinal).ThenBy(entry => entry.Function, StringComparer.Ordinal)
                .ThenBy(entry => entry.Arity).ToArray();
            string title = MfaCompatibilityNames.DisplayStatus(status);
            string heading = "## " + title + " (" + expected.Length + ")";
            int start = Array.IndexOf(lines, heading);
            Require(start >= 0 && lines.Count(line => line == heading) == 1);
            int end = Array.FindIndex(lines, start + 1, line => line.StartsWith("## ", StringComparison.Ordinal));
            if (end < 0) end = lines.Length;
            string[] actualRows = lines[(start + 1)..end].Where(line => line.StartsWith("| `", StringComparison.Ordinal)).ToArray();
            string anchor = title.ToLowerInvariant().Replace("%", "").Replace(" ", "-");
            Require(full.Contains("## " + title + "\n", StringComparison.Ordinal));
            Require(actualRows.Length == expected.Length);
            Require(actualRows.SequenceEqual(expected.Select(entry => "| " + MfaCompatibilityMarkdown.Code(entry.Key)
                + " | [" + MfaCompatibilityIndexNames.Details + "](" + MfaCompatibilityNames.Report + "#" + anchor + ") |")));
            totalRows += actualRows.Length;
        }
        Require(totalRows == ledger.Entries.Length && !index.Contains('\r'));
    }

    private static void MustReject(Action action)
    {
        bool rejected = false;
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }
        Require(rejected);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(MfaCompatibilityHostNames.Failure);
    }
}
