namespace Erlang;

internal sealed partial class ProcessTermConstruction514
{
    private readonly ProcessTermConstruction514? magicOwner576;
    private List<NativeOffheapHeader576>? createdMagic576;

    internal void PrepareMagicTracking576()
    {
        ProcessTermConstruction514 owner = magicOwner576 ?? this;
        (owner.createdMagic576 ??= new()).EnsureCapacity(checked((owner.createdMagic576?.Count ?? 0) + 1));
    }

    internal void TrackMagic576(NativeOffheapHeader576 value)
        => (magicOwner576 ?? this).createdMagic576!.Add(value);

    internal void RollbackMagic576()
    {
        if (magicOwner576 is not null || createdMagic576 is not { Count: > 0 } created)
            return;
        // Forks share this transaction's exact capability and rollback owner.
        // Actual published destination roots have already promoted their nodes.
        created[0].Owner.RollbackMagicReferences576(created);
        created.Clear();
    }
}
