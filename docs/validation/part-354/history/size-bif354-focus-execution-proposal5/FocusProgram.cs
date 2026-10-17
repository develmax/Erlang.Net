using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;
using Erlang.Compiler;
using Erlang.Differential;

string mode = args[0];
string output = args[^1];
string? name = mode == FocusNames.Case ? args[1] : null;
var watch = Stopwatch.StartNew();
bool passed = false;
string? failure = null;
object? observation = null;
var sources = new List<object>();
string? currentSource = null;
try
{
    if (mode == FocusNames.Parse)
    {
        foreach (string path in Directory.GetFiles(FocusNames.SourceDirectory, "*.erl").Order(StringComparer.Ordinal))
        {
            currentSource = Path.GetFileNameWithoutExtension(path);
            string source = File.ReadAllText(path, Encoding.UTF8);
            _ = new Parser(source).ParseModule();
            sources.Add(new { Name = Path.GetFileNameWithoutExtension(path), Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) });
        }
        if (sources.Count != 22)
            throw new InvalidOperationException("Expected 22 independent source files before any target execution");
    }
    else if (mode == FocusNames.Case)
    {
        var fixture = SizeErrorCases.Modules.Single(item => item.Name == name);
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        var actual = await CompiledModuleExecution.RunCaptured(artifact, fixture.CallerContext);
        observation = new
        {
            Name = name,
            SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fixture.Source))),
            fixture.CallerContext,
            fixture.CompilerPolicy,
            artifact.CSharpSha256,
            ActualEtfHex = Convert.ToHexString(ExternalTermFormat.Encode(actual.Result)),
            ExpectedEtfHex = Convert.ToHexString(ExternalTermFormat.Encode(fixture.Expected)),
            Actual = actual.Result.ToString(),
            Expected = fixture.Expected.ToString(),
            actual.StandardOutputHex,
            actual.StandardErrorHex
        };
        if (!actual.Result.Equals(fixture.Expected))
            throw new InvalidOperationException("Actual compiled result differs from source-derived expected");
        if (!string.IsNullOrEmpty(actual.StandardOutputHex) || !string.IsNullOrEmpty(actual.StandardErrorHex))
            throw new InvalidOperationException("Unexpected guest streams");
    }
    else if (mode == FocusNames.HostMode)
    {
        name = FocusNames.Host;
        await Erlang.Tests.BacktraceDepthHostChecks.IndependentRuntimes();
    }
    else if (mode == FocusNames.CommonHostMode || mode == FocusNames.HandlerMode)
    {
        string typeName;
        string method;
        if (mode == FocusNames.CommonHostMode)
        {
            name = FocusNames.CommonHost;
            typeName = "Erlang.Tests.CommonErrorCaptureHostChecks";
            method = "RunIdentityRollbackAndAsyncSnapshot";
        }
        else
        {
            method = args[1];
            if (method is not ("RunState" or "RunFrames" or "RunNestedFrames" or "RunSourceLease" or "RunIsolation"))
                throw new ArgumentException("Unlisted existing handler control");
            name = "handler/" + method;
            typeName = "Erlang.Tests.ProcessErrorHandlerHostChecks";
        }
        var type = typeof(Erlang.Tests.BacktraceDepthHostChecks).Assembly.GetType(typeName, throwOnError: true)!;
        var target = type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!;
        var pending = target.Invoke(null, null) as Task ?? throw new InvalidOperationException("Expected original Task method");
        await pending;
    }
    else
        throw new ArgumentException("Unknown focus mode");
    passed = true;
}
catch (Exception error)
{
    failure = error.ToString();
}
var assemblies = new[]
{
    typeof(Term).Assembly,
    typeof(ProcessRuntime).Assembly,
    typeof(Parser).Assembly,
    typeof(ExternalTermFormat).Assembly,
    typeof(CompiledModuleCases).Assembly,
    typeof(Erlang.Tests.BacktraceDepthHostChecks).Assembly,
    Assembly.GetExecutingAssembly()
}.Distinct().Select(assembly => new
{
    Name = assembly.GetName().Name,
    Path = Path.GetFullPath(assembly.Location),
    Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location)))
}).ToArray();
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
{
    Name = name ?? mode,
    Passed = passed,
    Failure = failure,
    ElapsedSeconds = watch.Elapsed.TotalSeconds,
    Sources = sources,
    CurrentSource = currentSource,
    Observation = observation,
    LoadedAssemblies = assemblies,
    NativeExecuted = false,
    FullValidationExecuted = false,
    ReadinessExecuted = false
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine((passed ? "PASS " : "FAIL ") + (name ?? mode));
return passed ? 0 : 1;
