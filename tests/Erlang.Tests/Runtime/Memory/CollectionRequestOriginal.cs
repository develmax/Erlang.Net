using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class CollectionRequestOriginal
{
    internal static byte[] Beam() => Read(CollectionRequestNames.BeamResource, CollectionRequestNames.BeamSha256);

    internal static Term Outcome() => ExternalTermFormat.Decode(Read(CollectionRequestNames.OutcomeResource, CollectionRequestNames.OutcomeSha256));

    private static byte[] Read(string name, string expected)
    {
        using Stream stream = typeof(CollectionRequestOriginal).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidDataException(CollectionRequestNames.Failure);
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] bytes = output.ToArray();
        if (Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(CollectionRequestNames.Failure);

        return bytes;
    }
}
