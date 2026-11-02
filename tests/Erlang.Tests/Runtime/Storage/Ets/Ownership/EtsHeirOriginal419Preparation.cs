using Erlang;
namespace Erlang.Tests;

internal static class EtsHeirOriginal419Preparation
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
            EtsHeirOriginal419Access.Resource(EtsHeirOriginal419Names.FacetBeam),
            new BeamDependencySelection([EtsHeirOriginal419Names.Runtime, EtsHeirOriginal419Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsHeirOriginal419Access.Require(facet.Name == EtsHeirOriginal419Names.Ets
            && actual.Count == EtsHeirOriginal419Names.CapturedNatives
            && actual.SetEquals(EtsHeirOriginal419Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == EtsHeirOriginal419Names.Ets).ToArray();
        EtsHeirOriginal419Access.Require(exports.Length == EtsHeirOriginal419Names.PublicExports);
        foreach (var export in exports)
            EtsHeirOriginal419Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }

    internal static PreparedBeamImage Prepare171(ProcessRuntime runtime, ModuleRegistry registry, global::Erlang.Tests.PreparedBeamFixtureScope171 scope)
    {
        PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(
            EtsHeirOriginal419Access.Resource(EtsHeirOriginal419Names.FacetBeam),
            new BeamDependencySelection([EtsHeirOriginal419Names.Runtime, EtsHeirOriginal419Names.Ets])
        ));
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsHeirOriginal419Access.Require(facet.Name == EtsHeirOriginal419Names.Ets
            && actual.Count == EtsHeirOriginal419Names.CapturedNatives
            && actual.SetEquals(EtsHeirOriginal419Names.NativeSignatures));

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
