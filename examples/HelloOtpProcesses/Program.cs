using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static HelloOtpProcesses.ProcessLibraryNames;

await using var runtime = new ProcessRuntime();
await runtime.InitializeModulesAsync(registry =>
{
    Erlang.Otp.OtpModules.Register(registry);
    PreparedBeamImage[] images = Libraries.Concat([GenSysObserver, GenServer, StatusCallback, StatusLegacy]).Select(module =>
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, ImagesDirectory, module + Beam));
        if (Convert.ToHexString(SHA256.HashData(bytes)) != Digests[module + Beam])
            throw new InvalidOperationException(Failure);

        return registry.PrepareBeam(bytes, new BeamDependencySelection(Dependencies));
    }).ToArray();
    if (registry.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
        throw new InvalidOperationException(Failure);
    foreach (PreparedBeamImage image in images)
        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
            if (!ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image))
                throw new InvalidOperationException(Failure);
});
var completed = new TaskCompletionSource<Term>();
ProcessHandle actor = runtime.Spawn(async context =>
{
    try
    {
        completed.TrySetResult(await runtime.Modules.Call(
            context,
            GenSysObserver,
            Run,
            Term.A(All)
        ));
    }
    catch (Exception failure)
    {
        completed.TrySetException(failure);
    }

    return Nil.Value;
});
Term observations = await completed.Task.WaitAsync(TimeSpan.FromSeconds(60));
await actor.Completion.WaitAsync(TimeSpan.FromSeconds(60));
Console.WriteLine(JsonSerializer.Serialize(new { OriginalModules = Libraries, Observations = Cons.Items(observations).Count(), Outcomes = observations.ToString() }));
