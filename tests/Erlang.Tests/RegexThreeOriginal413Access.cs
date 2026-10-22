using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class RegexThreeOriginal413Access
{
    internal static byte[] Read(string name)
    {
        byte[] Raw(string file)
        {
            using var stream = typeof(RegexThreeOriginal413Access).Assembly.GetManifestResourceStream(RegexThreeOriginal413Names.ResourcePrefix + file)
                ?? throw new InvalidOperationException(RegexThreeOriginal413Names.Failure);
            using var output = new MemoryStream();
            stream.CopyTo(output);

            return output.ToArray();
        }
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(Raw(RegexThreeOriginal413Names.ResourceManifest))!;
        var bytes = Raw(name);
        if (!manifest.TryGetValue(name, out var expected) || Convert.ToHexString(SHA256.HashData(bytes)) != expected) throw new InvalidOperationException(RegexThreeOriginal413Names.Failure);

        return bytes;
    }
}
