using System.Text.Json;
using Erlang;

namespace Erlang.Tests;

internal static class KernelBootOriginal453Runtime
{
    internal static async Task<ProcessRuntime> Create(string admission)
    {
        ProcessRuntime runtime = await StandardBootOriginal412Runtime.Create(admission, true, true);
        try
        {
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                async (registry, scope) =>
            {
                await runtime.InitializeModulesAsync(_ =>
                {
                    {
                        RuntimeBootstrapInternalPorts416.Register(runtime, registry);
                    }
                    {
                        RuntimeBootstrapAtomics.Register(runtime, registry);
                    }
                    {
                        RuntimeBootstrapEtsInspection420.Register(runtime, registry);
                    }
                });
                string[] modules = JsonSerializer.Deserialize<string[]>(StandardBootOriginal412Access.Resource(StandardBootOriginal412Names.Dependencies))!;
                var dependencies = new BeamDependencySelection(modules.Append(KernelBootOriginal453Names.Worker));
                var images = new List<PreparedBeamImage>
                            {
                                scope.Prepare(() => registry.PrepareBeam(RegexThreeOriginal413Access.Read(RegexThreeOriginal413Names.Module + KernelBootOriginal453Names.Beam), dependencies))
                            };
                foreach (string module in new[] { KernelBootOriginal453Names.Internal, KernelBootOriginal453Names.Purger })
                    images.Add(scope.Prepare(() => registry.PrepareBeam(KernelBootOriginal453Access.Read(module + KernelBootOriginal453Names.Beam), dependencies)));

                return _ =>
                {
                    KernelBootOriginal453Access.Require(registry.PublishPreparedBeam(images.ToArray()).Status == BeamPublicationStatus.Published);
                    foreach (PreparedBeamImage image in images)
                        foreach (var export in registry.Exports.Where(item => item.Module == image.Name))
                            KernelBootOriginal453Access.Require(ReferenceEquals(registry.SourceOwner(image.Name, export.Function, export.Arity), image));
                    // Idempotence must survive ownership moving to the exact original BEAM wrappers.
                    RuntimeBootstrapFileSystem.Register(runtime, registry);
                };
            }
            );

            return runtime;
        }
        catch
        {
            await runtime.DisposeAsync();
            throw;
        }
    }
}
