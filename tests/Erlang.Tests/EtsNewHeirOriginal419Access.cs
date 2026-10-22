using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class EtsNewHeirOriginal419Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsNewHeirOriginal419Names.Failure);
    }

    internal static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using var input = typeof(EtsNewHeirOriginal419Access).Assembly.GetManifestResourceStream(EtsNewHeirOriginal419Names.Prefix + file)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(EtsNewHeirOriginal419Names.Manifest))!;
        byte[] bytes = Read(name);
        Require(hashes.TryGetValue(name, out string? hash) && hash == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }
}
