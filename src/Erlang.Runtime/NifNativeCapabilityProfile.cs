using System.Security.Cryptography;

namespace Erlang;

// Trusted deployment metadata, not a proof that a binary was built from this source.
internal sealed class NifNativeCapabilityProfile
{
    internal string LibraryPath { get; }
    internal string LibrarySha256 { get; }
    internal string? BridgePath { get; }
    internal string? BridgeSha256 { get; }

    internal NifNativeCapabilityProfile(
        string libraryPath,
        string trustedLibrarySha256,
        string reviewedSourceSha256,
        string? bridgePath = null,
        string? trustedBridgeSha256 = null
    )
    {
        if (!string.Equals(reviewedSourceSha256, NifNativeNames.SourceSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(NifNativeDiagnostics.TrustedSource);
        ValidatePathAndHash(libraryPath, trustedLibrarySha256);
        if (bridgePath is not null && trustedBridgeSha256 is not null)
            ValidatePathAndHash(bridgePath, trustedBridgeSha256);
        else if (bridgePath is not null || trustedBridgeSha256 is not null)
            throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
        LibraryPath = libraryPath;
        LibrarySha256 = trustedLibrarySha256;
        BridgePath = bridgePath;
        BridgeSha256 = trustedBridgeSha256;
    }

    private static void ValidatePathAndHash(string path, string hash)
    {
        if (!Path.IsPathFullyQualified(path) || hash.Length != NifNativeAbi.HashHexCharacters ||
            !hash.All(char.IsAsciiHexDigit))
            throw new ArgumentException(NifNativeDiagnostics.TrustedSource);
    }

    internal static FileStream OpenVerified(string path, string hash)
    {
        var file = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        );
        try
        {
            if (!Convert.ToHexString(SHA256.HashData(file)).Equals(hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(NifNativeDiagnostics.UntrustedLibraryHash);

            return file;
        }
        catch
        {
            file.Dispose();
            throw;
        }
    }
}
