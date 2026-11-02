namespace Erlang;

public sealed partial class ProcessContext
{
    // Public garbage_collect reaches this safe point for admitted guest actors.
    // Never-admitted opaque Host contexts retain their separate CLR collector.
    internal ProcessTermCollection508 CollectOwnedGeneration508(bool requestedMajor)
    {
        if (!VmStorageBackingEnabled || !RuntimeActorExecutionTurn.IsOwnedBy(this))
            throw new InvalidOperationException(ResourcePolicy508Names.PilotRequired);

        Runtime.MaterializePendingSequenceTraceToken(this);
        var result = TermHeap506.CollectGeneration508(requestedMajor, GarbageCollectionPolicy.Threshold);
        Runtime.ImportOnHeapMessagesAfterCollection(this);

        return result;
    }
}
