using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class RegexNativeOriginal411Access
{
    internal static byte[] Read(string name)
    {
        byte[] Raw(string file)
        {
            using var stream = typeof(RegexNativeOriginal411Access).Assembly.GetManifestResourceStream(RegexNativeOriginal411Names.ResourcePrefix + file)
                ?? throw new InvalidOperationException(RegexNativeOriginal411Names.Failure);
            using var output = new MemoryStream();
            stream.CopyTo(output);

            return output.ToArray();
        }
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(Raw(RegexNativeOriginal411Names.ResourceManifest))!;
        var bytes = Raw(name);
        if (!manifest.TryGetValue(name, out var expected) || Convert.ToHexString(SHA256.HashData(bytes)) != expected) throw new InvalidOperationException(RegexNativeOriginal411Names.Failure);

        return bytes;
    }
}
