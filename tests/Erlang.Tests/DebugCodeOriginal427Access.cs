using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DebugCodeOriginal427Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DebugCodeOriginal427Names.Failure);
    }

    internal static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using var input = typeof(DebugCodeOriginal427Access).Assembly.GetManifestResourceStream(DebugCodeOriginal427Names.Prefix + file)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(DebugCodeOriginal427Names.Manifest))!;
        byte[] bytes = Read(name);
        Require(hashes.TryGetValue(name, out string? hash) && hash == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }
}
