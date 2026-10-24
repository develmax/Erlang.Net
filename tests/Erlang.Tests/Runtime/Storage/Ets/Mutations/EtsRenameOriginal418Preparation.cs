using Erlang;
namespace Erlang.Tests;

internal static class EtsRenameOriginal418Preparation
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
        RuntimeBootstrapEtsRename418.Register(runtime, registry);
        PreparedBeamImage facet = registry.PrepareBeam(
            EtsRenameOriginal418Access.Resource(EtsRenameOriginal418Names.FacetBeam),
            new BeamDependencySelection([EtsRenameOriginal418Names.Runtime, EtsRenameOriginal418Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsRenameOriginal418Access.Require(facet.Name == EtsRenameOriginal418Names.Ets
            && actual.Count == EtsRenameOriginal418Names.CapturedNatives
            && actual.SetEquals(EtsRenameOriginal418Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == EtsRenameOriginal418Names.Ets).ToArray();
        EtsRenameOriginal418Access.Require(exports.Length == EtsRenameOriginal418Names.PublicExports);
        foreach (var export in exports)
            EtsRenameOriginal418Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }
}
