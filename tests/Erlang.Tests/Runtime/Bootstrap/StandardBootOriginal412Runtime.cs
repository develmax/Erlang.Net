using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class StandardBootOriginal412Runtime
{
    internal static async Task<ProcessRuntime> Create(
        string admission,
        bool includeCollectors = false,
        bool includeWholePersistent = false,
        bool ownedProfile = false,
        Func<ProcessRuntime, ModuleRegistry, PreparedBeamImage>? prepareEts = null
    )
    {
        var runtime = OwnedGuest506RuntimeFactory.Create(ownedProfile);
        try
        {
            _ = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, admission);
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                async (registry, scope) =>
            {
                await runtime.InitializeModulesAsync(_ =>
                {
                    {
                        RuntimeNativePrimitiveProviders.Register(
                            runtime,
                            registry,
                            RuntimeNativePrimitives.EtsStorageCore | (includeWholePersistent ? RuntimeNativePrimitives.None : RuntimeNativePrimitives.PersistentTermCore)
                        );
                    }
                    {
                        RuntimeBootstrapFileSystem.Register(runtime, registry);
                    }
                    {
                        RuntimeBootstrapScheduler.Register(runtime, registry);
                    }
                    {
                        EtsDeletionOriginal412Preparation.Register171(runtime, registry);
                    }
                });
                PreparedBeamImage deletionFacet = prepareEts is null
                    ? EtsDeletionOriginal412Preparation.Prepare171(runtime, registry, scope)
                    : scope.Prepare(() => prepareEts(runtime, registry));
                StandardBootOriginal412Access.Require(deletionFacet.Name == EtsDeletionOriginal412Names.Ets);
                string[] declared = JsonSerializer.Deserialize<string[]>(
                    StandardBootOriginal412Access.Resource(StandardBootOriginal412Names.Dependencies))!;
                string[] modules = JsonSerializer.Deserialize<string[]>(
                    StandardBootOriginal412Access.Resource(StandardBootOriginal412Names.Modules))!;
                var dependencies = new BeamDependencySelection(declared);
                PreparedBeamImage[] images = modules.Select(module => scope.Prepare(() => registry.PrepareBeam(
                    StandardBootOriginal412Access.Resource(module + StandardBootOriginal412Names.Beam), dependencies
                ))).ToArray();
                images = prepareEts is null ? images.Append(deletionFacet).ToArray() : images.Prepend(deletionFacet).ToArray();
                if (includeWholePersistent)
                    images = images.Append(StandardBootPersistent417Preparation.Prepare171(registry, dependencies, scope)).ToArray();
                if (includeCollectors)
                    images = images.Concat(StandardBootCollectors415Preparation.Prepare171(registry, dependencies, scope)).ToArray();

                return _ =>
                {
                    RuntimeBootstrapTty.Register(
                        runtime,
                        registry,
                        images.Single(image => image.Name == StandardBootOriginal412Names.Tty),
                        RuntimeBootstrapTtyProfile.WindowsRedirectedNoTermcapAnsi
                    );
                    StandardBootOriginal412Access.Require(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
                    if (prepareEts is null)
                        EtsDeletionOriginal412Preparation.AssertPublished(registry, deletionFacet);
                    foreach (PreparedBeamImage image in images)
                        foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                            StandardBootOriginal412Access.Require(ReferenceEquals(registry.SourceOwner(
                            image.Name, export.Function, export.Arity), image));
                };
            }
            );

            return runtime;
        }
        catch (Exception original)
        {
            try
            {
                await runtime.DisposeAsync();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }
}
