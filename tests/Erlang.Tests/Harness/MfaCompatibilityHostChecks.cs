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
        // Historical rendering keeps source hashes; readiness still rejects the changed implementation.
        MfaCompatibility.Load(
            docs,
            MfaCompatibilityHostNames.Tag,
            MfaCompatibilityHostNames.Commit,
            registered,
            validateSourceBindings: false
        );
        File.WriteAllText(Path.Combine(docs, MfaCompatibilityHostNames.AuditFile), MfaCompatibilityNames.Pending);
        MustReject(() => MfaCompatibility.Load(
            docs,
            MfaCompatibilityHostNames.Tag,
            MfaCompatibilityHostNames.Commit,
            registered,
            validateSourceBindings: false
        ));
        File.WriteAllText(Path.Combine(docs, MfaCompatibilityHostNames.AuditFile), MfaCompatibilityHostNames.Scope);
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
        string commands = string.Join("\n", Directory.GetFiles(output, "*.md", SearchOption.AllDirectories).Select(File.ReadAllText));
        Require(ledger.Entries.All(value => commands.Contains(value.Key, StringComparison.Ordinal)));
        foreach (string command in new[] { "erlang:</2", "erlang:>/2", "erlang:=</2", "erlang:>=/2" })
            Require(commands.Contains("| `" + command + "` |", StringComparison.Ordinal));
        Require(MfaCompatibilityMarkdown.Code("`") == "`` ` ``");
        string sentinel = Path.Combine(output, "api", "user-notes.md");
        File.WriteAllText(sentinel, MfaCompatibilityHostNames.Scope);
        string[] previous = JsonSerializer.Deserialize<string[]>(File.ReadAllText(Path.Combine(output, "api", "generated-files.json")))!;
        var proof = new MfaCompatibilityEvidence
        {
            Kind = MfaCompatibilityNames.Audit,
            Path = "proof with spaces.json",
            Sha256 = new string('a', MfaCompatibilityNames.Sha256Length),
            Scope = "Evidence <scope> | preserved"
        };
        var reduced = ledger with { Entries = ledger.Entries.Take(2).Select(entry => entry with { Evidence = [proof], Scope = "Contract <scope> | preserved" }).ToArray() };
        await MfaCompatibilityDocuments.Write(reduced, docs, output);
        var generated = MfaCompatibilityDocuments.Generate(reduced, docs, output);
        Require(File.ReadAllText(sentinel) == MfaCompatibilityHostNames.Scope);
        Require(previous.Where(file => file.StartsWith("api/", StringComparison.Ordinal) && !generated.ContainsKey(file)).All(file => !File.Exists(Path.Combine(output, file))));
        Require(generated.Keys.Count(file => file.StartsWith("api/evidence/", StringComparison.Ordinal)) == 1);
        string proofPage = generated.Single(file => file.Key.StartsWith("api/evidence/", StringComparison.Ordinal)).Value;
        Require(proofPage.Contains(proof.Sha256, StringComparison.Ordinal) && proofPage.Contains("Evidence &lt;scope&gt; \\| preserved", StringComparison.Ordinal));
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
