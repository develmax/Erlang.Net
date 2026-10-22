using System.Security.Cryptography;

namespace HelloBeam;

internal static class BeamExampleAssets
{
    internal static byte[] Read(string module, string expectedSha)
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(
            AppContext.BaseDirectory,
            BeamExampleNames.Modules,
            module + BeamExampleNames.Extension
        ));
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expectedSha)
            throw new InvalidDataException(BeamExampleNames.AssetFailure);

        return bytes;
    }
}
