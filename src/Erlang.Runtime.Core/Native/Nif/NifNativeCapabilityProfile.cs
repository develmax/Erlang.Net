using System.Security.Cryptography;

namespace Erlang;

// Optional deployment hash admission is separate from ordinary host filesystem loading.
internal sealed class NifNativeCapabilityProfile
{
    internal string LibraryPath
    {
        get;
    }
    internal NifNativeLibraryPath NativeLibraryPath { get; }

    internal string? ExpectedLibrarySha256 { get; }
    internal string LibrarySha256 => ExpectedLibrarySha256
        ?? throw new InvalidOperationException(NifNativeHostFileNames.NoExpectedDigest);
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
        NativeLibraryPath = NifNativeLibraryPath.FromHostPath(libraryPath);
        ExpectedLibrarySha256 = trustedLibrarySha256;
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
        NativeLibraryPath = capability.NativeLibraryPath;
        ExpectedLibrarySha256 = capability.LibrarySha256;
        BridgePath = capability.BridgePath;
        BridgeSha256 = capability.BridgeSha256;
        ReallocationEnabled = false;
        ReallocationBridgePath = null;
        ReallocationBridgeSha256 = null;
    }

    // Ordinary OTP loading trusts the host filesystem; no reviewed-source claim or synthetic digest.
    internal static NifNativeCapabilityProfile ForHostLibrary(string libraryPath)
    {
        ArgumentNullException.ThrowIfNull(libraryPath);
        if (!Path.IsPathFullyQualified(libraryPath))
            throw new ArgumentException(NifNativeDiagnostics.TrustedSource);

        return new NifNativeCapabilityProfile(libraryPath);
    }

    internal static NifNativeCapabilityProfile ForHostLibrary(NifNativeLibraryPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return new NifNativeCapabilityProfile(path);
    }

    private NifNativeCapabilityProfile(NifNativeLibraryPath path)
    {
        LibraryPath = path.DisplayPath;
        NativeLibraryPath = path;
    }

    private NifNativeCapabilityProfile(string libraryPath)
    {
        LibraryPath = libraryPath;
        NativeLibraryPath = NifNativeLibraryPath.FromHostPath(libraryPath);
    }

    internal FileStream? OpenLibraryFile() => ExpectedLibrarySha256 is { } hash
        ? OpenVerified(NativeLibraryPath, hash)
        : NativeLibraryPath.IsUnixBasename ? null : OpenFile(NativeLibraryPath);

    private static FileStream OpenFile(NifNativeLibraryPath path) => OperatingSystem.IsWindows()
        ? new FileStream(
            path.DisplayPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        )
        : NifNativeUnixFile.OpenRead(path);

    private static void ValidatePathAndHash(string path, string hash)
    {
        if (!Path.IsPathFullyQualified(path) || hash.Length != NifNativeAbi.HashHexCharacters ||
            !hash.All(char.IsAsciiHexDigit))
            throw new ArgumentException(NifNativeDiagnostics.TrustedSource);
    }

    internal static FileStream OpenVerified(string path, string hash) =>
        OpenVerified(NifNativeLibraryPath.FromHostPath(path), hash);

    private static FileStream OpenVerified(NifNativeLibraryPath path, string hash)
    {
        var file = OpenFile(path);
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
