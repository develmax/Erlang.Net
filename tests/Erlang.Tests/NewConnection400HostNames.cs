namespace Erlang.Tests;

internal static class NewConnection400HostNames
{
    internal const string ModuleName = "erts_internal";
    internal const string Function = "new_connection";
    internal const string Test = "host/new-connection400-native-pending-connected-exiting-and-capacity";
    internal const string Kernel = "net_kernel";
    internal const string Admission = "AdmitObservedLocalNode";
    internal const string CaptureFailure = "CaptureDeferredReasonFailure";
    internal const string RuntimeGate = "gate";
    internal const string Remote = "newconn400_host_remote@localhost";
    internal const string OtherRemote = "newconn400_host_other@localhost";
    internal const string Local = "newconn400_host_local@localhost";
    internal const string InvalidNode = "invalid.node@host";
    internal const string Invariant = "Native new_connection400 invariant failed";
    internal const uint ConnectionMask = 0xffffff;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
