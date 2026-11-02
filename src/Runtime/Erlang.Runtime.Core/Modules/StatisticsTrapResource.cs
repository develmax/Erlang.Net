using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang;

internal static class StatisticsTrapResource
{
    private static byte[] Read(string resource)
    {
        using Stream stream = typeof(ModuleRegistry).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidDataException(StatisticsTrapNames.InvalidImage);
        using var data = new MemoryStream();
        stream.CopyTo(data);

        return data.ToArray();
    }

    internal static byte[] AuthenticatedImage()
    {
        byte[] beam = Read(StatisticsTrapNames.Resource);
        byte[] source = Read(StatisticsTrapNames.SourceResource);
        using JsonDocument provenance = JsonDocument.Parse(Read(StatisticsTrapNames.ProvenanceResource));
        if (Convert.ToHexString(SHA256.HashData(beam)) != StatisticsTrapNames.ImageSha256
            || Convert.ToHexString(SHA256.HashData(source)) != StatisticsTrapNames.SourceSha256
            || provenance.RootElement.GetProperty("SourceCommit").GetString() != StatisticsTrapNames.Pin)
            throw new InvalidDataException(StatisticsTrapNames.InvalidImage);

        return beam;
    }
}
