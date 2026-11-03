using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.BeamBuild;

internal static class BeamBuildStorage
{
    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    internal static string HashFile(string path)
    {
        using Stream input = File.OpenRead(path);

        return Convert.ToHexString(SHA256.HashData(input));
    }

    internal static string WriteRequest<T>(
        string directory,
        string prefix,
        string extension,
        T value
    )
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, new JsonSerializerOptions { WriteIndented = true });
        string path = Path.Combine(directory, prefix + Hash(bytes) + extension);
        BeamDeclaredPaths.RequireNoReparse(path);
        if (Directory.Exists(path) || File.Exists(path) && !File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
            throw new InvalidDataException(BeamBuildNames.Invalid);
        AtomicJson(path, value);

        return path;
    }

    internal static void AtomicJson<T>(string path, T value)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, new JsonSerializerOptions { WriteIndented = true });
        if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
            return;
        string temporary = path + BeamBuildArtifactNames.TemporarySeparator + Guid.NewGuid().ToString(BeamBuildArtifactNames.GuidCompactFormat) + BeamBuildArtifactNames.TemporarySuffix;
        try
        {
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
