namespace Erlang;

// Internal host execution policy, not evidence that a binary implements its reviewed source.
// No module/function names participate in execution permission or entry semantics.
internal sealed class NifNativeReviewedBasicAbi
{
    internal string LibraryPath { get; }
    internal string LibrarySha256 { get; }
    internal string ReviewedBuildReceiptSha256 { get; }
    internal string? BridgePath { get; }
    internal string? BridgeSha256 { get; }

    internal NifNativeReviewedBasicAbi(
        string libraryPath,
        string trustedLibrarySha256,
        string reviewedBuildReceiptSha256,
        string? bridgePath = null,
        string? trustedBridgeSha256 = null
    )
    {
        Validate(libraryPath, trustedLibrarySha256);
        ValidateDigest(reviewedBuildReceiptSha256);
        if (bridgePath is not null && trustedBridgeSha256 is not null)
            Validate(bridgePath, trustedBridgeSha256);
        else if (bridgePath is not null || trustedBridgeSha256 is not null)
            throw new ArgumentException(NifNativeDiagnostics.BridgeContract);
        if (OperatingSystem.IsLinux() && bridgePath is null)
            throw new ArgumentException(NifNativeDiagnostics.BridgeContract);
        LibraryPath = libraryPath;
        LibrarySha256 = trustedLibrarySha256;
        ReviewedBuildReceiptSha256 = reviewedBuildReceiptSha256;
        BridgePath = bridgePath;
        BridgeSha256 = trustedBridgeSha256;
    }

    private static void Validate(string path, string digest)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!Path.IsPathFullyQualified(path))
            throw new ArgumentException(NifNativeDiagnostics.TrustedSource);
        ValidateDigest(digest);
    }

    private static void ValidateDigest(string digest)
    {
        ArgumentNullException.ThrowIfNull(digest);
        if (digest.Length != NifNativeAbi.HashHexCharacters || !digest.All(char.IsAsciiHexDigit))
            throw new ArgumentException(NifNativeDiagnostics.TrustedSource);
    }
}
