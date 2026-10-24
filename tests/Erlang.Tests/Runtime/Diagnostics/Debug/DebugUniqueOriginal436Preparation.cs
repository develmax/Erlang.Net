namespace Erlang.Tests;

internal static class DebugUniqueOriginal436Preparation
{
    internal static PreparedBeamImage Prepare(ModuleRegistry registry, BeamDependencySelection dependencies)
    {
        PreparedBeamImage image = registry.PrepareBeam(DebugUniqueOriginal436Access.Resource(DebugUniqueOriginal436Names.FacetBeam), dependencies);
        DebugUniqueOriginal436Access.Require(image.Name == DebugUniqueOriginal436Names.Debug && image.NativePrimitiveExports.Count == DebugUniqueOriginal436Names.Signatures.Length);
        foreach (var s in DebugUniqueOriginal436Names.Signatures) DebugUniqueOriginal436Access.Require(image.NativePrimitiveExports.Contains(new BeamFunctionSignature(s.Function, s.Arity)));

        return image;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage image)
    {
        var exports = registry.Exports.Where(e => e.Module == DebugUniqueOriginal436Names.Debug).ToArray();
        DebugUniqueOriginal436Access.Require(exports.Length == 4);
        foreach (var e in exports) DebugUniqueOriginal436Access.Require(ReferenceEquals(registry.SourceOwner(e.Module, e.Function, e.Arity), image));
    }
}
