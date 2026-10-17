namespace Erlang.NifNativeBackend;

internal sealed record NifMapSizeCallbackBuildReceipt(
    string Status,
    string Platform,
    string BuilderSourceFreezeSha256,
    string SourceManifestSha256,
    NifMapSizeCallbackImage[] Images
);
