using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using HashAllocationDiagnostic;

if (args.Length != 1)
    throw new ArgumentException("Usage: HashAllocationDiagnostic output.json");

var assembly = typeof(ProcessRuntime).Assembly;
var algorithm = assembly.GetType(DiagnosticParameters.AlgorithmType, throwOnError: true)!;
var method = algorithm.GetMethod(DiagnosticParameters.AlgorithmMethod, BindingFlags.Public | BindingFlags.Static)!;
var direct = method.CreateDelegate<Func<Term, uint>>();
var samples = new AllocationSample[DiagnosticParameters.Samples * 2];
string? failure = null;
await using (var runtime = new ProcessRuntime())
{
    var process = runtime.Spawn(async context =>
    {
        try
        {
            var binary = new BitString(new byte[DiagnosticParameters.BinaryBytes]);
            await runtime.Modules.Call(context, DiagnosticParameters.Module, DiagnosticParameters.Function, binary, Term.I(DiagnosticParameters.FullRange));
            for (int index = 0; index < DiagnosticParameters.Samples; index++)
            {
                int startingThread = Environment.CurrentManagedThreadId;
                long ticks = Stopwatch.GetTimestamp();
                long before = GC.GetAllocatedBytesForCurrentThread();
                ValueTask<Term> pending = runtime.Modules.Call(context, DiagnosticParameters.Module, DiagnosticParameters.Function, binary, Term.I(DiagnosticParameters.FullRange));
                bool synchronous = pending.IsCompletedSuccessfully;
                if (!synchronous)
                    throw new InvalidDataException("Registry call did not complete synchronously; diagnostic window is invalid");
                Term result = pending.GetAwaiter().GetResult();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                int endingThread = Environment.CurrentManagedThreadId;
                long elapsed = Stopwatch.GetTimestamp() - ticks;
                samples[index] = new("ModuleRegistry.Call", index, allocated, startingThread, endingThread, synchronous, result.ToString()!, elapsed);
            }
            direct(binary);
            for (int index = 0; index < DiagnosticParameters.Samples; index++)
            {
                int startingThread = Environment.CurrentManagedThreadId;
                long ticks = Stopwatch.GetTimestamp();
                long before = GC.GetAllocatedBytesForCurrentThread();
                uint result = direct(binary);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                int endingThread = Environment.CurrentManagedThreadId;
                long elapsed = Stopwatch.GetTimestamp() - ticks;
                samples[DiagnosticParameters.Samples + index] = new("ErlangNativeHash.Portable2 delegate", index, allocated, startingThread, endingThread, true, result.ToString(System.Globalization.CultureInfo.InvariantCulture), elapsed);
            }
        }
        catch (Exception exception)
        {
            failure = exception.ToString();
            throw;
        }

        return Nil.Value;
    });
    await process.Completion;
}

string[] variables = ["DOTNET_TieredCompilation", "COMPlus_TieredCompilation", "DOTNET_TieredPGO", "COMPlus_TieredPGO", "DOTNET_TC_QuickJit", "COMPlus_TC_QuickJit", "DOTNET_ReadyToRun", "COMPlus_ReadyToRun", "DOTNET_PROCESSOR_COUNT"];
var loaded = new[] { typeof(ProcessRuntime).Assembly, typeof(Term).Assembly }.Select(item => new
{
    item.FullName,
    item.Location,
    item.ManifestModule.ModuleVersionId,
    Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(item.Location)))
}).ToArray();
var report = new
{
    Status = failure is null ? "DiagnosticCompleted" : "DiagnosticFailed",
    DiagnosticOnly = true,
    NativeProof = false,
    SourceHead = DiagnosticParameters.SourceHead,
    Framework = RuntimeInformation.FrameworkDescription,
    RuntimeVersion = Environment.Version.ToString(),
    RuntimeInformation.ProcessArchitecture,
    RuntimeInformation.OSDescription,
    Environment.ProcessorCount,
    StopwatchFrequency = Stopwatch.Frequency,
    Environment = variables.ToDictionary(key => key, key => System.Environment.GetEnvironmentVariable(key)),
    Assemblies = loaded,
    DiagnosticParameters.BinaryBytes,
    DiagnosticParameters.FullRange,
    DiagnosticParameters.OriginalBudgetBytes,
    Samples = samples,
    Failure = failure
};
await File.WriteAllTextAsync(args[0], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");
Console.WriteLine($"{report.Status}: {samples.Count(sample => sample is not null)} allocation samples; diagnostic only");
if (failure is not null)
    System.Environment.ExitCode = 1;
