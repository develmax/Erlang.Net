using Erlang;
namespace Erlang.Tests;

internal static class EtsNewHeirOriginal419Preparation
{
    // Explicit generated source fragment, not unchanged whole official ets.beam.
    // Explicit real native34 composition; whole official ETS51 gate remains unchanged.
    internal static PreparedBeamImage Prepare(ProcessRuntime runtime, ModuleRegistry registry)
    {
        RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        RuntimeBootstrapEtsQueries414.Register(runtime, registry);
        RuntimeBootstrapEtsMutations414.Register(runtime, registry);
        RuntimeBootstrapEtsTraversal416.Register(runtime, registry);
        RuntimeBootstrapEtsCounter417.Register(runtime, registry);
        RuntimeBootstrapEtsRename418.Register(runtime, registry);
        RuntimeBootstrapEtsHeir419.Register(runtime, registry);
        PreparedBeamImage facet = registry.PrepareBeam(
            EtsNewHeirOriginal419Access.Resource(EtsNewHeirOriginal419Names.FacetBeam),
            new BeamDependencySelection([EtsNewHeirOriginal419Names.Runtime, EtsNewHeirOriginal419Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsNewHeirOriginal419Access.Require(facet.Name == EtsNewHeirOriginal419Names.Ets
            && actual.Count == EtsNewHeirOriginal419Names.CapturedNatives
            && actual.SetEquals(EtsNewHeirOriginal419Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == EtsNewHeirOriginal419Names.Ets).ToArray();
        EtsNewHeirOriginal419Access.Require(exports.Length == EtsNewHeirOriginal419Names.PublicExports);
        foreach (var export in exports)
            EtsNewHeirOriginal419Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }

    internal static PreparedBeamImage Prepare171(ProcessRuntime runtime, ModuleRegistry registry, global::Erlang.Tests.PreparedBeamFixtureScope171 scope)
    {
        PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(
            EtsNewHeirOriginal419Access.Resource(EtsNewHeirOriginal419Names.FacetBeam),
            new BeamDependencySelection([EtsNewHeirOriginal419Names.Runtime, EtsNewHeirOriginal419Names.Ets])
        ));
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsNewHeirOriginal419Access.Require(facet.Name == EtsNewHeirOriginal419Names.Ets
            && actual.Count == EtsNewHeirOriginal419Names.CapturedNatives
            && actual.SetEquals(EtsNewHeirOriginal419Names.NativeSignatures));

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
    }
}
