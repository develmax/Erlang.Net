namespace Erlang.NifNativeBackend;

internal sealed record NifTextBinaryCallbackBuildReceipt(
    string Status,
    string Platform,
    string BuilderSourceFreezeSha256,
    string SourceManifestSha256,
    NifTextBinaryCallbackImage[] Images
);
