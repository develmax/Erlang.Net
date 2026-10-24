using Erlang;
namespace Erlang.Tests;

internal static class CountersOriginal421Preparation
{
    // Exact selected pinned native clauses; full original 71-native gate remains intact.
    // Whole-module admission and physical cross-VM identity are not claimed.
    internal static PreparedBeamImage Prepare(ProcessRuntime runtime, ModuleRegistry registry)
    {
        PreparedBeamImage facet = registry.PrepareBeam(
            CountersOriginal421Access.Resource(CountersOriginal421Names.FacetBeam),
            new BeamDependencySelection([CountersOriginal421Names.Runtime, CountersOriginal421Names.Internal])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        CountersOriginal421Access.Require(facet.Name == CountersOriginal421Names.Internal
            && actual.Count == CountersOriginal421Names.CapturedNatives
            && actual.SetEquals(CountersOriginal421Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == CountersOriginal421Names.Internal).ToArray();
        CountersOriginal421Access.Require(exports.Length == CountersOriginal421Names.PublicExports);
        foreach (var export in exports)
            CountersOriginal421Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }
}
