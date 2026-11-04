using System.Security.Cryptography;
using System.Text.Json;

internal static class FileCompatibilityAdmission
{
    internal static void Verify(string repository, string directory)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, FileCompatibilityFiles.Provenance)));
        JsonElement root = document.RootElement;
        if (root.GetProperty(FileCompatibilityProvenanceFields.Baseline).GetString() != FileCompatibilityReference.Baseline
            || root.GetProperty(FileCompatibilityProvenanceFields.Commit).GetString() != FileCompatibilityReference.Commit)
        {
            throw new InvalidDataException(FileCompatibilityDiagnostics.Provenance);
        }
        VerifyRows(
            root.GetProperty(FileCompatibilityProvenanceFields.Inputs),
            repository,
            FileCompatibilityFiles.RequiredInputs
        );
        VerifyRows(
            root.GetProperty(FileCompatibilityProvenanceFields.Reports),
            directory,
            FileCompatibilityFiles.RequiredReports
        );
    }

    private static void VerifyRows(JsonElement rows, string directory, IReadOnlyList<string> required)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement row in rows.EnumerateArray())
        {
            string path = row.GetProperty(FileCompatibilityProvenanceFields.Path).GetString()!;
            if (!required.Contains(path, StringComparer.Ordinal) || !seen.Add(path))
            {
                throw new InvalidDataException(FileCompatibilityDiagnostics.Provenance);
            }
            string actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, path))));
            if (!string.Equals(actual, row.GetProperty(FileCompatibilityProvenanceFields.Sha256).GetString(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(FileCompatibilityDiagnostics.Digest);
            }
        }
        if (!seen.SetEquals(required))
        {
            throw new InvalidDataException(FileCompatibilityDiagnostics.Provenance);
        }
    }
}
