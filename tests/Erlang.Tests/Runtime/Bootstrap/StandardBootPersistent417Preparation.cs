namespace Erlang.Tests;

internal static class StandardBootPersistent417Preparation
{
    internal static PreparedBeamImage Prepare(ModuleRegistry registry, BeamDependencySelection dependencies)
    {
        byte[] bytes = NativeProvider408Access.Resource(PersistentNative417Names.Image);
        PreparedBeamImage image = registry.PrepareBeam(bytes, dependencies);
        StandardBootOriginal412Access.Require(image.Name == PersistentNative417Names.Module
            && image.NativePrimitiveExports.Count == PersistentNative417Names.NativeCount
            && PersistentNative417Names.Signatures.All(signature => image.NativePrimitiveExports.Contains(signature)));

        return image;
    }
}
