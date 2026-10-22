using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class CodePurgerOriginal432Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(CodePurgerOriginal432Names.Failure);
    }

    internal static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using var input = typeof(CodePurgerOriginal432Access).Assembly.GetManifestResourceStream(CodePurgerOriginal432Names.Prefix + file)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(CodePurgerOriginal432Names.Manifest))!;
        byte[] bytes = Read(name);
        Require(hashes.TryGetValue(name, out string? hash) && hash == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }
}
