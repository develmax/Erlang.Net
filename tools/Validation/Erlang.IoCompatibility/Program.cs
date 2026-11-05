using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static IoCompatibilityNames;

if (args.Length is not (3 or 4) || args[2] is not (nameof(RuntimeModuleBackend.Managed) or nameof(RuntimeModuleBackend.Beam))
    || args.Length == 4 && args[3] != Scenarios)
{
    Console.Error.WriteLine(Usage);

    return 2;
}
string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
bool managed = args[2] == nameof(RuntimeModuleBackend.Managed);
bool scenarios = args.Length == 4;
string stdlib = Path.Combine(repository, Stdlib);
string kernel = Path.Combine(repository, Kernel);
using JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, Provenance)));
int depth = provenance.RootElement.TryGetProperty(BacktraceField, out JsonElement depthValue) ? depthValue.GetInt32() : DefaultDepth;
void Verify(string field, string path)
{
    if (provenance.RootElement.GetProperty(field).GetString() != Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))))
    {
        throw new InvalidDataException(ProvenanceFailure);
    }
}
if (provenance.RootElement.GetProperty("SourceCommit").GetString() != BaselineCommit)
{
    throw new InvalidDataException(ProvenanceFailure);
}
Verify("ObserverSha256", Path.Combine(repository, ObserverSource));
Verify("ObserverBeamSha256", Path.Combine(directory, Observer + BeamExtension));
Verify("CorpusSha256", Path.Combine(directory, Cases));
Verify("ExportsSha256", Path.Combine(directory, Exports));
Verify("FeatureStateSha256", Path.Combine(directory, FeatureState));
Verify("SuiteSourceSha256", Path.Combine(repository, SuiteSource));
Verify("SuiteBeamSha256", Path.Combine(directory, Suite + BeamExtension));
string[] dependencies = Directory.GetFiles(Path.Combine(repository, Original), BeamPattern, SearchOption.AllDirectories)
    .Select(Path.GetFileNameWithoutExtension).Cast<string>().Append(ErlangModule).Append(Observer).Append(Suite)
    .Concat(SuiteDependencies).Distinct().ToArray();
byte[] Image(string module) => File.ReadAllBytes(Path.Combine(module == Suite ? directory
    : File.Exists(Path.Combine(stdlib, module + BeamExtension)) ? stdlib : kernel, module + BeamExtension));
// Prepare all original helpers with their actual import closure in one publication.
await using var runtime = new ProcessRuntime();
await runtime.InitializeModulesAsync(registry =>
{
    RuntimeNativePrimitiveProviders.Register(runtime, registry, RuntimeNativePrimitives.PersistentTermCore | RuntimeNativePrimitives.InternalScalars413);
    if (managed)
    {
        RuntimeBootstrapIoLibrary.Register(runtime, registry);
        IoLibrary.Register(registry);
    }
});
var images = new List<PreparedBeamImage>();
foreach (string module in Helpers.Concat(managed ? [] : new[] { IoLibrary.Name }))
{
    Console.WriteLine(Preparation + module);
    images.Add(runtime.Modules.PrepareBeam(Image(module), new BeamDependencySelection(dependencies)));
}
images.Add(runtime.Modules.PrepareBeam(File.ReadAllBytes(Path.Combine(directory, Observer + BeamExtension)), new BeamDependencySelection(dependencies)));
await runtime.InitializeModulesAsync(registry =>
{
    if (registry.PublishPreparedBeam(images.ToArray()).Status != BeamPublicationStatus.Published)
    {
        throw new InvalidOperationException(PublicationFailure);
    }
});
var expectedExports = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, Exports)))).Cast<TupleTerm>()
    .Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet();
var actualExports = runtime.Modules.Exports.Where(export => export.Module == IoLibrary.Name && export.Function != ModuleInfo)
    .Select(export => (export.Function, export.Arity)).ToHashSet();
if (!expectedExports.SetEquals(IoLibrary.Exports) || !expectedExports.SetEquals(actualExports))
{
    throw new InvalidOperationException(ExportFailure);
}
foreach (var export in IoLibrary.Exports)
{
    if (!runtime.Modules.HasExport(IoLibrary.Name, export.Name, export.Arity)
        || (runtime.Modules.SourceOwner(IoLibrary.Name, export.Name, export.Arity) is PreparedBeamImage) == managed)
    {
        throw new InvalidOperationException(OwnerFailure);
    }
}
var failures = new List<object>();
var initialized = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle initialization = runtime.Spawn(async context =>
{
    try
    {
        await runtime.Modules.Call(
            context,
            ErlangModule,
            SystemFlag,
            Term.A(BacktraceFlag),
            Term.I(depth)
        );
        foreach (TupleTerm field in Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, FeatureState)))))
        {
            await runtime.Modules.Call(
                context,
                Persistent,
                Put,
                Term.Tuple(Term.A(Features), field.Items[0]),
                field.Items[1]
            );
        }
        initialized.TrySetResult();
    }
    catch (Exception error)
    {
        initialized.TrySetException(error);
    }

    return Nil.Value;
});
await initialized.Task.WaitAsync(Deadline);
await initialization.Completion.WaitAsync(Deadline);
Term[] rows = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, Cases))).Term)
    .Where(row => !scenarios || ((TupleTerm)row).Items[0] is TupleTerm { Items.Count: 2 }).ToArray();
int passed = 0;
foreach (TupleTerm row in rows)
{
    if (row.Items[0] is TupleTerm { Items.Count: 2 })
    {
        Console.WriteLine(Scenario + row.Items[0]);
    }
    var completion = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle worker = runtime.Spawn(async context =>
    {
        try
        {
            var observed = (TupleTerm)await runtime.Modules.Call(
                context,
                Observer,
                Observe,
                row.Items[0],
                context.Self
            );
            completion.TrySetResult(observed.Items[1]);
        }
        catch (ErlangException error)
        {
            completion.TrySetResult(Term.Tuple(
                Term.A(Uncaught),
                Term.A(error.ExceptionClass),
                error.Reason,
                error.StackTraceTerm
            ));
        }
        catch (Exception error)
        {
            completion.TrySetException(error);
        }

        return Nil.Value;
    });
    Term actual = await completion.Task.WaitAsync(Deadline);
    await worker.Completion.WaitAsync(Deadline);
    if (actual.Equals(row.Items[1]))
    {
        passed++;
    }
    else
    {
        failures.Add(new { Case = row.Items[0].ToString(), Expected = row.Items[1].ToString(), Actual = actual.ToString() });
    }
}
File.WriteAllText(
    Path.Combine(directory, args[2] + (scenarios ? ScenarioSuffix : string.Empty) + JsonExtension),
    JsonSerializer.Serialize(
        new
        {
            Backend = args[2],
            Passed = passed,
            Total = rows.Length,
            BacktraceDepth = depth,
            Failures = failures,
            LibrarySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(IoLibrary).Assembly.Location))),
            RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location)))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine($"{args[2]}: {passed}/{rows.Length}");

return failures.Count == 0 ? 0 : 1;
