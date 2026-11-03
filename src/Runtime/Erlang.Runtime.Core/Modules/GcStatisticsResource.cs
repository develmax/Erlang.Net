using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang;

internal static class GcStatisticsResource
{
    private static byte[] Read(string name)
    {
        using Stream source = typeof(ModuleRegistry).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidDataException(GcStatisticsNames.Invalid);
        using var output = new MemoryStream();
        source.CopyTo(output);

        return output.ToArray();
    }

    internal static byte[] AuthenticatedImage()
    {
        byte[] image = Read(GcStatisticsNames.ImageResource);
        byte[] source = Read(GcStatisticsNames.SourceResource);
        byte[] metadata = Read(GcStatisticsNames.ProvenanceResource);
        using JsonDocument provenance = JsonDocument.Parse(metadata);
        if (Convert.ToHexString(SHA256.HashData(image)) != GcStatisticsNames.ImageSha256
            || Convert.ToHexString(SHA256.HashData(source)) != GcStatisticsNames.SourceSha256
            || Convert.ToHexString(SHA256.HashData(metadata)) != GcStatisticsNames.ProvenanceSha256
            || provenance.RootElement.GetProperty(GcStatisticsNames.ProvenanceCommit).GetString() != GcStatisticsNames.Pin)
            throw new InvalidDataException(GcStatisticsNames.Invalid);

        return image;
    }
}
