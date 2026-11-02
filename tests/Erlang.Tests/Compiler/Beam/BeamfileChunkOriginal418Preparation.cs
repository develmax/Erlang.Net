using Erlang;

namespace Erlang.Tests;

internal static class BeamfileChunkOriginal418Preparation
{
    // Only the pinned original native stub clause is selected into this facet.
    // No full original erts_internal71 completeness gate is relaxed.
    internal static PreparedBeamImage Prepare(ModuleRegistry registry, BeamDependencySelection dependencies)
    {
        PreparedBeamImage facet = registry.PrepareBeam(BeamfileChunkOriginal418Access.Resource(BeamfileChunkOriginal418Names.FacetBeam), dependencies);
        BeamfileChunkOriginal418Access.Require(facet.Name == BeamfileChunkOriginal418Names.Internal
            && facet.NativePrimitiveExports.Count == BeamfileChunkOriginal418Names.CapturedNatives
            && facet.NativePrimitiveExports.Contains(new BeamFunctionSignature(BeamfileChunkOriginal418Names.NativeFunction, 2)));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == BeamfileChunkOriginal418Names.Internal).ToArray();
        BeamfileChunkOriginal418Access.Require(exports.Length == BeamfileChunkOriginal418Names.PublicExports
            && registry.HasExport(BeamfileChunkOriginal418Names.Internal, BeamfileChunkOriginal418Names.NativeFunction, 2)
            && registry.HasExport(BeamfileChunkOriginal418Names.Internal, BeamfileChunkOriginal418Names.ModuleInfo, 0)
            && registry.HasExport(BeamfileChunkOriginal418Names.Internal, BeamfileChunkOriginal418Names.ModuleInfo, 1));
        foreach (var export in exports)
            BeamfileChunkOriginal418Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }

    internal static PreparedBeamImage Prepare171(ModuleRegistry registry, BeamDependencySelection dependencies, global::Erlang.Tests.PreparedBeamFixtureScope171 scope)
    {
        PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(BeamfileChunkOriginal418Access.Resource(BeamfileChunkOriginal418Names.FacetBeam), dependencies));
        BeamfileChunkOriginal418Access.Require(facet.Name == BeamfileChunkOriginal418Names.Internal
            && facet.NativePrimitiveExports.Count == BeamfileChunkOriginal418Names.CapturedNatives
            && facet.NativePrimitiveExports.Contains(new BeamFunctionSignature(BeamfileChunkOriginal418Names.NativeFunction, 2)));

        return facet;
    }
}
