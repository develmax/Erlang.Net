using Erlang;
namespace Erlang.Tests;

internal static class EtsInfoOriginal422Preparation
{
    // Explicit generated source fragment, not unchanged whole official ets.beam.
    // Explicit real native51 composition; whole official ETS51 gate remains unchanged.
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
        RuntimeBootstrapEtsReplace420.Register(runtime, registry);
        RuntimeBootstrapEtsInspection420.Register(runtime, registry);
        RuntimeBootstrapEtsInfo420.Register(runtime, registry);
        PreparedBeamImage facet = registry.PrepareBeam(
            EtsInfoOriginal422Access.Resource(EtsInfoOriginal422Names.FacetBeam),
            new BeamDependencySelection([EtsInfoOriginal422Names.Runtime, EtsInfoOriginal422Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsInfoOriginal422Access.Require(facet.Name == EtsInfoOriginal422Names.Ets
            && actual.Count == EtsInfoOriginal422Names.CapturedNatives
            && actual.SetEquals(EtsInfoOriginal422Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == EtsInfoOriginal422Names.Ets).ToArray();
        EtsInfoOriginal422Access.Require(exports.Length == EtsInfoOriginal422Names.PublicExports);
        foreach (var export in exports)
            EtsInfoOriginal422Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }
}
