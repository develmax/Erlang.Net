using Erlang;
namespace Erlang.Tests;

internal static class EtsCountersOriginal417Preparation
{
    // Explicit generated source fragment, not unchanged whole official ets.beam.
    // Register real ETS11 before this explicit deletion2/query3/mutation5 composition.
    internal static PreparedBeamImage Prepare(ProcessRuntime runtime, ModuleRegistry registry)
    {
        RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        RuntimeBootstrapEtsQueries414.Register(runtime, registry);
        RuntimeBootstrapEtsMutations414.Register(runtime, registry);
        RuntimeBootstrapEtsTraversal416.Register(runtime, registry);
        RuntimeBootstrapEtsCounter417.Register(runtime, registry);
        PreparedBeamImage facet = registry.PrepareBeam(
            EtsCountersOriginal417Access.Resource(EtsCountersOriginal417Names.FacetBeam),
            new BeamDependencySelection([EtsCountersOriginal417Names.Runtime, EtsCountersOriginal417Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsCountersOriginal417Access.Require(facet.Name == EtsCountersOriginal417Names.Ets
            && actual.Count == EtsCountersOriginal417Names.CapturedNatives
            && actual.SetEquals(EtsCountersOriginal417Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == EtsCountersOriginal417Names.Ets).ToArray();
        EtsCountersOriginal417Access.Require(exports.Length == EtsCountersOriginal417Names.PublicExports);
        foreach (var export in exports)
            EtsCountersOriginal417Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }

    internal static PreparedBeamImage Prepare171(ProcessRuntime runtime, ModuleRegistry registry, global::Erlang.Tests.PreparedBeamFixtureScope171 scope)
    {
        PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(
            EtsCountersOriginal417Access.Resource(EtsCountersOriginal417Names.FacetBeam),
            new BeamDependencySelection([EtsCountersOriginal417Names.Runtime, EtsCountersOriginal417Names.Ets])
        ));
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsCountersOriginal417Access.Require(facet.Name == EtsCountersOriginal417Names.Ets
            && actual.Count == EtsCountersOriginal417Names.CapturedNatives
            && actual.SetEquals(EtsCountersOriginal417Names.NativeSignatures));

        return facet;
    }

    internal static void Register171(ProcessRuntime runtime, ModuleRegistry registry)
    {
        RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        RuntimeBootstrapEtsQueries414.Register(runtime, registry);
        RuntimeBootstrapEtsMutations414.Register(runtime, registry);
        RuntimeBootstrapEtsTraversal416.Register(runtime, registry);
        RuntimeBootstrapEtsCounter417.Register(runtime, registry);
    }
}
