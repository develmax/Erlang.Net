namespace Erlang.Tests;

internal static class DebugUnalignedOriginal421Preparation
{
    internal static PreparedBeamImage Prepare(ModuleRegistry registry, BeamDependencySelection dependencies)
    {
        PreparedBeamImage image = registry.PrepareBeam(DebugUnalignedOriginal421Access.Resource(DebugUnalignedOriginal421Names.FacetBeam), dependencies);
        DebugUnalignedOriginal421Access.Require(image.Name == DebugUnalignedOriginal421Names.Debug && image.NativePrimitiveExports.Count == DebugUnalignedOriginal421Names.Signatures.Length);
        foreach (var s in DebugUnalignedOriginal421Names.Signatures) DebugUnalignedOriginal421Access.Require(image.NativePrimitiveExports.Contains(new BeamFunctionSignature(s.Function, s.Arity)));

        return image;
    }

    internal static void AssertPublished(ModuleRegistry registry, PreparedBeamImage image)
    {
        var exports = registry.Exports.Where(e => e.Module == DebugUnalignedOriginal421Names.Debug).ToArray();
        DebugUnalignedOriginal421Access.Require(exports.Length == 3);
        foreach (var e in exports) DebugUnalignedOriginal421Access.Require(ReferenceEquals(registry.SourceOwner(e.Module, e.Function, e.Arity), image));
    }
}
