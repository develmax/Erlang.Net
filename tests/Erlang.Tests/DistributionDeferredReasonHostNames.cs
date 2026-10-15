namespace Erlang.Tests;

internal static class DistributionDeferredReasonHostNames
{
    internal const byte EtfVersion = 131;
    internal const string Test = "runtime/private-deferred-distribution-reason-callback-foundation";
    internal const string Factory = "CreateDeferredDistributionExitReason";
    internal const string Prepare = "Prepare";
    internal const string RuntimeGate = "gate";
    internal const string CaptureFailure = "CaptureDeferredReasonFailure";
    internal const string Local = "deferred_reason_local@localhost";
    internal const string Remote = "deferred_reason_remote@localhost";
    internal const string Failure = "Deferred reason foundation assertion failed";
    internal const string Payload = "owned_deferred_reason";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
