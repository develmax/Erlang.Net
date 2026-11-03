using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DebugDirtyGuestCapture565Access
{
    internal static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using Stream? input = typeof(DebugDirtyGuestCapture565Access).Assembly.GetManifestResourceStream(DebugDirtyOriginal424Names.AdapterPrefix565 + file);
            if (input is null) throw new InvalidOperationException(DebugDirtyOriginal424Names.AdapterMissing565);
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(DebugDirtyOriginal424Names.AdapterManifest565))!;
        DebugDirtyOriginal424Access.Require(hashes.Count == 2
            && hashes.ContainsKey(DebugDirtyOriginal424Names.AdapterSource565)
            && hashes.ContainsKey(DebugDirtyOriginal424Names.AdapterBeam565));
        byte[] bytes = Read(name);
        DebugDirtyOriginal424Access.Require(hashes.TryGetValue(name, out string? hash)
            && hash == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }
}
