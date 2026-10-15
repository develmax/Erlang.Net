using Erlang;

namespace Erlang.Tests;

internal static class DistributionPayloadMonitorHostNames
{
    internal const string Test = "runtime/distribution-payload-monitor-deferred-raw-lifecycle";
    internal const string Local = "payload_monitor_local@localhost";
    internal const string Remote = "payload_monitor_remote@localhost";
    internal const string Target = "payload_monitor_target";
    internal const string Failure = "Deferred payload monitor assertion failed";
    internal const string Reason = "payload_monitor_reason";
    internal const string FunctionModule = "payload_monitor_function";
    internal const string Gate = "gate";
    internal const int FunctionUniqueBytes = 16;
    internal const int FunctionIndex = 9;
    internal const byte EtfVersion = 131;
    internal const int MissingId = 500000;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    internal static readonly Atom Value = Term.A(Reason);
}
