using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

if (args.Length != 3)
{
    Console.Error.WriteLine(DetsCompatibilityDiagnostics.Usage);

    return 2;
}
string repository = Path.GetFullPath(args[0]);
string capture = Path.GetFullPath(args[1]);
string output = Path.GetFullPath(args[2]);
if (Directory.Exists(output))
{
    throw new IOException(DetsCompatibilityDiagnostics.ExistingOutput);
}
Directory.CreateDirectory(output);
var files = new List<string>();
var expectedRows = new List<Term>();
var inputs = new List<object>();
foreach (var (name, code) in new[] { ("set", "Set"), ("bag", "Bag"), ("duplicate_bag", "DuplicateBag") })
{
    string foreign = Path.Combine(output, "foreign-" + name + ".dets");
    string input = Path.Combine(capture, name + ".dets");
    inputs.Add(new { Name = Path.GetFileName(input), Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))) });
    File.Copy(input, foreign);
    using (var stream = new FileStream(foreign, FileMode.Open, FileAccess.ReadWrite))
    {
        var file = new ReflectedDetsFile(stream);
        if (!file.Lookup(Term.I(2)).SequenceEqual(new Term[] { Term.Tuple(Term.I(2), Term.A("d")) }))
        {
            throw new InvalidOperationException(DetsCompatibilityDiagnostics.ForeignRead);
        }
        if (file.Insert([Term.Tuple(Term.A("atomic"), Term.I(1)), Term.Tuple(Term.I(2), Term.A("replaced"))], true)
            || file.Lookup(Term.A("atomic")).Length != 0)
        {
            throw new InvalidOperationException(DetsCompatibilityDiagnostics.PartialInsert);
        }
        var marker = Term.Tuple(Term.A("managed"), new Integer(System.Numerics.BigInteger.One << 200));
        _ = file.Insert([marker]);
        file.Delete(Term.I(2));
        file.Save();
        files.Add(foreign);
        expectedRows.Add(Cons.From(file.Objects().OrderBy(value => value, Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right)))));
    }
    string created = Path.Combine(output, "created-" + name + ".dets");
    Term[] values = Enumerable.Range(1, 700).Select(index => (Term)Term.Tuple(Term.I(index), Term.I(index * index))).ToArray();
    using (var stream = new FileStream(created, FileMode.CreateNew, FileAccess.ReadWrite))
    {
        ReflectedDetsFile.Create(stream, code, Array.Empty<Term>());
        var file = new ReflectedDetsFile(stream);
        _ = file.Insert(values);
        file.Save();
        files.Add(created);
        expectedRows.Add(Cons.From(values));
    }
}
using JsonDocument admission = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repository,
    "artifacts/library376-official-compiler/official-admission.json")));
string installed = admission.RootElement.GetProperty("InstalledOtpRoot").GetString()!;
string manifest = admission.RootElement.GetProperty("ExecutionManifestPath").GetString()!;
string manifestHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(manifest)));
if (admission.RootElement.GetProperty("ReleaseVersion").GetString() != "29.1.1"
    || admission.RootElement.GetProperty("SourceCommit").GetString() != "ad05823719d77c8faee87348ea39513d4e2f99c5"
    || manifestHash != admission.RootElement.GetProperty("ExecutionManifestSha256").GetString())
{
    throw new InvalidDataException(DetsCompatibilityDiagnostics.Admission);
}
using JsonDocument oracleInputs = JsonDocument.Parse(File.ReadAllBytes(manifest));
void VerifyOracle()
{
    foreach (JsonElement entry in oracleInputs.RootElement.EnumerateArray())
    {
        string path = entry.GetProperty("Path").GetString()!;
        if (!path.EndsWith("/erl.exe", StringComparison.Ordinal) && !path.EndsWith("/beam.smp.dll", StringComparison.Ordinal)
            && !path.EndsWith("/dets.beam", StringComparison.Ordinal) && !path.EndsWith("/dets_v9.beam", StringComparison.Ordinal))
        {
            continue;
        }
        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(installed, path))));
        if (hash != entry.GetProperty("Sha256").GetString())
        {
            throw new InvalidDataException(DetsCompatibilityDiagnostics.OracleIdentity);
        }
    }
}
VerifyOracle();
string Quote(string text) => "\"" + text.Replace('\\', '/').Replace("\"", "\\\"") + "\"";
string expression = "Rows=[begin {ok,T}=dets:open_file(F),L=lists:sort(dets:match_object(T,'_')),ok=dets:close(T),L end || F <- ["
    + string.Join(',', files.Select(Quote)) + "]],ok=file:write_file(" + Quote(Path.Combine(output, "original-reader.etf")) + ",term_to_binary(Rows)),halt().";
var start = new ProcessStartInfo(Path.Combine(installed, "bin/erl.exe"))
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true
};
foreach (string argument in new[] { "-noshell", "-eval", expression })
{
    start.ArgumentList.Add(argument);
}
using Process original = Process.Start(start)!;
Task<string> stdout = original.StandardOutput.ReadToEndAsync();
Task<string> stderr = original.StandardError.ReadToEndAsync();
await original.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));
File.WriteAllText(Path.Combine(output, "oracle.log"), await stdout + await stderr);
VerifyOracle();
if (original.ExitCode != 0)
{
    throw new InvalidOperationException(DetsCompatibilityDiagnostics.OracleRead);
}
Term actual = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(output, "original-reader.etf")));
Term expected = Cons.From(expectedRows);
File.WriteAllBytes(Path.Combine(output, "expected.etf"), ExternalTermFormat.Encode(expected));
File.WriteAllText(
    Path.Combine(output, "receipt.json"),
    JsonSerializer.Serialize(
        new
        {
            OracleExecutionManifestSha256 = manifestHash,
            Inputs = inputs,
            Cases = files.Count,
            Equal = actual.Equals(expected),
            ProductionAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(DetsModule).Assembly.Location))),
            Mvid = typeof(DetsModule).Module.ModuleVersionId,
            Files = files.Select(path => new { Name = Path.GetFileName(path), Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) })
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
if (!actual.Equals(expected))
{
    throw new InvalidOperationException(DetsCompatibilityDiagnostics.Mismatch);
}
Console.WriteLine(DetsCompatibilityDiagnostics.Passed);
return 0;
