using Erlang;
namespace Erlang.Tests;

internal static class EtsQueriesOriginal414Preparation
{
    // Explicit generated source fragment, not unchanged whole official ets.beam.
    // Register existing ETS11 first. Separate opt-ins add two deletion and three query native kernels.
    internal static PreparedBeamImage Prepare(ProcessRuntime runtime, ModuleRegistry registry)
    {
        RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        RuntimeBootstrapEtsQueries414.Register(runtime, registry);
        PreparedBeamImage facet = registry.PrepareBeam(
            EtsQueriesOriginal414Access.Resource(EtsQueriesOriginal414Names.FacetBeam),
            new BeamDependencySelection([EtsQueriesOriginal414Names.Runtime, EtsQueriesOriginal414Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsQueriesOriginal414Access.Require(facet.Name == EtsQueriesOriginal414Names.Ets
            && actual.Count == EtsQueriesOriginal414Names.CapturedNatives
            && actual.SetEquals(EtsQueriesOriginal414Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == EtsQueriesOriginal414Names.Ets).ToArray();
        EtsQueriesOriginal414Access.Require(exports.Length == EtsQueriesOriginal414Names.PublicExports);
        foreach (var export in exports)
            EtsQueriesOriginal414Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }

    internal static PreparedBeamImage Prepare171(ProcessRuntime runtime, ModuleRegistry registry, global::Erlang.Tests.PreparedBeamFixtureScope171 scope)
    {
        PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(
            EtsQueriesOriginal414Access.Resource(EtsQueriesOriginal414Names.FacetBeam),
            new BeamDependencySelection([EtsQueriesOriginal414Names.Runtime, EtsQueriesOriginal414Names.Ets])
        ));
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsQueriesOriginal414Access.Require(facet.Name == EtsQueriesOriginal414Names.Ets
            && actual.Count == EtsQueriesOriginal414Names.CapturedNatives
            && actual.SetEquals(EtsQueriesOriginal414Names.NativeSignatures));

        return facet;
    }

    internal static void Register171(ProcessRuntime runtime, ModuleRegistry registry)
    {
        RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        RuntimeBootstrapEtsQueries414.Register(runtime, registry);
    }
}
