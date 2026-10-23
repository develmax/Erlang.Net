namespace Erlang;

public sealed record RuntimeLibraryManifestModule(
    string Module,
    RuntimeModuleBackend Backend,
    string? Image,
    string? Sha256,
    string[] Dependencies
);
