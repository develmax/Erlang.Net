using Erlang;
using Erlang.Compiler;
using Erlang.Differential;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

if (args.Length != ProcessErrorManagedFocusNames.ArgumentCount || File.Exists(args[0]) ||
    args[3] is not (ProcessErrorManagedFocusNames.All or ProcessErrorManagedFocusNames.Historical))
    throw new ArgumentException(ProcessErrorManagedFocusNames.Usage);
string reportPath = Path.GetFullPath(args[0]);
string freezePath = Path.GetFullPath(args[1]);
string freezeSha = args[2].ToUpperInvariant();
if (Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(freezePath))) != freezeSha)
    throw new InvalidOperationException(ProcessErrorManagedFocusNames.FreezeMismatch);
bool historical = args[3] == ProcessErrorManagedFocusNames.Historical;
CompiledModuleCase[] all = ProcessErrorHandlerCases.Modules.ToArray();
if (all.Length != ProcessErrorManagedFocusNames.Cases)
    throw new InvalidOperationException(ProcessErrorManagedFocusNames.CaseCountMismatch);
CompiledModuleCase[] selected = historical ? [all[0]] : all;
Assembly[] assemblies = [typeof(ProcessRuntime).Assembly, typeof(CodeGeneration).Assembly,
    typeof(GeneratedModuleCompiler).Assembly, typeof(Term).Assembly];
var identities = assemblies.Select(assembly => new
{
    Name = assembly.GetName().Name,
    Path = assembly.Location,
    Bytes = new FileInfo(assembly.Location).Length,
    Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))),
    LoadedModuleVersionId = assembly.ManifestModule.ModuleVersionId
}).ToArray();
Term historicalBadArgument = Term.Tuple(
    Term.A(ProcessErrorManagedFocusNames.FailureOutcomeTag),
    Term.A(ProcessErrorManagedFocusNames.ErrorExceptionClass),
    Term.A(ProcessErrorManagedFocusNames.BadArgumentReason));
var rows = new List<object>();
int matches = 0;
int confirmedHistoricalFailures = 0;
foreach (CompiledModuleCase fixture in selected)
{
    string? generatedHash = null;
    CompiledModuleStreamResult? actual = null;
    Exception? failure = null;
    try
    {
        using GeneratedModuleArtifact artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        generatedHash = artifact.CSharpSha256;
        actual = await CompiledModuleExecution.RunCaptured(artifact, fixture.CallerContext);
    }
    catch (Exception exception)
    {
        failure = exception;
    }
    bool emptyStreams = actual is not null && actual.StandardOutputHex.Length == 0 && actual.StandardErrorHex.Length == 0;
    bool match = failure is null && actual is not null && actual.Result.Equals(fixture.Expected) && emptyStreams;
    bool expectedRed = historical && failure is null && actual is not null && actual.Result.Equals(historicalBadArgument) && emptyStreams;
    if (match)
        matches++;
    if (expectedRed)
        confirmedHistoricalFailures++;
    rows.Add(new
    {
        Case = fixture.Name,
        Source = fixture.Source,
        SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fixture.Source))),
        GeneratedCSharpSha256 = generatedHash,
        CallerContext = fixture.CallerContext.ToString(),
        Actual = actual?.Result.ToString(),
        Expected = fixture.Expected.ToString(),
        StandardOutputHex = actual?.StandardOutputHex,
        StandardErrorHex = actual?.StandardErrorHex,
        FixturePassed = match,
        ExpectedHistoricalBadArgumentObserved = expectedRed,
        Exception = failure?.ToString()
    });
}
bool binariesUnchanged = identities.All(identity =>
    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(identity.Path))) == identity.Sha256);
bool freezeUnchanged = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(freezePath))) == freezeSha;
bool contractPassed = historical ? confirmedHistoricalFailures == 1 && matches == 0 : matches == selected.Length;
bool recipePassed = contractPassed && binariesUnchanged && freezeUnchanged;
var report = new
{
    RecipePassed = recipePassed,
    Mode = args[3],
    ExpectedCases = selected.Length,
    Executed = rows.Count,
    FixturePassed = matches,
    FixtureFailed = rows.Count - matches,
    HistoricalBadArgumentConfirmed = confirmedHistoricalFailures,
    Unexecuted = selected.Length - rows.Count,
    ApprovedSourceFreezePath = freezePath,
    ApprovedSourceFreezeSha256 = freezeSha,
    SourceFreezeUnchanged = freezeUnchanged,
    ActualLoadedAssemblies = identities,
    LoadedAssemblyFilesUnchanged = binariesUnchanged,
    Cases = rows,
    OriginalExecuted = false,
    NativeFixtureExecuted = false,
    OuterProcessDeadlineRequired = true,
    HistoricalHostInitializationStillRequiredSeparately = historical
};
Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
await using (FileStream file = new(reportPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
    await JsonSerializer.SerializeAsync(file, report, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(JsonSerializer.Serialize(new { report.RecipePassed, report.Executed, report.FixturePassed, report.FixtureFailed }));
return recipePassed ? 0 : 1;
