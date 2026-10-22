using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class InterpreterCode411Resources
{
    internal static byte[] Read()
    {
        Dictionary<string, string> manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(ReadResource(InterpreterCode411Names.ManifestResource))!;
        byte[] bytes = ReadResource(InterpreterCode411Names.Resource);
        if (manifest.Count != 1 || !manifest.TryGetValue(InterpreterCode411Names.Module + ".beam", out string? expected)
            || Convert.ToHexString(SHA256.HashData(bytes)) != expected)
            throw new InvalidDataException(InterpreterCode411Names.Failure);

        return bytes;
    }

    private static byte[] ReadResource(string name)
    {
        using Stream stream = typeof(InterpreterCode411Resources).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidDataException(InterpreterCode411Names.Failure);
        using var result = new MemoryStream();
        stream.CopyTo(result);

        return result.ToArray();
    }
}
