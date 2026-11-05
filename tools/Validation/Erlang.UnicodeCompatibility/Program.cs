using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static UnicodeCompatibilityNames;

if (args.Length is not (3 or 4) || args[2] is not ("Managed" or "Beam"))
{
    Console.Error.WriteLine(Usage);

    return 2;
}
string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
string corpusFile = args.Length == 4 && args[3].EndsWith(".etf", StringComparison.Ordinal) ? args[3] : "original.etf";
string assemblyHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(UnicodeModule).Assembly.Location)));
string runtimeAssemblyHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location)));
bool managed = args[2] == "Managed";
string foundation = Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp");
byte[] Original(string name) => File.ReadAllBytes(Path.Combine(foundation, name + ".beam"));
RuntimeLibrarySelection Selection(bool useManaged) => new([
    RuntimeModuleSelection.Managed("erlang"),
    RuntimeModuleSelection.Managed("lists"),
    managed ? RuntimeModuleSelection.Managed("unicode_util") : RuntimeModuleSelection.Beam("unicode_util", Original("unicode_util"), ["lists"]),
    useManaged ? RuntimeModuleSelection.Managed("unicode", ["lists", "unicode_util"])
        : RuntimeModuleSelection.Beam("unicode", Original("unicode"), ["lists", "unicode_util"])
]);
bool refused = false;
try
{
    _ = RuntimeModuleSelection.Managed("unicode");
}
catch (ArgumentException)
{
    refused = true;
}
if (!refused)
{
    throw new InvalidOperationException(Failure);
}
await using var runtime = await ProcessRuntime.CreateAsync(Selection(managed), RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
RuntimeLibraryModuleSnapshot selected = runtime.LibrarySelection.Modules.Single(module => module.Module == "unicode");
if (selected.Backend != (managed ? RuntimeModuleBackend.Managed : RuntimeModuleBackend.Beam)
    || selected.InputSha256 != (managed ? null : Convert.ToHexString(SHA256.HashData(Original("unicode"))))
    || !UnicodeModule.Exports.ToHashSet().SetEquals(selected.Exports.Where(value => value.Name != "module_info").Select(value => (value.Name, value.Arity)))
    || selected.NativePrimitives.Count != 3)
{
    throw new InvalidOperationException(Failure);
}
Term declared = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "exports.etf")));
if (!UnicodeModule.Exports.ToHashSet().SetEquals(Cons.Items(declared).Cast<TupleTerm>().Select(value => (((Atom)value.Items[0]).Name, (int)((Integer)value.Items[1]).Value))))
{
    throw new InvalidOperationException(Failure);
}
using PreparedBeamImage observer = runtime.Modules.PrepareBeam(
    File.ReadAllBytes(Path.Combine(directory, Observer + ".beam")),
    new BeamDependencySelection(["unicode", "erlang", "lists", "binary", "file", "filename", Observer])
);
await runtime.InitializeModulesAsync(registry =>
{
    if (registry.PublishPreparedBeam(observer).Status != BeamPublicationStatus.Published)
    {
        throw new InvalidOperationException(Failure);
    }
});
Term[] rows = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, corpusFile)))).ToArray();
var failures = new List<object>();
int values = 0;
int exceptions = 0;
int index = 0;
int batchSize = corpusFile == "extra.etf" ? 1 : BatchSize;
foreach (Term[] batch in rows.Chunk(batchSize))
{
    var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle worker = runtime.Spawn(async context =>
    {
        try
        {
            foreach (TupleTerm row in batch)
            {
                var expected = (TupleTerm)row.Items[2];
                // Execute the identical worker body directly. Its returned send term
                // contains the complete captured outcome and the runtime-local pid.
                // No exception frame is removed or normalized by this transport.
                var sent = (TupleTerm)await runtime.Modules.Call(
                    context,
                    Observer,
                    "worker",
                    row.Items[0],
                    row.Items[1],
                    context.Self
                );
                Term actual = sent.Items[1];
                if (((Atom)expected.Items[0]).Name == "ok")
                {
                    values++;
                }
                else
                {
                    exceptions++;
                }
                if (!actual.Equals(expected))
                {
                    failures.Add(new { Index = index, Function = row.Items[0].ToString(), Arguments = row.Items[1].ToString(), Expected = expected.ToString(), Actual = actual.ToString() });
                }
                index++;
            }
            completion.TrySetResult();
        }
        catch (Exception error)
        {
            completion.TrySetException(error);
        }

        return Nil.Value;
    });
    await completion.Task.WaitAsync(TimeSpan.FromMinutes(2));
    await worker.Completion.WaitAsync(TimeSpan.FromSeconds(10));
    Console.WriteLine($"{args[2]}: {index}/{rows.Length}, differences={failures.Count}");
    if (args.Length == 4 && args[3] == "first-failure" && failures.Count != 0)
    {
        break;
    }
}
var backingCompletion = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle backingWorker = runtime.Spawn(async context =>
{
    try
    {
        backingCompletion.TrySetResult(await runtime.Modules.Call(context, Observer, "backing"));
    }
    catch (Exception error)
    {
        backingCompletion.TrySetException(error);
    }

    return Nil.Value;
});
Term backing = await backingCompletion.Task.WaitAsync(TimeSpan.FromSeconds(30));
await backingWorker.Completion.WaitAsync(TimeSpan.FromSeconds(10));
Term expectedBacking = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "backing.etf")));
int caseFailures = failures.Count;
bool backingMatched = backing.Equals(expectedBacking);
if (!backingMatched)
{
    failures.Add(new { Function = "backing", Expected = expectedBacking.ToString(), Actual = backing.ToString() });
}
if (assemblyHash != Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(UnicodeModule).Assembly.Location))))
{
    throw new InvalidOperationException(Failure);
}
string reportName = corpusFile == "original.etf" ? args[2] + "-report.json" : args[2] + "-" + corpusFile + "-report.json";
string report = Path.Combine(directory, reportName);
File.WriteAllText(
    report,
    JsonSerializer.Serialize(
        new
        {
            Backend = args[2],
            Baseline = "OTP-29.1.1",
            Exports = UnicodeModule.Exports.Count,
            Cases = index,
            Values = values,
            Exceptions = exceptions,
            Passed = index - caseFailures,
            CompleteCorpus = index == rows.Length,
            CorpusFile = corpusFile,
            BackingMatched = backingMatched,
            BackingProbes = Cons.Items(expectedBacking).Count(),
            Failures = failures,
            selected.InputSha256,
            selected.ManagedAssemblies,
            CorpusSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, corpusFile)))),
            ManagedAssemblySha256 = assemblyHash,
            RuntimeAssemblySha256 = runtimeAssemblyHash
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);

return failures.Count == 0 ? 0 : 1;
