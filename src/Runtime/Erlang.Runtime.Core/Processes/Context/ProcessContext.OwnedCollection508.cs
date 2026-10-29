namespace Erlang;

public sealed partial class ProcessContext
{
    // Internal safe-point entry only. Public garbage_collect/resource reporting
    // is intentionally unchanged until every live guest domain is owned/rooted.
    internal ProcessTermCollection508 CollectOwnedGeneration508(bool requestedMajor)
    {
        if (!OwnedExecutionEnabled683 || !RuntimeActorExecutionTurn.IsOwnedBy(this))
            throw new InvalidOperationException(ResourcePolicy508Names.PilotRequired);

        return TermHeap506.CollectGeneration508(requestedMajor, GarbageCollectionPolicy.Threshold);
    }
}
