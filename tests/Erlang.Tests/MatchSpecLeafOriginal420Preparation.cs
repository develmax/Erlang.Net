using Erlang;
namespace Erlang.Tests;

internal static class MatchSpecLeafOriginal420Preparation
{
    // Explicit generated source fragment, not unchanged whole official ets.beam.
    // Explicit real native40 composition; whole official ETS51 gate remains unchanged.
    internal static PreparedBeamImage Prepare(ProcessRuntime runtime, ModuleRegistry registry)
    {
        RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        RuntimeBootstrapEtsQueries414.Register(runtime, registry);
        RuntimeBootstrapEtsMutations414.Register(runtime, registry);
        RuntimeBootstrapEtsTraversal416.Register(runtime, registry);
        RuntimeBootstrapEtsCounter417.Register(runtime, registry);
        RuntimeBootstrapEtsRename418.Register(runtime, registry);
        RuntimeBootstrapEtsHeir419.Register(runtime, registry);
        RuntimeBootstrapEtsCompiled419.Register(runtime, registry);
        RuntimeBootstrapEtsAll420.Register(runtime, registry);
        RuntimeBootstrapEtsFixation420.Register(runtime, registry);
        PreparedBeamImage facet = registry.PrepareBeam(
            MatchSpecLeafOriginal420Access.Resource(MatchSpecLeafOriginal420Names.FacetBeam),
            new BeamDependencySelection([MatchSpecLeafOriginal420Names.Runtime, MatchSpecLeafOriginal420Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        MatchSpecLeafOriginal420Access.Require(facet.Name == MatchSpecLeafOriginal420Names.Ets
            && actual.Count == MatchSpecLeafOriginal420Names.CapturedNatives
            && actual.SetEquals(MatchSpecLeafOriginal420Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == MatchSpecLeafOriginal420Names.Ets).ToArray();
        MatchSpecLeafOriginal420Access.Require(exports.Length == MatchSpecLeafOriginal420Names.PublicExports);
        foreach (var export in exports)
            MatchSpecLeafOriginal420Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }
}
