using Erlang;
namespace Erlang.Tests;

internal static class EtsTraversalOriginal416Preparation
{
    // Explicit generated source fragment, not unchanged whole official ets.beam.
    // Register real ETS11 before this explicit deletion2/query3/mutation5/traversal8 composition.
    internal static PreparedBeamImage Prepare(ProcessRuntime runtime, ModuleRegistry registry)
    {
        RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        RuntimeBootstrapEtsQueries414.Register(runtime, registry);
        RuntimeBootstrapEtsMutations414.Register(runtime, registry);
        RuntimeBootstrapEtsTraversal416.Register(runtime, registry);
        PreparedBeamImage facet = registry.PrepareBeam(
            EtsTraversalOriginal416Access.Resource(EtsTraversalOriginal416Names.FacetBeam),
            new BeamDependencySelection([EtsTraversalOriginal416Names.Runtime, EtsTraversalOriginal416Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsTraversalOriginal416Access.Require(facet.Name == EtsTraversalOriginal416Names.Ets
            && actual.Count == EtsTraversalOriginal416Names.CapturedNatives
            && actual.SetEquals(EtsTraversalOriginal416Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == EtsTraversalOriginal416Names.Ets).ToArray();
        EtsTraversalOriginal416Access.Require(exports.Length == EtsTraversalOriginal416Names.PublicExports);
        foreach (var export in exports)
            EtsTraversalOriginal416Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }
}
