namespace Erlang.Tests;

internal static class SpawnPriorityTestNames
{
    internal const string AllReadyOrder = "runtime/priority/all-ready-order";
    internal const string EscapedRegisteredCallback = "runtime/priority/escaped-registered-callback";
    internal const string SynchronousTurnBorrow = "runtime/priority/synchronous-turn-borrow";
    internal const string LowLogicalEpochs = "runtime/priority/low-logical-epochs";
    internal const string WaitingHighCapacity = "runtime/priority/waiting-high-capacity";
    internal const string QueuedPriorityChange = "runtime/priority/queued-priority-change";
    internal const string HaltCleanup = "runtime/priority/halt-cleanup";
    internal const string KilledOwnerRetirement = "runtime/priority/killed-owner-retirement";
}
