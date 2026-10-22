namespace Erlang;

internal static class LibraryManifestNames
{
    internal const string Invalid = "Invalid library backend manifest";
    internal const string HashMismatch = "Library image SHA256 differs from the declared manifest";
    internal const string RelativeImage = "Library images must use relative paths within the manifest directory";
    internal const string BeamExtension = ".beam";
    internal const string ParentDirectory = "..";
}
