namespace Erlang.Tests;

internal static class DebugLocksOriginal419Preparation
{
    internal static PreparedBeamImage Prepare(ModuleRegistry registry, BeamDependencySelection dependencies)
    {
        PreparedBeamImage image = registry.PrepareBeam(DebugLocksOriginal419Access.Resource(DebugLocksOriginal419Names.FacetBeam), dependencies);
        DebugLocksOriginal419Access.Require(image.Name == DebugLocksOriginal419Names.Debug && image.NativePrimitiveExports.Count == DebugLocksOriginal419Names.Signatures.Length);
        foreach (var s in DebugLocksOriginal419Names.Signatures) DebugLocksOriginal419Access.Require(image.NativePrimitiveExports.Contains(new BeamFunctionSignature(s.Function, s.Arity)));

        return image;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage image)
    {
        var exports = registry.Exports.Where(e => e.Module == DebugLocksOriginal419Names.Debug).ToArray();
        DebugLocksOriginal419Access.Require(exports.Length == 6);
        foreach (var e in exports) DebugLocksOriginal419Access.Require(ReferenceEquals(registry.SourceOwner(e.Module, e.Function, e.Arity), image));
    }
}
