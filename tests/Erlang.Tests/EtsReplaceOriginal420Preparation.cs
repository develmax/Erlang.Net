using Erlang;
namespace Erlang.Tests;

internal static class EtsReplaceOriginal420Preparation
{
    // Explicit generated source fragment, not unchanged whole official ets.beam.
    // Explicit real native49 composition; whole official ETS51 gate remains unchanged.
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
        PreparedBeamImage facet = registry.PrepareBeam(
            EtsReplaceOriginal420Access.Resource(EtsReplaceOriginal420Names.FacetBeam),
            new BeamDependencySelection([EtsReplaceOriginal420Names.Runtime, EtsReplaceOriginal420Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsReplaceOriginal420Access.Require(facet.Name == EtsReplaceOriginal420Names.Ets
            && actual.Count == EtsReplaceOriginal420Names.CapturedNatives
            && actual.SetEquals(EtsReplaceOriginal420Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == EtsReplaceOriginal420Names.Ets).ToArray();
        EtsReplaceOriginal420Access.Require(exports.Length == EtsReplaceOriginal420Names.PublicExports);
        foreach (var export in exports)
            EtsReplaceOriginal420Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }
}
