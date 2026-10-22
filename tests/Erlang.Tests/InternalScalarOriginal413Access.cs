using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class InternalScalarOriginal413Access
{
    internal static byte[] Read(string name)
    {
        byte[] Raw(string file)
        {
            using var stream = typeof(InternalScalarOriginal413Access).Assembly.GetManifestResourceStream(InternalScalarOriginal413Names.ResourcePrefix + file)
                ?? throw new InvalidOperationException(InternalScalarOriginal413Names.Failure);
            using var output = new MemoryStream();
            stream.CopyTo(output);

            return output.ToArray();
        }
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(Raw(InternalScalarOriginal413Names.ResourceManifest))!;
        var bytes = Raw(name);
        if (!manifest.TryGetValue(name, out var expected) || Convert.ToHexString(SHA256.HashData(bytes)) != expected) throw new InvalidOperationException(InternalScalarOriginal413Names.Failure);

        return bytes;
    }
}
