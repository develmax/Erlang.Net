namespace Erlang.HaltProbe;

internal sealed record NativeHaltDumpEvidence(
    string Path,
    bool Exists,
    long? Length,
    string? Sha256,
    IReadOnlyList<byte> Prefix,
    bool SignatureMatches,
    string? Error
);
