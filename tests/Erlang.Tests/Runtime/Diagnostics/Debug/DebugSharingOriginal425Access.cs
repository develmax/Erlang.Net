using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DebugSharingOriginal425Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DebugSharingOriginal425Names.Failure);
    }

    internal static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using var input = typeof(DebugSharingOriginal425Access).Assembly.GetManifestResourceStream(DebugSharingOriginal425Names.Prefix + file)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(DebugSharingOriginal425Names.Manifest))!;
        byte[] bytes = Read(name);
        Require(hashes.TryGetValue(name, out string? hash) && hash == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }
}
