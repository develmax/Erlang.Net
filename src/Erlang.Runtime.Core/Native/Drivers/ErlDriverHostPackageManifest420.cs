namespace Erlang;

internal sealed record ErlDriverHostPackageManifest420(
    int Protocol,
    string EntryPoint,
    ErlDriverHostPackageFile420[] Files
);
