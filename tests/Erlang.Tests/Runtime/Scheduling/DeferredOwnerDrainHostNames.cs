namespace Erlang.Tests;

internal static class DeferredOwnerDrainHostNames
{
    internal const string Test = "runtime/private-ordered-deferred-owner-signal-drain";
    internal const string CycleException = "Erlang.DistributionSignalNestedWaitException";
    internal const string FunctionA = "deferred_factory_a";
    internal const string FunctionB = "deferred_factory_b";
    internal const int FunctionArity = 0;
    internal const int FunctionIndex = 0;
    internal const int FunctionOldUnique = 1;
    internal const int FunctionUniqueBytes = 16;
    internal const string Queue = "QueueDeferredProcessSignal";
    internal const string SendPending = "SendPending";
    internal const string AdmissionType = "Erlang.DeferredMailboxAdmission";
    internal const string Gate = "gate";
    internal const string First = "first_deferred_message";
    internal const string Second = "second_deferred_message";
    internal const string Prefix = "old_prefix_signal";
    internal const string PrepareFirst = "first_outside_gate_preparation";
    internal const string Middle = "old_middle_signal";
    internal const string PrepareSecond = "second_outside_gate_preparation";
    internal const string Failure = "Ordered owner drain assertion failed";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
