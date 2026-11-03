namespace Erlang.Tests;

internal static class InternalPortsOriginal416Preparation
{
    internal static void Prepare(ProcessRuntime runtime, ModuleRegistry registry)
    {
        RuntimeBootstrapInternalPorts416.Register(runtime, registry);
        var selection = new BeamDependencySelection([InternalPortsOriginal416Names.Runtime, InternalPortsOriginal416Names.Internal]);
        PreparedBeamImage facet = registry.PrepareBeam(InternalPortsOriginal416Access.Resource(InternalPortsOriginal416Names.FacetBeam), selection);
        PreparedBeamImage worker = registry.PrepareBeam(InternalPortsOriginal416Access.Resource(InternalPortsOriginal416Names.WorkerBeam), selection);
        InternalPortsOriginal416Access.Require(facet.Name == InternalPortsOriginal416Names.Internal
            && facet.NativePrimitiveExports.Count == InternalPortsOriginal416Names.NativeCount
            && facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet()
                .SetEquals(InternalPortsOriginal416Names.NativeSignatures)
            && worker.Name == InternalPortsOriginal416Names.Module && worker.NativePrimitiveExports.Count == 0);
        InternalPortsOriginal416Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
        foreach (PreparedBeamImage image in new[] { facet, worker })
            foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                InternalPortsOriginal416Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
    }

    internal static async Task Prepare171Async(ProcessRuntime runtime)
    {
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            async (registry, scope) =>
        {
            await runtime.InitializeModulesAsync(_ =>
            {
                RuntimeBootstrapInternalPorts416.Register(runtime, registry);
            });
            var selection = new BeamDependencySelection([InternalPortsOriginal416Names.Runtime, InternalPortsOriginal416Names.Internal]);
            PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(InternalPortsOriginal416Access.Resource(InternalPortsOriginal416Names.FacetBeam), selection));
            PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(InternalPortsOriginal416Access.Resource(InternalPortsOriginal416Names.WorkerBeam), selection));
            InternalPortsOriginal416Access.Require(facet.Name == InternalPortsOriginal416Names.Internal
                && facet.NativePrimitiveExports.Count == InternalPortsOriginal416Names.NativeCount
                && facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet()
                    .SetEquals(InternalPortsOriginal416Names.NativeSignatures)
                && worker.Name == InternalPortsOriginal416Names.Module && worker.NativePrimitiveExports.Count == 0);

            return _ =>
            {
                InternalPortsOriginal416Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                foreach (PreparedBeamImage image in new[] { facet, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        InternalPortsOriginal416Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            };
        }
        );
    }
}
