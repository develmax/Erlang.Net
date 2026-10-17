namespace Erlang.NifNativeBackend;

internal sealed record NifLinuxCallbackAdmission(
    string Status,
    string SubjectHead,
    string BridgePath,
    string BridgeSha256,
    string BridgeReceiptPath,
    string BridgeReceiptSha256,
    NifLinuxCallbackImage[] Images
);
