using Erlang;
namespace Erlang.Tests;

internal static class EtsDeletionOriginal412Preparation
{
    // Explicit generated source fragment, not unchanged whole official ets.beam.
    // Register existing ETS11 first. This separate opt-in adds only two real native kernels.
    internal static PreparedBeamImage Prepare(ProcessRuntime runtime, ModuleRegistry registry)
    {
        RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        PreparedBeamImage facet = registry.PrepareBeam(
            EtsDeletionOriginal412Access.Resource(EtsDeletionOriginal412Names.FacetBeam),
            new BeamDependencySelection([EtsDeletionOriginal412Names.Runtime, EtsDeletionOriginal412Names.Ets])
        );
        var actual = facet.NativePrimitiveExports.Select(signature => (signature.Name, signature.Arity)).ToHashSet();
        EtsDeletionOriginal412Access.Require(facet.Name == EtsDeletionOriginal412Names.Ets
            && actual.Count == EtsDeletionOriginal412Names.CapturedNatives
            && actual.SetEquals(EtsDeletionOriginal412Names.NativeSignatures));

        return facet;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage facet)
    {
        var exports = registry.Exports.Where(export => export.Module == EtsDeletionOriginal412Names.Ets).ToArray();
        EtsDeletionOriginal412Access.Require(exports.Length == EtsDeletionOriginal412Names.PublicExports);
        foreach (var export in exports)
            EtsDeletionOriginal412Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), facet));
    }
}
