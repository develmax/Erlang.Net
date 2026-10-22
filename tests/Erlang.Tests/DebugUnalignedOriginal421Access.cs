using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DebugUnalignedOriginal421Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DebugUnalignedOriginal421Names.Failure);
    }

    internal static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using var input = typeof(DebugUnalignedOriginal421Access).Assembly.GetManifestResourceStream(DebugUnalignedOriginal421Names.Prefix + file)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(DebugUnalignedOriginal421Names.Manifest))!;
        byte[] bytes = Read(name);
        Require(hashes.TryGetValue(name, out string? hash) && hash == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }
}
