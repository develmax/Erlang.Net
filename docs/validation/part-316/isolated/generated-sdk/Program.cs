using Erlang;
using Erlang.Compiler;
using Erlang.Differential;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

const int CaseDeadlineSeconds = 40;
const int ExpectedCaseCount = 10;
if (args.Length != 1 || File.Exists(args[0]))
    throw new ArgumentException(nameof(args));
string output = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
CompiledModuleCase[] fixtures = SystemAllocatorQueryCases.Modules.Append(SystemAllocatorQueryCases.FullInfoBackendObservation).ToArray();
if (fixtures.Length != ExpectedCaseCount)
    throw new InvalidOperationException(nameof(fixtures));
var rows = new List<object>();
bool passed = true;
foreach (CompiledModuleCase fixture in fixtures)
{
    string? generated = null;
    Term? actual = null;
    Exception? failure = null;
    try
    {
        actual = await Task.Run(async () =>
        {
            using GeneratedModuleArtifact artifact = GeneratedModuleCompiler.Compile(fixture.Source);
            generated = artifact.CSharpSha256;
            return await CompiledModuleExecution.Run(artifact, fixture.CallerContext);
        }).WaitAsync(TimeSpan.FromSeconds(CaseDeadlineSeconds));
    }
    catch (Exception error) { failure = error; }
    bool match = failure is null && actual is not null && actual.Equals(fixture.Expected);
    rows.Add(new { Case = fixture.Name, SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fixture.Source))),
        GeneratedCSharpSha256 = generated, CallerContext = fixture.CallerContext.ToString(),
        Actual = actual?.ToString(), Expected = fixture.Expected.ToString(), Passed = match, Exception = failure?.ToString() });
    if (!match) { passed = false; break; }
}
Assembly[] assemblies = [typeof(ProcessRuntime).Assembly, typeof(CodeGeneration).Assembly, typeof(GeneratedModuleCompiler).Assembly, typeof(Term).Assembly];
var loaded = assemblies.Select(assembly => new { Name = assembly.GetName().Name, Path = assembly.Location,
    Bytes = new FileInfo(assembly.Location).Length, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))) }).ToArray();
var report = new { Passed = passed && rows.Count == fixtures.Length, ExpectedCases = fixtures.Length, Executed = rows.Count,
    Unexecuted = fixtures.Length - rows.Count, CaseDeadlineSeconds, ActualLoadedAssemblies = loaded,
    Cases = rows, OriginalExecuted = false, NativeFixtureExecuted = false };
await using (FileStream file = new(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
    await JsonSerializer.SerializeAsync(file, report, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(JsonSerializer.Serialize(new { report.Passed, report.Executed, report.Unexecuted }));
Environment.ExitCode = report.Passed ? 0 : 1;
