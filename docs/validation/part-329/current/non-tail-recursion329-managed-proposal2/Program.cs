using Erlang;
using Erlang.Compiler;
using Erlang.Differential;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

if (args.Length is not (NonTailRecursionManagedNames.RequiredArguments or NonTailRecursionManagedNames.RetainedArguments))
    throw new ArgumentException(NonTailRecursionManagedNames.Usage);
string reportPath = Path.GetFullPath(args[0]);
string freezePath = Path.GetFullPath(args[1]);
string approvedSha = args[2].ToUpperInvariant();
string selector = args[3];
bool retained = selector == NonTailRecursionManagedNames.RetainedSelector;
bool interpreted = selector == NonTailRecursionManagedNames.InterpretedSelector;
if (File.Exists(reportPath) || args.Length != (retained ? NonTailRecursionManagedNames.RetainedArguments : NonTailRecursionManagedNames.RequiredArguments))
    throw new ArgumentException(NonTailRecursionManagedNames.Usage);
if (Convert.FromHexString(approvedSha).Length != SHA256.HashSizeInBytes || Hash(freezePath) != approvedSha)
    throw new InvalidOperationException(NonTailRecursionManagedNames.FreezeChanged);
CompiledModuleCase[] catalog = NonTailRecursionCases.Modules.ToArray();
if (catalog.Length != NonTailRecursionManagedNames.ExpectedNewCases || catalog.Select(fixture => fixture.Name).Distinct().Count() != catalog.Length)
    throw new InvalidOperationException(NonTailRecursionManagedNames.InvalidFixture);
CompiledModuleCase selected;
string? originalSourcePath = null;
if (retained)
{
    originalSourcePath = Path.GetFullPath(args[4]);
    if (Hash(originalSourcePath) != NonTailRecursionManagedNames.RetainedSha256)
        throw new InvalidOperationException(NonTailRecursionManagedNames.OriginalSourceChanged);
    string source = new UTF8Encoding(false, true).GetString(await File.ReadAllBytesAsync(originalSourcePath));
    selected = new CompiledModuleCase(NonTailRecursionManagedNames.RetainedCase, source,
        Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion)));
}
else if (interpreted)
{
    string depth = NonTailRecursionCaseNames.FiniteDepth.ToString(System.Globalization.CultureInfo.InvariantCulture);
    string source = "begin " + NonTailRecursionCaseNames.AnonymousBody + "{F(F," + depth + ")," + depth + "} end";
    selected = new CompiledModuleCase(NonTailRecursionManagedNames.InterpretedCase, source,
        Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.Tuple(Term.I(NonTailRecursionCaseNames.FiniteChecksum), Term.I(NonTailRecursionCaseNames.FiniteDepth))));
}
else
{
    string name = NonTailRecursionCaseNames.Prefix + selector;
    selected = catalog.SingleOrDefault(fixture => fixture.Name == name)
        ?? throw new ArgumentException(NonTailRecursionManagedNames.InvalidFixture);
}
Assembly[] assemblies = [typeof(ProcessRuntime).Assembly, typeof(Execution).Assembly,
    typeof(GeneratedModuleCompiler).Assembly, typeof(Term).Assembly, Assembly.GetExecutingAssembly()];
var identities = assemblies.Select(assembly => new
{
    Name = assembly.GetName().Name,
    Path = assembly.Location,
    Bytes = new FileInfo(assembly.Location).Length,
    Sha256 = Hash(assembly.Location),
    LoadedModuleVersionId = assembly.ManifestModule.ModuleVersionId
}).ToArray();
CompiledModuleStreamResult? actual = null;
Exception? failure = null;
string? generatedSha = null;
try
{
    if (interpreted)
        actual = await RunInterpreted(selected.Source);
    else
    {
        using GeneratedModuleArtifact compiled = GeneratedModuleCompiler.Compile(selected.Source);
        generatedSha = compiled.CSharpSha256;
        actual = await CompiledModuleExecution.RunCaptured(compiled, selected.CallerContext);
    }
}
catch (Exception exception)
{
    failure = exception;
}
bool emptyStreams = actual is not null && actual.StandardOutputHex.Length == 0 && actual.StandardErrorHex.Length == 0;
bool fixturePassed = failure is null && actual is not null && actual.Result.Equals(selected.Expected) && emptyStreams;
bool binariesUnchanged = identities.All(identity => Hash(identity.Path) == identity.Sha256);
bool freezeUnchanged = Hash(freezePath) == approvedSha;
bool originalUnchanged = originalSourcePath is null || Hash(originalSourcePath) == NonTailRecursionManagedNames.RetainedSha256;
bool recipePassed = fixturePassed && binariesUnchanged && freezeUnchanged && originalUnchanged;
var report = new
{
    RecipePassed = recipePassed,
    Selector = selector,
    ExecutionKind = interpreted ? "interpreted-module-null" : "generated-module",
    ExpectedCases = 1,
    Executed = 1,
    FixturePassed = fixturePassed ? 1 : 0,
    FixtureFailed = fixturePassed ? 0 : 1,
    Case = selected.Name,
    Source = selected.Source,
    SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(selected.Source))),
    GeneratedCSharpSha256 = generatedSha,
    CallerContext = selected.CallerContext.ToString(),
    CompilerPolicy = selected.CompilerPolicy.ToString(),
    Expected = selected.Expected.ToString(),
    Actual = actual?.Result.ToString(),
    StandardOutputHex = actual?.StandardOutputHex,
    StandardErrorHex = actual?.StandardErrorHex,
    Exception = failure?.ToString(),
    ApprovedSourceFreezePath = freezePath,
    ApprovedSourceFreezeSha256 = approvedSha,
    SourceFreezeUnchanged = freezeUnchanged,
    ActualLoadedAssemblies = identities,
    LoadedAssemblyFilesUnchanged = binariesUnchanged,
    RetainedOriginal328SourcePath = originalSourcePath,
    RetainedOriginal328SourceExact = originalUnchanged,
    EightNewCatalogNames = catalog.Select(fixture => fixture.Name).ToArray(),
    OriginalExecuted = false,
    NativeFixtureExecuted = false,
    OuterProcessDeadlineAndTreeSettlementRequired = true,
    FatalStackOverflowMayProduceNoReport = true,
    ChildRunCapturedCompletedBeforeReport = actual is not null
};
Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
await using (FileStream file = new(reportPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
    await JsonSerializer.SerializeAsync(file, report, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(JsonSerializer.Serialize(new { report.RecipePassed, report.Selector, report.FixturePassed, report.FixtureFailed }));
return recipePassed ? 0 : 1;

static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));


static async Task<CompiledModuleStreamResult> RunInterpreted(string source)
{
    Expr expression = new Parser(source).ParseExpression();
    using var output = new MemoryStream();
    using var error = new MemoryStream();
    using var streams = new RuntimeStandardStreams(output, error);
    await using var runtime = new ProcessRuntime(streams);
    Term? result = null;
    ProcessHandle process = runtime.Spawn(async context =>
    {
        try
        {
            result = Term.Tuple(Term.A(OracleOutcomeTags.Success), await Execution.EvaluateAsync(expression, context));
        }
        catch (ErlangException failure)
        {
            result = Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A(failure.ExceptionClass), failure.Reason);
        }
        return Nil.Value;
    });
    Term reason = await process.Completion;
    if (!reason.Equals(Term.A(ProcessExitReasons.Normal)) || result is null)
        result = Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A(ErlangExceptionClasses.Exit), reason);
    return new(result, Convert.ToHexString(output.ToArray()), Convert.ToHexString(error.ToArray()));
}
