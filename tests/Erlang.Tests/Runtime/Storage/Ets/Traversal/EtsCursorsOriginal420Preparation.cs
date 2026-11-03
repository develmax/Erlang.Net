using Erlang;
namespace Erlang.Tests;

internal static class EtsCursorsOriginal420Preparation
{
    // Explicit generated source fragment, not unchanged whole official ets.beam.
    // Explicit real native48 composition; whole official ETS51 gate remains unchanged.
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
        RuntimeBootstrapEtsCursor420.Register(runtime, registry);
        PreparedBeamImage facet = registry.PrepareBeam(
            EtsCursorsOriginal420Access.Resource(EtsCursorsOriginal420Names.FacetBeam),
            new BeamDependencySelection([EtsCursorsOriginal420Names.Runtime, EtsCursorsOriginal420Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsCursorsOriginal420Access.Require(facet.Name == EtsCursorsOriginal420Names.Ets
            && actual.Count == EtsCursorsOriginal420Names.CapturedNatives
            && actual.SetEquals(EtsCursorsOriginal420Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == EtsCursorsOriginal420Names.Ets).ToArray();
        EtsCursorsOriginal420Access.Require(exports.Length == EtsCursorsOriginal420Names.PublicExports);
        foreach (var export in exports)
            EtsCursorsOriginal420Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }

    internal static PreparedBeamImage Prepare171(ProcessRuntime runtime, ModuleRegistry registry, global::Erlang.Tests.PreparedBeamFixtureScope171 scope)
    {
        PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(
            EtsCursorsOriginal420Access.Resource(EtsCursorsOriginal420Names.FacetBeam),
            new BeamDependencySelection([EtsCursorsOriginal420Names.Runtime, EtsCursorsOriginal420Names.Ets])
        ));
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsCursorsOriginal420Access.Require(facet.Name == EtsCursorsOriginal420Names.Ets
            && actual.Count == EtsCursorsOriginal420Names.CapturedNatives
            && actual.SetEquals(EtsCursorsOriginal420Names.NativeSignatures));

        return facet;
    }

    internal static void Register171(ProcessRuntime runtime, ModuleRegistry registry)
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
        RuntimeBootstrapEtsCursor420.Register(runtime, registry);
    }
}
