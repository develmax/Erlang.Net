using Erlang;
namespace Erlang.Tests;

internal static class EtsMutationsOriginal414Preparation
{
    // Explicit generated source fragment, not unchanged whole official ets.beam.
    // Register real ETS11 before this explicit deletion2/query3/mutation5 composition.
    internal static PreparedBeamImage Prepare(ProcessRuntime runtime, ModuleRegistry registry)
    {
        RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        RuntimeBootstrapEtsQueries414.Register(runtime, registry);
        RuntimeBootstrapEtsMutations414.Register(runtime, registry);
        PreparedBeamImage facet = registry.PrepareBeam(
            EtsMutationsOriginal414Access.Resource(EtsMutationsOriginal414Names.FacetBeam),
            new BeamDependencySelection([EtsMutationsOriginal414Names.Runtime, EtsMutationsOriginal414Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsMutationsOriginal414Access.Require(facet.Name == EtsMutationsOriginal414Names.Ets
            && actual.Count == EtsMutationsOriginal414Names.CapturedNatives
            && actual.SetEquals(EtsMutationsOriginal414Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == EtsMutationsOriginal414Names.Ets).ToArray();
        EtsMutationsOriginal414Access.Require(exports.Length == EtsMutationsOriginal414Names.PublicExports);
        foreach (var export in exports)
            EtsMutationsOriginal414Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }
}
