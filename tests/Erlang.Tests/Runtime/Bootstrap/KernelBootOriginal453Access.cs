using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class KernelBootOriginal453Access
{
    internal static byte[] Read(string name)
    {
        byte[] Raw(string file)
        {
            using var stream = typeof(KernelBootOriginal453Access).Assembly.GetManifestResourceStream(KernelBootOriginal453Names.Prefix + file)
                ?? throw new InvalidOperationException(KernelBootOriginal453Names.Failure);
            using var output = new MemoryStream();
            stream.CopyTo(output);

            return output.ToArray();
        }
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(Raw(KernelBootOriginal453Names.Manifest))!;
        byte[] bytes = Raw(name);
        Require(manifest.TryGetValue(name, out string? hash) && Convert.ToHexString(SHA256.HashData(bytes)) == hash);

        return bytes;
    }

    internal static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(KernelBootOriginal453Names.Failure);
    }
}
