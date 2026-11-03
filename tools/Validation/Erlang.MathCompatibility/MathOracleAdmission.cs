using System.Security.Cryptography;
using System.Text.Json;

internal static class MathOracleAdmission
{
    internal static void Validate(string directory)
    {
        using var receipt = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, MathCompatibilityNames.OracleIdentity)));
        JsonElement root = receipt.RootElement;
        if (root.GetProperty("Version").GetString() != MathCompatibilityNames.Version)
        {
            throw new InvalidDataException(MathCompatibilityDiagnostics.OracleIdentity);
        }
        var admitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement entry in root.GetProperty("Files").EnumerateArray())
        {
            string name = entry.GetProperty("Name").GetString()!;
            if (Path.GetFileName(name) != name || name.Contains('/') || name.Contains('\\') || !admitted.Add(name))
            {
                throw new InvalidDataException(MathCompatibilityDiagnostics.OracleIdentity);
            }
            string path = Path.Combine(directory, name);
            if (new FileInfo(path).Length != entry.GetProperty("Length").GetInt64()
                || Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) != entry.GetProperty("Sha256").GetString())
            {
                throw new InvalidDataException(MathCompatibilityDiagnostics.OracleIdentity);
            }
        }
        if (!MathCompatibilityNames.RequiredFiles.All(admitted.Contains))
        {
            throw new InvalidDataException(MathCompatibilityDiagnostics.OracleIdentity);
        }
    }
}
