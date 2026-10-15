using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;
using Erlang.Compiler;
using Erlang.Differential;

const string SourceFreezeName = "artifacts/root-part301-final-source-freeze.json";
const string SourceFreezeHash = "A763E55596D771736233F06A197ED46485013D8EFD0D01D13B854BE44AFAED90";
const string ExpectedHead = "ca33514ba537688aaa8a265200b8d270a01f98fb";
const string MigratedName = "erlang-system-profile-dead-receiver";
const int NewCaseCount = 8;
const int TotalCaseCount = 9;
string repository = Path.GetFullPath(args[0]);
string output = Path.GetFullPath(args[1]);
if (File.Exists(output))
    throw new InvalidOperationException("Fresh output path required");

string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

string CurrentHead()
{
    var start = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
    start.ArgumentList.Add("-C");
    start.ArgumentList.Add(repository);
    start.ArgumentList.Add("rev-parse");
    start.ArgumentList.Add("HEAD");
    using var process = Process.Start(start) ?? throw new InvalidOperationException("Git read failed");
    string head = process.StandardOutput.ReadToEnd().Trim();
    string error = process.StandardError.ReadToEnd();
    if (!process.WaitForExit(10000) || process.ExitCode != 0)
        throw new InvalidOperationException("Git head read failed: " + error);

    return head;
}

int VerifySourceFreeze()
{
    string path = Path.Combine(repository, SourceFreezeName);
    if (Digest(path) != SourceFreezeHash)
        throw new InvalidOperationException("Actual301 source freeze drift");
    using var document = JsonDocument.Parse(File.ReadAllBytes(path));
    var root = document.RootElement;
    if (root.GetProperty("SourcePaths").GetInt32() != 14 || root.GetProperty("CSharpPaths").GetInt32() != 12)
        throw new InvalidOperationException("Expected actual301 fourteen/twelve source closure");
    int count = 0;
    foreach (var row in root.GetProperty("Files").EnumerateArray())
    {
        string file = Path.Combine(repository, row.GetProperty("Path").GetString()!);
        if (Digest(file) != row.GetProperty("Sha256").GetString() || new FileInfo(file).Length != row.GetProperty("Bytes").GetInt64())
            throw new InvalidOperationException("Actual301 source changed: " + file);
        count++;
    }
    if (count != 14)
        throw new InvalidOperationException("Incomplete301 source closure");

    return count;
}

string beforeHead = CurrentHead();
if (beforeHead != ExpectedHead)
    throw new InvalidOperationException("Expected published301 Main head");
int before = VerifySourceFreeze();
var fixtures = ProcessInfoDefaultSubsetCases.Modules
    .Concat(SystemProfileCases.Modules.Where(row => row.Name == MigratedName)).ToArray();
if (ProcessInfoDefaultSubsetCases.Modules.Count != NewCaseCount || fixtures.Length != TotalCaseCount || fixtures.Select(row => row.Name).Distinct().Count() != TotalCaseCount)
    throw new InvalidOperationException("Exactly eight new and one migrated source required");
var rows = new List<object>();
foreach (var fixture in fixtures)
{
    using var generated = GeneratedModuleCompiler.Compile(fixture.Source);
    rows.Add(new
    {
        fixture.Name,
        fixture.Source,
        SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fixture.Source))),
        generated.CSharpSha256,
        GeneratedAssemblyCompiledAndLoaded = true,
        FixtureBodyExecuted = false
    });
    Console.WriteLine("COMPILED " + fixture.Name + " " + generated.CSharpSha256);
}
var inputs = new[]
{
    (Name: "Erlang.Compiler", Loaded: typeof(CodeGeneration).Assembly.Location, Main: "src/Erlang.Compiler/bin/Release/net10.0/Erlang.Compiler.dll"),
    (Name: "Erlang.Runtime", Loaded: typeof(ProcessRuntime).Assembly.Location, Main: "src/Erlang.Runtime/bin/Release/net10.0/Erlang.Runtime.dll"),
    (Name: "Erlang.Differential", Loaded: typeof(GeneratedModuleCompiler).Assembly.Location, Main: "tools/Erlang.Differential/bin/Release/net10.0/Erlang.Differential.dll")
};
var libraries = inputs.Select(row =>
{
    string main = Path.Combine(repository, row.Main);
    string loadedHash = Digest(row.Loaded);
    if (loadedHash != Digest(main))
        throw new InvalidOperationException("Loaded dependency differs from actual Main output: " + row.Name);

    return new { row.Name, LoadedPath = row.Loaded, LoadedSha256 = loadedHash, ActualMainPath = main, ActualMainSha256 = Digest(main) };
}).ToArray();
int after = VerifySourceFreeze();
string afterHead = CurrentHead();
if (afterHead != beforeHead)
    throw new InvalidOperationException("Main head changed during generation");
var report = new
{
    Status = "ACTUAL_MAIN301_NINE_GENERATED_HASH_EXPORT",
    Head = beforeHead,
    HeadAfter = afterHead,
    SourceFreezePath = SourceFreezeName,
    SourceFreezeSha256 = SourceFreezeHash,
    SourceFreezePathsVerifiedBefore = before,
    SourceFreezePathsVerifiedAfter = after,
    CaseCount = rows.Count,
    OrderedCases = rows,
    ActualInputDlls = libraries,
    FixtureBodyExecuted = false,
    OriginalExecuted = false,
    NativeExecuted = false,
    MainTrackedSourceEdited = false,
    SdkOriginalParityClaim = false
};
File.WriteAllText(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
Console.WriteLine("EXPORTED " + rows.Count + " " + Digest(output));
