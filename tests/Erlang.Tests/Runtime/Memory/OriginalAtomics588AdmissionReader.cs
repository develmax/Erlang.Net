using System.Security.Cryptography;
using System.Text.Json;
using static Erlang.Tests.OriginalAtomics588Names;

namespace Erlang.Tests;

internal static class OriginalAtomics588AdmissionReader
{
    internal static OriginalAtomics588Admission Read(string repository, string path, string expected)
    {
        Require(Path.IsPathFullyQualified(path) && Digest(path) == expected.ToUpperInvariant());
        var receipt = JsonSerializer.Deserialize<OriginalAtomics588Admission>(File.ReadAllBytes(path))!;
        bool sourceBuilt = receipt.StockExecutionProfile == OriginalAtomics588MetadataNames.SourceBuiltStockExecutionProfile;
        Require(sourceBuilt || receipt.StockExecutionProfile == OriginalAtomics588MetadataNames.StockExecutionProfile);
        var worker = receipt.Worker;
        Require(worker.OriginalAcquisitionPassed && worker.PinnedCommit == OriginalReference563Names.Pin && worker.Job == Job);
        var bound = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in receipt.Evidence)
        {
            Require(bound.TryAdd(row.Path, row.Sha256));
            Require(Digest(Resolve(repository, row.Path)) == row.Sha256);
        }
        foreach (var row in new[] {
            (worker.SourcePath,worker.SourceSha256),(worker.BeamPath,worker.BeamSha256),
            (worker.WorkerSourcePath,worker.WorkerSourceSha256),(worker.WorkerBeamPath,worker.WorkerBeamSha256),
            (worker.RawPath,worker.RawSha256),(worker.DeterministicPath,worker.DeterministicSha256),
            (worker.CompletionPath,worker.CompletionSha256),(worker.InventoryPath,worker.InventorySha256),
            (worker.ProviderBeforePath,worker.ProviderBeforeSha256),(worker.ProviderAfterPath,worker.ProviderAfterSha256)})
            Require(bound.TryGetValue(row.Item1, out string? value) && value == row.Item2);
        using var provider = JsonDocument.Parse(File.ReadAllBytes(Resolve(repository, worker.ProviderBeforePath)));
        bool sourceBuiltProvider = provider.RootElement.GetProperty(OriginalAtomics588MetadataNames.ProviderProfileField).GetString()
            == OriginalAtomics588MetadataNames.SourceBuiltProviderProfile;
        Require(sourceBuilt == sourceBuiltProvider && (!sourceBuilt || OperatingSystem.IsLinux()));
        Require(Digest(Path.Combine(repository, Fixture)) == worker.SourceSha256
            && Digest(Path.Combine(repository, OriginalReference563Names.WorkerFixturePath)) == worker.WorkerSourceSha256);
        Require(receipt.Stock.Select(row => row.Module).SequenceEqual(new[] { Atomics, Counters }));
        foreach (var stock in receipt.Stock)
            Require(bound.TryGetValue(stock.BeamPath, out string? value) && value == stock.BeamSha256);
        Require(bound.ContainsKey(receipt.InitialPreloadedOwnersPath) && bound.ContainsKey(receipt.StockLoadReceiptsPath)
            && bound.ContainsKey(receipt.StockNativeWitnessPath));
        Require(bound.ContainsKey(receipt.WorkerOwnersPath) && bound.ContainsKey(receipt.StockOwnersBeforePath) && bound.ContainsKey(receipt.StockOwnersAfterPath));
        Require(File.ReadAllBytes(Resolve(repository, receipt.StockOwnersBeforePath)).AsSpan()
            .SequenceEqual(File.ReadAllBytes(Resolve(repository, receipt.StockOwnersAfterPath))));

        return receipt;
    }

    internal static string Resolve(string repository, string relative) => OriginalReference563AdmissionReader.Resolve(repository, relative);

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidDataException(Failure);
    }

    private static string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
