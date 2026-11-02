namespace Erlang.Tests;

internal static class DdllErrorOriginal416Preparation
{
    internal static void Prepare(ProcessRuntime runtime, ModuleRegistry registry)
    {
        RuntimeBootstrapDdllError416.Register(runtime, registry);
        var selection = new BeamDependencySelection([DdllErrorOriginal416Names.Runtime, DdllErrorOriginal416Names.Internal]);
        PreparedBeamImage facet = registry.PrepareBeam(DdllErrorOriginal416Access.Resource(DdllErrorOriginal416Names.FacetBeam), selection);
        PreparedBeamImage worker = registry.PrepareBeam(DdllErrorOriginal416Access.Resource(DdllErrorOriginal416Names.WorkerBeam), selection);
        DdllErrorOriginal416Access.Require(facet.Name == DdllErrorOriginal416Names.Internal
            && facet.NativePrimitiveExports.Count == DdllErrorOriginal416Names.NativeCount
            && facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet()
                .SetEquals(DdllErrorOriginal416Names.NativeSignatures)
            && worker.Name == DdllErrorOriginal416Names.Module && worker.NativePrimitiveExports.Count == 0);
        DdllErrorOriginal416Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
        foreach (PreparedBeamImage image in new[] { facet, worker })
            foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                DdllErrorOriginal416Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
    }

    internal static async Task Prepare171Async(ProcessRuntime runtime)
    {
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            async (registry, scope) =>
        {
            await runtime.InitializeModulesAsync(_ =>
            {
                RuntimeBootstrapDdllError416.Register(runtime, registry);
            });
            var selection = new BeamDependencySelection([DdllErrorOriginal416Names.Runtime, DdllErrorOriginal416Names.Internal]);
            PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(DdllErrorOriginal416Access.Resource(DdllErrorOriginal416Names.FacetBeam), selection));
            PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(DdllErrorOriginal416Access.Resource(DdllErrorOriginal416Names.WorkerBeam), selection));
            DdllErrorOriginal416Access.Require(facet.Name == DdllErrorOriginal416Names.Internal
                && facet.NativePrimitiveExports.Count == DdllErrorOriginal416Names.NativeCount
                && facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet()
                    .SetEquals(DdllErrorOriginal416Names.NativeSignatures)
                && worker.Name == DdllErrorOriginal416Names.Module && worker.NativePrimitiveExports.Count == 0);

            return _ =>
            {
                DdllErrorOriginal416Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                foreach (PreparedBeamImage image in new[] { facet, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        DdllErrorOriginal416Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            };
        }
        );
    }
}
