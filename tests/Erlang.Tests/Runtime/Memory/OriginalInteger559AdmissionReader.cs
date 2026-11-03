using System.Security.Cryptography;
using System.Text.Json;
using static Erlang.Tests.OriginalInteger559Names;

namespace Erlang.Tests;

internal static class OriginalInteger559AdmissionReader
{
    internal static OriginalInteger559Admission Read(string repository, string path, string expected)
    {
        if (!Path.IsPathFullyQualified(path) || string.IsNullOrWhiteSpace(expected))
            throw new InvalidOperationException(AdmissionMissing);
        if (Digest(path) != expected)
            throw new InvalidDataException(AdmissionMissing);
        var receipt = JsonSerializer.Deserialize<OriginalInteger559Admission>(File.ReadAllBytes(path))!;
        if (!receipt.OriginalAcquisitionPassed || receipt.PinnedCommit != Pin || receipt.Job != OriginalJob)
            throw new InvalidDataException(AdmissionMissing);
        foreach (var file in new[]
        {
            (receipt.SourcePath, receipt.SourceSha256), (receipt.BeamPath, receipt.BeamSha256),
            (receipt.WorkerBeamPath, receipt.WorkerBeamSha256),
            (receipt.WorkerSourcePath, receipt.WorkerSourceSha256), (receipt.RawPath, receipt.RawSha256),
            (receipt.DeterministicPath, receipt.DeterministicSha256), (receipt.CompletionPath, receipt.CompletionSha256),
            (receipt.InventoryPath, receipt.InventorySha256), (receipt.ProviderBeforePath, receipt.ProviderBeforeSha256),
            (receipt.ProviderAfterPath, receipt.ProviderAfterSha256)
        })
            if (Digest(Resolve(repository, file.Item1)) != file.Item2)
                throw new InvalidDataException(AdmissionMissing);
        if (Digest(Path.Combine(repository, WorkerFixturePath)) != receipt.WorkerSourceSha256
            || Digest(Path.Combine(repository, FixturePath)) != receipt.SourceSha256)
            throw new InvalidDataException(AdmissionMissing);

        return receipt;
    }

    internal static string Resolve(string repository, string relative)
    {
        string resolved = Path.GetFullPath(Path.Combine(repository, relative));
        string back = Path.GetRelativePath(repository, resolved);
        if (Path.IsPathRooted(back) || back == ParentDirectory || back.StartsWith(ParentDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidDataException(AdmissionMissing);

        return resolved;
    }

    private static string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
