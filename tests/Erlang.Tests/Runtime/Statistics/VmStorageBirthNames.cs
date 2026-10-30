namespace Erlang.Tests;

internal static class VmStorageBirthNames
{
    internal const string Birth = "runtime/vm-storage/born-before-pure-host-body";
    internal const string Dictionary = "runtime/vm-storage/pure-host-dictionary-real-collection";
    internal const string Isolation = "runtime/vm-storage/two-vms-and-host-clr-gc-isolation";
    internal const string Drain = "runtime/vm-storage/actual-dictionary-borrow-delays-physical-drain";
    internal const string Rollback = "runtime/vm-storage/actual-dispatch-admission-rollback";
    internal const string Failure = "Real VM storage ownership, collector or physical lifecycle differs";
    internal const string Heap = "termHeap506";
    internal const string DictionaryFrame = "frame";
    internal const string DictionaryData = "data";
    internal const string Gate = "gate";
    internal const string HeapGate = "Gate";
    internal const string OwnersGate = "ownedTermOwnersGate506";
    internal const string Owners = "ownedTermOwners506";
    internal const string ExecutionEnabled = "OwnedExecutionEnabled683";
    internal const string CurrentInvocation = "CurrentOwned506";
    internal const string Dispatcher = "actorDispatcher";
    internal const string Retire = "CompleteWhenIdle";
    internal const string Statistics = "Statistics";
    internal const string SampleVmCollections = "SampleVmCollections";
    internal const string Borrow = "Borrow";
    internal const string BorrowValue = "Value";
    internal const string Closing = "closing";
    internal const string GarbageCollect = "garbage_collect";
    internal const string Type = "type";
    internal const string Major = "major";
    internal const string Drop = "storage_drop";
    internal const string Keep = "storage_keep";
    internal const string UnexpectedBody = "A rejected dispatcher admission executed the actor body";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
