using Erlang;
namespace Erlang.Tests;

internal static class EtsAllOriginal420Preparation
{
    // Explicit generated source fragment, not unchanged whole official ets.beam.
    // Explicit real native38 composition; whole official ETS51 gate remains unchanged.
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
        PreparedBeamImage facet = registry.PrepareBeam(
            EtsAllOriginal420Access.Resource(EtsAllOriginal420Names.FacetBeam),
            new BeamDependencySelection([EtsAllOriginal420Names.Runtime, EtsAllOriginal420Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsAllOriginal420Access.Require(facet.Name == EtsAllOriginal420Names.Ets
            && actual.Count == EtsAllOriginal420Names.CapturedNatives
            && actual.SetEquals(EtsAllOriginal420Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == EtsAllOriginal420Names.Ets).ToArray();
        EtsAllOriginal420Access.Require(exports.Length == EtsAllOriginal420Names.PublicExports);
        foreach (var export in exports)
            EtsAllOriginal420Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }
}
