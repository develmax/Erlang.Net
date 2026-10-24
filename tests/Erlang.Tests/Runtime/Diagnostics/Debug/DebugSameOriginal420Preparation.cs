namespace Erlang.Tests;

internal static class DebugSameOriginal420Preparation
{
    internal static PreparedBeamImage Prepare(ModuleRegistry registry, BeamDependencySelection dependencies)
    {
        PreparedBeamImage image = registry.PrepareBeam(DebugSameOriginal420Access.Resource(DebugSameOriginal420Names.FacetBeam), dependencies);
        DebugSameOriginal420Access.Require(image.Name == DebugSameOriginal420Names.Debug && image.NativePrimitiveExports.Count == DebugSameOriginal420Names.Signatures.Length);
        foreach (var s in DebugSameOriginal420Names.Signatures) DebugSameOriginal420Access.Require(image.NativePrimitiveExports.Contains(new BeamFunctionSignature(s.Function, s.Arity)));

        return image;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage image)
    {
        var exports = registry.Exports.Where(e => e.Module == DebugSameOriginal420Names.Debug).ToArray();
        DebugSameOriginal420Access.Require(exports.Length == 3);
        foreach (var e in exports) DebugSameOriginal420Access.Require(ReferenceEquals(registry.SourceOwner(e.Module, e.Function, e.Arity), image));
    }
}
