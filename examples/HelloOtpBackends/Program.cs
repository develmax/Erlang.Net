using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using HelloOtpBackends;

if (args.Length != 0 && (args.Length != 2 || args[0] != BackendExampleNames.ObserveOption || Directory.Exists(args[1])))
    throw new ArgumentException(BackendExampleNames.Usage);
RuntimeLibrarySelection selection = RuntimeLibraryManifest.Load(Path.Combine(AppContext.BaseDirectory, RuntimeLibraryManifest.FileName));
await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(selection);
foreach (RuntimeLibraryModuleSnapshot module in runtime.LibrarySelection.Modules)
{
    if (!ReferenceEquals(module.Image, runtime.Modules.CurrentCodeImage(module.Module)) || runtime.Modules.HasOldCode(module.Module))
        throw new InvalidOperationException(BackendExampleNames.Failure);
    foreach (BeamFunctionSignature export in module.Exports)
    {
        object? owner = runtime.Modules.SourceOwner(module.Module, export.Name, export.Arity);
        if (module.Backend == RuntimeModuleBackend.Beam
            ? owner is not PreparedBeamImage image || image.Name != module.Module
            : owner is not null)
            throw new InvalidOperationException(BackendExampleNames.Failure);
    }
}
byte[] observerBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, BackendExampleNames.ObserverFile));
if (Convert.ToHexString(SHA256.HashData(observerBytes)) != BackendExampleNames.ObserverSha256)
    throw new InvalidDataException(BackendExampleNames.Failure);
PreparedBeamImage observer = runtime.Modules.PrepareBeam(
    observerBytes,
    new BeamDependencySelection(selection.Modules.Select(module => module.Module).Append(BackendExampleNames.Runtime))
);
if (observer.Name != BackendExampleNames.Observer || runtime.Modules.PublishPreparedBeam(observer).Status != BeamPublicationStatus.Published
    || !ReferenceEquals(observer, runtime.Modules.SourceOwner(observer.Name, BackendExampleNames.Run, 0)))
    throw new InvalidOperationException(BackendExampleNames.Failure);
var result = new TaskCompletionSource<Term[]>(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle worker = runtime.Spawn(async context =>
{
    try
    {
        Term values = await runtime.Modules.Call(context, BackendExampleNames.Observer, BackendExampleNames.Run);
        Term stacks = await runtime.Modules.Call(context, BackendExampleNames.Observer, BackendExampleNames.Stacks);
        result.TrySetResult([values, stacks]);
    }
    catch (Exception error)
    {
        result.TrySetException(error);
        throw;
    }

    return Nil.Value;
});
Term[] observed = await result.Task.WaitAsync(BackendExampleNames.Deadline);
if (!(await worker.Completion.WaitAsync(BackendExampleNames.Deadline)).Equals(Term.A(BackendExampleNames.Normal)))
    throw new InvalidOperationException(BackendExampleNames.Failure);
var diagnostics = runtime.LibrarySelection.Modules.Select(module => new
{
    module.Module,
    Backend = module.Backend.ToString(),
    module.InputSha256,
    NativeMd5 = Convert.ToHexString(module.NativeMd5.ToArray()),
    Exports = module.Exports.Count,
    NativePrimitives = module.NativePrimitives.Select(value => new { value.Name, value.Arity }),
    module.ManagedAssemblies,
    Dependencies = module.Dependencies.Select(value => new { value.Module, Backend = value.Backend.ToString() })
}).ToArray();
string report = JsonSerializer.Serialize(diagnostics, RuntimeLibraryManifest.SerializationOptions);
if (args.Length != 0)
{
    Directory.CreateDirectory(args[1]);
    string[] files = [BackendExampleNames.ValuesFile, BackendExampleNames.StacksFile];
    for (int index = 0; index < files.Length; index++)
        File.WriteAllBytes(
            Path.Combine(args[1], files[index]),
            ExternalTermFormat.Encode(
                observed[index],
                new EtfEncodingOptions { MinorVersion = BackendExampleNames.EtfMinorVersion, FunctionOwner = runtime.InitializationPid }
            )
        );
    File.WriteAllText(Path.Combine(args[1], BackendExampleNames.ReportFile), report);
}
Console.WriteLine(report);
Console.WriteLine(observed[0]);
