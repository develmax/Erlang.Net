using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class ReUnicodeOriginal453Access
{
    internal static byte[] Read(string name)
    {
        byte[] Raw(string file)
        {
            using var stream = typeof(ReUnicodeOriginal453Access).Assembly.GetManifestResourceStream(ReUnicodeOriginal453Names.Prefix + file)
                ?? throw new InvalidOperationException(ReUnicodeOriginal453Names.Failure);
            using var output = new MemoryStream();
            stream.CopyTo(output);

            return output.ToArray();
        }
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(Raw(ReUnicodeOriginal453Names.Manifest))
            ?? throw new InvalidOperationException(ReUnicodeOriginal453Names.Failure);
        byte[] bytes = Raw(name);
        if (!manifest.TryGetValue(name, out string? hash) || Convert.ToHexString(SHA256.HashData(bytes)) != hash)
            throw new InvalidOperationException(ReUnicodeOriginal453Names.Failure);

        return bytes;
    }

    internal static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ReUnicodeOriginal453Names.Failure);
    }
}
