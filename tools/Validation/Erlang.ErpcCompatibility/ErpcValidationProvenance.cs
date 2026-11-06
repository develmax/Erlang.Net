using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.ErpcValidation;

internal static class ErpcValidationProvenance
{
    internal static int Verify(string repository, string directory)
    {
        using JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, "provenance.json")));
        using JsonDocument contract = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repository, "tools/Validation/Erlang.ErpcCompatibility/reference-contract.json")));
        JsonElement root = provenance.RootElement;
        if (root.GetProperty("Baseline").GetString() != contract.RootElement.GetProperty("ReleaseTag").GetString()
            || root.GetProperty("SourceCommit").GetString() != contract.RootElement.GetProperty("SourceCommit").GetString())
            throw new InvalidDataException(ErpcValidationDiagnostics.Baseline);
        foreach (JsonElement file in root.GetProperty("SourceFiles").EnumerateArray())
        {
            if (Hash(Path.Combine(repository, file.GetProperty("Path").GetString()!)) != file.GetProperty("Sha256").GetString())
                throw new InvalidDataException(ErpcValidationDiagnostics.Source);
        }
        foreach (JsonElement file in root.GetProperty("CaptureFiles").EnumerateArray())
        {
            if (Hash(Path.Combine(directory, file.GetProperty("Name").GetString()!)) != file.GetProperty("Sha256").GetString())
                throw new InvalidDataException(ErpcValidationDiagnostics.Capture);
        }

        return root.GetProperty("BacktraceDepth").GetInt32();
    }

    internal static int ExpectedCases(string repository)
    {
        using JsonDocument contract = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repository, "tools/Validation/Erlang.ErpcCompatibility/reference-contract.json")));
        JsonElement root = contract.RootElement;

        return root.GetProperty("PrimaryCases").GetInt32() + root.GetProperty("ScenarioCases").GetInt32() + root.GetProperty("UpstreamCases").GetInt32();
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
