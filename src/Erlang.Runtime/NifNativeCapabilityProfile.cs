using System.Security.Cryptography;

namespace Erlang;

// Trusted deployment metadata, not a proof that a binary was built from this source.
internal sealed class NifNativeCapabilityProfile
{
    internal string LibraryPath
    {
        get;
    }
    internal string LibrarySha256
    {
        get;
    }
    internal string? BridgePath
    {
        get;
    }
    internal string? BridgeSha256
    {
        get;
    }

    internal bool ReallocationEnabled
    {
        get;
    }
    internal string? ReallocationBridgePath
    {
        get;
    }
    internal string? ReallocationBridgeSha256
    {
        get;
    }

    internal NifNativeCapabilityProfile(
        string libraryPath,
        string trustedLibrarySha256,
        string reviewedSourceSha256,
        string? bridgePath = null,
        string? trustedBridgeSha256 = null
    ) : this(
        libraryPath,
        trustedLibrarySha256,
        reviewedSourceSha256,
        bridgePath,
        trustedBridgeSha256,
        null,
        null
    )
    {
    }

    internal NifNativeCapabilityProfile(
        string libraryPath,
        string trustedLibrarySha256,
        string reviewedSourceSha256,
        string? bridgePath,
        string? trustedBridgeSha256,
        string? reallocationBridgePath,
        string? trustedReallocationBridgeSha256
    )
    {
        bool legacy = string.Equals(reviewedSourceSha256, NifNativeNames.SourceSha256, StringComparison.OrdinalIgnoreCase);
        bool reallocation = string.Equals(reviewedSourceSha256, NifNativeReallocationNames.ReviewedProfileDigest, StringComparison.OrdinalIgnoreCase);
        if (!legacy && !reallocation)
            throw new InvalidOperationException(NifNativeDiagnostics.TrustedSource);
        ValidatePathAndHash(libraryPath, trustedLibrarySha256);
        if (bridgePath is not null && trustedBridgeSha256 is not null)
            ValidatePathAndHash(bridgePath, trustedBridgeSha256);
        else if (bridgePath is not null || trustedBridgeSha256 is not null)
            throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
        if (reallocationBridgePath is not null && trustedReallocationBridgeSha256 is not null)
        {
            if (!reallocation || !OperatingSystem.IsLinux())
                throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
            ValidatePathAndHash(reallocationBridgePath, trustedReallocationBridgeSha256);
        }
        else if (reallocationBridgePath is not null || trustedReallocationBridgeSha256 is not null)
            throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
        if (reallocation && OperatingSystem.IsLinux() && (bridgePath is null || reallocationBridgePath is null))
            throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
        LibraryPath = libraryPath;
        LibrarySha256 = trustedLibrarySha256;
        BridgePath = bridgePath;
        BridgeSha256 = trustedBridgeSha256;
        ReallocationEnabled = reallocation;
        ReallocationBridgePath = reallocationBridgePath;
        ReallocationBridgeSha256 = trustedReallocationBridgeSha256;
    }

    // Separate opt-in reviewed deployment ABI policy; legacy source-digest constructors above are unchanged.
    internal static NifNativeCapabilityProfile ForReviewedBasicAbi(NifNativeReviewedBasicAbi capability)
    {
        ArgumentNullException.ThrowIfNull(capability);

        return new NifNativeCapabilityProfile(capability);
    }

    private NifNativeCapabilityProfile(NifNativeReviewedBasicAbi capability)
    {
        LibraryPath = capability.LibraryPath;
        LibrarySha256 = capability.LibrarySha256;
        BridgePath = capability.BridgePath;
        BridgeSha256 = capability.BridgeSha256;
        ReallocationEnabled = false;
        ReallocationBridgePath = null;
        ReallocationBridgeSha256 = null;
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
