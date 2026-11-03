using System.Security.Cryptography;
using System.Text.Json;
using static Erlang.Tests.OriginalFloat556Names;

namespace Erlang.Tests;

internal static class OriginalFloat556Metadata
{
    internal static RuntimeNativeMetadataImage Read(
        string repository,
        OriginalFloat556Admission receipt,
        string metadataCompletion,
        string metadataSha
    ) => ReadProvider(
        repository,
        receipt.ProviderBeforePath,
        metadataCompletion,
        metadataSha
    );

    internal static RuntimeNativeMetadataImage ReadProvider(
        string repository,
        string providerBeforePath,
        string metadataCompletion,
        string metadataSha
    )
    {
        Require(Digest(metadataCompletion) == metadataSha.ToUpperInvariant());
        using var metadataDocument = JsonDocument.Parse(File.ReadAllBytes(metadataCompletion));
        JsonElement metadata = metadataDocument.RootElement;
        Require(metadata.GetProperty(MetadataPin).GetString() == Pin && metadata.GetProperty(OriginalOnly).GetBoolean()
            && metadata.GetProperty(AcquisitionPassed).GetBoolean());
        using var provider = JsonDocument.Parse(File.ReadAllBytes(OriginalFloat556AdmissionReader.Resolve(repository, providerBeforePath)));
        Require(metadata.GetProperty(ProviderAdmissionSha256).GetString() == provider.RootElement.GetProperty(AdmissionSha256).GetString());
        string metadataDirectory = Path.GetDirectoryName(Path.GetFullPath(metadataCompletion))!;
        foreach (var row in metadata.GetProperty(Files).EnumerateArray())
        {
            string name = row.GetProperty(PathField).GetString()!;
            Require(!Path.IsPathRooted(name));
            string file = Path.GetFullPath(Path.Combine(metadataDirectory, name));
            Require(file.StartsWith(
                metadataDirectory + Path.DirectorySeparatorChar,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
            ));
            Require(File.Exists(file) && new FileInfo(file).Length == row.GetProperty(Bytes).GetInt64()
                && Digest(file) == row.GetProperty(Sha256).GetString());
        }
        JsonElement profile = metadata.GetProperty(DynamicApplyNativeMetadata527Names.Field);
        string metadataName = profile.GetProperty(Name).GetString()!;
        Require(Path.GetFileName(metadataName) == metadataName);
        byte[] nativeBytes = File.ReadAllBytes(Path.Combine(metadataDirectory, metadataName));
        RuntimeNativeMetadataImage native = DynamicApplyNativeMetadata527Admission.Verify(metadataDirectory, profile, nativeBytes);
        Require(native.OriginalBeamSha256 == profile.GetProperty(Sha256).GetString());

        return native;
    }

    private static string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidDataException(Failure);
    }
}
