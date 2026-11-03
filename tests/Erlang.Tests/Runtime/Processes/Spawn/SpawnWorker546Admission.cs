using System.Security.Cryptography;
using System.Text.Json;
using static Erlang.Tests.SpawnWorker546Names;

namespace Erlang.Tests;

internal static class SpawnWorker546Admission
{
    internal static Term Read(string repository, string admissionPath, OriginalMap517Admission receipt)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(admissionPath));
        JsonElement root = document.RootElement;
        string inventoryPath = OriginalMap517AdmissionReader.Resolve(repository, receipt.InventoryPath);
        Require(Digest(inventoryPath) == receipt.InventorySha256);
        using var inventory = JsonDocument.Parse(File.ReadAllBytes(inventoryPath));
        string source = Bound(SourceField, WorkerSourceFile);
        Require(Digest(source) == SourceContractSha256
            && Digest(source) == Digest(Path.Combine(repository, CurrentWorkerSource)));
        string witness = Bound(WitnessField, WitnessFile);

        return OwnedFunctionOriginal407Terms.Decode(File.ReadAllBytes(witness));

        string Bound(string field, string name)
        {
            var rows = inventory.RootElement.GetProperty("Files").EnumerateArray()
                .Where(row => row.GetProperty("Name").GetString() == name).ToArray();
            Require(rows.Length == 1);
            string expectedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(inventoryPath)!, name));
            string path = expectedPath;
            string sha = rows[0].GetProperty("Sha256").GetString()!;
            bool hasPath = root.TryGetProperty(field + "Path", out JsonElement declaredPath);
            bool hasSha = root.TryGetProperty(field + "Sha256", out JsonElement declaredSha);
            Require(hasPath == hasSha);
            if (hasPath)
            {
                path = OriginalMap517AdmissionReader.Resolve(repository, declaredPath.GetString()!);
                Require(Path.GetFullPath(path) == expectedPath && declaredSha.GetString() == sha);
            }
            Require(new FileInfo(path).Length == rows[0].GetProperty("Bytes").GetInt64()
                && Digest(path) == sha);

            return path;
        }
    }

    private static string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidDataException(Failure);
    }
}
