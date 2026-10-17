namespace Erlang.NifNativeBackend;

internal sealed record NifTermCallbackAdmission(
    string Status,
    string BuildReceiptPath,
    string BuildReceiptSha256,
    string BuilderFreezeSha256,
    string FixtureManifestSha256,
    string LibraryPath,
    string LibrarySha256,
    string NativeSourcePath,
    string NativeSourceSha256,
    string ErlangSourcePath,
    string ErlangSourceSha256
);
