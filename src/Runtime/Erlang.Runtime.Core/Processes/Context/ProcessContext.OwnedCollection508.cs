namespace Erlang;

public sealed partial class ProcessContext
{
    // Internal safe-point entry only. Public garbage_collect/resource reporting
    // is intentionally unchanged until every live guest domain is owned/rooted.
    internal ProcessTermCollection508 CollectOwnedGeneration508(bool requestedMajor)
    {
        if (!VmStorageBackingEnabled || !RuntimeActorExecutionTurn.IsOwnedBy(this))
            throw new InvalidOperationException(ResourcePolicy508Names.PilotRequired);

        var result = TermHeap506.CollectGeneration508(requestedMajor, GarbageCollectionPolicy.Threshold);
        Runtime.ImportOnHeapMessagesAfterCollection(this);

        return result;
    }
}
