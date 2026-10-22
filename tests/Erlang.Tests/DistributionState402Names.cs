namespace Erlang.Tests;

internal static class DistributionState402Names
{
    internal const string ModuleName = "erts_internal";
    internal const string AbortFunction = "abort_pending_connection";
    internal const string CreationFunction = "get_creation";
    internal const string NewConnection = "new_connection";
    internal const string Remote = "pending402@localhost";
    internal const string Other = "other402@localhost";
    internal const string ConfiguredNode = "configured402@localhost";
    internal const string MalformedHandle = "invalid_distribution_handle402";
    internal const string Undefined = "undefined";
    internal const string Success = "ok";
    internal const string ErrorInfo = "error_info";
    internal const string InfoModule = "module";
    internal const string NativeFormatter = "erl_erts_errors";
    internal const string Failure = "Native distribution creation/abort state invariant failed";
    internal const string PendingTest = "host/distribution402-native-abort-pending-idle-stale-and-foreign";
    internal const string CreationTest = "host/distribution402-native-creation-and-connected-abort";
    internal const string ExitingTest = "host/distribution402-native-abort-real-exiting-retains-entry";
    internal const int InvalidNodeInteger = 17;
    internal const byte InvalidPacketHeader = 0;
    internal const uint ConnectionMask = 0xffffff;
    internal const int NativeSmallAliasBits = 32;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
