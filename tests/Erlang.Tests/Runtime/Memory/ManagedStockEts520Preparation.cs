using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class ManagedStockEts520Preparation
{
    internal static PreparedBeamImage Prepare(
        ProcessRuntime runtime,
        ModuleRegistry registry,
        ManagedBatchImage513 image,
        byte[] originalBeam,
        string[] dependencies
    )
    {
        if (image.Module != ManagedStockEts520Names.Module
            || Convert.ToHexString(SHA256.HashData(originalBeam)) != image.Sha256)
            throw new InvalidDataException(ManagedStockEts520Names.ImageMismatch);

        // Whole original libraries require the existing native catalog before
        // any source wrappers are published. This includes the full ETS kernel.
        RuntimeNativePrimitiveProviders.RegisterCatalog(runtime, registry);
        PreparedBeamImage prepared = registry.PrepareBeam(originalBeam, new BeamDependencySelection(dependencies));
        if (prepared.Name != ManagedStockEts520Names.Module || prepared.OriginalBeamSha256 != image.Sha256)
            throw new InvalidDataException(ManagedStockEts520Names.ImageMismatch);

        return prepared;
    }
}
