namespace Erlang.Tests;

internal static class DebugInternalOriginal420Preparation
{
    internal static PreparedBeamImage Prepare(ModuleRegistry registry, BeamDependencySelection dependencies)
    {
        PreparedBeamImage image = registry.PrepareBeam(DebugInternalOriginal420Access.Resource(DebugInternalOriginal420Names.FacetBeam), dependencies);
        DebugInternalOriginal420Access.Require(image.Name == DebugInternalOriginal420Names.Debug && image.NativePrimitiveExports.Count == DebugInternalOriginal420Names.Signatures.Length);
        foreach (var s in DebugInternalOriginal420Names.Signatures) DebugInternalOriginal420Access.Require(image.NativePrimitiveExports.Contains(new BeamFunctionSignature(s.Function, s.Arity)));

        return image;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage image)
    {
        var exports = registry.Exports.Where(e => e.Module == DebugInternalOriginal420Names.Debug).ToArray();
        DebugInternalOriginal420Access.Require(exports.Length == 4);
        foreach (var e in exports) DebugInternalOriginal420Access.Require(ReferenceEquals(registry.SourceOwner(e.Module, e.Function, e.Arity), image));
    }
}
