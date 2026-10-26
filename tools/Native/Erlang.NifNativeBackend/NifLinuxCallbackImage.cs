namespace Erlang.NifNativeBackend;

internal sealed record NifLinuxCallbackImage(
    string Module,
    string LibraryPath,
    string LibrarySha256,
    string NativeSourcePath,
    string NativeSourceSha256,
    string ErlangSourcePath,
    string ErlangSourceSha256,
    string ManifestPath,
    string ManifestSha256,
    string BuildReceiptPath,
    string BuildReceiptSha256,
    string[] ObservationNames
);
