namespace Erlang.Tests;

internal static class RuntimeNodeAdmissionFoundationHostNames
{
    internal const string Test = "runtime/local-node-admission-owned-kernel-lifecycle";
    internal const string Admission = "AdmitObservedLocalNode";
    internal const string ConfiguredNode = "configured@localhost";
    internal const string FirstNode = "kernel_first@localhost";
    internal const string SecondNode = "kernel_second@localhost";
    internal const string RemoteNode = "remote_pending@localhost";
    internal const string InvalidNode = "invalid-node";
    internal const string IsAlive = "is_alive";
    internal const string Decode = "binary_to_term";
    internal const string CreationQuery = "system_info";
    internal const string Creation = "creation";
    internal const string NodeUp = "nodeup";
    internal const string NodeDown = "nodedown";
    internal const string SendAfter = "send_after";
    internal const string TimerMessage = "kernel_timer_message";
    internal const string Failure = "Owned local kernel admission control failed.";
    internal const string Accepted = "Invalid kernel admission was accepted.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
