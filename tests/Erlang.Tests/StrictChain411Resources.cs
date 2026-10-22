using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class StrictChain411Resources
{
    internal static byte[] Image(string module)
    {
        Dictionary<string, string> manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(StrictChain411Names.Manifest))!;
        string file = module + ".beam";
        byte[] bytes = Read(file);
        if (manifest.Count != 10 || !manifest.TryGetValue(file, out string? expected)
            || Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(StrictChain411Names.Error);

        return bytes;
    }

    private static byte[] Read(string file)
    {
        using Stream stream = typeof(StrictChain411Resources).Assembly.GetManifestResourceStream(StrictChain411Names.ResourcePrefix + file)
            ?? throw new InvalidDataException(StrictChain411Names.Error);
        using var output = new MemoryStream();
        stream.CopyTo(output);

        return output.ToArray();
    }
}
