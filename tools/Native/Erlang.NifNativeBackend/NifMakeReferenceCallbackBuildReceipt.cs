namespace Erlang.NifNativeBackend;

internal sealed record NifMakeReferenceCallbackBuildReceipt(
    string Status,
    string Platform,
    string BuilderSourceFreezeSha256,
    string SourceManifestSha256,
    NifMakeReferenceCallbackImage[] Images
);
