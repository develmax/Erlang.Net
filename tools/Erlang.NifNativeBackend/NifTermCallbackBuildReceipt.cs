namespace Erlang.NifNativeBackend;

internal sealed record NifTermCallbackBuildReceipt(
    string Status,
    string Platform,
    string BuilderSourceFreezeSha256,
    string SourceManifestSha256,
    NifTermCallbackImage[] Images
);
