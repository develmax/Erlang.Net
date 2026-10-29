using System.Reflection;

namespace Erlang.Tests;

internal static class ProcessLiveness626Names
{
    internal const string TerminalCase = "runtime/process-liveness626/real-closed-before-token-cancellation";
    internal const string LiveCase = "runtime/process-liveness626/live-unrelated-cancellation-visible";
    internal const string Gate = "gate";
    internal const string Terminated = "Terminated";
    internal const string Closed = "closed593";
    internal const string Failure = "Process cancellation ordering did not preserve real termination and live errors.";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(8);
}
