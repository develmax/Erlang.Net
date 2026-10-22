using System.Reflection;
namespace Erlang.Tests;

internal static class InternalSystemCheck431HostNames
{
    internal const string NativeType = "Erlang.InternalSystemCheckNative431", Capture = "TryCapture", Module = "erts_internal", Function = "system_check", Schedulers = "schedulers", Gather = "gather_system_check_result";
    internal const string Dispatcher = "actorDispatcher", Prepare = "PrepareAuxiliary420", Publish = "Publish", Completion = "Completion", Snapshot = "Snapshot";
    internal const string Ok = "ok", Sentinel = "preserved431", Kill = "kill", Killed = "killed", BadArgument = "badarg";
    internal const string Failure = "Internal system check431 real auxiliary ACK/lifecycle control failed.";
    internal const string StatusTest = "HOST/internal-system-check431-status-owner-source-helper";
    internal const string AckTest = "HOST/internal-system-check431-real-held-auxiliary-acks";
    internal const string CancelTest = "HOST/internal-system-check431-real-caller-exit-ack-join";
    internal const int Participants = 2, HeldLane = 2;
    internal const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
