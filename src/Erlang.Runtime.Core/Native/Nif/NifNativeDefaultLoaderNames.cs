namespace Erlang;

internal static class NifNativeDefaultLoaderNames
{
    internal const string UnixSuffix = ".so";
    internal const string WindowsSuffix = ".dll";
    internal const string LoadFailed = "load_failed";
    internal const string BadLibrary = "bad_lib";
    internal const string Load = "load";
    internal const string OldCode = "old_code";
    internal const string Reload = "reload";
    internal const string Upgrade = "upgrade";
    internal const string Permission = "The native image has not been approved for the reviewed execution ABI.";
    internal const string DriveRelativeFilename = "Drive-relative native library names are not implemented by this backend.";
    internal const string AlreadyConfigured = "The reviewed native execution policy is already installed.";
}
