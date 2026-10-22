using System.Text.Json;
using Erlang;

namespace Erlang.Tests;

internal static class PrimTtyOriginal412Runtime
{
    internal static async Task<ProcessRuntime> Create(string admission)
    {
        var runtime = new ProcessRuntime();
        try
        {
            // Genuine opt-in native five; original re8 remains unselected/refused.
            _ = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, admission);
            await runtime.InitializeModulesAsync(registry =>
            {
                RuntimeNativePrimitiveProviders.Register(runtime, registry,
                    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.PersistentTermCore);
                string[] declared = JsonSerializer.Deserialize<string[]>(
                    PrimTtyOriginal412Access.Resource(PrimTtyOriginal412Names.Dependencies))!;
                string[] modules = JsonSerializer.Deserialize<string[]>(
                    PrimTtyOriginal412Access.Resource(PrimTtyOriginal412Names.Modules))!;
                var dependencies = new BeamDependencySelection(declared);
                PreparedBeamImage[] images = modules.Select(module => registry.PrepareBeam(
                    PrimTtyOriginal412Access.Resource(module + PrimTtyOriginal412Names.Beam), dependencies
                )).ToArray();
                PreparedBeamImage tty = images.Single(image => image.Name == PrimTtyOriginal412Names.Tty);
                RuntimeBootstrapTty.Register(
                    runtime,
                    registry,
                    tty,
                    RuntimeBootstrapTtyProfile.WindowsRedirectedNoTermcapAnsi
                );
                PrimTtyOriginal412Access.Require(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
                foreach (PreparedBeamImage image in images)
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        PrimTtyOriginal412Access.Require(ReferenceEquals(registry.SourceOwner(
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
