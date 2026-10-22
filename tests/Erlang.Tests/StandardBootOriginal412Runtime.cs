using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class StandardBootOriginal412Runtime
{
    internal static async Task<ProcessRuntime> Create(string admission, bool includeCollectors = false, bool includeWholePersistent = false)
    {
        var runtime = new ProcessRuntime();
        try
        {
            _ = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, admission);
            await runtime.InitializeModulesAsync(registry =>
            {
                RuntimeNativePrimitiveProviders.Register(
                    runtime,
                    registry,
                    RuntimeNativePrimitives.EtsStorageCore | (includeWholePersistent ? RuntimeNativePrimitives.None : RuntimeNativePrimitives.PersistentTermCore)
                );
                RuntimeBootstrapFileSystem.Register(runtime, registry);
                RuntimeBootstrapScheduler.Register(runtime, registry);
                PreparedBeamImage deletionFacet = EtsDeletionOriginal412Preparation.Prepare(runtime, registry);
                string[] declared = JsonSerializer.Deserialize<string[]>(
                    StandardBootOriginal412Access.Resource(StandardBootOriginal412Names.Dependencies))!;
                string[] modules = JsonSerializer.Deserialize<string[]>(
                    StandardBootOriginal412Access.Resource(StandardBootOriginal412Names.Modules))!;
                var dependencies = new BeamDependencySelection(declared);
                PreparedBeamImage[] images = modules.Select(module => registry.PrepareBeam(
                    StandardBootOriginal412Access.Resource(module + StandardBootOriginal412Names.Beam), dependencies
                )).ToArray();
                images = images.Append(deletionFacet).ToArray();
                if (includeWholePersistent)
                    images = images.Append(StandardBootPersistent417Preparation.Prepare(registry, dependencies)).ToArray();
                if (includeCollectors)
                    images = images.Concat(StandardBootCollectors415Preparation.Prepare(registry, dependencies)).ToArray();
                RuntimeBootstrapTty.Register(
                    runtime,
                    registry,
                    images.Single(image => image.Name == StandardBootOriginal412Names.Tty),
                    RuntimeBootstrapTtyProfile.WindowsRedirectedNoTermcapAnsi
                );
                StandardBootOriginal412Access.Require(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
                EtsDeletionOriginal412Preparation.AssertPublished(registry, deletionFacet);
                foreach (PreparedBeamImage image in images)
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        StandardBootOriginal412Access.Require(ReferenceEquals(registry.SourceOwner(
                            image.Name, export.Function, export.Arity), image));
            });

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
