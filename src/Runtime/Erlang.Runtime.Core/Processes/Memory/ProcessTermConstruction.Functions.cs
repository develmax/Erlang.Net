namespace Erlang;

internal sealed partial class ProcessTermConstruction514
{
    private List<ProcessTermSequence506>? createdFunctions;

    internal void PrepareFunctionTracking()
    {
        ProcessTermConstruction514 owner = magicOwner576 ?? this;
        (owner.createdFunctions ??= new()).EnsureCapacity(checked((owner.createdFunctions?.Count ?? 0) + 1));
    }

    internal void TrackFunction(ProcessTermSequence506 block) => (magicOwner576 ?? this).createdFunctions!.Add(block);

    internal void RollbackFunctions()
    {
        if (magicOwner576 is not null || createdFunctions is not { Count: > 0 } created) return;
        ((ProcessTermHeap506)created[0].Owner).RollbackFunctionBlocks(created);
        created.Clear();
    }
}
