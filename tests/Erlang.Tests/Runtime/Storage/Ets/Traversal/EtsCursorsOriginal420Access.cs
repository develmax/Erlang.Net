using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class EtsCursorsOriginal420Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsCursorsOriginal420Names.Failure);
    }

    internal static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using var input = typeof(EtsCursorsOriginal420Access).Assembly.GetManifestResourceStream(EtsCursorsOriginal420Names.Prefix + file)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(EtsCursorsOriginal420Names.Manifest))!;
        byte[] bytes = Read(name);
        Require(hashes.TryGetValue(name, out string? hash) && hash == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }
}
