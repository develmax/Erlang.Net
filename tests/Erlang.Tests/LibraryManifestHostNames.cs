namespace Erlang.Tests;

internal static class LibraryManifestHostNames
{
    internal const string Prefix = "runtime/library-manifest389/";
    internal const string Beam = Prefix + "beam-owned-images-and-actual-source-owners";
    internal const string Mixed = Prefix + "managed-and-explicit-mixed-projects";
    internal const string Invalid = Prefix + "invalid-schema-path-hash-and-dependencies";
    internal const string NoFallback = Prefix + "valid-hash-wrong-module-refuses-startup";
    internal const string Failure = "Library manifest startup contract differs";
    internal const string RootPrefix = "library-manifest389-";
    internal const string ListImage = "lists.beam";
    internal const string QueueImage = "queue.beam";
    internal const string RelativeEscape = "../lists.beam";
    internal const string VersionName = "Version";
    internal const string ModulesName = "Modules";
    internal const string BackendName = "Backend";
    internal const string UnexpectedName = "Unexpected";
    internal const string EmptyManifest = "{}";
    internal const string DuplicateVersion = "{\"Version\":1,\"Version\":1,\"Modules\":[]}";
}
