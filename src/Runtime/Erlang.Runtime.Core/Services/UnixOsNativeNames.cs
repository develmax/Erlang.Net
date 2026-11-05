namespace Erlang;

internal static class UnixOsNativeNames
{
    internal const string Snapshot = "enfs_env_snapshot";
    internal const string Free = "enfs_env_free";
    internal const string SnapshotFailed = "Cannot capture the POSIX byte environment";
    internal const string SignalUnavailable = "The POSIX signal delivery capability is not installed";
}
