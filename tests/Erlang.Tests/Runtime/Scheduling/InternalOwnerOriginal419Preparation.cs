using Erlang;
namespace Erlang.Tests;

internal static class InternalOwnerOriginal419Preparation
{
    // Exact selected pinned native clauses; full original 71-native gate remains intact.
    // Whole-module admission and physical cross-VM identity are not claimed.
    internal static PreparedBeamImage Prepare(ProcessRuntime runtime, ModuleRegistry registry)
    {
        PreparedBeamImage facet = registry.PrepareBeam(
            InternalOwnerOriginal419Access.Resource(InternalOwnerOriginal419Names.FacetBeam),
            new BeamDependencySelection([InternalOwnerOriginal419Names.Runtime, InternalOwnerOriginal419Names.Internal])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        InternalOwnerOriginal419Access.Require(facet.Name == InternalOwnerOriginal419Names.Internal
            && actual.Count == InternalOwnerOriginal419Names.CapturedNatives
            && actual.SetEquals(InternalOwnerOriginal419Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == InternalOwnerOriginal419Names.Internal).ToArray();
        InternalOwnerOriginal419Access.Require(exports.Length == InternalOwnerOriginal419Names.PublicExports);
        foreach (var export in exports)
            InternalOwnerOriginal419Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }
}
