namespace Erlang.Tests;

internal static class BeamOnLoadHashNames
{
    internal const int OriginalResourceCount = 56;
    internal const char PathSeparator = '/';
    internal const char NamespaceSeparator = '.';
    internal const char FolderHyphen = '-';
    internal const char NamespaceUnderscore = '_';
    internal const string ResourcePrefix = ".Resources.BeamOnLoad.";
    internal const string ManifestSuffix = ".Resources.BeamOnLoadResourceHashes.json";
    internal const string Files = "Files";
    internal const string Path = "Path";
    internal const string Sha256 = "Sha256";
    internal const string CountFailure = "Original on_load resource count changed";
    internal const string HashFailure = "Original on_load resource bytes changed: ";
}
