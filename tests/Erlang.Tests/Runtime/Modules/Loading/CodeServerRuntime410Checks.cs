namespace Erlang.Tests;

internal static class CodeServerRuntime410Checks
{
    internal static async Task Run()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using ProcessRuntime runtime = await Create();

        await CodeServerOriginal410Checks.Run(runtime, cancellation.Token);
    }

    internal static Task<ProcessRuntime> Create() => Create(Array.Empty<string>());

    internal static Task<ProcessRuntime> Create(IEnumerable<string> fixtures)
        => Prepare(new ProcessRuntime(), fixtures);

    internal static async Task<ProcessRuntime> Prepare(ProcessRuntime runtime, IEnumerable<string> fixtures)
    {
        try
        {
            await runtime.InitializeModulesAsync(registry =>
            {
                RuntimeNativePrimitiveProviders.Register(runtime, registry,
                    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.PersistentTermCore);
                RuntimeBootstrapFileSystem.Register(runtime, registry);
            });
            string[] declared = System.Text.Json.JsonSerializer.Deserialize<string[]>(
                LibraryDependencyOriginalHostChecks.ReadRetainedResource(LibraryDependencyOriginalNames.Dependencies))!;
            var dependencies = new BeamDependencySelection(declared.Concat(CodeServerRuntime410Names.Modules)
                .Concat(fixtures)
                .Concat([CodeServerOriginal410Names.Erts, CodeServerOriginal410Names.Subject, CodeServerOriginal410Names.Observer,
                NativeProvider408Names.Ets, NativeProvider408Names.Module]));
            await PreparedBeamFixtureImages825.PrepareAndPublishAsync(
                runtime,
                CodeServerRuntime410Names.Modules.Select(module => module == LibraryDependencyOriginalNames.FileModule
                    ? LibraryDependencyOriginalHostChecks.ReadRetainedResource(module + CodeServerOriginal410Names.Beam)
                    : CodeServerOriginal410Fixture.Resource(module + CodeServerOriginal410Names.Beam)),
                dependencies,
                (registry, images) =>
                {
                    if (registry.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
                        throw new InvalidOperationException(CodeServerRuntime410Names.Failure);
                    foreach (PreparedBeamImage image in images)
                    {
                        var exports = registry.Exports.Where(export => export.Module == image.Name).ToArray();
                        if (exports.Length == 0 || exports.Any(export => !ReferenceEquals(
                            registry.SourceOwner(export.Module, export.Function, export.Arity),
                            image
                        )))
                            throw new InvalidOperationException(CodeServerRuntime410Names.Failure);
                    }
                }
            );
            await using (var actor = new RuntimeNodeFoundationHostActor(runtime))
                _ = await actor.InvokeAsync(async context => await runtime.Modules.Call(context,
                    CodeServerRuntime410Names.PrimFile, CodeServerRuntime410Names.OnLoad));

            return runtime;
        }
        catch
        {
            await runtime.DisposeAsync();

            throw;
        }
    }
}
