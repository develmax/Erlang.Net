using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class CountersOriginal421Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(CountersOriginal421Names.Failure);
    }

    internal static byte[] Resource(string name) => ProfileResource616(name, CountersOriginal421Names.Prefix);

    internal static byte[] ProfileResource616(string name, string prefix)
    {
        byte[] Read(string file)
        {
            using var input = typeof(CountersOriginal421Access).Assembly.GetManifestResourceStream(prefix + file)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(CountersOriginal421Names.Manifest))!;
        byte[] bytes = Read(name);
        Require(hashes.TryGetValue(name, out string? hash) && hash == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }
}
