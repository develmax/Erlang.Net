using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class StringPatternAllocationImages
{
    internal static void Publish(ProcessRuntime runtime)
    {
        var prepared = new List<PreparedBeamImage>();
        foreach (var image in StringPatternAllocationNames.Images)
        {
            using Stream resource = typeof(StringPatternAllocationImages).Assembly.GetManifestResourceStream(image.Resource)
                ?? throw new InvalidDataException(StringPatternAllocationNames.PublicationFailure);
            using var memory = new MemoryStream();
            resource.CopyTo(memory);
            byte[] bytes = memory.ToArray();
            if (Convert.ToHexString(SHA256.HashData(bytes)) != image.Sha256)
                throw new InvalidDataException(StringPatternAllocationNames.PublicationFailure);
            PreparedBeamImage candidate = runtime.Modules.PrepareBeam(
                bytes,
                new BeamDependencySelection(DynamicApplyBoundary520Imports.Read(bytes))
            );
            if (candidate.Name != image.Module)
                throw new InvalidDataException(StringPatternAllocationNames.PublicationFailure);
            prepared.Add(candidate);
        }
        if (runtime.Modules.PublishPreparedBeam(prepared.ToArray()).Status != BeamPublicationStatus.Published)
            throw new InvalidDataException(StringPatternAllocationNames.PublicationFailure);
    }
}
