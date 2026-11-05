using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static ErtsInternalCompatibilityNames;

if (args.Length != 3)
{
    Console.Error.WriteLine(Usage);

    return 2;
}
string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
RuntimeModuleBackend backend = Enum.Parse<RuntimeModuleBackend>(args[2]);
bool managed = backend == RuntimeModuleBackend.Managed;
if (!managed && backend != RuntimeModuleBackend.Beam)
{
    throw new ArgumentException(UnknownBackend);
}
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(Module)] : []),
    RuntimeNativePrimitives.PersistentTermCore | RuntimeNativePrimitives.EtsStorageCore,
    CancellationToken.None
);
string[] roots = [Erts, Kernel, Stdlib];
string[] dependencies = roots.SelectMany(root => Directory.GetFiles(Path.Combine(repository, root), BeamPattern))
    .Select(Path.GetFileNameWithoutExtension).Cast<string>().Append(Observer).Distinct().ToArray();
await runtime.InitializeModulesAsync(registry => ErtsInternalCompatibilityBootstrap.Register(runtime, registry));
var images = new List<PreparedBeamImage>();
foreach (string module in (managed ? new[] { Ets, Lists, Debug } : new[] { Module, Ets, Lists, Debug }).Append(Observer))
{
    string path = module == Observer ? Path.Combine(directory, module + Beam)
        : roots.Select(root => Path.Combine(repository, root, module + Beam)).First(File.Exists);
    images.Add(runtime.Modules.PrepareBeam(File.ReadAllBytes(path), new BeamDependencySelection(dependencies)));
}
await runtime.InitializeModulesAsync(registry =>
{
    if (registry.PublishPreparedBeam(images.ToArray()).Status != BeamPublicationStatus.Published)
    {
        throw new InvalidOperationException(Publication);
    }
});
var expectedExports = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, Exports))))
    .Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet();
var actualExports = runtime.Modules.Exports.Where(row => row.Module == Module && row.Function != ModuleInfo)
    .Select(row => (row.Function, row.Arity)).ToHashSet();
if (!actualExports.SetEquals(expectedExports)
    || actualExports.Any(row => (runtime.Modules.SourceOwner(Module, row.Function, row.Arity) is PreparedBeamImage) == managed))
{
    throw new InvalidOperationException(Ownership);
}
var failures = new List<object>();
int passed = 0;
Term[] rows = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, Corpus)))).ToArray();
foreach (TupleTerm row in rows)
{
    Term envelope = await Run(context => runtime.Modules.Call(
        context,
        Observer,
        Worker,
        row.Items[0],
        row.Items[1],
        context.Self
    ));
    Term actual = ((TupleTerm)envelope).Items[1];
    if (actual.Equals(row.Items[2]))
    {
        passed++;
    }
    else
    {
        failures.Add(new
        {
            Function = row.Items[0].ToString(),
            Arguments = row.Items[1].ToString(),
            Expected = row.Items[2].ToString(),
            Actual = actual.ToString()
        });
    }
}
Term[] effects = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, Effects)))).ToArray();
int effectPassed = 0;
foreach (TupleTerm row in effects)
{
    Console.WriteLine(row.Items[0]);
    Term envelope = await Run(context => runtime.Modules.Call(
        context,
        Observer,
        ScenarioWorker,
        row.Items[0],
        context.Self
    ));
    Term actual = ((TupleTerm)envelope).Items[1];
    if (actual.Equals(row.Items[1]))
    {
        effectPassed++;
    }
    else
    {
        failures.Add(new { Scenario = row.Items[0].ToString(), Expected = row.Items[1].ToString(), Actual = actual.ToString() });
    }
}
File.WriteAllText(
    Path.Combine(directory, backend + ReportSuffix),
    JsonSerializer.Serialize(
        new
        {
            Backend = backend.ToString(),
            Baseline,
            Exports = actualExports.Count,
            Cases = rows.Length,
            Passed = passed,
            Effects = effects.Length,
            EffectPassed = effectPassed,
            Failures = failures,
            SourceSha256 = Hash(Path.Combine(repository, Source)),
            BeamSha256 = Hash(Path.Combine(repository, Erts, Module + Beam)),
            DebugBeamSha256 = Hash(Path.Combine(repository, Kernel, Debug + Beam)),
            RuntimeSha256 = Hash(typeof(ProcessRuntime).Assembly.Location),
            RunnerSha256 = Hash(typeof(ErtsInternalCompatibilityNames).Assembly.Location),
            ObserverSha256 = Hash(Path.Combine(directory, Observer + Beam)),
            CorpusSha256 = Hash(Path.Combine(directory, Corpus)),
            EffectsSha256 = Hash(Path.Combine(directory, Effects))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine($"{backend}: {passed}/{rows.Length}; effects {effectPassed}/{effects.Length}; failures {failures.Count}");

return failures.Count == 0 ? 0 : 1;

static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

async Task<Term> Run(Func<ProcessContext, ValueTask<Term>> body)
{
    var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle worker = runtime.Spawn(async context =>
    {
        try
        {
            result.TrySetResult(await body(context));
        }
        catch (ErlangException error)
        {
            result.TrySetResult(Term.Tuple(
                Term.A(Uncaught),
                Term.A(error.ExceptionClass),
                error.Reason,
                error.StackTraceTerm
            ));
        }
        catch (Exception error)
        {
            result.TrySetException(error);
        }

        return Nil.Value;
    });
    Term value = await result.Task.WaitAsync(Deadline);
    await worker.Completion.WaitAsync(Deadline);

    return value;
}
